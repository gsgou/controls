# TrueDepth / depth-data capture (CameraView)

## Context

`CameraView` today captures a single `AVCaptureVideoDataOutput` (BGRA) on Apple, wraps each frame
as an `AppleCameraFrame`, and hands it to the analysis pipeline. There is no depth path anywhere —
a grep of the shipped assembly finds no `Depth`/`TrueDepth` symbols, and `CameraView` exposes no
`AVCaptureSession`, so a consumer cannot attach a depth output from outside either.

This plan adds **optional, opt-in depth capture** so an analyzer can read a per-frame depth map
alongside the RGB frame. The motivating use is **face anti-spoofing / liveness** (a printed photo or
a screen replay is geometrically flat; a real face has centimetres of structure), which the
downstream `Shiny.FaceIntelligence` stack wants but cannot get without this. Depth is broadly useful
beyond that (background segmentation, portrait effects, measurement), so it belongs in the camera
control, not in a face-specific package.

**Design stance: off by default, additive, zero change when off.** A new `DepthMode` property
defaults to `Off`. When `Off`, the session, the delegate, the frame type and the analyzer contract
are byte-for-byte what they are today — the depth output is never created and the synchronizer never
exists. Every new surface is a nullable that existing consumers ignore. This mirrors how `Filter`,
`IsTorchOn`, and the video-overlay path already gate optional behaviour.

