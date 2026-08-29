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
/// Sets the section's page margins.
/// </summary>
/// <remarks>
/// <para>
/// The whole <c>w:pgMar</c> element is captured before the write and handed back as the inverse, which
/// is what makes undo total: a document that had no margins element at all is restored to having none
/// rather than to the defaults this command would otherwise have to guess at. It also preserves
/// <c>w:gutter</c> and anything else Word wrote there, which a field-by-field inverse would quietly
/// drop.
/// </para>
/// <para>
/// The last section's properties, matching how <see cref="WordDocument.Page"/> is read. Multi-section
/// documents are not modelled — the reader takes one page setup for the document, so writing per
/// section would produce a document the view could not show.
/// </para>
/// </remarks>
public sealed record SetPageMarginsCommand(PageMargins Margins) : DocumentCommand
{
    public override string Name => "Page margins";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        ArgumentNullException.ThrowIfNull(this.Margins);

        if (!this.Margins.IsValid)
            throw new ArgumentOutOfRangeException(nameof(this.Margins), "A page margin must be a finite, non-negative length.");

        var section = context.SectionProperties(create: true);
        if (section is null)
            return new NoOpCommand();

        var existing = section.GetFirstChild<PageMargin>();
        var inverse = new RestorePageMarginCommand(existing?.CloneNode(true) as PageMargin);

        var margin = existing;

        if (margin is null)
        {
            margin = new PageMargin();
            Place(section, margin);
        }

        Write(margin, this.Margins);

        context.MarkPageSetupChanged();
        return inverse;
    }

    /// <summary>Writes the six measurements, leaving whatever else the element carries alone.</summary>
    internal static void Write(PageMargin margin, PageMargins values)
    {
        // w:top and w:bottom are signed in the schema and the sides are not, which is not an
        // inconsistency to paper over: a negative top margin is how Word puts content in the header
        // band, and there is no equivalent sideways.
        margin.Top = OoxmlUnits.PixelsToTwips(values.Top);
        margin.Bottom = OoxmlUnits.PixelsToTwips(values.Bottom);
        margin.Left = (uint)OoxmlUnits.PixelsToTwips(values.Left);
        margin.Right = (uint)OoxmlUnits.PixelsToTwips(values.Right);
        margin.Header = (uint)OoxmlUnits.PixelsToTwips(values.Header);
        margin.Footer = (uint)OoxmlUnits.PixelsToTwips(values.Footer);

        // Optional in the schema, required in practice: Word writes it on every section and a pgMar
        // without it round-trips back with one anyway.
        if (margin.Gutter is null)
            margin.Gutter = 0U;
    }

    /// <summary>
    /// Adds a <c>w:pgMar</c> in the one place the schema allows it.
    /// </summary>
    /// <remarks>
    /// <c>sectPr</c> is a sequence, not a bag: the header and footer references come first, then the
    /// paper size, then the margins. Appending would put the margins after <c>w:titlePg</c> in a
    /// document that has one, and Word refuses to open a section whose children are out of order —
    /// which is a corrupt-file dialog, not a layout bug.
    /// </remarks>
    internal static void Place(SectionProperties section, PageMargin margin)
    {
        if (section.GetFirstChild<PageSize>() is { } size)
        {
            size.InsertAfterSelf(margin);
            return;
        }

        var anchor = section.ChildElements.LastOrDefault(x =>
            x is HeaderReference or FooterReference or FootnoteProperties or EndnoteProperties or SectionType);

        if (anchor is not null)
            anchor.InsertAfterSelf(margin);
        else
            section.PrependChild(margin);
    }
}


/// <summary>
/// Turns the paper, swapping the two dimensions and recording which way round it now is.
/// </summary>
/// <remarks>
/// Both halves matter. Swapping the dimensions without <c>w:orient</c> gives a page the right shape
/// that Word still calls portrait — so its own Orientation control shows the wrong state, and the next
/// change from there flips it the wrong way. Writing the attribute without swapping gives a section
/// that claims landscape on portrait paper, which Word obeys by re-swapping on open.
/// </remarks>
public sealed record SetPageOrientationCommand(PageOrientation Orientation) : DocumentCommand
{
    public override string Name => "Page orientation";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var section = context.SectionProperties(create: true);
        if (section is null)
            return new NoOpCommand();

