# Burn overlays into recorded video (CameraView)

## Context

`CameraView.StartVideoRecordingAsync` currently hands the raw sensor feed to each platform's
native file-output API (`AVCaptureMovieFileOutput`, CameraX `Recorder`, WinRT
`LowLagMediaRecording`). The existing `CameraOverlayView`/`CameraOverlayDrawable` draw only on the
live on-screen preview — nothing they paint reaches the saved file. Users want arbitrary
text/images/shapes composited **into the video as it records** (watermark, timestamp, telemetry,
reticles, etc.).

This plan adds a per-frame overlay compositing path so anything drawn with a
`Microsoft.Maui.Graphics.ICanvas` is burned into the encoded MP4/MOV, across iOS, Mac Catalyst,
macOS (AppKit), Android, and Windows. **Blazor is included too** (`Shiny.Blazor.Controls.Camera`),
where burn-in is done in-browser via canvas compositing + `MediaRecorder`. When no overlay is
supplied, the existing fast native/browser recording path is used unchanged (no perf regression,
no behavior change).

**API parity note:** MAUI exposes an imperative `IVideoOverlayRenderer` (`ICanvas` per frame).
Blazor cannot use that shape — invoking .NET across JS interop at 30 fps is too chatty — so Blazor
gets a **declarative overlay-element model** (text / image / shape DTOs) drawn JS-side. This mirrors
the split the codebase already lives with (analyzers run native-per-frame on MAUI but JS-side in
Blazor). See the Blazor section below.

Note: the owned-encoder groundwork here (AVAssetWriter on Apple) is the same infrastructure the
planned rolling-buffer retroactive-capture feature needs — this is shared foundation.

## Public API (shared project: `src/Shiny.Controls.Camera.Shared`)

New overlay-renderer abstraction, drawn per encoded frame in the frame's pixel space:

```csharp
public interface IVideoOverlayRenderer
{
    // Invoked per encoded frame OFF the UI thread. Draw in pixel space (0,0)..(Width,Height).
    void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context);
}

public readonly record struct VideoOverlayContext(
    TimeSpan Elapsed, long FrameIndex, int Width, int Height, CameraFacing Facing);

// Convenience delegate impl
public sealed class DelegateVideoOverlay(Action<ICanvas, RectF, VideoOverlayContext> draw)
    : IVideoOverlayRenderer { /* forwards */ }

// Adapter so an existing IDrawable (e.g. CameraOverlayDrawable) can be reused as a burn-in overlay
public sealed class DrawableVideoOverlay(IDrawable drawable) : IVideoOverlayRenderer { /* forwards */ }
```

Add one property to `VideoRecordingOptions` (`src/Shiny.Maui.Controls.Camera/VideoRecording.cs`):

```csharp
/// <summary>Optional overlay composited into every recorded frame. Null = raw feed (fast path).</summary>
public IVideoOverlayRenderer? Overlay { get; set; }
```

Thread-safety is documented on the interface: `DrawOverlay` runs on a capture/encoder thread; a
renderer that reflects UI state must read it via volatile/immutable snapshots.

Optional nicety (include if cheap): a built-in `AnalyzerOverlayRenderer` that snapshots
`CameraView.Overlays`/`ScanWindow` each frame and reuses `CameraOverlayDrawable`'s draw logic, so
the same boxes the user sees on-preview can be burned in. Keep MVP focused on the user-supplied
renderer; this is an add-on.

## Cross-platform compositing strategy

Each platform hands us a native drawing surface per frame; we wrap it in a MAUI Graphics
`ICanvas` and call the user's `DrawOverlay`. This keeps the drawing code identical everywhere and
reuses the exact primitive `CameraOverlayDrawable` already uses.

Shared rule for all platforms: **overlay == null → existing native recorder path (unchanged);
overlay != null → owned/composited encode path.** This de-risks the change and preserves current
performance/behavior when the feature isn't used.

### Apple — iOS / Mac Catalyst (`Platforms/Apple/CameraViewHandler.Apple.cs` + `VideoFrameDelegate.cs`) and macOS AppKit (`Platforms/MacOS/CameraViewHandler.MacOS.cs`)

