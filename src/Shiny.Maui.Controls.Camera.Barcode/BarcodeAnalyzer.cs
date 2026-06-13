using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using ZXing;
using ZXing.Common;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>
/// Decodes 1D/2D barcodes and QR codes from each frame using ZXing.Net over the frame's luminance plane.
/// Cross-platform with no native dependency. Raises <see cref="BarcodeDetected"/> with the decoded format +
/// value, and draws a box (captioned with the value) around the code; clears it when no code is in view.
/// </summary>
public class BarcodeAnalyzer : FrameAnalyzer
{
    readonly BarcodeReaderGeneric reader;

    public BarcodeAnalyzer()
    {
        this.reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions { TryHarder = true, TryInverted = true }
        };
    }

    /// <inheritdoc/>
    public override string Id => "shiny.camera.barcode";

    /// <summary>Restrict to specific formats (null = all supported). Maps to ZXing PossibleFormats.</summary>
    public IList<BarcodeFormat>? Formats
    {
        get => this.reader.Options.PossibleFormats;
        set => this.reader.Options.PossibleFormats = value;
    }

    /// <summary>Box outline + caption color. Default a green accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#22C55E");

    /// <summary>Command invoked (with the <see cref="BarcodeDetectedEventArgs"/>) when a barcode is decoded.</summary>
    public static readonly BindableProperty BarcodeDetectedCommandProperty = BindableProperty.Create(
        nameof(BarcodeDetectedCommand), typeof(ICommand), typeof(BarcodeAnalyzer));

    /// <inheritdoc cref="BarcodeDetectedCommandProperty"/>
    public ICommand? BarcodeDetectedCommand
    {
        get => (ICommand?)this.GetValue(BarcodeDetectedCommandProperty);
        set => this.SetValue(BarcodeDetectedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for a decode; return <c>null</c> for no overlay. When
    /// unset the analyzer draws a single <see cref="BoxColor"/> box captioned with the value.
    /// </summary>
    public Func<BarcodeDetectedEventArgs, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when a barcode is decoded in a frame.</summary>
    public event EventHandler<BarcodeDetectedEventArgs>? BarcodeDetected;

    /// <inheritdoc/>
    public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        int w = frame.Width, h = frame.Height;
        var lum = frame.GetLuminance().ToArray();

        var source = new PlanarYUVLuminanceSource(lum, w, h, 0, 0, w, h, false);
        var result = this.reader.Decode(source);
        if (result == null)
            return default; // nothing in view -> clear this analyzer's box

        var raw = BoundingBox(result.ResultPoints, w, h);
        var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
        var args = new BarcodeDetectedEventArgs(result.BarcodeFormat, result.Text, box);

        this.Emit(() => this.BarcodeDetected?.Invoke(this, args), this.BarcodeDetectedCommand, args);

        var boxes = this.ResolveOverlay(args, this.OverlayProvider,
            () => new[] { new OverlayBox(box, this.BoxColor, result.Text, this.BoxColor) });
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

        // pad slightly so a 1D barcode's zero-height line still draws as a box
        var pad = Math.Max(w, h) * 0.02f;
        minX -= pad; maxX += pad; minY -= pad; maxY += pad;

        return new RectF(
            Math.Clamp(minX / w, 0, 1),
            Math.Clamp(minY / h, 0, 1),
            Math.Clamp((maxX - minX) / w, 0, 1),
            Math.Clamp((maxY - minY) / h, 0, 1)
        );
    }
}
