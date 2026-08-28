using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WText = DocumentFormat.OpenXml.Wordprocessing.Text;
using Shiny.Controls.Office.Editing;

namespace Shiny.Controls.Office.Document;

/// <summary>Inserts a page break at a position.</summary>
/// <remarks>
/// A page break is a run holding <c>w:br w:type="page"</c>, and it contributes no characters to the
/// paragraph's text — the reader projects it as a zero-width break, exactly as it does a line break.
/// That is why the inverse removes the element rather than deleting a one-character range: deleting a
/// range here would take a real character with it.
/// </remarks>
public sealed record InsertPageBreakCommand(DocumentPosition At) : DocumentCommand
{
    public override string Name => "Insert page break";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var paragraph = context.ParagraphElementAt(this.At.Block);
        if (paragraph is null)
            return new NoOpCommand();

        var run = new Run(new Break { Type = BreakValues.Page });
        WordParagraphEditor.InsertObject(paragraph, this.At.Offset, run);
        context.Reproject(this.At.Block);

        return new RemoveElementCommand(this.At.Block, run);
    }
}


/// <summary>Removes one element from a paragraph. The inverse of an insertion that added no text.</summary>
public sealed record RemoveElementCommand(int Block, OpenXmlElement Element) : DocumentCommand
{
    public override string Name => "Remove";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var parent = this.Element.Parent;
        if (parent is null)
            return new NoOpCommand();

        var anchor = this.Element.PreviousSibling();
        this.Element.Remove();
        context.Reproject(this.Block);

        return new RestoreElementCommand(this.Block, this.Element, parent, anchor);
    }
}


/// <summary>Puts a removed element back where it was.</summary>
public sealed record RestoreElementCommand(int Block, OpenXmlElement Element, OpenXmlElement Parent, OpenXmlElement? After)
    : DocumentCommand
{
    public override string Name => "Restore";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        if (this.After is not null && this.After.Parent == this.Parent)
            this.After.InsertAfterSelf(this.Element);
        else if (this.Parent.FirstChild is { } first)
            first.InsertBeforeSelf(this.Element);
        else
            this.Parent.AppendChild(this.Element);

        context.Reproject(this.Block);
        return new RemoveElementCommand(this.Block, this.Element);
    }
}


