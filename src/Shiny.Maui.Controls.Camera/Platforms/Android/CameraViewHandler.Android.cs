using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.Video;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Lang;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;
using AWidget = Android.Widget;
using ShinyFilter = Shiny.Controls.Camera.CameraFilter;

namespace Shiny.Maui.Controls.Camera;

// Android (CameraX). Included only for the net10.0-android TFM.
public partial class CameraViewHandler : ViewHandler<CameraView, AWidget.FrameLayout>, ICameraViewController
{
    PreviewView? previewView;
    CameraLifecycleOwner? lifecycleOwner;
    ProcessCameraProvider? cameraProvider;
    ImageCapture? imageCapture;
    VideoCapture? videoCapture;
    ImageAnalysis? imageAnalysis;
    Recorder? recorder;
    Recording? activeRecording;
    VideoRecordListener? recordListener;
    Java.Util.Concurrent.IExecutorService? analysisExecutor;
    ICamera? camera;
    // Burn-in video overlay: when set, BindUseCases attaches an OverlayEffect to the VideoCapture use case so
    // the renderer is composited into the recorded file.
    IVideoOverlayRenderer? recordingOverlay;
    AndroidX.Camera.Effects.OverlayEffect? overlayEffect;
    Android.OS.HandlerThread? overlayThread;
    // True from StartVideoRecordingAsync until the recording finalizes. CameraX guarantees Preview plus two
    // more use cases at LIMITED hardware level and a fourth only at LEVEL_3, so something has to give when an
    // analyzer and a recording are both live — and ImageCapture is it (see BindUseCases). Tracking the wish
    // separately from the binding is what lets a scanner app that never records keep photo capture.
    bool wantsVideoCapture;
    // The use-case shape currently bound. A change in either dimension (an analyzer toggling, a recording
    // starting or finishing) is what triggers a rebind.
    (bool Analyzing, bool Video) boundShape;

