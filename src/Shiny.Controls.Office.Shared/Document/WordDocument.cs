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
/// Read into one continuous flow, which either view can present. <see cref="DocumentPageLayout.Print"/>
/// slices that flow into pages and draws the headers, footers and page numbers this type exposes;
/// <see cref="DocumentPageLayout.Reflow"/> shows it as one column and ignores them. Slicing rather
/// than laying out per page is what keeps a single model honest for both.
/// </para>
/// <para>
/// Footnotes and endnotes are still not placed — they need their own reference area at the foot of
/// the page they are cited on, which is a different problem from breaking a flow into pages.
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
    readonly WordBodyReader chromeReader;
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

        // A second reader with its own numbering state: list counters are consumed as the body is
        // walked, and a numbered list in a header would otherwise continue the body's sequence.
        this.chromeReader = new WordBodyReader(main, styles, new WordNumbering(main), unsupported);
        this.RereadHeadersFooters();

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

    public PageSetup Page { get; private set; }

    /// <summary>
    /// The document's headers and footers, already read into the same block model the body uses.
    /// </summary>
    /// <remarks>
    /// Only drawn in <see cref="DocumentPageLayout.Print"/>: a reflowed column has no page edges to
    /// put them against.
    /// </remarks>
    public DocumentHeaderFooterSet HeadersFooters { get; private set; } = DocumentHeaderFooterSet.Empty;

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
                MarginBottom = margin.Bottom?.Value is { } bottom ? OoxmlUnits.TwipsToPixels(bottom) : setup.MarginBottom,
                HeaderDistance = margin.Header?.Value is { } header ? OoxmlUnits.TwipsToPixels(header) : setup.HeaderDistance,
                FooterDistance = margin.Footer?.Value is { } footer ? OoxmlUnits.TwipsToPixels(footer) : setup.FooterDistance
            };
        }

        // w:titlePg and w:evenAndOddHeaders are both "present unless explicitly false" — an on/off
        // element whose absence means off, and whose w:val="0" also means off.
        setup = setup with
        {
            DifferentFirstPage = IsOn(section?.GetFirstChild<TitlePage>()),
            DifferentOddAndEvenPages = IsOn(main.DocumentSettingsPart?.Settings?.GetFirstChild<EvenAndOddHeaders>())
        };

        return setup;
    }

    /// <summary>
    /// Reads an OOXML on/off element, which has three states and not two.
    /// </summary>
    /// <remarks>
    /// Absent means off; present with no <c>w:val</c> means on; present with <c>w:val="0"</c> means
    /// off again. Testing only the value treats every document without a <c>w:titlePg</c> as having
    /// a distinct first page, which shows up as a missing header on page one.
    /// </remarks>
    static bool IsOn(OnOffType? element) => element is not null && (element.Val is null || element.Val.Value);

    /// <summary>The last section's properties, created if the body has none.</summary>
    internal SectionProperties? SectionProperties(bool create)
    {
        if (this.body is null)
            return null;

        var existing = this.body.Elements<SectionProperties>().LastOrDefault();
        if (existing is not null || !create)
            return existing;

        // sectPr has to be the body's last child; anywhere else and Word rejects the document.
        var created = new SectionProperties();
        this.body.AppendChild(created);
        return created;
    }

    /// <summary>Re-reads the header and footer parts after one of them has been edited.</summary>
    internal void RereadHeadersFooters()
    {
        var set = new DocumentHeaderFooterSet();
        var main = this.Main;
        var section = main?.Document?.Body?.Elements<SectionProperties>().LastOrDefault();

        if (main is null || section is null)
        {
            this.HeadersFooters = set;
            return;
        }

        foreach (var reference in section.Elements<HeaderReference>())
        {
            if (PartFor(main, reference.Id?.Value) is HeaderPart part)
                set.SetHeader(KindOf(reference), new DocumentHeaderFooter(this.chromeReader.ReadContainer(part.Header)));
        }

        foreach (var reference in section.Elements<FooterReference>())
        {
            if (PartFor(main, reference.Id?.Value) is FooterPart part)
                set.SetFooter(KindOf(reference), new DocumentHeaderFooter(this.chromeReader.ReadContainer(part.Footer)));
        }

        this.HeadersFooters = set;
        this.Page = ReadPageSetup(main);
    }

    static DocumentPageKind KindOf(OpenXmlElement reference) => OoxmlUnits.EnumAttribute(reference, "type") switch
    {
        "first" => DocumentPageKind.First,
        "even" => DocumentPageKind.Even,
        _ => DocumentPageKind.Default
    };

    /// <summary>
    /// The part a header or footer reference points at, or null when the relationship is dangling.
    /// </summary>
    /// <remarks>
    /// A reference to a missing relationship throws rather than returning null, and a document with
    /// one is not otherwise broken — so it is caught and the header simply does not draw.
    /// </remarks>
    static OpenXmlPart? PartFor(MainDocumentPart main, string? id)
    {
        if (String.IsNullOrEmpty(id))
            return null;

        try
        {
            return main.GetPartById(id);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The live OOXML a header or footer is currently made of, for a command that means to add to it.
    /// </summary>
    /// <remarks>
    /// Cloned on the way out. The caller hands these back to a command that replaces the part's
    /// contents, and passing the live elements would have it re-parenting the very children it is
    /// about to remove.
    /// </remarks>
    internal IReadOnlyList<OpenXmlElement> ChromeElements(bool header, DocumentPageKind kind)
    {
        var main = this.Main;
        var section = main?.Document?.Body?.Elements<SectionProperties>().LastOrDefault();
        if (main is null || section is null)
            return [];

        var wanted = kind switch
        {
            DocumentPageKind.First => "first",
            DocumentPageKind.Even => "even",
            _ => "default"
        };

        var references = header
            ? section.Elements<HeaderReference>().Cast<OpenXmlElement>()
            : section.Elements<FooterReference>().Cast<OpenXmlElement>();

        var reference = references.FirstOrDefault(x => (OoxmlUnits.EnumAttribute(x, "type") ?? "default") == wanted);
        var id = header ? (reference as HeaderReference)?.Id?.Value : (reference as FooterReference)?.Id?.Value;

        var root = PartFor(main, id) switch
        {
            HeaderPart part => part.Header as OpenXmlElement,
            FooterPart part => part.Footer,
            _ => null
        };

        return root?.ChildElements.Select(x => x.CloneNode(true)).ToList() ?? [];
    }

    /// <summary>Marks the document changed after an edit that did not go through a block.</summary>
    internal void MarkChromeChanged()
    {
        this.RereadHeadersFooters();
        this.MarkChanged();
    }

    void ReportUnsupported(MainDocumentPart main)
    {
        // Everything here is preserved in the package - the viewer simply does not show it, and saying
        // so is better than letting someone assume a document has no comments because none appeared.
        if (main.Document?.Body?.Descendants<CommentRangeStart>().Any() == true)
            this.Unsupported.Report(new UnsupportedFeature("document", "Comments", UnsupportedSeverity.NotRendered));

        if (main.FootnotesPart is not null || main.EndnotesPart is not null)
            this.Unsupported.Report(new UnsupportedFeature("document", "Footnotes and endnotes", UnsupportedSeverity.NotRendered));

        if ((main.HeaderParts.Any() || main.FooterParts.Any()) && !this.HeadersFooters.HasAny)
        {
            this.Unsupported.Report(new UnsupportedFeature(
                "document", "Headers and footers", UnsupportedSeverity.NotRendered,
                "The parts are in the package but no section references them."));
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
