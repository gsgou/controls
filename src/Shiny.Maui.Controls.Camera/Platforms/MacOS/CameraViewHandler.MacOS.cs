using AppKit;
using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;

namespace Shiny.Maui.Controls.Camera;

// macOS AppKit (NSView / AVFoundation). Best-effort: AVFoundation bindings are solid, but the MAUI
// macOS host is preview-quality, so layout/permission edge cases may need on-device tuning.
public partial class CameraViewHandler : ViewHandler<CameraView, NSView>, ICameraViewController
{
    AVCaptureSession? session;
    AVCaptureDeviceInput? videoInput;
    AVCaptureDeviceInput? audioInput;
    AVCapturePhotoOutput? photoOutput;
    AVCaptureMovieFileOutput? movieOutput;
    AVCaptureVideoDataOutput? dataOutput;
    AVCaptureVideoPreviewLayer? previewLayer;
    AVCaptureDevice? device;
    MacVideoFrameDelegate? frameDelegate;
    MovieRecordingDelegate? recordingDelegate;
    NSImageView? filterView;
    readonly DispatchQueue sessionQueue = new("shiny.camera.session");
    readonly DispatchQueue videoQueue = new("shiny.camera.video");

    protected override NSView CreatePlatformView()
    {
        var view = new NSView { WantsLayer = true };
        view.Layer ??= new CALayer();
        return view;
    }

    protected override void ConnectHandler(NSView platformView)
    {
        base.ConnectHandler(platformView);
        this.InitPipeline();
        if (this.VirtualView.IsActive)
            _ = this.StartAsync();
    }

    protected override void DisconnectHandler(NSView platformView)
    {
        this.TeardownPipeline();
        this.TeardownSession();
        base.DisconnectHandler(platformView);
    }


