using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Barcode;

namespace Shiny.Maui.Controls.Camera.Documents;

/// <summary>
/// Recognizes a North American driver's license by reading the <b>PDF417</b> barcode on its back (native
/// scanner — Apple Vision / Android MLKit) and parsing the <b>AAMVA</b> record into a strongly-typed
/// <see cref="DriversLicense"/>. Raises <see cref="DocumentDetected"/> and draws a box (captioned with the
/// license number) around the barcode; clears it when no license is in view.
/// </summary>
public class DriversLicenseAnalyzer : FrameAnalyzer
{
    readonly BarcodeScanner scanner = new() { Formats = [BarcodeFormat.Pdf417] };

    /// <inheritdoc/>
    public override string Id => "shiny.camera.driverslicense";

    /// <summary>Box outline + caption color. Default a blue accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#3B82F6");

    /// <summary>Command invoked (with the <see cref="DocumentDetectedEventArgs{DriversLicense}"/>) on a recognition.</summary>
    public static readonly BindableProperty DocumentDetectedCommandProperty = BindableProperty.Create(
        nameof(DocumentDetectedCommand), typeof(ICommand), typeof(DriversLicenseAnalyzer));

    /// <inheritdoc cref="DocumentDetectedCommandProperty"/>
    public ICommand? DocumentDetectedCommand
    {
        get => (ICommand?)this.GetValue(DocumentDetectedCommandProperty);
        set => this.SetValue(DocumentDetectedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for the recognized license; return <c>null</c> for no
    /// overlay. When unset the analyzer draws a <see cref="BoxColor"/> box captioned with the license number.
    /// </summary>
    public Func<DriversLicense, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when a driver's license is recognized in a frame.</summary>
    public event EventHandler<DocumentDetectedEventArgs<DriversLicense>>? DocumentDetected;

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var codes = await this.scanner.ScanAsync(frame, ct).ConfigureAwait(false);

        foreach (var code in codes)
        {
            if (!AamvaParser.TryParse(code.Value, out var license))
                continue;

            var args = new DocumentDetectedEventArgs<DriversLicense>(license);
            this.Emit(() => this.DocumentDetected?.Invoke(this, args), this.DocumentDetectedCommand, args);

            return this.ResolveOverlay(license, this.OverlayProvider,
                () => new[] { new OverlayBox(code.BoundingBox, this.BoxColor, license.Number ?? "License", this.BoxColor) });
        }
        return null; // no license barcode in view -> clear
    }
}