    protected override AWidget.FrameLayout CreatePlatformView()
    {
        var layout = new AWidget.FrameLayout(this.Context);
        this.previewView = new PreviewView(this.Context)
        {
            LayoutParameters = new AWidget.FrameLayout.LayoutParams(
                Android.Views.ViewGroup.LayoutParams.MatchParent,
                Android.Views.ViewGroup.LayoutParams.MatchParent)
        };
        // PreviewView defaults to Performance mode, which renders into a SurfaceView whose content is
        // composited on a separate surface that ignores View.SetRenderEffect — so the live colour filter
        // never appears. Compatible mode renders into a TextureView, which honours the RenderEffect colour
        // matrix we apply in ApplyFilter.
        this.previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible!);
        layout.AddView(this.previewView);
        return layout;
    }

    protected override void ConnectHandler(AWidget.FrameLayout platformView)
    {
        base.ConnectHandler(platformView);
        this.lifecycleOwner = new CameraLifecycleOwner();
        this.InitPipeline();
        if (this.VirtualView.IsActive)
            _ = this.StartAsync();
    }

    protected override void DisconnectHandler(AWidget.FrameLayout platformView)
    {
        this.TeardownPipeline();
        try
        {
            this.imageAnalysis?.ClearAnalyzer();
            this.cameraProvider?.UnbindAll();
            this.DisposeOverlayEffect();
            this.overlayThread?.QuitSafely();
            this.lifecycleOwner?.Destroy();
            this.analysisExecutor?.Shutdown();
        }
        catch { /* tearing down */ }
        this.recordingOverlay = null;
        this.wantsVideoCapture = false;
        this.boundShape = default;
        this.overlayThread = null;
        this.analysisExecutor = null;
        this.camera = null;
        this.imageCapture = null;
        this.imageAnalysis = null;
        this.lifecycleOwner = null;
        base.DisconnectHandler(platformView);
    }


    Task<ProcessCameraProvider> GetProviderAsync()
    {
        if (this.cameraProvider != null)
            return Task.FromResult(this.cameraProvider);

        var tcs = new TaskCompletionSource<ProcessCameraProvider>();
        var future = ProcessCameraProvider.GetInstance(this.Context);
        future.AddListener(new Runnable(() =>
        {
            try { tcs.TrySetResult((ProcessCameraProvider)future.Get()!); }
            catch (System.Exception ex) { tcs.TrySetException(ex); }
        }), ContextCompat.GetMainExecutor(this.Context));
        return tcs.Task;
    }


    public async Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default)
    {
        var provider = await this.GetProviderAsync().ConfigureAwait(false);
        var list = new List<CameraInfo>();
        foreach (var info in provider.AvailableCameraInfos)
        {
            var c2 = AndroidX.Camera.Camera2.InterOp.Camera2CameraInfo.From(info);
            var id = c2.CameraId;
            var lens = c2.GetCameraCharacteristic(Android.Hardware.Camera2.CameraCharacteristics.LensFacing!) as Java.Lang.Integer;
            var facing = lens?.IntValue() switch
            {
                (int)Android.Hardware.Camera2.LensFacing.Front => CameraFacing.Front,
                (int)Android.Hardware.Camera2.LensFacing.Back => CameraFacing.Back,
                _ => CameraFacing.External
            };
            list.Add(new CameraInfo(id, $"{facing} camera ({id})", facing));
        }
        return list;
    }


    public async Task<bool> RequestPermissionAsync(CancellationToken ct = default)
    {
        var status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.Camera>()
        ).ConfigureAwait(false);
        return status == PermissionStatus.Granted;
    }


    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!await this.RequestPermissionAsync(ct).ConfigureAwait(false))
        {
            this.MaybeVirtualView?.OnCameraError("Camera permission denied");
            return;
        }

        var ctx = this.Context;
        var future = ProcessCameraProvider.GetInstance(ctx);
        future.AddListener(new Runnable(() =>
        {
            try
            {
                this.cameraProvider = (ProcessCameraProvider)future.Get()!;
                this.lifecycleOwner!.Start();
                this.BindUseCases();
            }
            catch (System.Exception ex)
            {
                this.MaybeVirtualView?.OnCameraError("Failed to start camera", ex);
            }
        }), ContextCompat.GetMainExecutor(ctx));
    }


    public Task StopAsync(CancellationToken ct = default)
    {
        this.lifecycleOwner?.Stop();
        return Task.CompletedTask;
    }


    public Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default)
    {
        if (this.imageCapture == null)
            throw new InvalidOperationException(this.activeRecording != null
                ? "Photo capture is unavailable while recording with a frame analyzer active — CameraX has no use-case budget left for ImageCapture on this hardware. It returns when the recording stops."
                : "Camera is not running");

        this.imageCapture.FlashMode = this.VirtualView.FlashMode switch
        {
            CameraFlashMode.On => ImageCapture.FlashModeOn,
            CameraFlashMode.Auto => ImageCapture.FlashModeAuto,
            _ => ImageCapture.FlashModeOff
        };

        // apply the same filter as the live preview so the captured still matches what the user sees
        var cb = new ImageCapturedCallback(this.VirtualView.Filter);
        this.imageCapture.TakePicture(ContextCompat.GetMainExecutor(this.Context)!, cb);
        return cb.Task;
    }


    public async Task StartVideoRecordingAsync(VideoRecordingOptions options, CancellationToken ct = default)
    {
        // VideoCapture may not be bound yet — with an analyzer running the camera sits on
        // Preview + ImageCapture + ImageAnalysis until a recording actually asks for it. Ask for it now, then
        // rebind if either that or a burn-in overlay (which needs an OverlayEffect attached to VideoCapture)
        // means the current binding is wrong. With no analyzer and no overlay this is the old path untouched.
        var needsRebind = this.videoCapture == null || options.Overlay != null;
        this.wantsVideoCapture = true;
        this.recordingOverlay = options.Overlay;

        if (needsRebind)
            await MainThread.InvokeOnMainThreadAsync(this.BindUseCases).ConfigureAwait(false);

        if (this.recorder == null)
        {
            this.wantsVideoCapture = false;
            this.recordingOverlay = null;
            throw new InvalidOperationException("Camera is not running");
        }

        var withAudio = options.IncludeAudio;
        if (withAudio)
        {
            var mic = await MainThread.InvokeOnMainThreadAsync(
                () => Permissions.RequestAsync<Permissions.Microphone>()).ConfigureAwait(false);
            withAudio = mic == PermissionStatus.Granted;
        }

        var path = options.FilePath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"shiny-{Guid.NewGuid():N}.mp4");
        var outputOptions = new FileOutputOptions.Builder(new Java.IO.File(path)).Build();

        var pending = this.recorder.PrepareRecording(this.Context, outputOptions);
        if (withAudio)
            pending = pending.WithAudioEnabled();

        this.recordListener = new VideoRecordListener(path);
        this.activeRecording = pending.Start(ContextCompat.GetMainExecutor(this.Context)!, this.recordListener);
    }


    public Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default)
    {
        if (this.activeRecording == null || this.recordListener == null)
            throw new InvalidOperationException("Not recording");

        this.activeRecording.Stop();
        var task = this.recordListener.Task;
        this.activeRecording = null;
        var hadOverlay = this.recordingOverlay != null;
        this.recordingOverlay = null;
        // release the claim on VideoCapture so ImageCapture can come back if an analyzer is still running
        this.wantsVideoCapture = false;

        // once the recording finalizes, rebind: detach the overlay effect (if any) and restore the use-case
        // shape now that the recording no longer needs VideoCapture.
        _ = task.ContinueWith(
            _ => MainThread.BeginInvokeOnMainThread(() =>
            {
                if (hadOverlay)
                    this.BindUseCases(); // rebind without the overlay effect
                this.RebindIfModeChanged();
            }),
            TaskScheduler.Default);
        return task;
    }


    // Build the Recorder for the requested quality and bitrate.
    //
    // The fallback strategy is what stops a quality request being a compatibility cliff: capture ladders vary
    // wildly across Android hardware, and QualitySelector.From with no fallback simply produces no supported
    // quality on a device that lacks the exact rung — which surfaces as a camera that will not bind, not as a
    // lower-resolution recording. LowerQualityOrHigherThan degrades first (smaller files are the safer
    // surprise) and only goes up if there is nothing below.
    Recorder BuildRecorder()
    {
        var quality = this.VirtualView.VideoQuality switch
        {
            VideoQuality.Lowest => Quality.Lowest!,
            VideoQuality.Low => Quality.Sd!,
            VideoQuality.Medium => Quality.Hd!,
            VideoQuality.High => Quality.Fhd!,
            VideoQuality.UltraHigh => Quality.Uhd!,
            _ => Quality.Highest!
        };

        var builder = new Recorder.Builder()
            .SetQualitySelector(QualitySelector.From(quality, FallbackStrategy.LowerQualityOrHigherThan(quality)!)!);

        // CameraX rejects a non-positive target outright, so a nonsense value is dropped rather than passed on
        if (this.VirtualView.VideoBitrate is > 0 and var bitrate)
            builder.SetTargetVideoEncodingBitRate(bitrate);

        return builder.Build();
    }


    // Frame rate rides on VideoCapture rather than the Recorder. It is a *range* in CameraX, and passing the
    // requested value as both bounds is deliberate: a range would let the device drift back up to its
    // preferred rate under good light, which defeats the point when the reason for asking was thermals or
    // file size rather than motion.
    VideoCapture BuildVideoCapture(Recorder rec)
    {
        if (this.VirtualView.VideoFrameRate is not > 0)
            return VideoCapture.WithOutput(rec);

        var fps = this.VirtualView.VideoFrameRate!.Value;

        // Builder.Build() is bound as Java.Lang.Object (the generic VideoCapture<T> erases in the binding),
        // so the cast is not optional
        return (VideoCapture)new VideoCapture.Builder(rec)
            .SetTargetFrameRate(new Android.Util.Range(Java.Lang.Integer.ValueOf(fps), Java.Lang.Integer.ValueOf(fps)))
            .Build();
    }


    // Force ImageAnalysis and VideoCapture onto the same field of view.
    //
    // CameraX sizes each use case independently — ImageAnalysis lands on a 4:3 buffer by default while the
    // Recorder is 16:9 — so the two see *different crops of the sensor*. That is invisible until something
    // maps a detection from one into the other: an analyzer's normalized bounding box drawn into the recorded
    // frame (the mapping IVideoOverlayRenderer documents) would sit visibly off the thing it is boxing, and
    // vertically off by the full 4:3-vs-16:9 letterbox. A ViewPort is CameraX's mechanism for giving every use
    // case in the group one shared crop rect, which makes that mapping correct by construction.
    //
    // Applied ONLY when analysis and recording are bound together — the case that could not exist at all
    // before — so no existing single-use-case configuration has its recorded field of view moved underneath it.
    ViewPort? BuildSharedViewPort(Preview preview)
    {
        // The PreviewView's own ViewPort matches what the user is looking at, which is the most useful thing
        // to align to. It is null until the view has been laid out, so fall back to the Recorder's 16:9.
        var fromPreview = this.previewView?.ViewPort;
        if (fromPreview != null)
            return fromPreview;

        return new ViewPort.Builder(new Android.Util.Rational(16, 9), preview.TargetRotation)
            .SetScaleType(ViewPort.FillCenter)
            .Build();
    }


    AndroidX.Camera.Effects.OverlayEffect CreateOverlayEffect(IVideoOverlayRenderer overlay)
    {
        if (this.overlayThread == null)
        {
            this.overlayThread = new Android.OS.HandlerThread("shiny.camera.overlay");
            this.overlayThread.Start();
        }
        var handler = new Android.OS.Handler(this.overlayThread.Looper!);
        var effect = new AndroidX.Camera.Effects.OverlayEffect(
            AndroidX.Camera.Core.CameraEffect.VideoCapture,
            3, // queue depth: frames buffered before dropping
            handler,
            new OverlayErrorConsumer(msg => this.MaybeVirtualView?.OnCameraError("Video overlay failed: " + msg)));
        effect.SetOnDrawListener(new OverlayDrawListener(this.Context, this.VirtualView.Facing, overlay));
        this.overlayEffect = effect;
        return effect;
    }

    void DisposeOverlayEffect()
    {
        try { this.overlayEffect?.Close(); }
        catch { /* already closed / detached */ }
        this.overlayEffect?.Dispose();
        this.overlayEffect = null;
    }


    // The use-case shape BindUseCases would produce right now. Compared against boundShape to decide whether
    // anything actually needs rebinding.
    (bool Analyzing, bool Video) DesiredShape()
    {
        var analyzing = this.Pipeline.HasAnalyzer;
        return (analyzing, this.wantsVideoCapture || !analyzing);
    }

    // Re-bind use cases when the wanted shape has moved away from the bound one — an analyzer being added,
    // removed or toggled, or a recording starting/finishing. Invoked via the OnAnalyzersSynced hook and after
    // a recording finalizes. A no-op while not started, mid-recording, or when the shape is unchanged.
    partial void OnAnalyzersSynced()
    {
        if (this.cameraProvider == null || this.lifecycleOwner == null)
            return; // not started yet — BindUseCases will read the current set when it runs
        if (this.activeRecording != null)
            return; // can't swap use cases mid-recording; deferred until it finalizes
        if (this.DesiredShape() == this.boundShape)
            return; // shape unchanged (e.g. a 2nd analyzer added) — runner set already updated, no rebind

        MainThread.BeginInvokeOnMainThread(this.RebindIfModeChanged);
    }

    void RebindIfModeChanged()
    {
        // re-check after marshalling to the main thread: state may have moved on
        if (this.cameraProvider != null
            && this.activeRecording == null
            && this.DesiredShape() != this.boundShape)
            this.BindUseCases();
    }


    // ---- property mappers ----

    static partial void MapFacing(CameraViewHandler handler, CameraView view) => handler.BindUseCases();

    static partial void MapIsActive(CameraViewHandler handler, CameraView view)
    {
        if (view.IsActive)
            _ = handler.StartAsync();
        else
            _ = handler.StopAsync();
    }

    static partial void MapTorch(CameraViewHandler handler, CameraView view)
        => handler.camera?.CameraControl.EnableTorch(view.IsTorchOn);

    static partial void MapFlashMode(CameraViewHandler handler, CameraView view) { /* applied at capture time */ }

    static partial void MapZoom(CameraViewHandler handler, CameraView view)
        => handler.camera?.CameraControl.SetZoomRatio((float)view.Zoom);

    static partial void MapScaleMode(CameraViewHandler handler, CameraView view)
    {
        if (handler.previewView != null)
            handler.previewView.SetScaleType(view.ScaleMode == PreviewScaleMode.AspectFit
                ? PreviewView.ScaleType.FitCenter
                : PreviewView.ScaleType.FillCenter);
    }

    static partial void MapOverlay(CameraViewHandler handler, CameraView view) { /* Phase 2 */ }

    static partial void MapFilter(CameraViewHandler handler, CameraView view) => handler.ApplyFilter(view.Filter);

    // Quality is baked into the Recorder at bind time, so a change means a rebind. Refused mid-recording:
    // rebinding tears down the Recorder that owns the file being written, which would truncate the clip. The
    // new value lands on the next binding — the one the recording already triggers when it stops.
    static partial void MapVideoQuality(CameraViewHandler handler, CameraView view)
    {
        if (handler.cameraProvider != null && handler.activeRecording == null)
            handler.BindUseCases();
    }


    // ---- internals ----

    void ApplyFilter(ShinyFilter filter)
    {
        if (this.previewView == null || !OperatingSystem.IsAndroidVersionAtLeast(31))
            return;

        var matrix = AndroidCameraFilters.ColorMatrix(filter);
        if (matrix == null)
            this.previewView.SetRenderEffect(null);
        else
            this.previewView.SetRenderEffect(
                RenderEffect.CreateColorFilterEffect(new ColorMatrixColorFilter(matrix)));
    }

    void BindUseCases()
    {
        if (this.cameraProvider == null || this.previewView == null || this.lifecycleOwner == null)
            return;

        var preview = new Preview.Builder().Build();
        preview.SurfaceProvider = this.previewView.SurfaceProvider;

        var selectorBuilder = new CameraSelector.Builder();
        if (!string.IsNullOrEmpty(this.VirtualView.CameraId))
            selectorBuilder.AddCameraFilter(new CameraIdFilter(this.VirtualView.CameraId!));
        else
            selectorBuilder.RequireLensFacing(this.VirtualView.Facing == CameraFacing.Front
                ? CameraSelector.LensFacingFront
                : CameraSelector.LensFacingBack);
        var selector = selectorBuilder.Build();

        this.imageCapture = null;
        this.imageAnalysis = null;
        this.videoCapture = null;
        this.recorder = null;

        // Use-case budget. CameraX guarantees Preview + 2 more at LIMITED hardware level; a fourth use case
        // needs LEVEL_3, which most phones are not. Preview + VideoCapture + ImageAnalysis IS a guaranteed
        // LIMITED combination, so analysis and recording are not actually mutually exclusive — a live-analysis
        // recorder (a dash cam reading signs or plates off its own feed) is supportable. What does not fit is
        // ImageCapture on top, so that is the one dropped, and only for as long as a recording is running.
        var analyzing = this.Pipeline.HasAnalyzer;
        var video = this.wantsVideoCapture || !analyzing;
        var useCases = new List<UseCase> { preview };

        if (!(analyzing && video))
        {
            this.imageCapture = new ImageCapture.Builder().Build();
            useCases.Add(this.imageCapture);
        }

        if (analyzing)
        {
            this.analysisExecutor ??= Java.Util.Concurrent.Executors.NewSingleThreadExecutor();
            this.imageAnalysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest!)
                .Build();
            this.imageAnalysis.SetAnalyzer(this.analysisExecutor!, new FrameAnalyzerBridge(this));
            useCases.Add(this.imageAnalysis);
        }

        if (video)
        {
            this.recorder = this.BuildRecorder();
            this.videoCapture = this.BuildVideoCapture(this.recorder);
            useCases.Add(this.videoCapture);
        }

        this.cameraProvider.UnbindAll();
        this.DisposeOverlayEffect();

        // A UseCaseGroup is needed for the burn-in overlay effect, and also whenever analysis and recording
        // run together so they can share a ViewPort (see BuildSharedViewPort).
        var overlayEffect = this.recordingOverlay is { } overlay && this.videoCapture != null
            ? this.CreateOverlayEffect(overlay)
            : null;
        var sharedViewPort = analyzing && video ? this.BuildSharedViewPort(preview) : null;

        if (overlayEffect != null || sharedViewPort != null)
        {
            var groupBuilder = new UseCaseGroup.Builder();
            foreach (var uc in useCases)
                groupBuilder.AddUseCase(uc);
            if (overlayEffect != null)
                groupBuilder.AddEffect(overlayEffect);
            if (sharedViewPort != null)
                groupBuilder.SetViewPort(sharedViewPort);
            this.camera = this.cameraProvider.BindToLifecycle(this.lifecycleOwner, selector, groupBuilder.Build());
        }
        else
        {
            this.camera = this.cameraProvider.BindToLifecycle(this.lifecycleOwner, selector, useCases.ToArray());
        }
        this.boundShape = (analyzing, video);

        this.ApplyFilter(this.VirtualView.Filter);

        this.ReportZoomRange();
        this.camera.CameraControl.SetZoomRatio((float)this.VirtualView.Zoom);
        this.camera.CameraControl.EnableTorch(this.VirtualView.IsTorchOn);
    }


    void ReportZoomRange()
    {
        var zoomState = this.camera?.CameraInfo.ZoomState?.Value as IZoomState;
        if (zoomState != null)
            this.MaybeVirtualView?.OnZoomRangeChanged(zoomState.MinZoomRatio, zoomState.MaxZoomRatio);
    }
}