**Apple-only for this phase.** See [Other platforms](#other-platforms) — Windows has a genuine
(IR-based) equivalent that is a deliberate phase 2, and Android has effectively nothing usable.

## The one real risk: enabling depth can change the RGB frame

This is the crux and the reason it must be a per-session mode, not a per-frame toggle.

1. **The device changes.** Depth requires selecting `BuiltInTrueDepthCamera` (front) or a
   depth-capable dual/LiDAR device (back). The current `DeviceTypes` array is wide/ultra/tele only,
   so front selection today resolves to `BuiltInWideAngleCamera`. TrueDepth has a different field of
   view and native formats than the plain wide-angle front camera — same scene, different framing.

2. **The active format is constrained.** Depth streams only on formats listed in the device's
   `SupportedDepthDataFormats`, paired with a video format that supports simultaneous depth. Today's
   session is preset-driven (see below), and no preset is guaranteed to be such a pairing, so depth
   mode must abandon preset-based configuration and set `ActiveFormat` + `ActiveDepthDataFormat`
   explicitly. That can change the **resolution and aspect ratio** of the BGRA frame analyzers receive.

The pixel *format* does not change — depth arrives as a **separate** `AVDepthData` buffer,
timestamp-synchronized with the video buffer via `AVCaptureDataOutputSynchronizer`. It is additive
data, not a rewrite of the video pixels. But because the framing/resolution can shift, **a template
captured with depth on and a probe captured with depth off went through different framing** — the
systematic-offset failure class the face stack already fights. Hence: depth is a mode you commit to
for the whole session (enroll *and* recognize), not something you flip mid-flow.

`AppleCameraFrame` already carries `Width`/`Height`/`Rotation`/`IsMirrored` per frame and the
downstream converter reads them per frame, so a different aspect ratio is *handled* correctly frame
to frame — the caution is purely about not mixing depth-on and depth-off captures of the same
identity.

## Corrections to earlier drafts of this plan

Two assumptions in the original draft were stale against `CameraViewHandler.Apple.cs` as it stands.
Both change the implementation, so they are called out rather than silently fixed.

**There is no fixed `SessionPreset = PresetHigh`.** `ConfigureSession` uses `ResolvePreset()`
(`CameraViewHandler.Apple.cs:383`), a `VideoQuality`-driven ladder defaulting to 1920x1080, and there
is a whole `ApplyVideoSettings()` path (`:251`) that reapplies preset + frame rate + bitrate. That
path actively conflicts with depth — see [Interaction with `ApplyVideoSettings`](#interaction-with-applyvideosettings).

**Frames are already oriented; there is no "native un-rotated" convention.** `OrientConnections`
(`:530`) sets `VideoOrientation = Portrait` and `VideoMirrored = front` on the data-output connection,
and `VideoFrameDelegate` then constructs the frame with `rotation: 0, mirrored: false`
(`VideoFrameDelegate.cs:77`, handler `:547`). Buffers arrive upright and front-mirrored. Every depth
surface must therefore be documented and delivered **in the same orientation and mirroring as the RGB
frame**, not in sensor-native space.

## Public API (shared project: `src/Shiny.Controls.Camera.Shared`)

### Open decision: `DepthMap? Depth` vs. a general auxiliary plane

Phase 2 on Windows delivers an **infrared luminance plane**, not a depth map (see
[Other platforms](#other-platforms)). If `CameraFrame` gains `DepthMap? Depth` now, Windows will want
a second nullable (`InfraredPlane? Infrared`) later, and analyzers will branch on both.

**Recommendation: ship `DepthMap? Depth` now anyway.** The two planes have genuinely different
semantics (metric distance with an accuracy flag vs. 8-bit reflectance), a premature `AuxiliaryPlane`
union would be a lowest-common-denominator type neither consumer wants, and the Windows path is gated
on an unresolved enumeration spike that may kill it outright. Revisit only if the Windows spike comes
back green.

### Depth map type

A new immutable, platform-neutral depth container. Depth is copied out of the native buffer inside
the capture callback (same lifetime discipline as `AppleCameraFrame`'s BGRA copy), so it outlives
the native buffer and is safe for async analyzers.

```csharp
namespace Shiny.Controls.Camera;

/// <summary>
/// A per-frame depth map, delivered in the same orientation and mirroring as the RGB frame it
/// accompanies (upright; horizontally mirrored on the front camera). Values are metres from the camera;
/// <see cref="float.NaN"/> where depth is unavailable (no return, occlusion, out of range) — unless the
/// platform's hole-filling was enabled, in which case see <see cref="IsFiltered"/>. Depth resolution is
/// typically much lower than the RGB frame (e.g. 640x480 vs 1080p) — use <see cref="Width"/>/<see cref="Height"/>,
/// not the RGB frame's. The capture pipeline guarantees the depth and video formats share a field of view
/// and aspect ratio, so normalized coordinates are directly comparable between the two.
/// </summary>
public sealed class DepthMap
{
    public DepthMap(float[] metres, int width, int height, DepthAccuracy accuracy, bool isFiltered)
    { /* store */ }

    /// <summary>Depth width in pixels.</summary>
    public int Width { get; }

    /// <summary>Depth height in pixels.</summary>
    public int Height { get; }

    /// <summary>Row-major depth in metres, length Width*Height. NaN = no depth at that pixel.</summary>
    public ReadOnlySpan<float> Metres { get; }

    /// <summary>Whether the device flagged the depth as absolute or relative (see AVDepthData.accuracy).</summary>
    public DepthAccuracy Accuracy { get; }

    /// <summary>True when the platform applied its hole-filling/smoothing (AVDepthData filtering).</summary>
    public bool IsFiltered { get; }

    /// <summary>
    /// Bilinearly sample depth at a normalized (0..1) point. NaN taps are excluded and the remaining
    /// weights renormalized; returns NaN only when all four taps are NaN. (Naive bilinear would let a
    /// single missing neighbour poison an otherwise valid sample.)
    /// </summary>
    public float SampleNormalized(float x, float y);
}

/// <summary>How much to trust <see cref="DepthMap.Metres"/> as true distance (maps AVDepthDataAccuracy).</summary>
public enum DepthAccuracy { Relative, Absolute }
```

### Depth accessor on `CameraFrame`

`CameraFrame` gains a nullable depth accessor, following the existing additive pattern (`GetLuminance`
is the precedent — an optional, lazily-produced plane on the base type). Default returns `null`, so
**every existing frame subclass and every existing analyzer is unaffected**.

```csharp
public abstract class CameraFrame : IDisposable
{
    // ... existing members unchanged ...

    /// <summary>
    /// The depth map captured with this frame, or <c>null</c> when depth was not requested
    /// (<see cref="DepthMode.Off"/>) or is unsupported on this platform/device. Same orientation and
    /// mirroring as the RGB frame but usually lower resolution — read its own
    /// <see cref="DepthMap.Width"/>/<see cref="DepthMap.Height"/>.
    /// </summary>
    public virtual DepthMap? Depth => null;
}
```

`AppleCameraFrame` overrides it (populated when depth mode is active); Android/Windows/Mac frames
inherit the `null` default and need no change.

### `CameraView` properties (`src/Shiny.Maui.Controls.Camera/CameraView.Properties.cs`)

```csharp
/// <summary>
/// Whether to capture per-frame depth alongside the RGB feed (Apple only, front TrueDepth or a
/// depth-capable back camera). Default <see cref="DepthMode.Off"/>. Changing this reconfigures the
/// session, and — because depth constrains the device and active format — can change the RGB frame's
/// resolution/aspect; do not mix depth-on and depth-off captures of the same subject. While depth is
/// active, <see cref="VideoQuality"/> is ignored (the active format is chosen for depth compatibility).
/// Query <see cref="IsDepthSupported"/> after the handler connects.
/// </summary>
public static readonly BindableProperty DepthModeProperty = BindableProperty.Create(
    nameof(DepthMode), typeof(DepthMode), typeof(CameraView), DepthMode.Off);

/// <summary>
/// Whether to let the platform hole-fill and smooth the depth map (AVDepthData filtering). Default
/// <c>false</c>. Filtering produces prettier depth for rendering, but it interpolates structure into
/// regions that had none and removes the NaN holes that tell an analyzer "no data here" — both of which
/// work against anti-spoofing. Leave off for security use, turn on for portrait/segmentation effects.
/// </summary>
public static readonly BindableProperty IsDepthFilteringEnabledProperty = BindableProperty.Create(
    nameof(IsDepthFilteringEnabled), typeof(bool), typeof(CameraView), false);

/// <summary>True when the current device can deliver depth (read-only; set by the handler).</summary>
public static readonly BindableProperty IsDepthSupportedProperty = BindableProperty.Create(
    nameof(IsDepthSupported), typeof(bool), typeof(CameraView), false, BindingMode.OneWayToSource);
```

`IsDepthSupported` follows the existing handler-writes-back pattern (`IsRecordingProperty`,
`CameraView.Properties.cs:121`).

```csharp
namespace Shiny.Controls.Camera;

/// <summary>How <see cref="CameraView"/> handles depth capture.</summary>
public enum DepthMode
{
    /// <summary>No depth. The session is exactly as it is without this feature (default).</summary>
    Off,

    /// <summary>
    /// Capture depth when the selected device supports it; otherwise run RGB-only and set
    /// <c>IsDepthSupported = false</c>. Never fails to start for lack of depth.
    /// </summary>
    WhenAvailable,

    /// <summary>
    /// Require depth: if the device can't deliver it, raise <c>OnCameraError</c> rather than silently
    /// capturing RGB-only. For flows where depth is a security control and a silent fallback is a bypass.
    /// </summary>
    Required
}
```

`WhenAvailable` vs `Required` is the important distinction for the liveness use case: a silent
RGB-only fallback is an attacker's easiest path, so a security caller wants `Required` and an explicit
error, while a portrait-effect caller wants `WhenAvailable` and graceful degradation.

### The `Required` invariant

Configure-time validation is not sufficient. If the synchronizer reports `DepthDataWasDropped` on a
given collection, delivering the video frame with `Depth == null` reintroduces exactly the silent
RGB-only fallback the mode exists to prevent — and an attacker who can induce drops (thermal load,
occluding the projector) gets a bypass.

**In `Required` mode, a frame without depth is never delivered to the pipeline.** Drop it. If drops
persist beyond a threshold, surface `OnCameraError` so the caller can fail the flow rather than wait
forever. In `WhenAvailable`, a depth-less frame is delivered normally with `Depth == null`.

## Apple implementation

All changes are in `Platforms/Apple/CameraViewHandler.Apple.cs`, one new delegate file, a refactor of
`VideoFrameDelegate`, and the `AppleCameraFrame` override. Everything is gated on
`VirtualView.DepthMode != DepthMode.Off`; the existing code path is taken unchanged otherwise.

### Device selection (`DeviceTypes` / `SelectDevice`)

Add `BuiltInTrueDepthCamera` (front) and, for the back camera, `BuiltInLiDarDepthCamera` plus
`BuiltInDualCamera` / `BuiltInDualWideCamera` (LiDAR is only on Pro devices; the dual cameras cover
the rest) — **only when depth is requested**, leaving the default RGB device choice untouched so
non-depth sessions keep selecting the wide-angle camera exactly as today.

Two mechanical points:

- `DeviceTypes` is a `static readonly` array (`:632`) consumed by the **static** `DiscoverDevice`
  (`:720`) and by `GetAvailableCamerasAsync` (`:639`). The depth-aware set has to be threaded through
  as a parameter — `DiscoverDevice` cannot read instance state as written. Decide deliberately whether
  `GetAvailableCamerasAsync` should start reporting TrueDepth/LiDAR devices (recommendation: no, keep
  the enumeration stable and let depth selection be internal).
- `BuiltInLiDarDepthCamera` is iOS 15.4+ and the repo's floor is 15.0 (`Directory.Build.props:11`), so
  it needs an `OperatingSystem.IsIOSVersionAtLeast(15, 4)` guard or it trips CA1416.

`SelectDevice` picks a depth-capable device for the requested `Facing`, and reports `IsDepthSupported`
from whether the chosen device exposes a usable `(videoFormat, depthFormat)` pairing.

### Format pairing

Choose a `(videoFormat, depthFormat)` pair from the device's `Formats` /
`SupportedDepthDataFormats` such that **the two share a field of view and aspect ratio**. This is a
hard requirement, not an optimisation: `DepthMap.SampleNormalized` and any face-box mapping the
consumer does are only valid alignment if normalized coordinates mean the same thing in both. A 4:3
depth format paired with a 16:9 video format is silently, systematically wrong.

### Session configuration (`ConfigureSession`)

Fork inside `ConfigureSession`:

- **Depth off (unchanged):** `SessionPreset = ResolvePreset()`, add `AVCaptureVideoDataOutput`, set its
  sample-buffer delegate on `videoQueue`. Exactly today's code.
- **Depth on:** do **not** set a preset; instead `LockForConfiguration`, set `ActiveFormat` +
  `ActiveDepthDataFormat` from the chosen pairing, add both the video data output and a new
  `AVCaptureDepthDataOutput` (`FilteringEnabled = VirtualView.IsDepthFilteringEnabled`,
  `AlwaysDiscardsLateDepthData = true`), then create an `AVCaptureDataOutputSynchronizer` over
  `[videoOutput, depthOutput]` and set its delegate. The video output's own sample-buffer delegate is
  **not** set in this mode — the synchronizer drives delivery.

Ordering matters: add the input and set the active format **before** `CanAddOutput(depthOutput)`, which
returns false unless the input device's active format supports depth.

`Required` mode raises `OnCameraError` and aborts if no depth-capable device/format is found;
`WhenAvailable` falls back to the depth-off configuration and leaves `IsDepthSupported = false`.

### Interaction with `ApplyVideoSettings`

**This is the highest-risk interaction in the change.** `ApplyVideoSettings()` (`:251`) assigns
`s.SessionPreset`, which resets `ActiveFormat` and drops `ActiveDepthDataFormat`. It is called from
three places: `ConfigureSession` immediately after `CommitConfiguration()` (`:411`), `ReconfigureInput`
on every facing change (`:708`), and `MapVideoQuality` on demand (`:247`). Left alone, depth would stop
after the very first configure, silently.

In depth mode `ApplyVideoSettings` must **skip the preset assignment entirely** and apply only
`ApplyFrameRate()` and `ApplyMovieOutputBitrate()`. `VideoQuality` is documented as ignored while depth
is active (the active format is chosen for depth compatibility instead).

### Facing changes (`ReconfigureInput`)

`ReconfigureInput` (`:689`) swaps the device input inside a Begin/Commit and re-runs
`ApplyVideoSettings` — it does **not** re-select an active format, re-add the depth output, or rebuild
the synchronizer. Front↔back is the common gesture and front TrueDepth is the motivating case, so this
is not an edge case.

Depth mode needs a branch in `ReconfigureInput` that, after the new input is added: re-runs format
pairing for the new device, sets `ActiveFormat`/`ActiveDepthDataFormat`, removes and re-adds the depth
output, rebuilds the synchronizer, and re-runs `OrientConnections` (below). If the new facing has no
depth-capable device, apply the same `Required`/`WhenAvailable` policy as at configure time.

### Connection orientation — the depth connection too

`OrientConnections` (`:530`) touches only the `dataOutput` connection. Without giving the depth-data
connection the same `VideoOrientation` / `VideoMirrored` treatment (or applying the equivalent EXIF
orientation to the `AVDepthData`), **the depth map arrives rotated 90° and horizontally flipped
relative to the RGB frame** — falsifying the alignment contract the whole feature rests on, and doing
it silently. A plane-fit residual on a transposed depth map still produces a plausible-looking number.

`OrientConnections` must handle both connections, and must be re-run after any facing change.

### New delegate: `DepthSyncDelegate` (`Platforms/Apple/DepthSyncDelegate.cs`)

Implements `IAVCaptureDataOutputSynchronizerDelegate`. Per synchronized collection: pull the
`AVCaptureSynchronizedSampleBufferData` (video) and `AVCaptureSynchronizedDepthData` (depth), apply the
`Required` invariant on drops, convert the `AVDepthData` to metres, and build the `AppleCameraFrame`
with the depth attached. Reuses the existing `WantFrames`/`OnFrame`/`OnError` plumbing so the pipeline
side is identical. Same hard rule as `VideoFrameDelegate`: no managed exception may escape into the
ObjC callback — swallow per-frame, report the first via `OnError`.

For the conversion, call `AVDepthData.ConvertToDepthDataType(DepthFloat32)` and read the result
directly rather than hand-rolling the disparity reciprocal; carry `AVDepthData.Accuracy` into
`DepthMap.Accuracy`.

### Refactoring `VideoFrameDelegate` (do NOT forward the sample buffer)

An earlier draft suggested that `DepthSyncDelegate` hold a `VideoFrameDelegate` and forward the video
sample buffer to it "to keep the filter code in one place". **That is a use-after-free and must not be
done.** `DidOutputSampleBuffer` unconditionally calls `sampleBuffer.Dispose()` in its `finally`
(`VideoFrameDelegate.cs:92`), but under the synchronizer the buffer is owned by the
`AVCaptureSynchronizedSampleBufferData` in the collection. It would also build and dispatch its own
depth-less `AppleCameraFrame` (`:77`), producing double delivery or frames with no depth.

`VideoFrameDelegate` does three separable jobs per frame:

1. render the filtered frame into the overlay `UIImageView` (`RenderFiltered`),
2. construct an `AppleCameraFrame` and hand it to `OnFrame`,
3. append to the burn-in recorder (`Recorder?.AppendVideo`).

Extract (1) and (3) into methods (or a small shared collaborator) that both delegates call, leaving
each delegate to own frame construction and buffer lifetime. Note that (3) matters for feature parity:
`StartVideoRecordingAsync` requires a non-null `frameDelegate` and sets `.Recorder` on it (`:122`,
`:150`), so without this the overlay/effects recording path breaks in depth mode.

### `AppleCameraFrame` (`Platforms/AppleShared/AppleCameraFrame.cs`)

Add an optional constructor parameter `DepthMap? depth = null` and an override
`public override DepthMap? Depth => this.depth;`. The BGRA copy is unchanged. The optional parameter
keeps the macOS AppKit head (which shares this file) compiling untouched.

**Pool the depth buffer.** A 640x480 float map is ~1.2 MB per frame — ~36 MB/s of gen-0 garbage at
30fps, on top of the existing per-frame BGRA copy. `CameraFrame` is already reference-counted with a
`ReleaseNative` hook (`CameraFrame.cs:58–69`), so rent from `ArrayPool<float>.Shared` in the delegate
and return it in `ReleaseNative`. This is nearly free to do now and awkward to retrofit once analyzers
hold `DepthMap` references.

### Teardown (`TeardownSession`)

Dispose the depth output and synchronizer alongside the existing outputs. Add two fields
(`AVCaptureDepthDataOutput? depthOutput`, `AVCaptureDataOutputSynchronizer? synchronizer`) with the
same null-guarded disposal the other outputs already use (`:819`).

### Open question for the on-device spike

`ConfigureSession` currently attaches photo + video-data + movie-file outputs (`:389–403`). Whether
`CanAddOutput(depthOutput)` succeeds with all of those already attached is device- and
format-dependent. If it does not, depth mode has to drop `AVCaptureMovieFileOutput` — losing the fast
native recording path while depth is on. That is a real functional trade-off and should be settled by
the spike, not discovered in the field. `CapturePhotoAsync` depth delivery
(`AVCapturePhotoOutput.DepthDataDeliveryEnabled`) is explicitly out of scope for the MVP.

## Other platforms

### Android — ruled out

- **CameraX has no depth API**, confirmed by the CameraX team, and our entire Android path is CameraX
  (`ProcessCameraProvider` + `ImageAnalysis`, `CameraViewHandler.Android.cs`).
- **Camera2 `DEPTH16`** exists behind `REQUEST_AVAILABLE_CAPABILITIES_DEPTH_OUTPUT`, but front-facing
  ToF is effectively extinct — a 2019–2020 flagship fad (Galaxy S10 5G, Note10+, Mate 20 Pro) that
  Samsung dropped; Pixel 4's face-unlock IR projector was never app-accessible. It would mean a
  Camera2 side-path, abandoning CameraX for that session, to serve a near-zero device population.
- **ARCore Depth API** is the tempting wrong answer: back-camera, world-scale, motion-stereo/ML-derived,
  requires its own session that cannot share the camera, and is not accurate at 30–40cm face distance.
- **ARCore Augmented Faces is the dangerous wrong answer** and is called out here so nobody adds it
  later as a "helpful" improvement: it is a 468-vertex mesh inferred from RGB, and it will happily fit
  a confident mesh to a printed photograph. Using it for liveness is **worse than no check**, because
  it manufactures a false positive signal.

Android frames inherit `CameraFrame.Depth => null`; `MapDepthMode` sets `IsDepthSupported = false`.

### Windows — a real equivalent, deliberate phase 2

WinRT has a genuine equivalent and our Windows head is already sitting on it. The handler drives
`MediaCapture` + `MediaFrameReader` and picks its source with
`s.Info.SourceKind == MediaFrameSourceKind.Color` (`CameraViewHandler.Windows.cs:109`) — that single
line is the extension point. The supporting API is in some ways better-shaped than AVFoundation's:

- `MultiSourceMediaFrameReader` — correlated multi-source delivery, the direct analogue of
  `AVCaptureDataOutputSynchronizer`.
- `DepthMediaFrameFormat.DepthScaleInMeters` — unit conversion handed to you rather than derived.
- `DepthCorrelatedCoordinateMapper` (`TryCreateCoordinateMapper`) — maps depth pixels into the colour
  camera's space **for you**, solving the orientation/mirroring/FOV alignment problem that the Apple
  path has to get right by hand.

**The catch is hardware, not API.** Windows Hello face is **IR, not depth** — a typical Hello laptop
exposes an `Infrared` source and no `Depth` source at all. True `Depth` sources mean RealSense /
Azure Kinect-class hardware, rare on consumer machines. So the realistic Windows signal is IR.

That is not a consolation prize. For anti-spoofing, **IR is arguably a stronger signal than depth**:
LCD/OLED panels emit essentially nothing at 850nm, so a screen-replay attack appears as a black
rectangle, and printed photos have skin-unlike IR reflectance. It is why Hello itself is IR-based. A
depth flatness test needs careful thresholding; "the face is invisible in IR" does not.

**Gate: an enumeration spike, before any commitment.** Two unresolved risks:

1. IR/depth source visibility depends on the vendor shipping the right camera driver (device MFT);
   sources can simply fail to enumerate, and some OEMs reserve the Hello IR camera.
2. Our Windows head is **WinUI 3 desktop, not UWP**, and Microsoft has not consistently extended these
   frame-source features beyond UWP. We already drive `MediaFrameReader` for Color in-process so the
   plumbing works, but whether `Infrared`/`Depth` sources enumerate in a non-packaged desktop process
   is genuinely unknown.

The spike is cheap: enumerate `MediaFrameSourceGroup` on a Hello-equipped machine and print every
`SourceKind`. Everything else is contingent on the result. Note the phase-2 deliverable would be an
**IR luminance plane, not a `DepthMap`** — see the [API shape decision](#open-decision-depthmap-depth-vs-a-general-auxiliary-plane).

Until then: Windows frames inherit `CameraFrame.Depth => null`; `MapDepthMode` sets
`IsDepthSupported = false`.

### macOS AppKit

No change. `AppleCameraFrame` is shared via `Platforms/AppleShared` and the new constructor parameter
is optional, so the macOS head compiles untouched and never populates depth.

### Blazor

No depth. `getUserMedia` has no equivalent surface. Stated explicitly because MAUI/Blazor parity is a
repo convention and silence reads as an oversight.

### The reframe for the consumer

For liveness *as a security gate*, the platform-native answer on Windows and Android is **not to
reimplement depth at all** — it is to delegate to the OS biometric stack:

- **Windows:** `KeyCredentialManager` / `UserConsentVerifier` (Windows Hello). Presentation-attack
  detection runs in a protected environment and the result is TPM-attested. You get a signed yes/no —
  you cannot get the IR frames or a liveness score, and that is the point.
- **Android:** `BiometricPrompt` at `BIOMETRIC_STRONG` (Class 3), optionally bound to a Keystore key
  with `setUserAuthenticationRequired`. Hardware-backed assertion, same shape.

That is a materially better security posture than anything built on raw frames, and it is a completely
different API surface from `CameraView` — it belongs in an auth abstraction, not a camera control.

**This has an API consequence.** `DepthMode.Required` is unsatisfiable on Windows and Android, and
"this platform cannot do camera-based liveness, use the OS instead" is a different condition from
"this device lacks depth". `Shiny.FaceIntelligence` needs to distinguish the two rather than treating
both as `IsDepthSupported = false`.

## Consumer usage (`Shiny.FaceIntelligence` side, illustrative — not part of this repo)

```csharp
// enrollment/recognition control opts in
this.Camera.DepthMode = DepthMode.Required;   // liveness is a security gate; no silent RGB fallback

// inside the analyzer, per frame:
if (frame.Depth is { } depth)
{
    var residual = PlaneFitResidual(depth, faceBoxInDepthSpace);
    if (residual < FlatThreshold)
        return LivenessResult.Spoofed;   // photo/screen: no real structure
}
```

The face stack maps its pixel `FaceBox` into depth space via the depth map's own dimensions, fits a
plane to the face region, and measures residual. That logic lives in the face package, behind an
`IFaceLivenessDetector` seam; this plan only delivers the depth pixels.

## Info.plist / permissions

No new permission. `NSCameraUsageDescription` already covers the capture session; TrueDepth uses the
same camera authorization. (ARKit face mesh would add disclosure obligations — this plan deliberately
uses AVFoundation depth, not ARKit, so it stays within the existing capture pipeline and permission.)

## Testing

- **Unit:** `DepthMap.SampleNormalized` — bilinear correctness, and specifically the NaN policy
  (partial NaN taps renormalize; all-NaN returns NaN). `DepthMode` mapper no-ops on non-Apple. These
  run in `tests/Shiny.Maui.Controls.Camera.Tests` (no device).
- **On-device (manual, TrueDepth iPhone):** the discrimination spike is the real acceptance test.
  - Log the depth-on video active-format dimensions vs. what `ResolvePreset()` would have chosen, to
    quantify the framing shift.
  - **Verify depth/RGB alignment explicitly** — overlay the depth map on the RGB frame and confirm
    orientation and mirroring match on both facings. This is the failure that looks fine in aggregate
    numbers.
  - Confirm depth survives a facing switch and a `VideoQuality` change (the two regressions this plan
    exists to prevent).
  - Measure plane-fit residual for a real face vs. a photo-of-the-face on a second screen. A wide
    margin is the go/no-go for the face-stack consumer.
  - Confirm whether the depth output can coexist with `AVCaptureMovieFileOutput`.

  Face ID-capable devices only; there is no simulator depth.

## Rollout / risk

- **Versioning:** additive API (one enum, two properties, one nullable virtual, one type). No breaking
  change; ships in a normal minor beta.
- **The MAUI/camera-beta pin.** The consuming repo pins `Microsoft.Maui.Controls 10.0.71` against
  these camera betas specifically to avoid the `AMM0000` Android manifest-merge break. This change is
  Apple-only and touches no Android dependencies, so it should not move that constraint — but it
  produces a **new camera beta**, and the standard rule applies: bump the camera package and re-verify
  the Android head together, don't let the two drift.
- **Scope guard:** MVP is front-camera TrueDepth depth delivery to analyzers, off by default. Not in
  scope: back-camera LiDAR tuning, photo-capture depth, Android depth, ARKit face mesh,
  portrait-matte/segmentation outputs, the Windows IR path (gated on its spike), or the liveness
  algorithm itself (that's the face package).

## File-by-file summary

| File | Change |
|---|---|
| `src/Shiny.Controls.Camera.Shared/DepthMap.cs` | **New.** `DepthMap` (NaN-aware sampling) + `DepthAccuracy`. |
| `src/Shiny.Controls.Camera.Shared/CameraEnums.cs` | Add `DepthMode` enum. |
| `src/Shiny.Controls.Camera.Shared/CameraFrame.cs` | Add `virtual DepthMap? Depth => null;`. |
| `src/Shiny.Maui.Controls.Camera/CameraView.Properties.cs` | Add `DepthMode`, `IsDepthFilteringEnabled`, `IsDepthSupported` bindable properties. |
| `src/Shiny.Maui.Controls.Camera/CameraViewHandler.cs` | Register `MapDepthMode` in `Mapper`; declare the `static partial`. |
| `src/Shiny.Maui.Controls.Camera/Platforms/Apple/CameraViewHandler.Apple.cs` | Depth forks in `ConfigureSession`, `ReconfigureInput`, `SelectDevice`/`DeviceTypes` (now parameterized), `OrientConnections` (depth connection), `ApplyVideoSettings` (skip preset), `TeardownSession`; `MapDepthMode`; depth fields. |
| `src/Shiny.Maui.Controls.Camera/Platforms/Apple/DepthSyncDelegate.cs` | **New.** Synchronizer delegate: pair video+depth, enforce the `Required` invariant, convert `AVDepthData`, build frame. |
| `src/Shiny.Maui.Controls.Camera/Platforms/Apple/VideoFrameDelegate.cs` | Extract filter-render and recorder-append so both delegates share them; do **not** forward sample buffers. |
| `src/Shiny.Maui.Controls.Camera/Platforms/AppleShared/AppleCameraFrame.cs` | Optional `DepthMap? depth` ctor arg, `Depth` override, pooled depth buffer returned in `ReleaseNative`. |
| `.../Platforms/{Android,Windows,MacOS}/*` | `MapDepthMode` no-op → `IsDepthSupported = false`; frames inherit `null` depth. |
| `tests/Shiny.Maui.Controls.Camera.Tests` | `DepthMap` sampling (incl. NaN policy) + mapper no-op tests. |
| `samples/Sample/Features/Camera/CameraPage.xaml(.cs)` | Depth toggle + a live readout (centre-pixel metres / plane residual) to make the feature demonstrable. |
| `README.md` | Document `DepthMode` under the Camera section. |
| `SKILLS/shiny-controls/` | Update the CameraView control doc so generated code knows about `DepthMode`. |
| `~/Desktop/dev/documentation` | Release-note entry in `src/content/docs/controls/release-notes.mdx`; menu node under the `Controls` topic in `src/sidebar-topics.mjs` if it gets its own page. |

## References

- [CameraX overview](https://developer.android.com/media/camera/camerax)
- [Is CameraX supported Time Of Flight (TOF) camera? — camerax-developers](https://groups.google.com/a/android.com/g/camerax-developers/c/iI-O5tWxiZs)
- [Using Time of Flight (ToF) Sensor to Capture Depth Data — Zebra](https://developer.zebra.com/blog/using-time-flight-tof-sensor-capture-depth-data)
- [Use Depth in your Android app — ARCore](https://developers.google.com/ar/develop/java/depth/developer-guide)
- [Windows Hello camera driver bring-up guide](https://learn.microsoft.com/en-us/windows-hardware/drivers/stream/windows-hello-camera-driver-bring-up-guide)
- [Infrared Camera in Media Foundation — alax.info](https://alax.info/blog/1911)
