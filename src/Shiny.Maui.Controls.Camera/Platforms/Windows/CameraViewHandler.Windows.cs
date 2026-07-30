using Microsoft.Maui.Handlers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WImagingSource = Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource;

namespace Shiny.Maui.Controls.Camera;

// Windows (WinUI 3). CaptureElement does not exist in WinUI 3, so preview and analysis are both driven
// from one MediaFrameReader: each frame is shown via a SoftwareBitmapSource and (when analyzers are
// present) wrapped into a WindowsCameraFrame for the pipeline.
public partial class CameraViewHandler : ViewHandler<CameraView, WGrid>, ICameraViewController
{
    MediaCapture? capture;
    MediaFrameReader? reader;
    WImage? previewImage;
    WImagingSource? previewSource;
    SoftwareBitmap? latest;
    LowLagMediaRecording? recording;
    bool starting;
    readonly object latestGate = new();

    protected override WGrid CreatePlatformView()
    {
        this.previewSource = new WImagingSource();
        this.previewImage = new WImage
        {
            Source = this.previewSource,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
        };
        var grid = new WGrid();
        grid.Children.Add(this.previewImage);
        return grid;
    }

    protected override void ConnectHandler(WGrid platformView)
    {
        base.ConnectHandler(platformView);
        this.InitPipeline();
        if (this.VirtualView.IsActive)
            _ = this.StartAsync();
    }

    protected override void DisconnectHandler(WGrid platformView)
    {
        this.TeardownPipeline();
        _ = this.StopAsync();
        base.DisconnectHandler(platformView);
    }


