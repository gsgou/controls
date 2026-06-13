using System.Collections.ObjectModel;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A cross-platform camera preview with zoom / torch / lens control, still capture, and a pluggable
/// frame-analysis pipeline (barcode, face, motion, OCR) whose detections are drawn as bounding boxes
/// over the preview. Backed by AVFoundation (iOS/macOS), CameraX (Android) and MediaCapture (Windows).
/// </summary>
public partial class CameraView : View
{
    /// <summary>Analyzers run against each frame. Add/remove freely; the running session picks up changes.</summary>
    public IList<IFrameAnalyzer> Analyzers { get; } = new ObservableCollection<IFrameAnalyzer>();

    /// <summary>Raised on the UI thread when a still photo has been captured.</summary>
    public event EventHandler<CameraPhoto>? MediaCaptured;

    /// <summary>Raised on the UI thread when a video recording finishes.</summary>
    public event EventHandler<CameraVideo>? VideoCaptured;

    /// <summary>Raised on the UI thread when the camera or pipeline reports an error.</summary>
    public event EventHandler<CameraErrorEventArgs>? CameraError;

    /// <summary>Raised on the UI thread whenever the aggregated set of detections changes.</summary>
    public event EventHandler<DetectionsChangedEventArgs>? DetectionsChanged;

    ICameraViewController? Controller => this.Handler as ICameraViewController;


    /// <summary>Request camera permission. Returns <c>true</c> when granted.</summary>
    public Task<bool> RequestPermissionAsync(CancellationToken ct = default)
        => this.Controller?.RequestPermissionAsync(ct) ?? Task.FromResult(false);

    /// <summary>List the cameras available on this device (use a <see cref="CameraInfo.Id"/> for <see cref="CameraId"/>).</summary>
    public Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default)
        => this.Controller?.GetAvailableCamerasAsync(ct) ?? Task.FromResult<IReadOnlyList<CameraInfo>>([]);

    /// <summary>Start the capture session and preview.</summary>
    public Task StartAsync(CancellationToken ct = default)
        => this.Controller?.StartAsync(ct) ?? Task.CompletedTask;

    /// <summary>Stop the capture session and preview.</summary>
    public Task StopAsync(CancellationToken ct = default)
        => this.Controller?.StopAsync(ct) ?? Task.CompletedTask;

    /// <summary>Capture a single still photo. Also raises <see cref="MediaCaptured"/>.</summary>
    public async Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default)
    {
        if (this.Controller is null)
            throw new InvalidOperationException("CameraView handler is not connected");

        var photo = await this.Controller.CapturePhotoAsync(ct).ConfigureAwait(false);
        this.MediaCaptured?.Invoke(this, photo);
        return photo;
    }


    /// <summary>Start recording video. Audio is included unless disabled via <paramref name="options"/>.</summary>
    public async Task StartVideoRecordingAsync(VideoRecordingOptions? options = null, CancellationToken ct = default)
    {
        if (this.Controller is null)
            throw new InvalidOperationException("CameraView handler is not connected");

        await this.Controller.StartVideoRecordingAsync(options ?? new VideoRecordingOptions(), ct).ConfigureAwait(false);
        this.IsRecording = true;
    }


    /// <summary>Stop the in-progress recording. Also raises <see cref="VideoCaptured"/>.</summary>
    public async Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default)
    {
        if (this.Controller is null)
            throw new InvalidOperationException("CameraView handler is not connected");

        var video = await this.Controller.StopVideoRecordingAsync(ct).ConfigureAwait(false);
        this.IsRecording = false;
        this.VideoCaptured?.Invoke(this, video);
        return video;
    }


    /// <summary>Invoked by the handler/pipeline to publish the latest detections. Raises <see cref="DetectionsChanged"/>.</summary>
    public void OnDetectionsChanged(IReadOnlyList<Detection> detections, int imageWidth, int imageHeight)
    {
        this.Detections = detections;
        this.DetectionsChanged?.Invoke(this, new DetectionsChangedEventArgs(detections, imageWidth, imageHeight));
    }

    /// <summary>Invoked by the handler to report an error. Raises <see cref="CameraError"/>.</summary>
    public void OnCameraError(string message, Exception? exception = null)
        => this.CameraError?.Invoke(this, new CameraErrorEventArgs(message, exception));

    /// <summary>Invoked by the handler to publish the supported zoom range it discovered.</summary>
    public void OnZoomRangeChanged(double minZoom, double maxZoom)
    {
        this.MinZoom = minZoom;
        this.MaxZoom = maxZoom;
    }
}


/// <summary>Carries the latest detection set to <see cref="CameraView.DetectionsChanged"/> subscribers.</summary>
public class DetectionsChangedEventArgs(IReadOnlyList<Detection> detections, int imageWidth, int imageHeight) : EventArgs
{
    /// <summary>The aggregated detections across all analyzers, in normalized upright image space.</summary>
    public IReadOnlyList<Detection> Detections { get; } = detections;

    /// <summary>Width of the analyzed image in pixels.</summary>
    public int ImageWidth { get; } = imageWidth;

    /// <summary>Height of the analyzed image in pixels.</summary>
    public int ImageHeight { get; } = imageHeight;
}


/// <summary>Carries an error from <see cref="CameraView.CameraError"/>.</summary>
public class CameraErrorEventArgs(string message, Exception? exception) : EventArgs
{
    /// <summary>A human-readable description of the failure.</summary>
    public string Message { get; } = message;

    /// <summary>The underlying exception, if any.</summary>
    public Exception? Exception { get; } = exception;
}
