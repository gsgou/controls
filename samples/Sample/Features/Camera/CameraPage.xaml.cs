using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls;
using Shiny.Maui.Controls.Camera;
using Shiny.Maui.Controls.Camera.Barcode;
using Shiny.Maui.Controls.Camera.Documents;
using Shiny.Maui.Controls.Camera.Face;
using Shiny.Maui.Controls.Camera.Motion;

namespace Sample.Features.Camera;

public partial class CameraPage : ShinyContentPage
{
    // sample-only: shared app-session list the document analyzers feed (see DocumentSessionPage)
    readonly DocumentSessionStore session;

    // bound by the options-sheet TableView
    public CameraFilter[] Filters { get; } = Enum.GetValues<CameraFilter>();
    public ObservableCollection<CameraInfo> Cameras { get; } = [];

    CameraInfo? selectedCamera;

    /// <summary>Two-way bound to the sheet's "Lens" picker; shows the active lens and switches to a chosen one.</summary>
    public CameraInfo? SelectedCamera
    {
        get => this.selectedCamera;
        set => this.SetCamera(value, fromUser: true);
    }

    /// <summary>Opens the scanned-documents session page (from the options sheet).</summary>
    public ICommand ViewSessionCommand { get; }

    /// <summary>Right-aligned count shown on the "Scanned documents" row; updates as captures arrive.</summary>
    public string ScannedSummary => this.session.Documents.Count switch
    {
        0 => "None yet",
        1 => "1 captured",
        var n => $"{n} captured"
    };

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
        this.session = IPlatformApplication.Current!.Services.GetRequiredService<DocumentSessionStore>();

        this.ViewSessionCommand = new Command(async () =>
        {
            this.OptionsPanel.IsOpen = false;
            await Shell.Current.GoToAsync("//documentsession");
        });

        this.BarcodeCommand = new Command<BarcodeDetectedEventArgs>(e => this.ShowStatus($"{e.Format}: {e.Value}"));
        this.MotionCommand = new Command<MotionEventArgs>(e => this.ShowStatus(e.InMotion ? "Motion detected" : "Motion stopped"));
        this.FaceCommand = new Command<FacesDetectedEventArgs>(e => this.ShowStatus($"{e.Faces.Count} face(s)"));

        this.InvoiceCommand = new Command<DocumentDetectedEventArgs<Invoice>>(e =>
        {
            var d = e.Document;
            var summary = $"Invoice {d.Number ?? "?"} — total {d.Total?.ToString("0.00") ?? "?"}, {d.Lines.Count} line(s)";
            this.Capture("Invoice", summary, Detail(
                ("Number", d.Number),
                ("Date", d.Date?.ToString("yyyy-MM-dd")),
                ("Total", d.Total?.ToString("0.00")),
                ("Lines", d.Lines.Count.ToString())));
        });
        this.LicenseCommand = new Command<DocumentDetectedEventArgs<DriversLicense>>(e =>
        {
            var d = e.Document;
            var summary = $"License {d.Number} — {d.FirstName} {d.LastName}";
            this.Capture("Driver's License", summary, Detail(
                ("Number", d.Number),
                ("Name", $"{d.FirstName} {d.LastName}".Trim()),
                ("Date of birth", d.DateOfBirth?.ToString("yyyy-MM-dd")),
                ("Expiry", d.Expiry?.ToString("yyyy-MM-dd")),
                ("Address", d.Address)));
        });
        this.CreditCardCommand = new Command<DocumentDetectedEventArgs<CreditCard>>(e =>
        {
            var d = e.Document;
            var last4 = d.Number is { Length: >= 4 } n ? n[^4..] : d.Number;
            var summary = $"{d.Type} •••• {last4} exp {d.Expiry?.ToString("MM/yy") ?? "?"}";
            this.Capture("Credit Card", summary, Detail(
                ("Type", d.Type.ToString()),
                ("Number", last4 is null ? null : $"•••• {last4}"),
                ("Expiry", d.Expiry?.ToString("MM/yy")),
                ("Name", $"{d.FirstName} {d.LastName}".Trim()),
                ("Company", d.CompanyName)));
        });
        this.PassportCommand = new Command<DocumentDetectedEventArgs<Passport>>(e =>
        {
            var d = e.Document;
            var summary = $"Passport {d.Number} — {d.GivenNames} {d.Surname} ({d.Nationality})";
            this.Capture("Passport", summary, Detail(
                ("Number", d.Number),
                ("Name", $"{d.GivenNames} {d.Surname}".Trim()),
                ("Nationality", d.Nationality),
                ("Issuing country", d.IssuingCountry),
                ("Date of birth", d.DateOfBirth?.ToString("yyyy-MM-dd")),
                ("Expiry", d.Expiry?.ToString("yyyy-MM-dd")),
                ("Sex", d.Sex.ToString())));
        });