    public async Task<bool> RequestPermissionAsync(CancellationToken ct = default)
    {
        var status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.Camera>());
        return status == PermissionStatus.Granted;
    }


    public async Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default)
    {
        var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
            Windows.Devices.Enumeration.DeviceClass.VideoCapture);

        var list = new List<CameraInfo>();
        foreach (var d in devices)
        {
            var facing = d.EnclosureLocation?.Panel switch
            {
                Windows.Devices.Enumeration.Panel.Front => CameraFacing.Front,
                Windows.Devices.Enumeration.Panel.Back => CameraFacing.Back,
                _ => CameraFacing.External
            };
            list.Add(new CameraInfo(d.Id, d.Name, facing, d.IsDefault));
        }
        return list;
    }


    public async Task StartAsync(CancellationToken ct = default)
    {
        if (this.capture != null || this.starting)
            return;

        this.starting = true;
        // Built locally and only published to the field once it is fully initialized. MediaCapture throws
        // 0xC00D36B6 ("needs to be initialized") from VideoDeviceController until InitializeAsync completes,
        // and the property mappers below run synchronously while this is still in flight - publishing early
        // means MapTorch/MapZoom can hit that window. On Windows a throw there escapes Shell's page-creation
        // path and wedges Shell navigation for the rest of the session.
        MediaCapture? pending = null;
        try
        {
            pending = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };
            if (!string.IsNullOrEmpty(this.VirtualView.CameraId))
                settings.VideoDeviceId = this.VirtualView.CameraId;
            await pending.InitializeAsync(settings);

            var source = pending.FrameSources.Values
                .FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color);
            if (source == null)
            {
                pending.Dispose();
                this.MaybeVirtualView?.OnCameraError("No color camera source found");
                return;
            }

            this.reader = await pending.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8);
            this.reader.FrameArrived += this.OnFrameArrived;
            await this.reader.StartAsync();

            this.capture = pending;
            pending = null;

            // the mappers that need a device controller were no-ops while we were starting up; apply them now
            MapTorch(this, this.VirtualView);
            MapZoom(this, this.VirtualView);
        }
        catch (Exception ex)
        {
            pending?.Dispose();
            this.MaybeVirtualView?.OnCameraError("Failed to start camera", ex);
        }
        finally
        {
            this.starting = false;
        }
    }


    public async Task StopAsync(CancellationToken ct = default)
    {
        try
        {
            if (this.reader != null)
            {
                this.reader.FrameArrived -= this.OnFrameArrived;
                await this.reader.StopAsync();
                this.reader.Dispose();
                this.reader = null;
            }
            this.capture?.Dispose();
            this.capture = null;
            lock (this.latestGate)
            {
                this.latest?.Dispose();
                this.latest = null;
            }
        }
        catch { /* tearing down */ }
    }


    void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        using var frame = sender.TryAcquireLatestFrame();
        var bitmap = frame?.VideoMediaFrame?.SoftwareBitmap;
        if (bitmap == null)
            return;

        // pipeline copy (synchronous), then keep a copy for preview + still capture
        if (this.Pipeline.HasAnalyzer)
            this.Pipeline.Process(new WindowsCameraFrame(bitmap, mirrored: false), default);

        var display = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        lock (this.latestGate)
        {
            this.latest?.Dispose();
            this.latest = SoftwareBitmap.Copy(display);
        }

        this.previewImage?.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                if (this.previewSource != null)
                    await this.previewSource.SetBitmapAsync(display);
            }
            catch { /* frame raced with teardown */ }
        });
    }


    public async Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default)
    {
        SoftwareBitmap? snapshot;
        lock (this.latestGate)
            snapshot = this.latest == null ? null : SoftwareBitmap.Copy(this.latest);

        if (snapshot == null)
            throw new InvalidOperationException("Camera is not running");

        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
        encoder.SetSoftwareBitmap(snapshot);
        await encoder.FlushAsync();

        var bytes = new byte[stream.Size];
        using (var reader = new DataReader(stream.GetInputStreamAt(0)))
        {
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
        }
        var result = new CameraPhoto(bytes, snapshot.PixelWidth, snapshot.PixelHeight);
        snapshot.Dispose();
        return result;
    }


    public async Task StartVideoRecordingAsync(VideoRecordingOptions options, CancellationToken ct = default)
    {
        if (this.capture == null)
            throw new InvalidOperationException("Camera is not running");

        // Burn-in overlays aren't wired on Windows yet: LowLagMediaRecording records straight from the capture
        // device and never sees our composited MediaFrameReader frames, so the overlay would not reach the file.
        // The plan's owned-encode path (IBasicVideoEffect or a MediaStreamSource + Win2D encode) is gated on a
        // Windows-host spike (see the risk register); until then we fail fast rather than silently drop the
        // overlay. The raw-feed recording path (Overlay == null) is fully supported.
        // TODO: implement Windows burn-in recording (Win2D compositing over MediaFrameReader frames).
        if (options.Overlay != null)
            throw new PlatformNotSupportedException(
                "Burn-in video overlays are not yet supported on Windows. Record without VideoRecordingOptions.Overlay, " +
                "or use the on-preview CameraOverlayView.");

        var path = options.FilePath ?? Path.Combine(Path.GetTempPath(), $"shiny-{Guid.NewGuid():N}.mp4");
        var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
        var storageFile = await folder.CreateFileAsync(Path.GetFileName(path), Windows.Storage.CreationCollisionOption.ReplaceExisting);

        var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
        this.recording = await this.capture.PrepareLowLagRecordToStorageFileAsync(profile, storageFile);
        await this.recording.StartAsync();
        this.recordingPath = storageFile.Path;
    }

    string? recordingPath;

    public async Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default)
    {
        if (this.recording == null)
            throw new InvalidOperationException("Not recording");

        await this.recording.StopAsync();
        await this.recording.FinishAsync();
        this.recording = null;
        return new CameraVideo(this.recordingPath ?? string.Empty);
    }


    static partial void MapFacing(CameraViewHandler handler, CameraView view) => _ = handler.RestartAsync();

    async Task RestartAsync()
    {
        await this.StopAsync();
        await this.StartAsync();
    }

    static partial void MapIsActive(CameraViewHandler handler, CameraView view)
    {
        if (view.IsActive)
            _ = handler.StartAsync();
        else
            _ = handler.StopAsync();
    }

    // The device controller is only reachable on a started camera, and it throws rather than returning null once
    // the device goes away. A mapper that throws escapes Shell's page-creation path and leaves Shell navigation
    // dead for the session, so swallow it and let the camera stay at its current setting.
    static Windows.Media.Devices.VideoDeviceController? DeviceController(CameraViewHandler handler)
    {
        if (handler.capture == null)
            return null;
        try
        {
            return handler.capture.VideoDeviceController;
        }
        catch
        {
            return null;
        }
    }

    static partial void MapTorch(CameraViewHandler handler, CameraView view)
    {
        try
        {
            if (DeviceController(handler)?.TorchControl is { Supported: true } torch)
                torch.Enabled = view.IsTorchOn;
        }
        catch { /* device dropped or does not support torch */ }
    }

    static partial void MapFlashMode(CameraViewHandler handler, CameraView view) { }

    static partial void MapZoom(CameraViewHandler handler, CameraView view)
    {
        try
        {
            if (DeviceController(handler)?.ZoomControl is { Supported: true } zoom)
            {
                var clamped = Math.Clamp((float)view.Zoom, zoom.Min, zoom.Max);
                zoom.Value = clamped;
                handler.MaybeVirtualView?.OnZoomRangeChanged(zoom.Min, zoom.Max);
            }
        }
        catch { /* device dropped or does not support zoom */ }
    }

    static partial void MapScaleMode(CameraViewHandler handler, CameraView view)
    {
        if (handler.previewImage != null)
            handler.previewImage.Stretch = view.ScaleMode == PreviewScaleMode.AspectFit
                ? Microsoft.UI.Xaml.Media.Stretch.Uniform
                : Microsoft.UI.Xaml.Media.Stretch.UniformToFill;
    }

    static partial void MapOverlay(CameraViewHandler handler, CameraView view) { /* drawn by managed overlay */ }

    static partial void MapFilter(CameraViewHandler handler, CameraView view) { /* best-effort: no live filter on Windows */ }
}
