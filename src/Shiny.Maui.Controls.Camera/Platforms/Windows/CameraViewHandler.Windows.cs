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
        if (this.capture != null)
            return;
        try
        {
            this.capture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };
            if (!string.IsNullOrEmpty(this.VirtualView.CameraId))
                settings.VideoDeviceId = this.VirtualView.CameraId;
            await this.capture.InitializeAsync(settings);

            var source = this.capture.FrameSources.Values
                .FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color);
            if (source == null)
            {
                this.VirtualView?.OnCameraError("No color camera source found");
                return;
            }

            this.reader = await this.capture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8);
            this.reader.FrameArrived += this.OnFrameArrived;
            await this.reader.StartAsync();
        }
        catch (Exception ex)
        {
            this.VirtualView?.OnCameraError("Failed to start camera", ex);
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

    static partial void MapTorch(CameraViewHandler handler, CameraView view)
    {
        var controller = handler.capture?.VideoDeviceController;
        if (controller?.TorchControl is { Supported: true } torch)
            torch.Enabled = view.IsTorchOn;
    }

    static partial void MapFlashMode(CameraViewHandler handler, CameraView view) { }

    static partial void MapZoom(CameraViewHandler handler, CameraView view)
    {
        var zoom = handler.capture?.VideoDeviceController?.ZoomControl;
        if (zoom is { Supported: true })
        {
            var clamped = Math.Clamp((float)view.Zoom, zoom.Min, zoom.Max);
            zoom.Value = clamped;
            handler.VirtualView?.OnZoomRangeChanged(zoom.Min, zoom.Max);
        }
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
