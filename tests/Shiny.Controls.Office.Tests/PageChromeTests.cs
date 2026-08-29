using Shiny.Controls.Office.Document;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// The page chrome the document editor's Insert tab drives: the running head and foot, the number on
/// every page, and the break between two of them.
/// </summary>
/// <remarks>
/// <see cref="DocumentEditorController.ChromeText"/> is what seeds the editor with what is already
/// there, so a header that cannot be read back is a header the user has to retype from scratch every
/// time they want to change a word of it.
/// </remarks>
public class PageChromeTests
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
    public async Task AHeaderReadsBackAsWhatWasSet()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.ChromeText(header: true).ShouldBeNull("the fixture has no header");

        controller.SetHeaderText("Field Report 2026");

        controller.ChromeText(header: true).ShouldBe("Field Report 2026");
    }

    [Fact]
    public async Task AFooterIsItsOwnStoryFromTheHeader()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetHeaderText("Top");
        controller.SetFooterText("Bottom");

        controller.ChromeText(header: true).ShouldBe("Top");
        controller.ChromeText(header: false).ShouldBe("Bottom");
    }

    [Fact]
    public async Task AnEmptyLineTakesTheHeaderAwayAgain()
    {
        // The only way back out of having one, which is why the toolbar maps blank to null rather than
        // writing an empty paragraph that still reserves the band.
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetHeaderText("Temporary");
        controller.ChromeText(header: true).ShouldNotBeNull();

        controller.SetHeaderText(null);
        controller.ChromeText(header: true).ShouldBeNull();
    }

    [Fact]
    public async Task SettingAHeaderIsUndoable()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetHeaderText("Field Report 2026");
        controller.ChromeText(header: true).ShouldBe("Field Report 2026");

        controller.Undo();

        controller.ChromeText(header: true).ShouldBeNull();
    }

    [Fact]
    public async Task APageNumberGoesWhereItWasAsked()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.InsertPageNumber(PageNumberPlacement.Header, PageNumberPosition.Right);

        // The field resolves per page, so what comes back is the text it last stood for rather than
        // the field code - enough to prove the header is no longer empty.
        controller.ChromeText(header: true).ShouldNotBeNull();
        controller.ChromeText(header: false).ShouldBeNull("the number was asked for in the header");
    }

    [Fact]
    public async Task APageNumberDoesNotDiscardTheHeaderAlreadyThere()
    {
        // Both write the same part, so the second one has to append rather than replace - otherwise
        // adding a number silently deletes the title the user just typed.
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.SetHeaderText("Field Report 2026");
        controller.InsertPageNumber(PageNumberPlacement.Header, PageNumberPosition.Right);

        controller.ChromeText(header: true).ShouldContain("Field Report 2026");
    }

    [Fact]
    public async Task APageBreakSplitsTheDocumentOntoASecondPage()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.PageLayout = DocumentPageLayout.Print;
        var before = controller.Pagination.Pages.Count;

        controller.Selection.MoveTo(DocumentPosition.Start);
        controller.InsertPageBreak();

        controller.Pagination.Pages.Count.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task PrintAndReflowAreDifferentLayouts()
    {
        // The toolbar's toggle flips exactly this, and the two differ in whether there are pages at
        // all - which is what the header and footer hang off.
        var (document, controller) = await SetupAsync();
        using var _ = document;

        controller.PageLayout = DocumentPageLayout.Print;
        controller.IsPaginated.ShouldBeTrue();

        controller.PageLayout = DocumentPageLayout.Reflow;
        controller.IsPaginated.ShouldBeFalse();
    }
}