The session already runs an `AVCaptureVideoDataOutput` with `VideoFrameDelegate`
(`Platforms/Apple/VideoFrameDelegate.cs`, and the macOS equivalent) receiving every BGRA
`CVPixelBuffer` upright/mirror-corrected via `OrientConnections`. Reuse it as the video source.

- Add an `AVAssetWriter` + `AVAssetWriterInput` (video) + `AVAssetWriterInputPixelBufferAdaptor`,
  and, when `IncludeAudio`, an `AVCaptureAudioDataOutput` feeding a second `AVAssetWriterInput`.
- In the video delegate, when recording-with-overlay is active: lock the `CVPixelBuffer` base
  address, wrap it in a `CGBitmapContext` (BGRA premultiplied, same dimensions), wrap that
  `CGContext` with `Microsoft.Maui.Graphics.Platform.PlatformCanvas`, invoke `DrawOverlay`, then
  append the (now-composited) buffer to the adaptor using the sample's PTS.
- Audio delegate appends CMSampleBuffers to the audio input.
- `StartVideoRecordingAsync`: overlay present → build/start the asset writer, arm the delegate's
  recording sink; overlay null → keep current `AVCaptureMovieFileOutput` path
  (`Apple.cs:114`, `MacOS.cs:112`).
- `StopVideoRecordingAsync`: finalize the writer (`FinishWriting`) and return `CameraVideo` with
  the file path + duration; null-overlay path unchanged.
- `.mov` output as today. Front camera already mirror-corrected by the connection, so overlay text
  renders correctly (drawn after mirroring).

### Android — CameraX (`Platforms/Android/CameraViewHandler.Android.cs`)

Use CameraX `OverlayEffect` (`androidx.camera.effects`), which composites a `Canvas` draw callback
onto selected targets via a `SurfaceProcessor` — an *effect*, not a use case, so it does **not**
consume the ~3-use-case budget and coexists with the existing ImageAnalysis/VideoCapture
exclusivity logic (`BindUseCases`, `Android.cs:295`).

- Add package `Xamarin.AndroidX.Camera.Effects` to the Android TFM refs in the csproj (currently
  only `.Core/.Camera2/.Lifecycle/.View/.Video` are referenced). **Confirmed available on NuGet at
  `1.6.1` / `1.6.1.1`, version-aligned with the existing 1.6.1 CameraX bindings** — pin `1.6.1` in
  `Directory.Packages.props`. Remaining unknown is only the exact bound `OverlayEffect` API shape.
- When `options.Overlay != null`, construct an `OverlayEffect` targeting **`VideoCapture` only**
  (not `Preview` — the on-screen `CameraOverlayView` still handles preview, avoids double-draw),
  register it via `UseCaseGroup.Builder().AddEffect(...)` / `BindToLifecycle`, and in its
  `OnDrawListener` wrap the supplied `android.graphics.Canvas` with
  `Microsoft.Maui.Graphics.Platform.PlatformCanvas` and call `DrawOverlay`. Apply the frame's
  `sensorToBufferTransform` to the canvas so overlay coordinates map to output-buffer space.
- Rebind to attach/detach the effect around recording start (mirror the existing rebind machinery
  in `OnAnalyzersSynced`/`RebindIfModeChanged`). Recording itself stays on the existing `Recorder`.

### Windows — WinUI3 / MediaCapture (`Platforms/Windows/CameraViewHandler.Windows.cs`)

