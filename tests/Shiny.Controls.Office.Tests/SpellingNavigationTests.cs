using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Spelling;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Stepping through a document's misspellings, which is what the Review tab's arrows drive.
/// </summary>
/// <remarks>
/// The point of the feature is that a red underline on a phone was decoration — the menu that acts on
/// one hangs off a long press, which nobody performs on a word they were not already suspicious of.
/// Walking the errors is what makes them reachable without knowing the gesture, so the walk has to
/// actually reach all of them.
/// </remarks>
public class SpellingNavigationTests
{
    static async Task<(WordDocument Document, DocumentEditorController Controller)> SetupAsync(params string[] misspelled)
    {
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);
        var controller = new DocumentEditorController(document, new FixedMeasurer(), new FakeSpellChecker(misspelled));
        controller.Resize(800, 4000);
        await controller.RefreshSpellingAsync();

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
    public async Task StepsOntoTheNextMisspellingAndSelectsIt()
    {
        var (document, controller) = await SetupAsync("Plain");
        using var _ = document;

        controller.Selection.MoveTo(DocumentPosition.Start);

        var found = await controller.GoToNextSpellingErrorAsync();

        found.ShouldNotBeNull();
        found!.Value.Word.ShouldBe("Plain");

        // Selected, not merely landed on: accepting a suggestion, ignoring and learning all operate on
        // the word rather than on a caret sitting inside it.
        controller.Selection.IsEmpty.ShouldBeFalse();
        controller.Selection.Anchor.Offset.ShouldBe(found.Value.Start);
        controller.Selection.Focus.Offset.ShouldBe(found.Value.End);
    }

    [Fact]
    public async Task WrapsAtTheEndRatherThanGoingQuiet()
    {
        // A "next" that stops at the last paragraph looks identical to one that has finished the
        // document — the user cannot tell whether the words above were ever checked.
        var (document, controller) = await SetupAsync("Plain");
        using var _ = document;

        var first = await controller.GoToNextSpellingErrorAsync();
        first.ShouldNotBeNull();

        // Park past every error, then ask again.
        controller.Selection.MoveTo(new DocumentPosition(document.Blocks.Count - 1, 0));

        var wrapped = await controller.GoToNextSpellingErrorAsync();

        wrapped.ShouldNotBeNull();
        wrapped!.Value.Word.ShouldBe("Plain");
    }

    [Fact]
    public async Task FindsTheOnlyErrorEvenWhenTheCaretIsAlreadyInIt()
    {
        var (document, controller) = await SetupAsync("Plain");
        using var _ = document;

        var first = await controller.GoToNextSpellingErrorAsync();
        first.ShouldNotBeNull();

        // The lap has to be a full one, or a document whose single error is the one under the caret
        // reports that it has none.
        var again = await controller.GoToNextSpellingErrorAsync();

        again.ShouldNotBeNull();
        again!.Value.Word.ShouldBe("Plain");
    }

    [Fact]
    public async Task BackwardsWalksTheOtherWay()
    {
        var (document, controller) = await SetupAsync("Plain", "Nested");
        using var _ = document;

        controller.Selection.MoveTo(DocumentPosition.Start);

        var forward = new List<string>();
        for (var i = 0; i < 2; i++)
            forward.Add((await controller.GoToNextSpellingErrorAsync())!.Value.Word);

        controller.Selection.MoveTo(new DocumentPosition(document.Blocks.Count - 1, 0));

        var backward = new List<string>();
        for (var i = 0; i < 2; i++)
            backward.Add((await controller.GoToNextSpellingErrorAsync(backwards: true))!.Value.Word);

        forward.ShouldBe(backward.AsEnumerable().Reverse().ToList());
    }

    [Fact]
    public async Task ReachesAnErrorThatHasNeverBeenOnScreen()
    {
        // The one that matters. The spelling pass only ever checks what is visible - nothing off
        // screen can show a squiggle - so the cache is empty for every block below the fold. A walk
        // that only read the cache stepped through a document full of misspellings and reported that
        // it had none, which is exactly what it did on a phone.
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);
        using var _ = document;

        var controller = new DocumentEditorController(document, new FixedMeasurer(), new FakeSpellChecker("Nested"));

        // A viewport tall enough for the first paragraph and no more, so "Nested" is never painted.
        controller.Resize(800, 40);
        await controller.RefreshSpellingAsync();

        controller.Selection.MoveTo(DocumentPosition.Start);

        var found = await controller.GoToNextSpellingErrorAsync();

        found.ShouldNotBeNull("the walk has to check a block before reading its errors, not trust the cache");
        found!.Value.Word.ShouldBe("Nested");
    }

    [Fact]
    public async Task ReportsNothingWhenTheDocumentIsClean()
    {
        var (document, controller) = await SetupAsync();
        using var _ = document;

        (await controller.GoToNextSpellingErrorAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task FindsNothingOnceTheWordHasBeenLearned()
    {
        // The Review arrows and the accessory bar's "Add" are two halves of one loop: learning a word
        // has to take it out of the walk, or the next tap on the arrow lands back on it.
        var (document, controller) = await SetupAsync("Plain");
        using var _ = document;

        (await controller.GoToNextSpellingErrorAsync()).ShouldNotBeNull();

        controller.LearnSpelling("Plain");
        await controller.RefreshSpellingAsync();

        (await controller.GoToNextSpellingErrorAsync()).ShouldBeNull();
    }
}
