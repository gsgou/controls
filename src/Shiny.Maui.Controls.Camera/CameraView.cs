using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A cross-platform camera preview with zoom / torch / lens control, still capture, and a pluggable
/// frame-analysis pipeline (barcode, face, motion, OCR, documents). Each analyzer raises its own
/// strongly-typed event for semantic results and returns styled <see cref="OverlayBox"/>es that are drawn
/// over the preview. Backed by AVFoundation (iOS/macOS), CameraX (Android) and MediaCapture (Windows).
/// </summary>
[ContentProperty(nameof(Analyzers))]
public partial class CameraView : View
{
    /// <summary>Analyzers run against each frame. Add/remove freely; the running session picks up changes.</summary>
    public IList<IFrameAnalyzer> Analyzers { get; } = new ObservableCollection<IFrameAnalyzer>();

    public CameraView()
    {
        // analyzers declared in XAML / added in code inherit this view's BindingContext so their Commands bind
        ((INotifyCollectionChanged)this.Analyzers).CollectionChanged += this.OnAnalyzersBindingContext;
        this.ScanCommand = new Command(this.Scan);
    }

    /// <summary>
    /// Arm every enabled analyzer for one scan: each stays silent (still drawing boxes) until its next confirmed
    /// detection, which it then delivers once. An analyzer's <c>OnDetected</c> returning <c>true</c> keeps it
    /// armed (continuous scanning); otherwise it disarms until the next call. Bind a button/Fab to
    /// <see cref="ScanCommand"/>, or call this directly.
    /// </summary>
    public void Scan()
    {
        foreach (var analyzer in this.Analyzers.OfType<FrameAnalyzer>())
            if (analyzer.IsEnabled)
                analyzer.Arm();
    }

    /// <summary>Command form of <see cref="Scan"/> — arms every enabled analyzer for one scan.</summary>
    public ICommand ScanCommand { get; }

    /// <summary>Disarm every analyzer, cancelling any in-progress scan. Boxes keep drawing; results stop until the next <see cref="Scan"/>.</summary>
    public void StopScanning()
    {
        foreach (var analyzer in this.Analyzers.OfType<FrameAnalyzer>())
            analyzer.Disarm();
    }

    void OnAnalyzersBindingContext(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null)
            return;
        foreach (var analyzer in e.NewItems.OfType<BindableObject>())
            SetInheritedBindingContext(analyzer, this.BindingContext);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        foreach (var analyzer in this.Analyzers.OfType<BindableObject>())
            SetInheritedBindingContext(analyzer, this.BindingContext);
    }

    /// <summary>Raised on the UI thread when a still photo has been captured.</summary>
    public event EventHandler<CameraPhoto>? MediaCaptured;

    /// <summary>Raised on the UI thread when a video recording finishes.</summary>
    public event EventHandler<CameraVideo>? VideoCaptured;

    /// <summary>Raised on the UI thread when the camera or pipeline reports an error.</summary>
    public event EventHandler<CameraErrorEventArgs>? CameraError;

    /// <summary>
    /// Raised on the UI thread whenever the aggregated set of overlay boxes (across all analyzers) changes.
    /// This is a presentation-only channel for drawing; subscribe to each analyzer's own typed event for
    /// semantic results (e.g. the decoded barcode value).
    /// </summary>
    public event EventHandler<CameraOverlaysChangedEventArgs>? OverlaysChanged;

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


    /// <summary>
    /// Capture a single still photo and then stop the capture session, atomically. Raises
    /// <see cref="MediaCaptured"/> for the photo (via <see cref="CapturePhotoAsync"/>) and returns it. Handy from
    /// an analyzer's <c>OnDetected</c> handler for a "scan then freeze" flow — capture the still, then
    /// <c>return false</c> to disarm.
    /// </summary>
    public async Task<CameraPhoto> CaptureAndStopAsync(CancellationToken ct = default)
    {
        var photo = await this.CapturePhotoAsync(ct).ConfigureAwait(false);
        await this.StopAsync(ct).ConfigureAwait(false);
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


    /// <summary>Invoked by the handler/pipeline to publish the latest overlay boxes. Raises <see cref="OverlaysChanged"/>.</summary>
    public void OnOverlaysChanged(IReadOnlyList<OverlayBox> overlays, int imageWidth, int imageHeight)
    {
        this.Overlays = overlays;
        this.OverlaysChanged?.Invoke(this, new CameraOverlaysChangedEventArgs(overlays, imageWidth, imageHeight));
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


/// <summary>Carries the latest aggregated overlay boxes to <see cref="CameraView.OverlaysChanged"/> subscribers.</summary>
public class CameraOverlaysChangedEventArgs(IReadOnlyList<OverlayBox> overlays, int imageWidth, int imageHeight) : EventArgs
{
    /// <summary>The aggregated boxes across all analyzers, in normalized upright image space.</summary>
    public IReadOnlyList<OverlayBox> Overlays { get; } = overlays;

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
