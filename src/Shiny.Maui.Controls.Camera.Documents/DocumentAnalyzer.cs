using System.Windows.Input;
using Microsoft.Maui.Controls;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Ocr;

namespace Shiny.Maui.Controls.Camera.Documents;

/// <summary>
/// Base class for OCR-backed document analyzers. Runs the shared <see cref="TextRecognizer"/> over each
/// frame, hands the recognized text to an <see cref="IDocumentParser{TDocument}"/>, raises
/// <see cref="DocumentDetected"/> (this analyzer's own strongly-typed event) on a successful parse, and
/// draws the parser's boxes. Derive and supply a parser for a specific document type (see
/// <see cref="InvoiceAnalyzer"/>, <see cref="HealthCardAnalyzer"/>).
/// </summary>
/// <typeparam name="TDocument">The strongly-typed document payload (e.g. <c>Invoice</c>).</typeparam>
public abstract class DocumentAnalyzer<TDocument> : FrameAnalyzer
{
    readonly TextRecognizer recognizer = new();
    readonly IDocumentParser<TDocument> parser;

    /// <param name="parser">Strategy turning recognized text into a typed payload + overlay boxes.</param>
    protected DocumentAnalyzer(IDocumentParser<TDocument> parser) => this.parser = parser;

    /// <summary>Command invoked (with the <see cref="DocumentDetectedEventArgs{TDocument}"/>) on a recognition.</summary>
    public static readonly BindableProperty DocumentDetectedCommandProperty = BindableProperty.Create(
        nameof(DocumentDetectedCommand), typeof(ICommand), typeof(DocumentAnalyzer<TDocument>));

    /// <inheritdoc cref="DocumentDetectedCommandProperty"/>
    public ICommand? DocumentDetectedCommand
    {
        get => (ICommand?)this.GetValue(DocumentDetectedCommandProperty);
        set => this.SetValue(DocumentDetectedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for the recognized document; return <c>null</c> for no
    /// overlay. When unset the analyzer draws the parser's boxes.
    /// </summary>
    public Func<TDocument, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when a document of this type is recognized in a frame.</summary>
    public event EventHandler<DocumentDetectedEventArgs<TDocument>>? DocumentDetected;

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var text = await this.recognizer.RecognizeAsync(frame, ct).ConfigureAwait(false);
        if (text.Count == 0)
            return null;

        if (!this.parser.TryParse(text, out var document, out var boxes))
            return null;

        var args = new DocumentDetectedEventArgs<TDocument>(document);
        this.Emit(() => this.DocumentDetected?.Invoke(this, args), this.DocumentDetectedCommand, args);

        return this.ResolveOverlay(document, this.OverlayProvider, () => boxes.Count == 0 ? null : boxes);
    }
}
