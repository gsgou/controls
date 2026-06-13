using Microsoft.Maui.Graphics;
using ZXing;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>Carries a decoded barcode to <see cref="BarcodeAnalyzer.BarcodeDetected"/> subscribers.</summary>
/// <param name="format">The barcode symbology (QR, Code128, EAN13, PDF417, …).</param>
/// <param name="value">The decoded payload text.</param>
/// <param name="boundingBox">Normalized bounds (0..1) of the barcode in upright image space.</param>
public class BarcodeDetectedEventArgs(BarcodeFormat format, string value, RectF boundingBox) : EventArgs
{
    /// <summary>The barcode symbology.</summary>
    public BarcodeFormat Format { get; } = format;

    /// <summary>The decoded payload text.</summary>
    public string Value { get; } = value;

    /// <summary>Normalized bounds (0..1) of the barcode in upright image space.</summary>
    public RectF BoundingBox { get; } = boundingBox;
}
