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

**Apple-only.** TrueDepth (front) and dual/LiDAR (back) depth are AVFoundation features with no
portable Android/Windows equivalent (ToF hardware is fragmented; CameraX/WinRT expose nothing
comparable). Other platforms return `null` depth and report the mode unsupported. This is the same
asymmetry the codebase already lives with for Apple-only Vision analyzers.

## The one real risk: enabling depth can change the RGB frame

This is the crux and the reason it must be a per-session mode, not a per-frame toggle.

1. **The device changes.** Depth requires selecting `BuiltInTrueDepthCamera` (front) or a
   depth-capable dual/LiDAR device (back). The current `DeviceTypes` array is wide/ultra/tele only,
   so front selection today resolves to `BuiltInWideAngleCamera`. TrueDepth has a different field of
   view and native formats than the plain wide-angle front camera — same scene, different framing.

2. **The active format is constrained.** Depth streams only on formats listed in the device's
   `SupportedDepthDataFormats`, paired with a video format that supports simultaneous depth. Today's
   `SessionPreset = PresetHigh` is not guaranteed to be such a pairing, so depth mode must abandon
   preset-based configuration and set `ActiveFormat` + `ActiveDepthDataFormat` explicitly. That can
   change the **resolution and aspect ratio** of the BGRA frame analyzers receive.

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

## Public API (shared project: `src/Shiny.Controls.Camera.Shared`)

### Depth map type

