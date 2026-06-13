namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// Implemented by the platform handler and consumed by <see cref="CameraView"/> so the control's async
/// methods (start/stop/capture/permission) can call straight into the native session without routing
/// imperative, result-returning operations through the property/command mapper.
/// </summary>
public interface ICameraViewController
{
    /// <summary>Request camera permission, returning <c>true</c> when granted.</summary>
    Task<bool> RequestPermissionAsync(CancellationToken ct = default);

    /// <summary>Enumerate the cameras available on the device.</summary>
    Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default);

    /// <summary>Start the capture session and preview.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stop the capture session and preview.</summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>Capture a single still photo.</summary>
    Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default);

    /// <summary>Begin recording video (and audio when <see cref="VideoRecordingOptions.IncludeAudio"/>).</summary>
    Task StartVideoRecordingAsync(VideoRecordingOptions options, CancellationToken ct = default);

    /// <summary>Stop the in-progress recording and return the finished file.</summary>
    Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default);
}
