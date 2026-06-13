using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

/// <summary>
/// Reusable native text recognizer used by <see cref="OcrAnalyzer"/> and the document analyzers. Recognizes
/// text in a frame via the platform OCR engine (Apple Vision, Android MLKit, Windows.Media.Ocr; empty on
/// bare net10.0), returning blocks in normalized upright image space.
/// </summary>
public partial class TextRecognizer
{
    /// <summary>Recognize text blocks in the frame. Returns an empty list when none / unsupported.</summary>
    public partial Task<List<RecognizedText>> RecognizeAsync(CameraFrame frame, CancellationToken ct);
}
