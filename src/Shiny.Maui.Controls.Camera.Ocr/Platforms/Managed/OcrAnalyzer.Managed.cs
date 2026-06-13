using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class OcrAnalyzer
{
    // No managed OCR engine on the platform-agnostic target; returns nothing.
    private partial Task<List<Detection>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
        => Task.FromResult(new List<Detection>());
}
