using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>No-op fallback for Windows; there is no native Windows barcode scanner.</summary>
public partial class BarcodeScanner
{
    public partial Task<List<DetectedBarcode>> ScanAsync(CameraFrame frame, CancellationToken ct)
        => Task.FromResult(new List<DetectedBarcode>());
}
