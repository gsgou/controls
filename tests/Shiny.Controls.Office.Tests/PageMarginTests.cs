using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Document;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Page margins: what gets written, what undo puts back, and what the layout does about it.
/// </summary>
/// <remarks>
/// Two things here are only visible from a test. The first is schema order — <c>sectPr</c> is a
/// sequence, and margins written in the wrong place produce a file Word refuses to open rather than
/// one that lays out oddly. The second is the layout cache: a top-or-bottom-only change does not alter
/// the measure, which is the key the cache is built on, so nothing would re-paginate without an
/// explicit invalidation.
/// </remarks>
public class PageMarginTests
{
    sealed class Fixed : Shiny.Controls.Office.Text.ITextMeasurer
    {
        public Shiny.Controls.Office.Text.TextMetrics Measure(ReadOnlySpan<char> text, Shiny.Controls.Office.Text.TextStyle style)
            => new(text.Length * 8, style.FontSize * 0.8, style.FontSize * 0.2);

        public Shiny.Controls.Office.Text.TextMetrics LineMetrics(Shiny.Controls.Office.Text.TextStyle style)
            => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    static async Task<WordDocument> OpenAsync()
        => await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);

    static PageMargin? MarginElementOf(WordDocument document)
        => document.Main?.Document?.Body?.Elements<SectionProperties>().LastOrDefault()?.GetFirstChild<PageMargin>();

