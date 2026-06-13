using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class TextRecognizer
{
    // No managed OCR engine on the platform-agnostic target; returns nothing.
    public partial Task<List<RecognizedText>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
        => Task.FromResult(new List<RecognizedText>());
}
