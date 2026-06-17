using System.Runtime.CompilerServices;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

/// <summary>
/// Reusable native text recognizer used by <see cref="OcrAnalyzer"/> and the document analyzers. Recognizes
/// text in a frame via the platform OCR engine (Apple Vision, Android MLKit, Windows.Media.Ocr; empty on
/// bare net10.0), returning blocks in normalized upright image space.
/// </summary>
/// <remarks>
/// OCR is the expensive step, so it runs <b>once per frame</b> no matter how many analyzers need it: the
/// camera pipeline fans the same <see cref="CameraFrame"/> instance out to every analyzer, so the first one
/// to ask recognizes the frame and the rest await the same <see cref="Task"/>. The cache is weak-keyed on the
/// frame, so the entry evicts as soon as the frame is collected.
/// </remarks>
public partial class TextRecognizer
{
    static readonly ConditionalWeakTable<CameraFrame, Lazy<Task<List<RecognizedText>>>> frameCache = new();
    static readonly ConditionalWeakTable<CameraFrame, Lazy<Task<RecognizedDocument>>> documentCache = new();

    /// <summary>
    /// Recognize text blocks in the frame. Returns an empty list when none / unsupported. The underlying OCR
    /// runs at most once per frame regardless of how many analyzers call it (the result is cached on the
    /// frame instance and shared).
    /// </summary>
    public Task<List<RecognizedText>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
        => frameCache
            .GetValue(frame, f => new Lazy<Task<List<RecognizedText>>>(() => this.RecognizeCoreAsync(f, ct)))
            .Value;

    /// <summary>
    /// Detect a document in the frame, deskew it (perspective-correct), and OCR the flattened crop — far more
    /// accurate for angled cards/IDs than whole-frame OCR. Falls back to whole-frame OCR (with a null
    /// <see cref="RecognizedDocument.Quad"/>) when no document is found or the platform has no rectifier (only
    /// Apple Vision today; Android/Windows/bare net10.0 fall back). Shared once per frame like
    /// <see cref="RecognizeAsync"/>.
    /// </summary>
    public Task<RecognizedDocument> RecognizeDocumentAsync(CameraFrame frame, CancellationToken ct)
        => documentCache
            .GetValue(frame, f => new Lazy<Task<RecognizedDocument>>(() => this.RecognizeDocumentCoreAsync(f, ct)))
            .Value;

    /// <summary>Platform OCR for a single frame. Invoked at most once per frame via <see cref="RecognizeAsync"/>.</summary>
    private partial Task<List<RecognizedText>> RecognizeCoreAsync(CameraFrame frame, CancellationToken ct);

    /// <summary>Platform detect-deskew-OCR for a single frame. Invoked at most once per frame via <see cref="RecognizeDocumentAsync"/>.</summary>
    private partial Task<RecognizedDocument> RecognizeDocumentCoreAsync(CameraFrame frame, CancellationToken ct);
}
