using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>
/// Reusable native barcode scanner used by <see cref="BarcodeAnalyzer"/> and the document analyzers (the
/// driver's-license PDF417 read). Scans a frame with the platform engine — Apple Vision
/// (<c>VNDetectBarcodesRequest</c>) on iOS/macOS, Android MLKit (<c>BarcodeScanning</c>) — returning the
/// codes in normalized upright image space. A no-op on Windows and bare net10.0 (no native scanner).
/// </summary>
public partial class BarcodeScanner
{
    /// <summary>Restrict scanning to specific symbologies (null = all supported). Filters the native scanner.</summary>
    public IList<BarcodeFormat>? Formats { get; set; }

    /// <summary>Scan the frame for barcodes. Returns an empty list when none are found / unsupported.</summary>
    public partial Task<List<DetectedBarcode>> ScanAsync(CameraFrame frame, CancellationToken ct);
}
