using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Shiny.Maui.Controls.Camera;

// iOS + MacCatalyst (UIKit / AVFoundation). Shared via the csproj Platforms/Apple include.
public partial class CameraViewHandler : ViewHandler<CameraView, CameraPreviewView>, ICameraViewController
{
    AVCaptureSession? session;
    AVCaptureDeviceInput? videoInput;
    AVCaptureDeviceInput? audioInput;
    AVCapturePhotoOutput? photoOutput;
    AVCaptureMovieFileOutput? movieOutput;
    AVCaptureVideoDataOutput? dataOutput;
    AVCaptureDevice? device;
    VideoFrameDelegate? frameDelegate;
    MovieRecordingDelegate? recordingDelegate;
    UIImageView? filterView;
    readonly DispatchQueue sessionQueue = new("shiny.camera.session");
    readonly DispatchQueue videoQueue = new("shiny.camera.video");

    protected override CameraPreviewView CreatePlatformView() => new();

    protected override void ConnectHandler(CameraPreviewView platformView)
    {
        base.ConnectHandler(platformView);
        this.InitPipeline();
        if (this.VirtualView.IsActive)
            _ = this.StartAsync();
    }

    protected override void DisconnectHandler(CameraPreviewView platformView)
    {
        this.TeardownPipeline();
        this.TeardownSession();
        base.DisconnectHandler(platformView);
    }


    public Task<bool> RequestPermissionAsync(CancellationToken ct = default)
    {
        var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
        if (status == AVAuthorizationStatus.Authorized)
            return Task.FromResult(true);

        return AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
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
        settings.FlashMode = this.VirtualView.FlashMode switch
        {
            CameraFlashMode.On => AVCaptureFlashMode.On,
            CameraFlashMode.Auto => AVCaptureFlashMode.Auto,
            _ => AVCaptureFlashMode.Off
        };

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


    // ---- property mappers ----

    static partial void MapFacing(CameraViewHandler handler, CameraView view)
        => handler.sessionQueue.DispatchAsync(handler.ReconfigureInput);

    static partial void MapIsActive(CameraViewHandler handler, CameraView view)
    {
        if (view.IsActive)
            _ = handler.StartAsync();
        else
            _ = handler.StopAsync();
    }

    static partial void MapTorch(CameraViewHandler handler, CameraView view)
        => handler.ApplyTorch(view.IsTorchOn);

    static partial void MapFlashMode(CameraViewHandler handler, CameraView view) { /* applied at capture time */ }

    static partial void MapZoom(CameraViewHandler handler, CameraView view)
        => handler.ApplyZoom(view.Zoom);

    static partial void MapScaleMode(CameraViewHandler handler, CameraView view)
    {
        if (handler.PlatformView is { } pv)
            pv.PreviewLayer.VideoGravity = view.ScaleMode == PreviewScaleMode.AspectFit
                ? AVLayerVideoGravity.ResizeAspect
                : AVLayerVideoGravity.ResizeAspectFill;
    }

    static partial void MapOverlay(CameraViewHandler handler, CameraView view) { /* Phase 2 */ }

    static partial void MapFilter(CameraViewHandler handler, CameraView view)
        => handler.MainThread(() => handler.ApplyFilter(view.Filter));


    // ---- internals ----

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

        this.dataOutput = new AVCaptureVideoDataOutput
        {
            AlwaysDiscardsLateVideoFrames = true,
            WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary
        };
        if (this.session.CanAddOutput(this.dataOutput))
            this.session.AddOutput(this.dataOutput);

        this.movieOutput = new AVCaptureMovieFileOutput();
        if (this.session.CanAddOutput(this.movieOutput))
            this.session.AddOutput(this.movieOutput);

        this.session.CommitConfiguration();
        this.ConfigureFocus();

        this.MainThread(() =>
        {
            try
            {
                if (this.PlatformView is not { } pv || this.session is not { } s)
                    return;

                pv.PreviewLayer.Session = s;
                this.SetupFilterView();
                this.dataOutput?.SetSampleBufferDelegate(this.frameDelegate, this.videoQueue);
                this.OrientConnections();
                this.ReportZoomRange();
                this.ApplyZoom(this.VirtualView.Zoom);
                this.ApplyTorch(this.VirtualView.IsTorchOn);
                this.ApplyFilter(this.VirtualView.Filter);
            }
            catch (Exception ex)
            {
                this.VirtualView?.OnCameraError("Camera preview setup failed", ex);
            }
        });
    }


    // Orient the frame-delivery connection so buffers arrive upright (portrait) and front-mirrored to
    // match the preview. The wrapped AppleCameraFrame then needs no further rotation/mirroring.
    void OrientConnections()
    {
        var front = this.VirtualView.Facing == CameraFacing.Front;
        var conn = this.dataOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!);
        if (conn == null)
            return;

        if (conn.SupportsVideoOrientation)
            conn.VideoOrientation = AVCaptureVideoOrientation.Portrait;

        if (conn.SupportsVideoMirroring)
        {
            conn.AutomaticallyAdjustsVideoMirroring = false;
            conn.VideoMirrored = front;
        }

