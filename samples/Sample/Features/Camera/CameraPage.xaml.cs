using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;
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

    // one analyzer runs at a time; these are built once and swapped into Camera.Analyzer by the picker
    readonly BarcodeAnalyzer barcode = new();
    readonly Dictionary<string, IFrameAnalyzer> analyzers;

    // a center band the barcode analyzer scans when "Restrict scan area" is on
    static readonly RectF ScanBand = new(0.1f, 0.4f, 0.8f, 0.2f);

    // bound by the options-sheet TableView
    public CameraFilter[] Filters { get; } = Enum.GetValues<CameraFilter>();
    public ObservableCollection<CameraInfo> Cameras { get; } = [];

    /// <summary>The detector names shown in the "Detector" picker.</summary>
    public string[] Detectors { get; }

    string selectedDetector = "None";

    /// <summary>Two-way bound to the sheet's "Detector" picker; swaps <see cref="CameraView.Analyzer"/> live.</summary>
    public string SelectedDetector
    {
        get => this.selectedDetector;
        set
        {
            this.selectedDetector = value;
            this.OnPropertyChanged(nameof(this.SelectedDetector));
            this.ApplyDetector();
        }
    }

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

    // Each analyzer's OnDetected handler runs (on the UI thread) with its typed args — but only while the
    // analyzer is armed (tap "Scan" -> Camera.ScanCommand). Each returns whether to keep scanning: barcode /
    // motion / face honor the Continuous switch; documents are single-shot (return false), optionally
    // capturing + stopping the preview.
    Func<BarcodesDetectedEventArgs, Task<bool>> OnBarcode { get; }
    Func<MotionEventArgs, Task<bool>> OnMotion { get; }
    Func<FacesDetectedEventArgs, Task<bool>> OnFace { get; }
    Func<DocumentDetectedEventArgs<Invoice>, Task<bool>> OnInvoice { get; }
    Func<DocumentDetectedEventArgs<Receipt>, Task<bool>> OnReceipt { get; }
    Func<DocumentDetectedEventArgs<BusinessCard>, Task<bool>> OnBusinessCard { get; }
    Func<DocumentDetectedEventArgs<HealthCard>, Task<bool>> OnHealthCard { get; }
    Func<DocumentDetectedEventArgs<DriversLicense>, Task<bool>> OnLicense { get; }
    Func<DocumentDetectedEventArgs<CreditCard>, Task<bool>> OnCreditCard { get; }
    Func<DocumentDetectedEventArgs<Passport>, Task<bool>> OnPassport { get; }

    public CameraPage()
    {
        this.session = IPlatformApplication.Current!.Services.GetRequiredService<DocumentSessionStore>();

        this.ViewSessionCommand = new Command(async () =>
        {
            this.OptionsPanel.IsOpen = false;
            await Shell.Current.GoToAsync("//documentsession");
        });

        this.OnBarcode = e => { this.ShowStatus($"{e.Barcodes.Count} code(s): {e.First.Format} {e.First.Value}"); return Task.FromResult(this.ContinuousSwitch.On); };
        this.OnMotion = e => { this.ShowStatus(e.InMotion ? "Motion detected" : "Motion stopped"); return Task.FromResult(this.ContinuousSwitch.On); };
        this.OnFace = e => { this.ShowStatus($"{e.Faces.Count} face(s)"); return Task.FromResult(this.ContinuousSwitch.On); };

        this.OnInvoice = e =>
        {
            var d = e.Document;
            var summary = $"Invoice {d.Number ?? "?"} — total {d.Total?.ToString("0.00") ?? "?"}, {d.Lines.Count} line(s)";
            return this.OnDocument("Invoice", summary, Detail(
                ("Number", d.Number),
                ("Date", d.Date?.ToString("yyyy-MM-dd")),
                ("Total", d.Total?.ToString("0.00")),
                ("Lines", d.Lines.Count.ToString())));
        };
        this.OnReceipt = e =>
        {
            var d = e.Document;
            var summary = $"{d.Merchant ?? "Receipt"} — total {d.Total?.ToString("0.00") ?? "?"}, {d.Lines.Count} item(s)";
            return this.OnDocument("Receipt", summary, Detail(
                ("Merchant", d.Merchant),
                ("Receipt #", d.ReceiptNumber),
                ("Date", d.Date?.ToString("yyyy-MM-dd")),
                ("Time", d.Time?.ToString("HH:mm")),
                ("Items", d.Lines.Count.ToString()),
                ("Subtotal", d.Subtotal?.ToString("0.00")),
                ("Tax", d.Tax?.ToString("0.00")),
                ("Tip", d.Tip?.ToString("0.00")),
                ("Discount", d.Discount?.ToString("0.00")),
                ("Total", d.Total?.ToString("0.00")),
                ("Payment", d.PaymentMethod),
                ("Card", d.CardLast4 is null ? null : $"•••• {d.CardLast4}")));
        };
        this.OnBusinessCard = e =>
        {
            var d = e.Document;
            var summary = $"{d.Name ?? "Business Card"}{(d.Company is null ? "" : $" — {d.Company}")}";
            return this.OnDocument("Business Card", summary, Detail(
                ("Name", d.Name),
                ("Title", d.JobTitle),
                ("Company", d.Company),
                ("Email", d.Email),
                ("Phone", d.Phone),
                ("Website", d.Website),
                ("Address", d.Address)));
        };
        this.OnHealthCard = e =>
        {
            var d = e.Document;
            var summary = $"Health Card {d.Number}{(d.Province is null ? "" : $" ({d.Province})")} — {d.Name}";
            return this.OnDocument("Health Card", summary, Detail(
                ("Number", d.Number),
                ("Name", d.Name),
                ("Province", d.Province),
                ("Issuer", d.Issuer),
                ("Expiry", d.Expiry?.ToString("yyyy-MM-dd"))));
        };
        this.OnLicense = e =>
        {
            var d = e.Document;
            var summary = $"License {d.Number} — {d.FirstName} {d.LastName}";
            return this.OnDocument("Driver's License", summary, Detail(
                ("Number", d.Number),
                ("Name", $"{d.FirstName} {d.LastName}".Trim()),
                ("Date of birth", d.DateOfBirth?.ToString("yyyy-MM-dd")),
                ("Expiry", d.Expiry?.ToString("yyyy-MM-dd")),
                ("Province / State", d.Jurisdiction),
                ("Address", d.Address)));
        };
        this.OnCreditCard = e =>
        {
            var d = e.Document;
            var last4 = d.Number is { Length: >= 4 } n ? n[^4..] : d.Number;
            var summary = $"{d.Type} •••• {last4} exp {d.Expiry?.ToString("MM/yy") ?? "?"}";
            return this.OnDocument("Credit Card", summary, Detail(
                ("Type", d.Type.ToString()),
                ("Number", last4 is null ? null : $"•••• {last4}"),
                ("Expiry", d.Expiry?.ToString("MM/yy")),
                ("Name", $"{d.FirstName} {d.LastName}".Trim()),
                ("Company", d.CompanyName)));
        };
        this.OnPassport = e =>
        {
            var d = e.Document;
            var summary = $"Passport {d.Number} — {d.GivenNames} {d.Surname} ({d.Nationality})";
            return this.OnDocument("Passport", summary, Detail(
                ("Number", d.Number),
                ("Name", $"{d.GivenNames} {d.Surname}".Trim()),
                ("Nationality", d.Nationality),
                ("Issuing country", d.IssuingCountry),
                ("Date of birth", d.DateOfBirth?.ToString("yyyy-MM-dd")),
                ("Expiry", d.Expiry?.ToString("yyyy-MM-dd")),
                ("Sex", d.Sex.ToString())));
        };

        // build the analyzers once and wire their OnDetected handlers; the picker swaps which one is active
        this.barcode.OnDetected = this.OnBarcode;
        this.analyzers = new()
        {
            ["Barcode / QR"] = this.barcode,
            ["Motion"] = new MotionAnalyzer { OnDetected = this.OnMotion },
            ["Face"] = new FaceAnalyzer { OnDetected = this.OnFace },
            ["Invoice"] = new InvoiceAnalyzer { OnDetected = this.OnInvoice },
            ["Receipt"] = new ReceiptAnalyzer { OnDetected = this.OnReceipt },
            ["Business Card"] = new BusinessCardAnalyzer { OnDetected = this.OnBusinessCard },
            ["Health Card"] = new HealthCardAnalyzer { OnDetected = this.OnHealthCard },
            ["Driver's License"] = new DriversLicenseAnalyzer { OnDetected = this.OnLicense },
            ["Credit Card"] = new CreditCardAnalyzer { OnDetected = this.OnCreditCard },
            ["Passport"] = new PassportAnalyzer { OnDetected = this.OnPassport },
        };
        this.Detectors = ["None", .. this.analyzers.Keys];

        InitializeComponent();
        this.BindingContext = this;

        // re-apply ShowBoundingBox / ScanWindow on the active analyzer when their switches toggle
        this.BoxesSwitch.PropertyChanged += (_, _) => this.ApplyBoxes();
        this.ScanAreaSwitch.PropertyChanged += (_, _) => this.ApplyScanArea();

        // open the options sheet at full screen
        this.OptionsPanel.Detents = new ObservableCollection<DetentValue> { DetentValue.Full };

        // keep the options-sheet count in sync as documents are captured
        this.session.Documents.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.ScannedSummary));

        this.Camera.CameraError += (_, e) => this.ShowStatus(e.Message);
        this.Camera.VideoCaptured += (_, v) => this.ShowStatus($"Saved video: {v.FilePath}");
    }

    // Swap the active analyzer (null = "None") and re-apply the box / scan-area options to it.
    void ApplyDetector()
    {
        if (this.Camera is null)
            return;
        this.Camera.Analyzer = this.analyzers.GetValueOrDefault(this.selectedDetector);
        this.ApplyBoxes();
        this.ApplyScanArea();
    }

    void ApplyBoxes()
    {
        if (this.Camera?.Analyzer is FrameAnalyzer fa)
            fa.ShowBoundingBox = this.BoxesSwitch.On;
    }

    // ScanWindow demo: restrict the barcode analyzer to a center band (the overlay frames it as a reticle).
    void ApplyScanArea()
        => this.barcode.ScanWindow = this.ScanAreaSwitch.On ? ScanBand : null;

    // Show the headline on-screen and add the document to the shared session list.
    void Capture(string kind, string summary, string detail)
    {
        this.ShowStatus(summary);
        this.session.Add(kind, summary, detail);
    }

    // OnDetected handler shared by every document analyzer: record the document, and when "Capture & stop" is
    // on grab a still and freeze the preview (tap the thumbnail to resume). Always returns false — documents are
    // single-shot, so the analyzer disarms until the next "Scan" tap.
    async Task<bool> OnDocument(string kind, string summary, string detail)
    {
        this.Capture(kind, summary, detail);
        if (this.CaptureStopSwitch.On)
        {
            var photo = await this.Camera.CaptureAndStopAsync();
            this.ShowThumbnail(photo);
            this.ShowStatus("Captured & stopped — tap the photo to resume");
        }
        return false;
    }

    void ShowThumbnail(CameraPhoto photo)
    {
        var bytes = photo.Data;
        this.PhotoThumbnail.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        this.PhotoThumbnail.IsVisible = true;
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

    async void OnThumbnailTapped(object? sender, TappedEventArgs e)
    {
        // restart the preview after a document "capture & stop" (tap Scan again to re-arm)
        await this.Camera.StartAsync();
        this.PhotoThumbnail.IsVisible = false;
        this.ShowStatus("Resumed");
    }

    void OnScanClicked(object? sender, EventArgs e)
    {
        this.Camera.Scan(); // arm the active analyzer for one scan
        this.ShowStatus("Scanning…");
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
