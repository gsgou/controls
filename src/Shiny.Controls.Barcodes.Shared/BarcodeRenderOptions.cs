namespace Shiny.Controls.Barcodes;

/// <summary>
/// Render parameters for a barcode.
/// </summary>
public sealed class BarcodeRenderOptions
{
    public int PixelWidth { get; set; } = 250;
    public int PixelHeight { get; set; } = 250;
    public int Margin { get; set; } = 10;

    /// <summary>Hex foreground color (#RRGGBB or #AARRGGBB). Defaults to black.</summary>
    public string ForegroundColor { get; set; } = "#000000";

    /// <summary>Hex background color (#RRGGBB or #AARRGGBB). Defaults to white.</summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>QR-only: error correction level. Ignored for non-QR formats.</summary>
    public QRErrorCorrection QRErrorCorrection { get; set; } = QRErrorCorrection.Medium;
}