        if (this.frameDelegate != null)
            this.frameDelegate.Mirrored = false; // connection already applies orientation + mirroring
    }


    void SetupFilterView()
    {
        if (this.filterView != null)
            return;

        this.filterView = new UIImageView
        {
            ContentMode = UIViewContentMode.ScaleAspectFill,
            Hidden = true,
            ClipsToBounds = true,
            // pin to the preview with constraints — an autoresizing mask off a zero starting frame (the bounds
            // are often 0 when the session configures, before layout) leaves the view 0×0, so a filtered frame
            // renders into nothing and the preview looks black
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        this.PlatformView.AddSubview(this.filterView);
        NSLayoutConstraint.ActivateConstraints(
        [
            this.filterView.LeadingAnchor.ConstraintEqualTo(this.PlatformView.LeadingAnchor),
            this.filterView.TrailingAnchor.ConstraintEqualTo(this.PlatformView.TrailingAnchor),
            this.filterView.TopAnchor.ConstraintEqualTo(this.PlatformView.TopAnchor),
            this.filterView.BottomAnchor.ConstraintEqualTo(this.PlatformView.BottomAnchor),
        ]);
        this.frameDelegate = new VideoFrameDelegate(this.filterView)
        {
            WantFrames = () => this.Pipeline.HasAnalyzer,
            OnFrame = frame => this.Pipeline.Process(frame, default),
            OnError = this.OnFrameError,
            Mirrored = this.VirtualView.Facing == CameraFacing.Front
        };
    }

    int frameErrorReported;

    void OnFrameError(Exception ex)
    {
        // surface only the first frame-processing failure, on the UI thread
        if (Interlocked.Exchange(ref this.frameErrorReported, 1) == 0)
            this.MainThread(() => this.VirtualView?.OnCameraError("Frame processing failed", ex));
    }


    void ApplyFilter(CameraFilter filter)
    {
        if (this.frameDelegate == null || this.filterView == null)
            return;

        var ci = AppleCameraFilters.Create(filter);
        this.frameDelegate.Filter = ci;

        // Show the filtered-frame overlay on top of the live preview while a filter is active. We must NOT
        // hide the preview layer to reveal it: PreviewLayer is the view's *backing* layer and the overlay is
        // a subview (a sublayer of it), so hiding the preview layer also hides the overlay — which blanked the
        // whole preview. The overlay's frames are opaque and fill the bounds, so they cover the live preview.
        this.filterView.Hidden = ci == null;
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


    static readonly AVCaptureDeviceType[] DeviceTypes =
    [
        AVCaptureDeviceType.BuiltInWideAngleCamera,
        AVCaptureDeviceType.BuiltInUltraWideCamera,
        AVCaptureDeviceType.BuiltInTelephotoCamera
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
        this.ConfigureFocus();

        this.MainThread(() =>
        {
            this.OrientConnections();
            this.ReportZoomRange();
            this.ApplyZoom(this.VirtualView.Zoom);
            this.ApplyTorch(this.VirtualView.IsTorchOn);
        });
    }


    static AVCaptureDevice? DiscoverDevice(AVCaptureDevicePosition position)
    {
        var discovery = AVCaptureDeviceDiscoverySession.Create(DeviceTypes, AVMediaTypes.Video, position);
        return discovery.Devices.FirstOrDefault();
    }


    // Put the device into continuous autofocus and clear any near-limit restriction so the lens can
    // re-focus as the subject distance changes. Without this the device can sit in a one-shot focus
    // mode (or keep a "Far" range restriction), which is exactly why moving in close — e.g. to scan a
    // document — leaves the preview blurry and never recovers.
    void ConfigureFocus()
    {
        if (this.device == null)
            return;
        try
        {
            if (!this.device.LockForConfiguration(out var err) || err != null)
                return;

            if (this.device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                this.device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;

            // allow the lens to travel all the way to its near limit (macro/close range)
            if (this.device.AutoFocusRangeRestrictionSupported)
                this.device.AutoFocusRangeRestriction = AVCaptureAutoFocusRangeRestriction.None;

            // drive focus from the centre of the frame, where the subject the user is moving toward sits
            if (this.device.FocusPointOfInterestSupported)
                this.device.FocusPointOfInterest = new CGPoint(0.5, 0.5);

            // smoother lens travel for video/preview rather than abrupt hunting
            if (this.device.SmoothAutoFocusSupported)
                this.device.SmoothAutoFocusEnabled = true;

            // keep exposure tracking too so a close subject is correctly exposed as well as focused
            if (this.device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                this.device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;

            this.device.UnlockForConfiguration();
        }
        catch (Exception ex)
        {
            this.VirtualView?.OnCameraError("Focus configuration failed", ex);
        }
    }


    void ApplyZoom(double zoom)
    {
        if (this.device == null)
            return;
        try
        {
            if (this.device.LockForConfiguration(out _))
            {
                var max = (double)this.device.ActiveFormat.VideoMaxZoomFactor;
                var min = (double)this.device.MinAvailableVideoZoomFactor;
                this.device.VideoZoomFactor = (System.Runtime.InteropServices.NFloat)Math.Clamp(zoom, min, max);
                this.device.UnlockForConfiguration();
            }
        }
        catch (Exception ex)
        {
            this.VirtualView?.OnCameraError("Zoom failed", ex);
        }
    }


    void ApplyTorch(bool on)
    {
        if (this.device is not { HasTorch: true })
            return;
        try
        {
            if (this.device.LockForConfiguration(out _))
            {
                if (this.device.TorchAvailable)
                    this.device.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
                this.device.UnlockForConfiguration();
            }
        }
        catch (Exception ex)
        {
            this.VirtualView?.OnCameraError("Torch failed", ex);
        }
    }


    void ReportZoomRange()
    {
        if (this.device == null)
            return;
        var min = (double)this.device.MinAvailableVideoZoomFactor;
        var max = (double)this.device.ActiveFormat.VideoMaxZoomFactor;
        this.VirtualView?.OnZoomRangeChanged(min, Math.Min(max, 10d));
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
            this.photoOutput?.Dispose();
            this.photoOutput = null;
            this.movieOutput?.Dispose();
            this.movieOutput = null;
            this.dataOutput?.Dispose();
            this.dataOutput = null;
            this.device = null;
        });
    }


    void MainThread(Action action) => UIApplication.SharedApplication.InvokeOnMainThread(action);
}