/// <summary>
/// Replaces one header or footer wholesale, creating or removing the part as needed.
/// </summary>
/// <remarks>
/// <para>
/// Coarse on purpose. A header is a handful of paragraphs that an app sets rather than edits, so the
/// undo unit is "the header as it was" — captured as XML before the change, restored as XML after.
/// The alternative, modelling a header as a second editable body with its own selection and caret,
/// is a much larger feature and is not what this is.
/// </para>
/// <para>
/// Passing null content removes the part and its reference, which is how a header is cleared.
/// </para>
/// </remarks>
public sealed record SetHeaderFooterCommand(
    bool IsHeader,
    DocumentPageKind Kind,
    IReadOnlyList<OpenXmlElement>? Content
) : DocumentCommand
{
    public override string Name => this.IsHeader ? "Set header" : "Set footer";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var main = context.Main;
        var section = context.SectionProperties(create: this.Content is not null);

        if (main is null || section is null)
            return new NoOpCommand();

        var previous = Capture(main, section, this.IsHeader, this.Kind);

        if (this.Content is null)
            Remove(main, section, this.IsHeader, this.Kind);
        else
            Write(main, section, this.IsHeader, this.Kind, this.Content);

        context.MarkChromeChanged();
        return previous;
    }

    /// <summary>The command that puts back whatever is there now.</summary>
    static SetHeaderFooterCommand Capture(MainDocumentPart main, SectionProperties section, bool isHeader, DocumentPageKind kind)
    {
        var root = RootOf(main, section, isHeader, kind);
        if (root is null)
            return new SetHeaderFooterCommand(isHeader, kind, null);

        // Cloned, because the elements themselves are about to be replaced or removed and an undo
        // holding the live ones would restore an empty shell.
        var content = root.ChildElements.Select(x => x.CloneNode(true)).ToList();
        return new SetHeaderFooterCommand(isHeader, kind, content);
    }

    static OpenXmlElement? RootOf(MainDocumentPart main, SectionProperties section, bool isHeader, DocumentPageKind kind)
    {
        var reference = ReferenceFor(section, isHeader, kind);
        var id = isHeader ? (reference as HeaderReference)?.Id?.Value : (reference as FooterReference)?.Id?.Value;

        if (String.IsNullOrEmpty(id))
            return null;

        try
        {
            return main.GetPartById(id) switch
            {
                HeaderPart header => header.Header,
                FooterPart footer => footer.Footer,
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    static OpenXmlElement? ReferenceFor(SectionProperties section, bool isHeader, DocumentPageKind kind)
    {
        var wanted = kind switch
        {
            DocumentPageKind.First => "first",
            DocumentPageKind.Even => "even",
            _ => "default"
        };

        var references = isHeader
            ? section.Elements<HeaderReference>().Cast<OpenXmlElement>()
            : section.Elements<FooterReference>().Cast<OpenXmlElement>();

        return references.FirstOrDefault(x => (OoxmlUnits.EnumAttribute(x, "type") ?? "default") == wanted);
    }

    static void Write(MainDocumentPart main, SectionProperties section, bool isHeader, DocumentPageKind kind, IReadOnlyList<OpenXmlElement> content)
    {
        var root = RootOf(main, section, isHeader, kind);

        if (root is null)
        {
            if (isHeader)
            {
                var part = main.AddNewPart<HeaderPart>();
                part.Header = new Header();
                root = part.Header;

                // References come before the page size and margins in sectPr; Word rejects a
                // document whose sectPr children are out of schema order.
                section.PrependChild(new HeaderReference { Type = TypeOf(kind), Id = main.GetIdOfPart(part) });
            }
            else
            {
                var part = main.AddNewPart<FooterPart>();
                part.Footer = new Footer();
                root = part.Footer;
                section.PrependChild(new FooterReference { Type = TypeOf(kind), Id = main.GetIdOfPart(part) });
            }
        }

        root.RemoveAllChildren();
        foreach (var element in content)
            root.AppendChild(element.CloneNode(true));
    }

    static void Remove(MainDocumentPart main, SectionProperties section, bool isHeader, DocumentPageKind kind)
    {
        var reference = ReferenceFor(section, isHeader, kind);
        if (reference is null)
            return;

        var id = isHeader ? (reference as HeaderReference)?.Id?.Value : (reference as FooterReference)?.Id?.Value;
        reference.Remove();

        if (String.IsNullOrEmpty(id))
            return;

        try
        {
            // The part goes too. Leaving it orphaned is valid OOXML but it is dead weight that Word
            // will keep round-tripping forever.
            main.DeletePart(id);
        }
        catch (Exception)
        {
        }
    }

    static HeaderFooterValues TypeOf(DocumentPageKind kind) => kind switch
    {
        DocumentPageKind.First => HeaderFooterValues.First,
        DocumentPageKind.Even => HeaderFooterValues.Even,
        _ => HeaderFooterValues.Default
    };
}


/// <summary>Builds the OOXML a header, footer or page number is made of.</summary>
public static class WordPageChrome
{
    /// <summary>A single paragraph of plain text at the given alignment.</summary>
    public static Paragraph TextParagraph(string text, PageNumberPosition alignment)
    {
        var paragraph = new Paragraph(new Run(new WText(text) { Space = SpaceProcessingModeValues.Preserve }));
        paragraph.PrependChild(new ParagraphProperties(new Justification { Val = JustificationOf(alignment) }));
        return paragraph;
    }

    /// <summary>
    /// A paragraph carrying a page number.
    /// </summary>
    /// <remarks>
    /// Written as <c>w:fldSimple</c> rather than the begin/instrText/separate/end run sequence. Both
    /// are valid and Word reads either; the simple form is one element instead of five and cannot be
    /// left half-written by an edit that goes wrong in the middle.
    /// </remarks>
    public static Paragraph PageNumberParagraph(PageNumberPosition alignment, PageNumberFormat format)
    {
        var paragraph = new Paragraph();
        paragraph.AppendChild(new ParagraphProperties(new Justification { Val = JustificationOf(alignment) }));

        if (format == PageNumberFormat.PageOfCount)
            paragraph.AppendChild(new Run(new WText("Page ") { Space = SpaceProcessingModeValues.Preserve }));

        paragraph.AppendChild(Field(" PAGE ", "1"));

        if (format == PageNumberFormat.PageOfCount)
        {
            paragraph.AppendChild(new Run(new WText(" of ") { Space = SpaceProcessingModeValues.Preserve }));
            paragraph.AppendChild(Field(" NUMPAGES ", "1"));
        }

        return paragraph;
    }

    /// <summary>
    /// A field with a cached result.
    /// </summary>
    /// <remarks>
    /// The cached text matters: it is what any other reader shows until it recalculates fields, and
    /// what this renderer falls back to for a field it cannot resolve. An empty one produces a
    /// document that looks like it has lost its page numbers everywhere but here.
    /// </remarks>
    static SimpleField Field(string instruction, string cached)
        => new(new Run(new WText(cached))) { Instruction = instruction };

    static JustificationValues JustificationOf(PageNumberPosition alignment) => alignment switch
    {
        PageNumberPosition.Center => JustificationValues.Center,
        PageNumberPosition.Right => JustificationValues.Right,
        _ => JustificationValues.Left
    };
}