        var existing = section.GetFirstChild<PageSize>();
        var inverse = new RestorePageSizeCommand(existing?.CloneNode(true) as PageSize);

        var size = existing;

        if (size is null)
        {
            // Seeded from the setup rather than from nothing: a section with no w:pgSz is Letter by
            // Word's own default, and writing the dimensions we are about to swap keeps the two in step.
            var setup = context.Page;

            size = new PageSize
            {
                Width = (uint)OoxmlUnits.PixelsToTwips(setup.Width),
                Height = (uint)OoxmlUnits.PixelsToTwips(setup.Height)
            };

            // w:pgSz leads the geometry in the sequence, before w:pgMar.
            if (section.GetFirstChild<PageMargin>() is { } margin)
                margin.InsertBeforeSelf(size);
            else
                Place(section, size);
        }

        var width = size.Width?.Value ?? 0;
        var height = size.Height?.Value ?? 0;

        var landscape = this.Orientation == PageOrientation.Landscape;
        var wantsWider = landscape;

        // Only swap when the paper is not already that way round. Asking for landscape twice must not
        // turn the page back.
        if (width > 0 && height > 0 && (width > height) != wantsWider)
        {
            size.Width = height;
            size.Height = width;
        }

        size.Orient = landscape ? PageOrientationValues.Landscape : PageOrientationValues.Portrait;

        context.MarkPageSetupChanged();
        return inverse;
    }

    /// <summary>Puts w:pgSz in the one place the schema allows it, when the section has none.</summary>
    static void Place(SectionProperties section, PageSize size)
    {
        var anchor = section.ChildElements.LastOrDefault(x =>
            x is HeaderReference or FooterReference or FootnoteProperties or EndnoteProperties or SectionType);

        if (anchor is not null)
            anchor.InsertAfterSelf(size);
        else
            section.PrependChild(size);
    }
}


/// <summary>Puts back the <c>w:pgSz</c> that was there — or the absence of one.</summary>
public sealed record RestorePageSizeCommand(PageSize? Previous) : DocumentCommand
{
    public override string Name => "Page orientation";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var section = context.SectionProperties(create: false);
        if (section is null)
            return new NoOpCommand();

        var current = section.GetFirstChild<PageSize>();
        var inverse = new RestorePageSizeCommand(current?.CloneNode(true) as PageSize);

        current?.Remove();

        if (this.Previous is not null)
        {
            var restored = (PageSize)this.Previous.CloneNode(true);

            if (section.GetFirstChild<PageMargin>() is { } margin)
                margin.InsertBeforeSelf(restored);
            else
                section.PrependChild(restored);
        }

        context.MarkPageSetupChanged();
        return inverse;
    }
}


/// <summary>Puts back the <c>w:pgMar</c> that was there — or the absence of one. The inverse of a margin change.</summary>
public sealed record RestorePageMarginCommand(PageMargin? Previous) : DocumentCommand
{
    public override string Name => "Page margins";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var section = context.SectionProperties(create: this.Previous is not null);
        if (section is null)
            return new NoOpCommand();

        var existing = section.GetFirstChild<PageMargin>();
        var inverse = new RestorePageMarginCommand(existing?.CloneNode(true) as PageMargin);

        if (this.Previous is null)
        {
            existing?.Remove();
        }
        else if (existing is not null)
        {
            existing.InsertAfterSelf(this.Previous.CloneNode(true));
            existing.Remove();
        }
        else
        {
            SetPageMarginsCommand.Place(section, (PageMargin)this.Previous.CloneNode(true));
        }

        context.MarkPageSetupChanged();
        return inverse;
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