Highest-effort platform (recorder doesn't read our `MediaFrameReader` frames). Two candidate
approaches — a short spike decides which; effect-based is preferred:

1. **Preferred — `IBasicVideoEffect`:** implement a video effect, register it before recording via
   `capture.AddVideoEffectAsync(new VideoEffectDefinition(...), MediaStreamType.VideoRecord)`, and
   in `ProcessFrame` draw the overlay onto the output frame with Win2D
   (`Microsoft.Graphics.Canvas` → `Microsoft.Maui.Graphics.Win2D.W2DCanvas`). Keeps the existing
   `LowLagMediaRecording`. **Spike must confirm the effect activates in a .NET WinUI3 desktop app**
   (activatable-class registration is the risk).
2. **Fallback — manual encode:** we already receive composited-capable BGRA `SoftwareBitmap`
   frames in `OnFrameArrived` (`Windows.cs:141`). Composite via Win2D, push
   `MediaStreamSample`s through a `MediaStreamSource` and encode to MP4 with `MediaTranscoder`.
   More code, but avoids the effect-registration uncertainty.
- Add Win2D (`Microsoft.Graphics.Canvas` / `CommunityToolkit.WinUI` equivalent) to the Windows TFM
  refs either way. Overlay-null path keeps the current `PrepareLowLagRecordToStorageFileAsync`
  flow unchanged (`Windows.cs:198`).

### Blazor — WebAssembly / MediaRecorder (`src/Shiny.Blazor.Controls.Camera`)

Easiest platform of all: the browser has a first-class primitive the native platforms lack —
`canvas.captureStream()`. Today `startRecording` (`wwwroot/camera.js:165`) records the raw
`state.stream` directly to WebM; the existing overlay `<canvas>` (`drawOverlay`, `camera.js:387`)
is preview-only. Burn-in swaps in a composited canvas stream.

**JS (`wwwroot/camera.js`):**
- Add a compositing canvas sized to `video.videoWidth/Height`. In a draw loop (`requestAnimationFrame`
  or a timed loop): `ctx.drawImage(video, 0, 0)`, then draw the overlay elements on top (reuse the
  existing `drawOverlay` primitives for shapes/text; add image/`drawImage` from data-URIs).
- Record the composite: `const v = canvas.captureStream(fps)`, merge audio
  (`new MediaStream([...v.getVideoTracks(), ...audioTracks])`), then `new MediaRecorder(merged)`.
- Overlay-null fast path unchanged: keep `new MediaRecorder(state.stream)` (`camera.js:180`) — no
  compositing cost when no overlay is set.
- The overlay spec (list of text/image/shape DTOs) is passed from .NET into `startRecording` once
  and cached on `state`; the draw loop re-reads it each frame. Dynamic content (running timestamp,
  REC dot) handled via built-in tokens (e.g. `{elapsed}`) rendered JS-side, so no per-frame interop.

**.NET (`CameraView.razor.cs`):**
- Overload `StartRecordingAsync` (currently `StartRecordingAsync(bool includeAudio)`, line 225) to
  accept the overlay spec, e.g. `StartRecordingAsync(VideoRecordingOptions? options)` where the
  Blazor `VideoRecordingOptions` carries `IncludeAudio` + `IReadOnlyList<VideoOverlayElement>?`.
- `VideoOverlayElement` is a flat DTO (kind = Text/Image/Shape, normalized position + anchor +
  style, image as a data-URI/byte[]) — **named DTOs, not anonymous types**, per the repo's Blazor
  interop rule (trimmed WASM crashes on anonymous types over interop). Pass the array to JS.

Output is **WebM (VP8/VP9)** — what `MediaRecorder` emits. Flag the Safari caveat (its
`MediaRecorder`/codec support is the weak spot, same asterisk already on the Blazor barcode path);
feature-detect `MediaRecorder.isTypeSupported` and fall back gracefully.

## Coordinate space, orientation & performance notes

- Overlay is drawn in encoded-frame pixel space; `VideoOverlayContext.Width/Height` let the
  renderer normalize. Reuse `CoordinateTransform` (shared project) if mapping normalized boxes.
- Apple frames are delivered upright + front-mirror-corrected (`OrientConnections`); Android needs
  the `sensorToBufferTransform`; document both so overlays aren't rotated/mirrored wrongly.
- Perf: composite on the existing capture/encoder thread; reuse a single bitmap context / render
  target across frames; only rebuild static overlay content when it changes. Append by sample PTS
  so any dropped frames just lower fps rather than desync. Call out that HD30 CPU compositing is
  non-trivial and a heavy `DrawOverlay` can drop frames.

## Files to change

- `src/Shiny.Controls.Camera.Shared/` — new `IVideoOverlayRenderer`, `VideoOverlayContext`,
  `DelegateVideoOverlay`, `DrawableVideoOverlay` (+ optional `AnalyzerOverlayRenderer`).
- `src/Shiny.Maui.Controls.Camera/VideoRecording.cs` — `Overlay` on `VideoRecordingOptions`.
- `src/Shiny.Maui.Controls.Camera/Platforms/Apple/{CameraViewHandler.Apple.cs, VideoFrameDelegate.cs}`
  + `Platforms/MacOS/CameraViewHandler.MacOS.cs` — AVAssetWriter + audio data output + compositing.
- `src/Shiny.Maui.Controls.Camera/Platforms/Android/CameraViewHandler.Android.cs` — OverlayEffect wiring.
- `src/Shiny.Maui.Controls.Camera/Platforms/Windows/CameraViewHandler.Windows.cs` — effect or manual encode.
- `src/Shiny.Maui.Controls.Camera/Shiny.Maui.Controls.Camera.csproj` + `Directory.Packages.props` —
  add `Xamarin.AndroidX.Camera.Effects` (Android TFM, pin 1.6.1) and Win2D (Windows TFM).
- `samples/Sample/Features/Camera/CameraPage.xaml.cs` — add a "record with overlay" demo
  (timestamp + logo/watermark) via `DelegateVideoOverlay`.
- **Blazor:** `src/Shiny.Blazor.Controls.Camera/wwwroot/camera.js` (canvas-composite +
  `captureStream` recording), `CameraView.razor.cs` (overload `StartRecordingAsync` with the
  overlay spec), plus a `VideoRecordingOptions` + `VideoOverlayElement` named DTO in the Blazor
  project. Add a matching demo to the Blazor camera sample page.

## Docs & sample updates (per CLAUDE.md — keep in sync)

- **README.md** — note burn-in video overlays under the CameraView section.
- **Local skill** `SKILLS/shiny-controls/camera.md` — document `VideoRecordingOptions.Overlay` +
  `IVideoOverlayRenderer` (MAUI) and the Blazor declarative overlay-element model with code samples
  so generated code matches on both hosts.
- **Docs repo** (`~/Desktop/dev/documentation`): add a release-notes entry
  (`src/content/docs/controls/release-notes.mdx`); this is a feature on an existing control, so add
  a sub-node under the CameraView node in `src/sidebar-topics.mjs` if it warrants its own page.
- **Screenshot TODO only** — do not capture. Leave `TODO: capture screenshots for camera video overlay`.
- Blog posts: only if explicitly requested later.

## Verification

- Build: `dotnet build Build.slnf`.
- iOS/Mac Catalyst + macOS + Android: run `samples/Sample/`, drive to the Camera feature page,
  record ~5s with the demo overlay, then play the saved file back (outside the app) and confirm the
  timestamp/watermark is baked into the pixels — not just shown live. Verify front-camera overlay is
  upright and unmirrored; verify audio is present when `IncludeAudio` is true.
- Windows: same, once the spike selects an approach.
- Blazor: run the Blazor sample in Chrome (and Safari to check the WebM caveat), record with the
  demo overlay, download the blob, and confirm the overlay is baked into the WebM pixels. Verify the
  overlay-null path still records the raw stream. Re-check under a **Release WASM publish** (trim/AOT)
  because of the named-DTO interop rule.
- Regression: record with no overlay on each platform (MAUI + Blazor) and confirm the existing
  native/browser path still produces a clean file (no perf/behavior change).
- Unit tests (`tests/`): cover the shared renderer/context types and the `IDrawable` adapter.

## Open risks

1. `OverlayEffect` bound-API surface (Android). Package availability is resolved:
   `Xamarin.AndroidX.Camera.Effects 1.6.1` is on NuGet and matches the existing CameraX binding
   version — only the exact managed API shape needs confirming during implementation.
2. `IBasicVideoEffect` activation in a .NET WinUI3 desktop app (Windows) — spike before committing;
   manual `MediaStreamSource` encode is the fallback.
3. HD30 CPU-compositing throughput; mitigate with reused render targets and PTS-based appends.
4. Blazor: Safari `MediaRecorder`/codec support (WebM output) — feature-detect and degrade
   gracefully; and the trimmed-WASM anonymous-type interop trap — use named DTOs for the overlay
   spec and verify with a Release publish.
