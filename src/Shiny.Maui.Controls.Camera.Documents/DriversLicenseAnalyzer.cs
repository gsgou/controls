using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using ZXing;
using ZXing.Common;

namespace Shiny.Maui.Controls.Camera.Documents;

/// <summary>
/// Recognizes a North American driver's license by decoding the <b>PDF417</b> barcode on its back and
/// parsing the <b>AAMVA</b> record into a strongly-typed <see cref="DriversLicense"/>. Deterministic (no
/// ML). Raises <see cref="DocumentDetected"/> and draws a box (captioned with the license number) around
/// the barcode; clears it when no license is in view.
/// </summary>
public class DriversLicenseAnalyzer : FrameAnalyzer
{
    readonly BarcodeReaderGeneric reader;

    public DriversLicenseAnalyzer()
    {
        this.reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.PDF_417]
            }
        };
    }

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
    public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        int w = frame.Width, h = frame.Height;
        var lum = frame.GetLuminance().ToArray();

        var source = new PlanarYUVLuminanceSource(lum, w, h, 0, 0, w, h, false);
        var result = this.reader.Decode(source);
        if (result == null || !AamvaParser.TryParse(result.Text, out var license))
            return default; // no license barcode in view -> clear

        var raw = BoundingBox(result.ResultPoints, w, h);
        var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
        var args = new DocumentDetectedEventArgs<DriversLicense>(license);

        this.Emit(() => this.DocumentDetected?.Invoke(this, args), this.DocumentDetectedCommand, args);

        var boxes = this.ResolveOverlay(license, this.OverlayProvider,
            () => new[] { new OverlayBox(box, this.BoxColor, license.Number ?? "License", this.BoxColor) });
        return new ValueTask<IReadOnlyList<OverlayBox>?>(boxes);
    }

    static RectF BoundingBox(ResultPoint[]? points, int w, int h)
    {
        if (points == null || points.Length == 0)
            return new RectF(0, 0, 1, 1);

        float minX = float.MaxValue, minY = float.MaxValue, maxX = 0, maxY = 0;
        foreach (var p in points)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }

        return new RectF(
            Math.Clamp(minX / w, 0, 1),
            Math.Clamp(minY / h, 0, 1),
            Math.Clamp((maxX - minX) / w, 0, 1),
            Math.Clamp((maxY - minY) / h, 0, 1)
        );
    }
}
