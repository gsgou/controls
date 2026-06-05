namespace Shiny.Controls.Barcodes;

/// <summary>
/// QR Code error correction levels. Higher values tolerate more damage at the cost of capacity.
/// </summary>
public enum QRErrorCorrection
{
    Low,
    Medium,
    Quartile,
    High
}
