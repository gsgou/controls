using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera;
using Shiny.Maui.Controls.Camera.Barcode;
using Shiny.Maui.Controls.Camera.Motion;
using Shiny.Maui.Controls.Camera.Ocr;
using FaceAnalyzer = Shiny.Maui.Controls.Camera.Face.FaceAnalyzer;

namespace Sample.Features.Camera;

public partial class CameraPage : ContentPage
{
    static readonly CameraFilter[] Filters = Enum.GetValues<CameraFilter>();
    readonly CameraOverlayDrawable overlayDrawable = new();
    IReadOnlyList<CameraInfo> cameras = [];

    public CameraPage()
    {
        InitializeComponent();
        this.Camera.CameraError += (_, e) => this.ShowStatus(e.Message);
        this.Camera.VideoCaptured += (_, v) => this.ShowStatus($"Saved video: {v.FilePath}");

        foreach (var f in Filters)
            this.FilterPicker.Items.Add(f.ToString());
        this.FilterPicker.SelectedIndex = 0;

        // frame analyzers — boxes are drawn by the overlay; barcode/field values show in the status label.
        // The OCR analyzer is given a sample IDocumentAnalyzer to demo the invoice field-extraction hook.
        this.Camera.Analyzers.Add(new BarcodeAnalyzer());
        this.Camera.Analyzers.Add(new MotionAnalyzer());
        this.Camera.Analyzers.Add(new FaceAnalyzer());
        this.Camera.Analyzers.Add(new OcrAnalyzer(new SampleInvoiceAnalyzer()) { IncludeTextBlocks = false });

        this.Overlay.Drawable = this.overlayDrawable;
        this.Camera.DetectionsChanged += this.OnDetections;
    }

    void OnDetections(object? sender, DetectionsChangedEventArgs e)
    {
        this.overlayDrawable.Detections = e.Detections;
        this.overlayDrawable.ImageAspect = e.ImageHeight == 0 ? 1f : (float)e.ImageWidth / e.ImageHeight;
        this.overlayDrawable.ScaleMode = this.Camera.ScaleMode;
        this.Overlay.Invalidate();

        var barcode = e.Detections.FirstOrDefault(d => d.Type == DetectionType.Barcode);
        if (barcode?.Value is { } value)
            this.ShowStatus($"Barcode: {value}");

        var field = e.Detections.FirstOrDefault(d => d.Type == DetectionType.DocumentField);
        if (field is not null)
            this.ShowStatus($"{field.Label}: {field.Value}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await this.Camera.RequestPermissionAsync())
        {
            this.ShowStatus("Camera permission denied");
            return;
        }

        await this.Camera.StartAsync();
        await this.LoadCamerasAsync();
    }

    async Task LoadCamerasAsync()
    {
        this.cameras = await this.Camera.GetAvailableCamerasAsync();
        this.CameraPicker.Items.Clear();
        foreach (var c in this.cameras)
            this.CameraPicker.Items.Add(c.Name);
        if (this.cameras.Count > 0 && this.CameraPicker.SelectedIndex < 0)
            this.CameraPicker.SelectedIndex = 0;
    }

    void OnCameraChanged(object? sender, EventArgs e)
    {
        var i = this.CameraPicker.SelectedIndex;
        if (i >= 0 && i < this.cameras.Count)
            this.Camera.CameraId = this.cameras[i].Id;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await this.Camera.StopAsync();
    }

    void OnFlipClicked(object? sender, EventArgs e)
        => this.Camera.Facing = this.Camera.Facing == Shiny.Controls.Camera.CameraFacing.Back
            ? Shiny.Controls.Camera.CameraFacing.Front
            : Shiny.Controls.Camera.CameraFacing.Back;

    void OnTorchClicked(object? sender, EventArgs e)
        => this.Camera.IsTorchOn = !this.Camera.IsTorchOn;

    void OnFilterChanged(object? sender, EventArgs e)
    {
        var i = this.FilterPicker.SelectedIndex;
        if (i >= 0 && i < Filters.Length)
            this.Camera.Filter = Filters[i];
    }

    async void OnRecordClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!this.Camera.IsRecording)
            {
                await this.Camera.StartVideoRecordingAsync();
                this.RecordButton.Text = "Stop";
                this.ShowStatus("Recording…");
            }
            else
            {
                this.RecordButton.Text = "Record";
                var video = await this.Camera.StopVideoRecordingAsync();
                this.ShowStatus($"Saved: {Path.GetFileName(video.FilePath)}");
            }
        }
        catch (Exception ex)
        {
            this.RecordButton.Text = "Record";
            this.ShowStatus("Record failed: " + ex.Message);
        }
    }

    async void OnCaptureClicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = await this.Camera.CapturePhotoAsync();
            this.ShowStatus($"Captured {photo.Width}x{photo.Height} ({photo.Data.Length / 1024} KB)");
        }
        catch (Exception ex)
        {
            this.ShowStatus("Capture failed: " + ex.Message);
        }
    }

    void ShowStatus(string message)
    {
        this.StatusLabel.Text = message;
        this.StatusLabel.IsVisible = true;
    }
}