    public Task<bool> RequestPermissionAsync(CancellationToken ct = default)
    {
        var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
        return status == AVAuthorizationStatus.Authorized
            ? Task.FromResult(true)
            : AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
    }


    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!await this.RequestPermissionAsync(ct).ConfigureAwait(false))
        {
            this.VirtualView?.OnCameraError("Camera permission denied");
            return;
        }

        this.sessionQueue.DispatchAsync(() =>
        {
            try
            {
                this.ConfigureSession();
                if (this.session is { Running: false })
                    this.session.StartRunning();
            }
            catch (Exception ex)
            {
                this.MainThread(() => this.VirtualView?.OnCameraError("Failed to start camera", ex));
            }
        });
    }


    public Task StopAsync(CancellationToken ct = default)
    {
        this.sessionQueue.DispatchAsync(() =>
        {
            if (this.session is { Running: true })
                this.session.StopRunning();
        });
        return Task.CompletedTask;
    }


    public Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default)
    {
        if (this.photoOutput == null)
            throw new InvalidOperationException("Camera is not running");

        var settings = AVCapturePhotoSettings.Create();
        var del = new PhotoCaptureDelegate
        {
            // apply the same filter as the live preview so the captured still matches what the user sees
            Filter = AppleCameraFilters.Create(this.VirtualView.Filter)
        };
        this.photoOutput.CapturePhoto(settings, del);
        return del.Task;
    }


    public Task StartVideoRecordingAsync(VideoRecordingOptions options, CancellationToken ct = default)
    {
        if (this.movieOutput == null)
            throw new InvalidOperationException("Camera is not running");

        if (options.IncludeAudio)
            this.EnsureAudioInput();

        var path = options.FilePath ?? Path.Combine(Path.GetTempPath(), $"shiny-{Guid.NewGuid():N}.mov");
        this.recordingDelegate = new MovieRecordingDelegate();
        this.movieOutput.StartRecordingToOutputFile(NSUrl.FromFilename(path), this.recordingDelegate);
        return Task.CompletedTask;
    }


    public Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default)
    {
        if (this.movieOutput is not { Recording: true } || this.recordingDelegate == null)
            throw new InvalidOperationException("Not recording");

        this.movieOutput.StopRecording();
        return this.recordingDelegate.Task;
    }


    static partial void MapFacing(CameraViewHandler handler, CameraView view)
        => handler.sessionQueue.DispatchAsync(handler.ReconfigureInput);

    static partial void MapIsActive(CameraViewHandler handler, CameraView view)
    {
        if (view.IsActive)
            _ = handler.StartAsync();
        else
            _ = handler.StopAsync();
    }

    static partial void MapTorch(CameraViewHandler handler, CameraView view) { /* macOS cameras lack torch */ }
    static partial void MapFlashMode(CameraViewHandler handler, CameraView view) { }

    static partial void MapZoom(CameraViewHandler handler, CameraView view) { /* macOS cameras lack zoom */ }

    static partial void MapScaleMode(CameraViewHandler handler, CameraView view)
    {
        if (handler.previewLayer != null)
            handler.previewLayer.VideoGravity = view.ScaleMode == PreviewScaleMode.AspectFit
                ? AVLayerVideoGravity.ResizeAspect
                : AVLayerVideoGravity.ResizeAspectFill;
    }

    static partial void MapOverlay(CameraViewHandler handler, CameraView view) { /* drawn by managed overlay */ }

    static partial void MapFilter(CameraViewHandler handler, CameraView view)
        => handler.MainThread(() => handler.ApplyFilter(view.Filter));


    void ConfigureSession()
    {
        if (this.session != null)
            return;

        this.session = new AVCaptureSession { SessionPreset = AVCaptureSession.PresetHigh };
        this.session.BeginConfiguration();
        this.AddVideoInput();

        this.photoOutput = new AVCapturePhotoOutput();
        if (this.session.CanAddOutput(this.photoOutput))
            this.session.AddOutput(this.photoOutput);

        this.dataOutput = new AVCaptureVideoDataOutput { AlwaysDiscardsLateVideoFrames = true };
        if (this.session.CanAddOutput(this.dataOutput))
            this.session.AddOutput(this.dataOutput);

        this.movieOutput = new AVCaptureMovieFileOutput();
        if (this.session.CanAddOutput(this.movieOutput))
            this.session.AddOutput(this.movieOutput);

        this.session.CommitConfiguration();

        this.MainThread(() =>
        {
            this.SetupLayers();
            this.dataOutput.SetSampleBufferDelegate(this.frameDelegate, this.videoQueue);
            this.ApplyFilter(this.VirtualView.Filter);
        });
    }


    void SetupLayers()
    {
        var host = this.PlatformView;
        host.AutoresizesSubviews = true;

        this.previewLayer = new AVCaptureVideoPreviewLayer(this.session!)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill,
            Frame = host.Bounds,
            AutoresizingMask = CAAutoresizingMask.WidthSizable | CAAutoresizingMask.HeightSizable
        };
        host.Layer!.AddSublayer(this.previewLayer);

        this.filterView = new NSImageView
        {
            Frame = host.Bounds,
            AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
            ImageScaling = NSImageScale.AxesIndependently,
            Hidden = true
        };
        host.AddSubview(this.filterView);

        this.frameDelegate = new MacVideoFrameDelegate(this.filterView)
        {
            WantFrames = () => this.Pipeline.HasAnalyzers,
            OnFrame = frame => this.Pipeline.Process(frame, default),
            Mirrored = this.VirtualView.Facing == CameraFacing.Front
        };
    }


    void ApplyFilter(CameraFilter filter)
    {
        if (this.frameDelegate == null || this.filterView == null || this.previewLayer == null)
            return;

        var ci = AppleCameraFilters.Create(filter);
        this.frameDelegate.Filter = ci;
        var active = ci != null;
        this.filterView.Hidden = !active;
        this.previewLayer.Hidden = active;
    }


    static readonly AVCaptureDeviceType[] DeviceTypes =
    [
        AVCaptureDeviceType.BuiltInWideAngleCamera,
        AVCaptureDeviceType.ExternalUnknown
    ];

    public Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default)
    {
        var discovery = AVCaptureDeviceDiscoverySession.Create(DeviceTypes, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
        IReadOnlyList<CameraInfo> list = discovery.Devices
            .Select(d => new CameraInfo(d.UniqueID, d.LocalizedName, ToFacing(d.Position)))
            .ToList();
        return Task.FromResult(list);
    }

    static CameraFacing ToFacing(AVCaptureDevicePosition position) => position switch
    {
        AVCaptureDevicePosition.Front => CameraFacing.Front,
        AVCaptureDevicePosition.Back => CameraFacing.Back,
        _ => CameraFacing.External
    };

    AVCaptureDevice? SelectDevice()
    {
        var id = this.VirtualView.CameraId;
        if (!string.IsNullOrEmpty(id) && AVCaptureDevice.DeviceWithUniqueID(id) is { } byId)
            return byId;

        var position = this.VirtualView.Facing == CameraFacing.Front
            ? AVCaptureDevicePosition.Front
            : AVCaptureDevicePosition.Back;
        return DiscoverDevice(position) ?? DiscoverDevice(AVCaptureDevicePosition.Unspecified);
    }

    void AddVideoInput()
    {
        this.device = this.SelectDevice();
        if (this.device == null)
        {
            this.MainThread(() => this.VirtualView?.OnCameraError("No camera device found"));
            return;
        }

        var input = AVCaptureDeviceInput.FromDevice(this.device, out var error);
        if (error != null || input == null)
        {
            this.MainThread(() => this.VirtualView?.OnCameraError("Cannot open camera: " + error?.LocalizedDescription));
            return;
        }

        if (this.session!.CanAddInput(input))
            this.session.AddInput(input);
        this.videoInput = input;
    }


    void ReconfigureInput()
    {
        if (this.session == null)
            return;

        this.session.BeginConfiguration();
        if (this.videoInput != null)
        {
            this.session.RemoveInput(this.videoInput);
            this.videoInput.Dispose();
            this.videoInput = null;
        }
        this.AddVideoInput();
        this.session.CommitConfiguration();

        this.MainThread(() =>
        {
            if (this.frameDelegate != null)
                this.frameDelegate.Mirrored = this.VirtualView.Facing == CameraFacing.Front;
        });
    }


    void EnsureAudioInput()
    {
        if (this.audioInput != null || this.session == null)
            return;

        var mic = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
        if (mic == null)
            return;

        var input = AVCaptureDeviceInput.FromDevice(mic, out var err);
        if (err != null || input == null)
            return;

        this.session.BeginConfiguration();
        if (this.session.CanAddInput(input))
        {
            this.session.AddInput(input);
            this.audioInput = input;
        }
        this.session.CommitConfiguration();
    }


    static AVCaptureDevice? DiscoverDevice(AVCaptureDevicePosition position)
    {
        var discovery = AVCaptureDeviceDiscoverySession.Create(DeviceTypes, AVMediaTypes.Video, position);
        return discovery.Devices.FirstOrDefault();
    }


    void TeardownSession()
    {
        this.sessionQueue.DispatchAsync(() =>
        {
            if (this.session == null)
                return;
            if (this.movieOutput is { Recording: true })
                this.movieOutput.StopRecording();
            if (this.session.Running)
                this.session.StopRunning();
            this.session.Dispose();
            this.session = null;
            this.videoInput?.Dispose();
            this.videoInput = null;
            this.audioInput?.Dispose();
            this.audioInput = null;
            this.photoOutput = null;
            this.movieOutput = null;
            this.dataOutput = null;
            this.device = null;
        });
    }


    void MainThread(Action action) => NSApplication.SharedApplication.InvokeOnMainThread(action);
}
