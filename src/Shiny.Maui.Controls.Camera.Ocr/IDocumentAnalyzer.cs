using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

/// <summary>
/// Pluggable structured-extraction hook. Given the OCR text blocks recognized in a frame (each a
/// <see cref="DetectionType.Text"/> <see cref="Detection"/> with its <see cref="Detection.Value"/> and
/// box), produce structured fields as <see cref="DetectionType.DocumentField"/> detections — e.g. an
/// invoice "Total" or "Date". Implement this with rules, a cloud Document AI, or an LLM. The OCR analyzer
/// ships without a built-in implementation; supply your own to turn raw text into labelled values.
/// </summary>
public interface IDocumentAnalyzer
{
    /// <summary>Extract structured fields from the recognized text blocks.</summary>
    ValueTask<IReadOnlyList<Detection>> ExtractAsync(IReadOnlyList<Detection> textBlocks, CancellationToken ct);
}
