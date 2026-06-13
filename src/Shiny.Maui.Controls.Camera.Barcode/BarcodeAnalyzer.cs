using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using ZXing;
using ZXing.Common;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>
/// Decodes 1D/2D barcodes and QR codes from each frame using ZXing.Net over the frame's luminance plane.
/// Cross-platform with no native dependency. <see cref="Detection.Value"/> holds the decoded payload and
/// <see cref="Detection.Label"/> the barcode format.
/// </summary>
public class BarcodeAnalyzer : IFrameAnalyzer
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
    public string Id => "shiny.camera.barcode";

    /// <summary>Restrict to specific formats (null = all supported). Maps to ZXing PossibleFormats.</summary>
    public IList<BarcodeFormat>? Formats
    {
        get => this.reader.Options.PossibleFormats;
        set => this.reader.Options.PossibleFormats = value;
    }

    /// <inheritdoc/>
    public ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        int w = frame.Width, h = frame.Height;
        var lum = frame.GetLuminance().ToArray();

        var source = new PlanarYUVLuminanceSource(lum, w, h, 0, 0, w, h, false);
        var result = this.reader.Decode(source);
        if (result == null)
            return new ValueTask<DetectionResult?>(DetectionResult.Empty(this.Id));

        var raw = BoundingBox(result.ResultPoints, w, h);
        var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
        var detection = new Detection(DetectionType.Barcode, box, result.BarcodeFormat.ToString(), result.Text);
        return new ValueTask<DetectionResult?>(new DetectionResult(this.Id, [detection]));
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
