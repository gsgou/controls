using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class TextRecognizer
{
    // No managed OCR engine on the platform-agnostic target; returns nothing.
    private partial Task<List<RecognizedText>> RecognizeCoreAsync(CameraFrame frame, CancellationToken ct)
        => Task.FromResult(new List<RecognizedText>());
}
