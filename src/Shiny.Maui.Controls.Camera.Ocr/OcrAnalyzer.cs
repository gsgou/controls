using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Ocr;

/// <summary>
/// Recognizes text in each frame (native Vision / MLKit / Windows.Media.Ocr) as
/// <see cref="DetectionType.Text"/> detections. When an <see cref="IDocumentAnalyzer"/> is supplied, its
/// extracted <see cref="DetectionType.DocumentField"/> detections are appended — the hook for invoice/
/// label parsing.
/// </summary>
public partial class OcrAnalyzer : IFrameAnalyzer
{
    readonly IDocumentAnalyzer? document;

    /// <param name="documentAnalyzer">Optional structured-field extractor (invoice/label parsing).</param>
    public OcrAnalyzer(IDocumentAnalyzer? documentAnalyzer = null) => this.document = documentAnalyzer;

    /// <inheritdoc/>
    public string Id => "shiny.camera.ocr";

    /// <summary>Emit raw text-block detections in addition to extracted fields. Default <c>true</c>.</summary>
    public bool IncludeTextBlocks { get; set; } = true;

    /// <inheritdoc/>
    public async ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var text = await this.RecognizeAsync(frame, ct).ConfigureAwait(false);

        if (this.document != null && text.Count > 0)
        {
            var fields = await this.document.ExtractAsync(text, ct).ConfigureAwait(false);
            var combined = new List<Detection>(this.IncludeTextBlocks ? text : []);
            combined.AddRange(fields);
            return new DetectionResult(this.Id, combined);
        }

        return new DetectionResult(this.Id, this.IncludeTextBlocks ? text : []);
    }

    // Implemented per platform (returns the recognized text blocks in upright normalized space).
    private partial Task<List<Detection>> RecognizeAsync(CameraFrame frame, CancellationToken ct);
}
