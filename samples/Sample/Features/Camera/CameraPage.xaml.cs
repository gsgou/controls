using System.Windows.Input;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera;
using Shiny.Maui.Controls.Camera.Barcode;
using Shiny.Maui.Controls.Camera.Documents;
using Shiny.Maui.Controls.Camera.Face;
using Shiny.Maui.Controls.Camera.Motion;

namespace Sample.Features.Camera;

public partial class CameraPage : ContentPage
{
    static readonly CameraFilter[] Filters = Enum.GetValues<CameraFilter>();
    IReadOnlyList<CameraInfo> cameras = [];

    // The analyzers are declared in XAML inside <cam:CameraView>; these commands are bound to them and fire
    // (on the UI thread) with each analyzer's typed event args.
    public ICommand BarcodeCommand { get; }
    public ICommand MotionCommand { get; }
    public ICommand FaceCommand { get; }
    public ICommand InvoiceCommand { get; }
    public ICommand LicenseCommand { get; }
    public ICommand CreditCardCommand { get; }
    public ICommand PassportCommand { get; }

    public CameraPage()
    {
        this.BarcodeCommand = new Command<BarcodeDetectedEventArgs>(e => this.ShowStatus($"{e.Format}: {e.Value}"));
        this.MotionCommand = new Command<MotionEventArgs>(e => this.ShowStatus(e.InMotion ? "Motion detected" : "Motion stopped"));
        this.FaceCommand = new Command<FacesDetectedEventArgs>(e => this.ShowStatus($"{e.Faces.Count} face(s)"));
        this.InvoiceCommand = new Command<DocumentDetectedEventArgs<Invoice>>(e =>
            this.ShowStatus($"Invoice {e.Document.Number ?? "?"} — total {e.Document.Total?.ToString("0.00") ?? "?"}, {e.Document.Lines.Count} line(s)"));
        this.LicenseCommand = new Command<DocumentDetectedEventArgs<DriversLicense>>(e =>
            this.ShowStatus($"License {e.Document.Number} — {e.Document.FirstName} {e.Document.LastName}"));
        this.CreditCardCommand = new Command<DocumentDetectedEventArgs<CreditCard>>(e =>
        {
            var last4 = e.Document.Number is { Length: >= 4 } n ? n[^4..] : e.Document.Number;
            this.ShowStatus($"{e.Document.Type} •••• {last4} exp {e.Document.Expiry?.ToString("MM/yy") ?? "?"}");
        });
        this.PassportCommand = new Command<DocumentDetectedEventArgs<Passport>>(e =>
            this.ShowStatus($"Passport {e.Document.Number} — {e.Document.GivenNames} {e.Document.Surname} ({e.Document.Nationality})"));

        InitializeComponent();
        this.BindingContext = this;

        this.Camera.CameraError += (_, e) => this.ShowStatus(e.Message);
        this.Camera.VideoCaptured += (_, v) => this.ShowStatus($"Saved video: {v.FilePath}");

        foreach (var f in Filters)
            this.FilterPicker.Items.Add(f.ToString());
        this.FilterPicker.SelectedIndex = 0;
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
