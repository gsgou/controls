using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>No-op fallback for the platform-agnostic (net10.0) target; barcode scanning requires a native backend.</summary>
public partial class BarcodeScanner
{
    public partial Task<List<DetectedBarcode>> ScanAsync(CameraFrame frame, CancellationToken ct)
        => Task.FromResult(new List<DetectedBarcode>());
}