    /// <summary>A one-paragraph document whose section properties are exactly what is passed in.</summary>
    static byte[] BuildWithSection(params DocumentFormat.OpenXml.OpenXmlElement[] sectionChildren)
    {
        using var buffer = new MemoryStream();

        using (var package = WordprocessingDocument.Create(buffer, WordprocessingDocumentType.Document, autoSave: false))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

            var body = main.Document.Body!;
            body.AppendChild(new Paragraph(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Body"))));
            body.AppendChild(new SectionProperties(sectionChildren));

            main.Document.Save();
            package.Save();
        }

        return buffer.ToArray();
    }


    [Fact]
    public async Task SettingMarginsWritesTwipsAndReReadsTheSetup()
    {
        using var document = await OpenAsync();

        document.Execute(new SetPageMarginsCommand(PageMargins.Narrow));

        var element = MarginElementOf(document).ShouldNotBeNull();
        element.Left!.Value.ShouldBe(720u);
        element.Right!.Value.ShouldBe(720u);
        element.Top!.Value.ShouldBe(720);
        element.Bottom!.Value.ShouldBe(720);
        element.Header!.Value.ShouldBe(720u);
        element.Footer!.Value.ShouldBe(720u);

        // The cached geometry every paint reads has to follow, or the change is only visible after a
        // reload.
        document.Page.MarginLeft.ShouldBe(48, 0.01);
        document.Page.Margins.Bottom.ShouldBe(48, 0.01);
    }


    [Fact]
    public async Task GutterIsWrittenWhenTheElementIsNew()
    {
        using var document = await WordDocument.OpenAsync(
            new MemoryStream(BuildWithSection(new PageSize { Width = 12240, Height = 15840 })),
            editable: true);

        document.Execute(new SetPageMarginsCommand(PageMargins.Normal));

        MarginElementOf(document)!.Gutter!.Value.ShouldBe(0u);
    }


    [Fact]
    public async Task UndoPutsTheOriginalMarginsBack()
    {
        using var document = await OpenAsync();

        document.Execute(new SetPageMarginsCommand(PageMargins.Wide));
        document.Page.MarginLeft.ShouldBe(192, 0.01);

        document.Undo.Undo();

        document.Page.MarginLeft.ShouldBe(96, 0.01);
        MarginElementOf(document)!.Left!.Value.ShouldBe(1440u);

        document.Undo.Redo();
        document.Page.MarginLeft.ShouldBe(192, 0.01);
    }


    /// <summary>
    /// A document that had no <c>w:pgMar</c> goes back to having none.
    /// </summary>
    /// <remarks>
    /// The reason the inverse captures the whole element rather than six numbers: with a field-by-field
    /// inverse there is nothing to restore to, so undo would have to invent the defaults — and would
    /// leave behind an element the file never had, which a byte-comparing round trip then reports.
    /// </remarks>
    [Fact]
    public async Task UndoRemovesAMarginElementTheDocumentNeverHad()
    {
        using var document = await WordDocument.OpenAsync(
            new MemoryStream(BuildWithSection(new PageSize { Width = 12240, Height = 15840 })),
            editable: true);

        MarginElementOf(document).ShouldBeNull();

        document.Execute(new SetPageMarginsCommand(PageMargins.Moderate));
        MarginElementOf(document).ShouldNotBeNull();

        document.Undo.Undo();

        MarginElementOf(document).ShouldBeNull();
        document.Page.MarginLeft.ShouldBe(PageSetup.Letter.MarginLeft, 0.01);
    }


    /// <summary>
    /// Everything the section already carried survives a margin change.
    /// </summary>
    /// <remarks>
    /// <c>w:gutter</c> is the one that bites: a document bound for printing carries a gutter, and an
    /// element rebuilt from the six measurements would silently drop it.
    /// </remarks>
    [Fact]
    public async Task AGutterAlreadyOnTheSectionIsKept()
    {
        using var document = await WordDocument.OpenAsync(
            new MemoryStream(BuildWithSection(
                new PageSize { Width = 12240, Height = 15840 },
                new PageMargin { Left = 1440, Right = 1440, Top = 1440, Bottom = 1440, Gutter = 720 })),
            editable: true);

        document.Execute(new SetPageMarginsCommand(PageMargins.Narrow));

        MarginElementOf(document)!.Gutter!.Value.ShouldBe(720u);
    }


    [Fact]
    public async Task MarginsAreWrittenInSchemaOrderAfterThePageSize()
    {
        using var document = await WordDocument.OpenAsync(
            new MemoryStream(BuildWithSection(
                new PageSize { Width = 12240, Height = 15840 },
                new TitlePage())),
            editable: true);

        document.Execute(new SetPageMarginsCommand(PageMargins.Normal));

        var section = document.Main!.Document!.Body!.Elements<SectionProperties>().Last();
        var order = section.ChildElements.Select(x => x.GetType().Name).ToList();

        order.ShouldBe(["PageSize", "PageMargin", "TitlePage"]);
    }


    /// <summary>
    /// With no page size to sit behind, the margins still go after the header reference.
    /// </summary>
    /// <remarks>
    /// References come first in the sequence, so prepending — which is what the header commands do for
    /// their own elements — would be exactly wrong here.
    /// </remarks>
    [Fact]
    public async Task MarginsFollowTheHeaderReferenceWhenThereIsNoPageSize()
    {
        using var document = await WordDocument.OpenAsync(
            new MemoryStream(BuildWithSection(
                new HeaderReference { Type = HeaderFooterValues.Default, Id = "rId99" },
                new TitlePage())),
            editable: true);

        document.Execute(new SetPageMarginsCommand(PageMargins.Normal));

        var section = document.Main!.Document!.Body!.Elements<SectionProperties>().Last();
        var order = section.ChildElements.Select(x => x.GetType().Name).ToList();

        order.ShouldBe(["HeaderReference", "PageMargin", "TitlePage"]);
    }


    [Fact]
    public async Task ANegativeMarginIsRefused()
    {
        using var document = await OpenAsync();

        Should.Throw<ArgumentOutOfRangeException>(
            () => document.Execute(new SetPageMarginsCommand(PageMargins.Normal with { Left = -1 })));
    }


    [Fact]
    public async Task MarginsSurviveASaveAndReopen()
    {
        using var document = await OpenAsync();
        document.Execute(new SetPageMarginsCommand(PageMargins.Narrow));

        using var reopened = await WordDocument.OpenAsync(new MemoryStream(document.ToArray()));

        reopened.Page.MarginLeft.ShouldBe(48, 0.01);
        reopened.Page.MarginTop.ShouldBe(48, 0.01);
    }


    [Fact]
    public void ThePresetsAreWordsOwnMeasurements()
    {
        PageMarginPresets.All.Select(x => x.Name).ShouldBe(["Normal", "Narrow", "Moderate", "Wide"]);

        PageMargins.Normal.Left.ShouldBe(96, 0.01);
        PageMargins.Narrow.Top.ShouldBe(48, 0.01);
        PageMargins.Moderate.Left.ShouldBe(72, 0.01);
        PageMargins.Moderate.Top.ShouldBe(96, 0.01);
        PageMargins.Wide.Left.ShouldBe(192, 0.01);
        PageMargins.Wide.Top.ShouldBe(96, 0.01);
    }


    // ---- what the editor does with them ----

    static async Task<(WordDocument Document, DocumentEditorController Controller)> SetupAsync()
    {
        var document = await OpenAsync();
        var controller = new DocumentEditorController(document, new Fixed())
        {
            PageLayout = DocumentPageLayout.Print
        };

        controller.Resize(900, 600);
        return (document, controller);
    }


    [Fact]
    public async Task NarrowingTheMarginsWidensTheContentBox()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        var before = controller.ContentWidth;

        controller.SetPageMargins(PageMargins.Narrow);

        controller.PageMargins.Left.ShouldBe(48, 0.01);
        controller.ContentWidth.ShouldBe(before + 96, 0.01);
    }


    /// <summary>
    /// A change to the top and bottom alone still re-paginates.
    /// </summary>
    /// <remarks>
    /// The one case the layout cache cannot notice by itself: the measure is unchanged, so without an
    /// explicit invalidation the pages keep the boundaries they were sliced at and only the paper
    /// around them moves.
    /// </remarks>
    [Fact]
    public async Task ATopMarginChangeRepaginates()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        var width = controller.ContentWidth;
        var before = controller.Pagination.Pages[0].FlowBottom;

        controller.SetPageMargins(controller.PageMargins with { Top = 288, Bottom = 288 });

        controller.ContentWidth.ShouldBe(width, 0.01, "the measure is deliberately unchanged here");
        controller.Pagination.Pages[0].FlowBottom.ShouldBeLessThan(before);
    }


    [Fact]
    public async Task TheEditorRefusesMarginChangesOnAReadOnlyDocument()
    {
        using var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()));
        var controller = new DocumentEditorController(document, new Fixed());

        controller.SetPageMargins(PageMargins.Narrow);

        document.Page.MarginLeft.ShouldBe(96, 0.01);
    }
}
