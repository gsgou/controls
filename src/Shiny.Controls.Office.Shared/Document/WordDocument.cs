using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Text;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// An open <c>.docx</c>, read into a reflowable block model.
/// </summary>
/// <remarks>
/// <para>
/// This is a **viewer**. The document is presented as a continuous flow rather than paginated: page
/// breaks, headers, footers and footnote placement all depend on a full pagination engine, and
/// pretending to have one produces page boundaries in the wrong places, which is worse than honestly
/// not having pages at all.
/// </para>
/// <para>
/// The package is still held open and untouched, so the same document can later gain an editor without
/// changing how it is read.
/// </para>
/// </remarks>
public sealed class WordDocument : OfficeDocument
{
    readonly WordprocessingDocument document;
    readonly List<DocumentBlock> blocks = new();
    readonly WordBodyReader reader;
    readonly Body? body;
    bool contentChanged;

    WordDocument(MemoryStream buffer, string? path, WordprocessingDocument document, IUnsupportedFeatureSink unsupported)
        : base(buffer, path, unsupported)
    {
        this.document = document;

        var main = document.MainDocumentPart
            ?? throw new InvalidDataException("The package has no main document part.");

        var styles = new WordStyleResolver(main);
        var numbering = new WordNumbering(main);
        this.reader = new WordBodyReader(main, styles, numbering, unsupported);
        this.body = main.Document?.Body;

        this.DefaultStyle = styles.DefaultRunStyle;
        this.Page = ReadPageSetup(main);
        this.blocks.AddRange(this.reader.ReadBody(this.body));
        this.Undo = new UndoStack<WordDocument>(this);

        this.ReportUnsupported(main);
    }

    public IReadOnlyList<DocumentBlock> Blocks => this.blocks;

    /// <summary>Undo history for edits. Empty for a document opened read-only.</summary>
    public UndoStack<WordDocument> Undo { get; }

    /// <summary>True when the document was opened for editing.</summary>
    public bool IsEditable { get; private set; }

    /// <summary>Raised after any edit, so a view can re-lay-out and repaint.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>Applies an edit through the undo stack.</summary>
    public void Execute(IEditCommand<WordDocument> command)
    {
        if (!this.IsEditable)
            throw new InvalidOperationException("This document was opened read-only. Use OpenAsync(..., editable: true).");

        this.Undo.Execute(command);
    }

    public PageSetup Page { get; }

    public TextStyle DefaultStyle { get; }

    /// <summary>Headings in document order, for a navigation pane or outline.</summary>
    public IEnumerable<(int Level, string Text)> Outline()
        => this.blocks
            .OfType<DocumentParagraph>()
            .Where(x => x.Format.OutlineLevel > 0 && x.PlainText.Length > 0)
            .Select(x => (x.Format.OutlineLevel, x.PlainText));

    /// <summary>The whole document as plain text, one line per paragraph.</summary>
    public string PlainText => string.Join(
        Environment.NewLine,
        this.blocks.OfType<DocumentParagraph>().Select(x => x.PlainText));

    public static async Task<WordDocument> OpenAsync(
        string path,
        IUnsupportedFeatureSink? unsupported = null,
        bool editable = false,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(path, cancellationToken).ConfigureAwait(false);
        return Create(buffer, path, unsupported, editable);
    }

    public static async Task<WordDocument> OpenAsync(
        Stream source,
        IUnsupportedFeatureSink? unsupported = null,
        bool editable = false,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(source, cancellationToken).ConfigureAwait(false);
        return Create(buffer, null, unsupported, editable);
    }

