using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Document;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Turning the paper, which is two things at once: the dimensions swap and the section says so.
/// </summary>
/// <remarks>
/// Doing only one is the failure worth testing. Swapping without <c>w:orient</c> gives a page the
/// right shape that Word still calls portrait; writing the attribute without swapping gives a section
/// that claims landscape on portrait paper, which Word obeys by re-swapping it on open.
/// </remarks>
public class PageOrientationTests
{
    static async Task<(WordDocument Document, DocumentEditorController Controller)> SetupAsync()
    {
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);
        var controller = new DocumentEditorController(document, new FixedMeasurer());
        controller.Resize(800, 1200);

        return (document, controller);
    }

    sealed class FixedMeasurer : Shiny.Controls.Office.Text.ITextMeasurer
    {
        public Shiny.Controls.Office.Text.TextMetrics Measure(ReadOnlySpan<char> text, Shiny.Controls.Office.Text.TextStyle style)
            => new(text.Length * 8, style.FontSize * 0.8, style.FontSize * 0.2);

        public Shiny.Controls.Office.Text.TextMetrics LineMetrics(Shiny.Controls.Office.Text.TextStyle style)
            => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    [Fact]
    public async Task ADocumentStartsPortrait()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.PageOrientation.ShouldBe(PageOrientation.Portrait);
        document.Page.Width.ShouldBeLessThan(document.Page.Height);
    }

    [Fact]
    public async Task LandscapeSwapsTheDimensions()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        var portraitWidth = document.Page.Width;
        var portraitHeight = document.Page.Height;

        controller.SetPageOrientation(PageOrientation.Landscape);

        controller.PageOrientation.ShouldBe(PageOrientation.Landscape);
        document.Page.Width.ShouldBe(portraitHeight, 0.5);
        document.Page.Height.ShouldBe(portraitWidth, 0.5);
    }

    [Fact]
    public async Task AskingForTheSameOrientationTwiceDoesNotTurnItBack()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetPageOrientation(PageOrientation.Landscape);
        var width = document.Page.Width;

        controller.SetPageOrientation(PageOrientation.Landscape);

        document.Page.Width.ShouldBe(width, 0.5);
    }

    [Fact]
    public async Task TheSectionSaysLandscapeAsWellAsBeingIt()
    {
        // Both halves, or Word's own Orientation control shows the wrong state and the next change
        // from there flips the page the wrong way.
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetPageOrientation(PageOrientation.Landscape);

        using var saved = new MemoryStream();
        await document.SaveToAsync(saved);

        saved.Position = 0;
        using var reopened = WordprocessingDocument.Open(saved, false);

        var section = reopened.MainDocumentPart!.Document!.Body!.Elements<SectionProperties>().Last();
        var size = section.GetFirstChild<PageSize>()!;

        size.Orient!.Value.ShouldBe(PageOrientationValues.Landscape);
        size.Width!.Value.ShouldBeGreaterThan(size.Height!.Value);
    }

    [Fact]
    public async Task TurningThePaperIsUndoable()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        var width = document.Page.Width;

        controller.SetPageOrientation(PageOrientation.Landscape);
        controller.Undo();

        controller.PageOrientation.ShouldBe(PageOrientation.Portrait);
        document.Page.Width.ShouldBe(width, 0.5);
    }

    [Fact]
    public async Task LandscapeSurvivesASaveAndReopen()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetPageOrientation(PageOrientation.Landscape);

        using var saved = new MemoryStream();
        await document.SaveToAsync(saved);

        saved.Position = 0;
        using var reopened = await WordDocument.OpenAsync(saved, editable: true);

        reopened.Page.Orientation.ShouldBe(PageOrientation.Landscape);
        reopened.Page.Width.ShouldBeGreaterThan(reopened.Page.Height);
    }
}