        InitializeComponent();
        this.BindingContext = this;

        // open the options sheet at full screen
        this.OptionsPanel.Detents = new ObservableCollection<DetentValue> { DetentValue.Full };

        // keep the options-sheet count in sync as documents are captured
        this.session.Documents.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.ScannedSummary));

        this.Camera.CameraError += (_, e) => this.ShowStatus(e.Message);
        this.Camera.VideoCaptured += (_, v) => this.ShowStatus($"Saved video: {v.FilePath}");
    }

    // Show the headline on-screen and add the document to the shared session list.
    void Capture(string kind, string summary, string detail)
    {
        this.ShowStatus(summary);
        this.session.Add(kind, summary, detail);
    }

    // Join the non-empty "Label: value" lines into a detail block.
    static string Detail(params (string Label, string? Value)[] fields)
        => string.Join("\n", fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => $"{f.Label}: {f.Value}"));

    void OnOptionsClicked(object? sender, EventArgs e) => this.OptionsPanel.IsOpen = true;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // The CameraView starts its own preview and requests permission itself (reporting any denial via
        // CameraError) — so we don't gate on it here. We only need the handler to be connected before asking
        // it for the lens list; on first appearance it may not be yet, so wait for it.
        this.Camera.IsActive = true;
        if (this.Camera.Handler is not null)
            _ = this.LoadCamerasAsync();
        else
            this.Camera.HandlerChanged += this.OnCameraHandlerReady;
    }

    void OnCameraHandlerReady(object? sender, EventArgs e)
    {
        if (this.Camera.Handler is null)
            return;
        this.Camera.HandlerChanged -= this.OnCameraHandlerReady;
        _ = this.LoadCamerasAsync();
    }

    async Task LoadCamerasAsync()
    {
        var available = await this.Camera.GetAvailableCamerasAsync();
        this.Cameras.Clear();
        foreach (var c in available)
            this.Cameras.Add(c);

        // show the active lens in the picker (display only — don't reassign CameraId and force a reconfigure)
        this.SetCamera(
            this.Cameras.FirstOrDefault(c => c.Facing == this.Camera.Facing) ?? this.Cameras.FirstOrDefault(),
            fromUser: false);
    }

    void SetCamera(CameraInfo? value, bool fromUser)
    {
        if (ReferenceEquals(this.selectedCamera, value))
            return;
        this.selectedCamera = value;
        this.OnPropertyChanged(nameof(this.SelectedCamera));
        if (fromUser && value is not null)
            this.Camera.CameraId = value.Id;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.Camera.IsActive = false;
    }

    void OnFlipClicked(object? sender, EventArgs e)
        => this.Camera.Facing = this.Camera.Facing == Shiny.Controls.Camera.CameraFacing.Back
            ? Shiny.Controls.Camera.CameraFacing.Front
            : Shiny.Controls.Camera.CameraFacing.Back;

    void OnTorchClicked(object? sender, EventArgs e)
        => this.Camera.IsTorchOn = !this.Camera.IsTorchOn;

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
            // show the captured still so the applied filter is visible
            var bytes = photo.Data;
            this.PhotoThumbnail.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            this.PhotoThumbnail.IsVisible = true;
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