A new immutable, platform-neutral depth container. Depth is copied out of the native buffer inside
the capture callback (same lifetime discipline as `AppleCameraFrame`'s BGRA copy), so it outlives
the native buffer and is safe for async analyzers.

```csharp
namespace Shiny.Controls.Camera;

/// <summary>
/// A per-frame depth map in native (un-rotated) image space, aligned to the RGB frame it accompanies.
/// Values are metres from the camera; <see cref="float.NaN"/> where depth is unavailable (no return,
/// occlusion, out of range). Depth resolution is typically much lower than the RGB frame (e.g. 640x480
/// vs 1080p) — use <see cref="Width"/>/<see cref="Height"/>, not the RGB frame's.
/// </summary>
public sealed class DepthMap
{
    public DepthMap(float[] metres, int width, int height, DepthAccuracy accuracy, bool isFiltered)
    { /* store */ }

    /// <summary>Depth width in pixels (native image space, before rotation).</summary>
    public int Width { get; }

    /// <summary>Depth height in pixels (native image space, before rotation).</summary>
    public int Height { get; }

    /// <summary>Row-major depth in metres, length Width*Height. NaN = no depth at that pixel.</summary>
    public ReadOnlySpan<float> Metres { get; }

    /// <summary>Whether the device flagged the depth as absolute or relative (see AVDepthData.accuracy).</summary>
    public DepthAccuracy Accuracy { get; }

    /// <summary>True when the platform applied its hole-filling/smoothing (AVDepthData filtering).</summary>
    public bool IsFiltered { get; }

    /// <summary>Bilinearly sample depth at a normalized (0..1) point in native image space.</summary>
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
    /// (<see cref="DepthMode.Off"/>) or is unsupported on this platform/device. Aligned to the RGB
    /// frame but usually lower resolution — read its own <see cref="DepthMap.Width"/>/<see cref="DepthMap.Height"/>.
    /// </summary>
    public virtual DepthMap? Depth => null;
}
```

`AppleCameraFrame` overrides it (populated when depth mode is active); Android/Windows/Mac frames
inherit the `null` default and need no change.

### `CameraView` property (`src/Shiny.Maui.Controls.Camera/CameraView.Properties.cs`)

```csharp
/// <summary>
/// Whether to capture per-frame depth alongside the RGB feed (Apple only, front TrueDepth or a
/// depth-capable back camera). Default <see cref="DepthMode.Off"/>. Changing this reconfigures the
/// session, and — because depth constrains the device and active format — can change the RGB frame's
/// resolution/aspect; do not mix depth-on and depth-off captures of the same subject. Query
/// <see cref="IsDepthSupported"/> after the handler connects.
/// </summary>
public static readonly BindableProperty DepthModeProperty = BindableProperty.Create(
    nameof(DepthMode), typeof(DepthMode), typeof(CameraView), DepthMode.Off);

/// <summary>True when the current device can deliver depth (read-only; set by the handler).</summary>
public static readonly BindableProperty IsDepthSupportedProperty = BindableProperty.Create(
    nameof(IsDepthSupported), typeof(bool), typeof(CameraView), false, BindingMode.OneWayToSource);
```

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

## Apple implementation

All changes are in `Platforms/Apple/CameraViewHandler.Apple.cs`, one new delegate file, and the
`AppleCameraFrame` override. Everything is gated on `VirtualView.DepthMode != DepthMode.Off`; the
existing code path is taken unchanged otherwise.

### Device selection (`DeviceTypes` / `SelectDevice`)

Add `BuiltInTrueDepthCamera` (front) and `BuiltInLiDarDepthCamera` (back, where present) to the
discovery set **only when depth is requested** — leaving the default RGB device choice untouched so
non-depth sessions keep selecting the wide-angle camera exactly as today. `SelectDevice` picks a
depth-capable device for the requested `Facing`, and reports `IsDepthSupported` from whether the
chosen device has a non-empty `ActiveFormat.SupportedDepthDataFormats` (or the format we select does).

### Session configuration (`ConfigureSession`)

Fork inside `ConfigureSession`:

- **Depth off (unchanged):** `SessionPreset = PresetHigh`, add `AVCaptureVideoDataOutput`, set its
  sample-buffer delegate on `videoQueue`. Exactly today's code.
- **Depth on:** do **not** set a preset; instead choose a `(videoFormat, depthFormat)` pairing from
  the device's `Formats`/`SupportedDepthDataFormats`, `LockForConfiguration`, set `ActiveFormat` +
  `ActiveDepthDataFormat`, add both the video data output and a new `AVCaptureDepthDataOutput`
  (`FilteringEnabled = true`, `AlwaysDiscardsLateDepthData = true`), then create an
  `AVCaptureDataOutputSynchronizer` over `[videoOutput, depthOutput]` and set its delegate. The video
  output's own sample-buffer delegate is **not** set in this mode — the synchronizer drives delivery.

`Required` mode raises `OnCameraError` and aborts if no depth-capable device/format is found;
`WhenAvailable` falls back to the depth-off configuration and leaves `IsDepthSupported = false`.

### New delegate: `DepthSyncDelegate` (`Platforms/Apple/DepthSyncDelegate.cs`)

Implements `IAVCaptureDataOutputSynchronizerDelegate`. Per synchronized collection: pull the
`AVCaptureSynchronizedSampleBufferData` (video) and `AVCaptureSynchronizedDepthData` (depth), skip if
either is dropped, convert the `AVDepthData` (to `DepthDataType.DisparityFloat32`→depth or
`DepthFloat32`) into a managed `float[]` of metres inside the callback, and build the
`AppleCameraFrame` with the depth attached. Reuses the existing `WantFrames`/`OnFrame`/`OnError`
plumbing so the pipeline side is identical. Same hard rule as `VideoFrameDelegate`: no managed
exception may escape into the ObjC callback — swallow per-frame, report the first via `OnError`.

The RGB rendering/filter path stays on `VideoFrameDelegate` for depth-off. To avoid duplicating the
CIFilter preview logic, factor the "wrap pixel buffer → filter → OnFrame" body into a shared helper
both delegates call, or have `DepthSyncDelegate` hold a `VideoFrameDelegate` and forward the video
sample buffer to it for filtering while owning frame construction. (MVP: forward; keeps the filter
code in one place.)

### `AppleCameraFrame` (`Platforms/AppleShared/AppleCameraFrame.cs`)

Add an optional constructor parameter `DepthMap? depth = null` and an override `public override
DepthMap? Depth => this.depth;`. The BGRA copy is unchanged. Depth is already materialized (copied in
the delegate) so no per-frame lazy work is added; alternatively defer the `AVDepthData`→`float[]`
conversion into `MaterializeDepth()` if we want to skip it when an analyzer never reads depth — MVP
copies eagerly for simplicity, matching the eager BGRA copy.

### Teardown (`TeardownSession`)

Dispose the depth output and synchronizer alongside the existing outputs. Add two fields
(`AVCaptureDepthDataOutput? depthOutput`, `AVCaptureDataOutputSynchronizer? synchronizer`) with the
same null-guarded disposal the other outputs already use.

## Other platforms

- **Android / Windows / macOS AppKit:** no change to frame classes — they inherit `CameraFrame.Depth
  => null`. The property mappers for `DepthMode` are no-ops that set `IsDepthSupported = false`. (A
  future Android ToF/Depth16 path via CameraX `ImageAnalysis` on `DEPTH16`-capable devices is possible
  but explicitly out of scope; the seam is ready for it.)

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

- **Unit:** `DepthMap.SampleNormalized` bilinear correctness and NaN handling; `DepthMode` mapper
  no-ops on non-Apple. These run in `Shiny.Maui.Controls.Camera.Tests` (no device).
- **On-device (manual, TrueDepth iPhone):** the discrimination spike is the real acceptance test —
  log the depth-on video active-format dimensions vs. `PresetHigh` (quantify the framing shift), and
  measure plane-fit residual for a real face vs. a photo-of-the-face on a second screen. A wide margin
  is the go/no-go for the face-stack consumer. Face ID-capable devices only; there is no simulator
  depth.

## Rollout / risk

- **Versioning:** additive API (one enum, one property, one nullable virtual, one type). No breaking
  change; ships in a normal minor beta.
- **The MAUI/camera-beta pin.** The consuming repo pins `Microsoft.Maui.Controls 10.0.71` against
  these camera betas specifically to avoid the `AMM0000` Android manifest-merge break. This change is
  Apple-only and touches no Android dependencies, so it should not move that constraint — but it
  produces a **new camera beta**, and the standard rule applies: bump the camera package and re-verify
  the Android head together, don't let the two drift.
- **Scope guard:** MVP is front-camera TrueDepth depth delivery to analyzers, off by default. Not in
  scope: back-camera LiDAR tuning, Android depth, ARKit face mesh, portrait-matte/segmentation
  outputs, or the liveness algorithm itself (that's the face package).

## File-by-file summary

| File | Change |
|---|---|
| `src/Shiny.Controls.Camera.Shared/DepthMap.cs` | **New.** `DepthMap` + `DepthAccuracy`. |
| `src/Shiny.Controls.Camera.Shared/CameraEnums.cs` | Add `DepthMode` enum. |
| `src/Shiny.Controls.Camera.Shared/CameraFrame.cs` | Add `virtual DepthMap? Depth => null;`. |
| `src/Shiny.Maui.Controls.Camera/CameraView.Properties.cs` | Add `DepthMode` + `IsDepthSupported` bindable properties. |
| `src/Shiny.Maui.Controls.Camera/Platforms/Apple/CameraViewHandler.Apple.cs` | Depth-gated forks in `ConfigureSession`, `SelectDevice`/`DeviceTypes`, `TeardownSession`; `MapDepthMode`; depth fields. |
| `src/Shiny.Maui.Controls.Camera/Platforms/Apple/DepthSyncDelegate.cs` | **New.** Synchronizer delegate: pair video+depth, convert `AVDepthData`, build frame. |
| `src/Shiny.Maui.Controls.Camera/Platforms/AppleShared/AppleCameraFrame.cs` | Optional `DepthMap? depth` ctor arg + `Depth` override. |
| `.../Platforms/{Android,Windows,MacOS}/*` | `MapDepthMode` no-op → `IsDepthSupported = false`; frames inherit `null` depth. |
| `tests/Shiny.Maui.Controls.Camera.Tests` | `DepthMap` sampling + mapper no-op tests. |
```