    static WordDocument Create(MemoryStream buffer, string? path, IUnsupportedFeatureSink? unsupported, bool editable = false)
    {
        var sink = unsupported ?? NullUnsupportedFeatureSink.Instance;
        WordprocessingDocument document;
        try
        {
            // AutoSave off for the same reason as the workbook: OpenXml otherwise re-serialises every
            // part it materialised, so merely opening a document would rewrite it.
            document = WordprocessingDocument.Open(buffer, isEditable: editable, new OpenSettings { AutoSave = false });
        }
        catch
        {
            buffer.Dispose();
            throw;
        }

        try
        {
            return new WordDocument(buffer, path, document, sink) { IsEditable = editable };
        }
        catch
        {
            document.Dispose();
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Writes pending edits into the package. An unedited document is never re-serialised, so opening
    /// and saving without changing anything produces a byte-identical file.
    /// </summary>
    protected override void FlushToPackage()
    {
        if (!this.contentChanged)
            return;

        this.document.MainDocumentPart?.Document?.Save();
        this.document.Save();
        this.contentChanged = false;
    }

    // ---- editing surface, driven by the commands ----

    /// <summary>The main part, for the commands that need to add relationships to it.</summary>
    internal MainDocumentPart? Main => this.document.MainDocumentPart;

    /// <summary>
    /// Stores image bytes as a part of the document and returns the relationship id to reference it by.
    /// </summary>
    /// <remarks>
    /// Every insertion gets its own part, even for bytes already in the package. De-duplicating would
    /// mean hashing every existing image on every drop, and two references to one part is a trap:
    /// deleting either picture has to leave the part alone, which is a lifetime rule nothing else here
    /// needs.
    /// </remarks>
    internal string? AddImagePart(byte[] data, string contentType)
    {
        if (this.Main is not { } main)
            return null;

        var part = main.AddImagePart(contentType);
        using var stream = new MemoryStream(data, writable: false);
        part.FeedData(stream);

        return main.GetIdOfPart(part);
    }

    /// <summary>
    /// An id no drawing in the document is using.
    /// </summary>
    /// <remarks>
    /// Word requires <c>wp:docPr/@id</c> to be unique across the document and non-zero. A duplicate is
    /// one of the few things it treats as corruption rather than repairing quietly, so the id is taken
    /// from the current maximum rather than from a counter that would restart at one on the next open.
    /// </remarks>
    internal uint NextDrawingId()
    {
        var max = 0U;

        foreach (var properties in this.body?.Descendants<Drawing.Wordprocessing.DocProperties>() ?? [])
        {
            if (properties.Id?.Value is { } id && id > max)
                max = id;
        }

        return max + 1;
    }

    internal Paragraph? ParagraphElementAt(int block)
        => this.BlockElementAt(block) as Paragraph;

    /// <summary>
    /// The body element a block was read from, whatever kind it is.
    /// </summary>
    /// <remarks>
    /// The paragraph-typed accessor above is what the text commands want, since they have nothing to
    /// say about a table. This one is what the block commands and the undo snapshot want, because a
    /// body holds tables too and an undo that could only put paragraphs back would silently drop a
    /// table it had just removed.
    /// </remarks>
    internal OpenXmlElement? BlockElementAt(int block) => block >= 0 && block < this.blocks.Count
        ? this.blocks[block] switch
        {
            DocumentParagraph paragraph => paragraph.Element,
            DocumentTable table => table.Element,
            _ => null
        }
        : null;

    /// <summary>Re-reads one block from its (now edited) XML.</summary>
    internal void Reproject(int block)
    {
        if (block < 0 || block >= this.blocks.Count)
            return;

        if (this.BlockElementAt(block) is { } element)
            this.blocks[block] = this.reader.RereadBlock(element);

        this.MarkChanged();
    }

    internal void InsertBlockAfter(int block, OpenXmlElement element)
    {
        this.blocks.Insert(block + 1, this.reader.RereadBlock(element));
        this.MarkChanged();
    }

    internal void RemoveBlockAfter(int block)
    {
        if (block + 1 < this.blocks.Count)
            this.blocks.RemoveAt(block + 1);

        this.MarkChanged();
    }

    internal void RemoveBlock(int block)
    {
        if (block < 0 || block >= this.blocks.Count)
            return;

        this.BlockElementAt(block)?.Remove();
        this.blocks.RemoveAt(block);
        this.MarkChanged();
    }

    /// <summary>
    /// Clones the paragraphs a range touches, so an edit can be reversed by putting them back.
    /// </summary>
    internal RestoreBlocksCommand CaptureRange(DocumentRange range)
        => this.CaptureBlocks(range.Start.Block, range.End.Block - range.Start.Block + 1);

    internal RestoreBlocksCommand CaptureBlocks(int start, int count)
    {
        var snapshot = new List<OpenXmlElement>();
        for (var i = start; i < start + count && i < this.blocks.Count; i++)
        {
            if (this.BlockElementAt(i) is { } element)
                snapshot.Add(element.CloneNode(true));
        }

        return new RestoreBlocksCommand(start, snapshot.Count, snapshot);
    }

    /// <summary>Replaces a span of blocks with cloned elements, in the body and in the projection.</summary>
    internal void ReplaceBlocks(int start, int count, IReadOnlyList<OpenXmlElement> replacements)
    {
        if (this.body is null)
            return;

        // Anchor on the element before the span, so the replacements land in the right place even when
        // the span itself is being removed entirely.
        var anchor = start > 0 ? this.BlockElementAt(start - 1) : null;

        for (var i = Math.Min(start + count, this.blocks.Count) - 1; i >= start; i--)
        {
            this.BlockElementAt(i)?.Remove();

            if (i < this.blocks.Count)
                this.blocks.RemoveAt(i);
        }

        var index = start;
        foreach (var replacement in replacements)
        {
            var clone = replacement.CloneNode(true);

            if (anchor is null)
                this.body.InsertAt(clone, 0);
            else
                anchor.InsertAfterSelf(clone);

            anchor = clone;
            this.blocks.Insert(index++, this.reader.RereadBlock(clone));
        }

        this.MarkChanged();
    }

    void MarkChanged()
    {
        this.contentChanged = true;
        this.MarkDirty();
        this.ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    static PageSetup ReadPageSetup(MainDocumentPart main)
    {
        var section = main.Document?.Body?.Elements<SectionProperties>().LastOrDefault();
        var size = section?.GetFirstChild<PageSize>();
        var margin = section?.GetFirstChild<PageMargin>();

        var setup = PageSetup.Letter;

        if (size?.Width?.Value is { } width)
            setup = setup with { Width = OoxmlUnits.TwipsToPixels(width) };

        if (size?.Height?.Value is { } height)
            setup = setup with { Height = OoxmlUnits.TwipsToPixels(height) };

        if (margin is not null)
        {
            setup = setup with
            {
                MarginLeft = margin.Left?.Value is { } left ? OoxmlUnits.TwipsToPixels(left) : setup.MarginLeft,
                MarginRight = margin.Right?.Value is { } right ? OoxmlUnits.TwipsToPixels(right) : setup.MarginRight,
                MarginTop = margin.Top?.Value is { } top ? OoxmlUnits.TwipsToPixels(top) : setup.MarginTop,
                MarginBottom = margin.Bottom?.Value is { } bottom ? OoxmlUnits.TwipsToPixels(bottom) : setup.MarginBottom
            };
        }

        return setup;
    }

    void ReportUnsupported(MainDocumentPart main)
    {
        // Everything here is preserved in the package - the viewer simply does not show it, and saying
        // so is better than letting someone assume a document has no comments because none appeared.
        if (main.Document?.Body?.Descendants<CommentRangeStart>().Any() == true)
            this.Unsupported.Report(new UnsupportedFeature("document", "Comments", UnsupportedSeverity.NotRendered));

        if (main.FootnotesPart is not null || main.EndnotesPart is not null)
            this.Unsupported.Report(new UnsupportedFeature("document", "Footnotes and endnotes", UnsupportedSeverity.NotRendered));

        if (main.HeaderParts.Any() || main.FooterParts.Any())
        {
            this.Unsupported.Report(new UnsupportedFeature(
                "document", "Headers and footers", UnsupportedSeverity.NotRendered,
                "The viewer reflows content and has no pages to attach them to."));
        }

        if (main.Document?.Body?.Descendants<InsertedRun>().Any() == true ||
            main.Document?.Body?.Descendants<DeletedRun>().Any() == true)
        {
            this.Unsupported.Report(new UnsupportedFeature(
                "document", "Tracked changes", UnsupportedSeverity.NotRendered,
                "Insertions render as normal text; deletions are hidden."));
        }

        if (main.VbaProjectPart is not null)
            this.Unsupported.Report(new UnsupportedFeature("vbaProject", "Macros", UnsupportedSeverity.NotRendered));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.document.Dispose();

        base.Dispose(disposing);
    }
}
