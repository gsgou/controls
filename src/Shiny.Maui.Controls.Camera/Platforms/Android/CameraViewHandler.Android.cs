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
    // CameraX's ~3-use-case budget makes ImageAnalysis and VideoCapture mutually exclusive, so the bound set
    // is decided up-front from whether any analyzer is enabled. Tracks which mode is currently bound so an
    // analyzer toggle that flips it (the enabled set crossing zero) triggers a rebind.
    bool boundAnalysisMode;

    protected override AWidget.FrameLayout CreatePlatformView()
    {
        var layout = new AWidget.FrameLayout(this.Context);
        this.previewView = new PreviewView(this.Context)
        {
            LayoutParameters = new AWidget.FrameLayout.LayoutParams(
                Android.Views.ViewGroup.LayoutParams.MatchParent,
                Android.Views.ViewGroup.LayoutParams.MatchParent)
        };
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
            this.lifecycleOwner?.Destroy();
            this.analysisExecutor?.Shutdown();
        }
        catch { /* tearing down */ }
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
            this.VirtualView?.OnCameraError("Camera permission denied");
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
                this.VirtualView?.OnCameraError("Failed to start camera", ex);
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
            throw new InvalidOperationException("Camera is not running");

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
        if (this.recorder == null)
            throw new InvalidOperationException("Video recording is unavailable while frame analyzers are active (Android binds ImageAnalysis instead of VideoCapture). Clear CameraView.Analyzers to record.");

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

        // analyzers enabled while recording couldn't be bound (VideoCapture held the slot); now the recording
        // is finalizing, re-evaluate so they take effect
        _ = task.ContinueWith(
            _ => MainThread.BeginInvokeOnMainThread(this.RebindIfModeChanged),
            TaskScheduler.Default);
        return task;
    }


    // Re-bind use cases when the enabled-analyzer set has crossed the ImageAnalysis<->VideoCapture boundary.
    // Invoked via the OnAnalyzersSynced hook (analyzer added/removed or IsEnabled toggled) and after a
    // recording finalizes. A no-op while not started, mid-recording, or when the mode is unchanged.
    partial void OnAnalyzersSynced()
    {
        if (this.cameraProvider == null || this.lifecycleOwner == null)
            return; // not started yet — BindUseCases will read the current set when it runs
        if (this.activeRecording != null)
            return; // can't swap the video use case mid-recording; deferred until it finalizes
        if (this.Pipeline.HasAnalyzers == this.boundAnalysisMode)
            return; // mode unchanged (e.g. a 2nd analyzer added) — runner set already updated, no rebind

        MainThread.BeginInvokeOnMainThread(this.RebindIfModeChanged);
    }

    void RebindIfModeChanged()
    {
        // re-check after marshalling to the main thread: state may have moved on
        if (this.cameraProvider != null
            && this.activeRecording == null
            && this.Pipeline.HasAnalyzers != this.boundAnalysisMode)
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

        this.imageCapture = new ImageCapture.Builder().Build();

        // CameraX allows Preview + 2 more use cases on most hardware, so analysis and recording are
        // mutually exclusive: bind ImageAnalysis when analyzers are present, otherwise VideoCapture.
        var useCases = new List<UseCase> { preview, this.imageCapture };
        this.imageAnalysis = null;
        this.videoCapture = null;
        this.recorder = null;

        if (this.Pipeline.HasAnalyzers)
        {
            this.analysisExecutor ??= Java.Util.Concurrent.Executors.NewSingleThreadExecutor();
            this.imageAnalysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest!)
                .Build();
            this.imageAnalysis.SetAnalyzer(this.analysisExecutor!, new FrameAnalyzerBridge(this));
            useCases.Add(this.imageAnalysis);
        }
        else
        {
            this.recorder = new Recorder.Builder()
                .SetQualitySelector(QualitySelector.From(Quality.Hd!))
                .Build();
            this.videoCapture = VideoCapture.WithOutput(this.recorder);
            useCases.Add(this.videoCapture);
        }

        this.cameraProvider.UnbindAll();
        this.camera = this.cameraProvider.BindToLifecycle(this.lifecycleOwner, selector, useCases.ToArray());
        this.boundAnalysisMode = this.Pipeline.HasAnalyzers;

        this.ApplyFilter(this.VirtualView.Filter);

        this.ReportZoomRange();
        this.camera.CameraControl.SetZoomRatio((float)this.VirtualView.Zoom);
        this.camera.CameraControl.EnableTorch(this.VirtualView.IsTorchOn);
    }


    void ReportZoomRange()
    {
        var zoomState = this.camera?.CameraInfo.ZoomState?.Value as IZoomState;
        if (zoomState != null)
            this.VirtualView?.OnZoomRangeChanged(zoomState.MinZoomRatio, zoomState.MaxZoomRatio);
    }
}
