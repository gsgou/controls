using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Spelling;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// A checker that flags exactly the words it is told to.
/// </summary>
/// <remarks>
/// Stands in for the platform ones, which cannot run in a test at all: they are UIKit, AppKit, an
/// Android service and a COM object. What is portable — the tokenizer's exclusions, the per-paragraph
/// cache, the ignore list, and the edit an accepted suggestion makes — is what these tests cover.
/// </remarks>
sealed class FakeSpellChecker(params string[] misspelled) : SpellCheckerBase
{
    readonly HashSet<string> words = new(misspelled, StringComparer.OrdinalIgnoreCase);

    public override bool IsAvailable { get; } = true;

    /// <summary>How many times the checker was actually asked, so caching can be proved.</summary>
    public int Checks { get; private set; }

    public List<string> Learned { get; } = [];

    protected override ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken)
    {
        this.Checks++;

        IReadOnlyList<SpellingError> found = SpellingTokenizer
            .Words(text)
            .Select(x => new SpellingError(x.Start, x.Length, text.Substring(x.Start, x.Length)))
            .Where(x => this.words.Contains(x.Word))
            .ToList();

        return new ValueTask<IReadOnlyList<SpellingError>>(found);
    }

    protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken)
        => new(new[] { word.ToUpperInvariant(), word + "!" });

    public override void Learn(string word)
    {
        base.Learn(word);
        this.Learned.Add(word);
    }
}

public class SpellingTokenizerTests
{
    static string[] WordsIn(string text)
        => SpellingTokenizer.Words(text).Select(x => text.Substring(x.Start, x.Length)).ToArray();

    [Fact]
    public void SplitsOnNonLetters()
        => WordsIn("The quick, brown fox.").ShouldBe(["The", "quick", "brown", "fox"]);

    [Fact]
    public void KeepsContractionsAndHyphensWhole()
        => WordsIn("A well-known thing you don't question").ShouldBe(["well-known", "thing", "you", "don't", "question"]);

    [Fact]
    public void TrailingApostropheDoesNotGlueToTheNextWord()
        => WordsIn("the dogs' bowls").ShouldBe(["the", "dogs", "bowls"]);

    /// <summary>No dictionary has these, so checking them underlines the whole document.</summary>
    [Theory]
    [InlineData("Ship it to NASA today", "NASA")]
    [InlineData("Call getUserName first", "getUserName")]
    [InlineData("The XmlHttpRequest object", "XmlHttpRequest")]
    public void SkipsWordsNoDictionaryCouldKnow(string text, string excluded)
        => WordsIn(text).ShouldNotContain(excluded);

    [Fact]
    public void SkipsSingleLetters()
        => WordsIn("a b see").ShouldBe(["see"]);

    [Fact]
    public void WordAtFindsTheWordUnderAnOffset()
    {
        const string text = "The quick brown fox";

        SpellingTokenizer.WordAt(text, 5).ShouldBe((4, 5));
        SpellingTokenizer.WordAt(text, 4).ShouldBe((4, 5));
        SpellingTokenizer.WordAt(text, 9).ShouldBe((4, 5));
    }

    [Theory]
    [InlineData("See https://shinylib.net/controls for more")]
    [InlineData("Mail allan@example.com about it")]
    [InlineData("Open src/Shiny.Maui.Controls now")]
    [InlineData(@"Open C:\Users\aritchie now")]
    public void RecognisesTokensThatAreNotProse(string text)
    {
        // The offset of the second word, which is the one inside the URL/path in each case.
        var offset = text.IndexOf(' ') + 1;
        SpellingTokenizer.IsInsideUri(text, offset).ShouldBeTrue();
    }

    [Fact]
    public void OrdinaryProseIsNotMistakenForAUri()
        => SpellingTokenizer.IsInsideUri("just some ordinary words", 5).ShouldBeFalse();
}

public class SpellCheckerBaseTests
{
    [Fact]
    public async Task IgnoredWordsAreFilteredCentrally()
    {
        var checker = new FakeSpellChecker("teh");

        (await checker.CheckAsync("teh cat")).Count.ShouldBe(1);

        checker.Ignore("TEH");
        (await checker.CheckAsync("teh cat")).ShouldBeEmpty();
    }

    [Fact]
    public async Task LearningAWordAlsoStopsReportingIt()
    {
        var checker = new FakeSpellChecker("Shiny");
        checker.Learn("Shiny");

        (await checker.CheckAsync("Shiny controls")).ShouldBeEmpty();
        checker.Learned.ShouldBe(["Shiny"]);
    }

    [Fact]
    public async Task TheNullCheckerReportsNothingAndSaysSo()
    {
        NullSpellChecker.Instance.IsAvailable.ShouldBeFalse();

        (await NullSpellChecker.Instance.CheckAsync("teh cat")).ShouldBeEmpty();
        (await NullSpellChecker.Instance.SuggestAsync("teh")).ShouldBeEmpty();
    }

    /// <summary>
    /// A host package registers its platform checker at startup, which must not overwrite a choice the
    /// application made first.
    /// </summary>
    [Fact]
    public void RegisteringADefaultNeverOverridesAnExplicitOne()
    {
        var original = SpellCheckers.Default;

        try
        {
            var mine = new FakeSpellChecker();
            SpellCheckers.Default = mine;

            SpellCheckers.SetDefaultIfUnset(new FakeSpellChecker());
            SpellCheckers.Default.ShouldBeSameAs(mine);

            // Back to unset, and the platform one is taken.
            SpellCheckers.Default = null!;
            var platform = new FakeSpellChecker();
            SpellCheckers.SetDefaultIfUnset(platform);
            SpellCheckers.Default.ShouldBeSameAs(platform);
        }
        finally
        {
            // The default is process-wide: leaving a fake behind would silently change every later test.
            SpellCheckers.Default = original;
        }
    }
}

public class DocumentSpellCheckTests
{
    static async Task<WordDocument> OpenAsync()
        => await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);

    static int BodyBlock(WordDocument document)
        => document.Blocks.ToList().FindIndex(x => x is DocumentParagraph p && p.PlainText.StartsWith("Plain body text"));

    [Fact]
    public async Task UnchangedParagraphsAreNotCheckedTwice()
    {
        using var document = await OpenAsync();
        var checker = new FakeSpellChecker("Plain");
        var spelling = new DocumentSpellCheck(checker);
        var blocks = document.Blocks;

        await spelling.RefreshAsync(blocks, 0, blocks.Count - 1);
        var first = checker.Checks;
        first.ShouldBeGreaterThan(0);

        await spelling.RefreshAsync(blocks, 0, blocks.Count - 1);
        checker.Checks.ShouldBe(first);
    }

    [Fact]
    public async Task EditingAParagraphRechecksIt()
    {
        using var document = await OpenAsync();
        var checker = new FakeSpellChecker("Plain");
        var spelling = new DocumentSpellCheck(checker);
        var block = BodyBlock(document);

        await spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);
        var before = checker.Checks;

        document.Execute(new InsertTextCommand(new DocumentPosition(block, 0), "X"));
        await spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);

        // Only the edited paragraph: its text is the cache key, and nothing else changed.
        checker.Checks.ShouldBe(before + 1);
    }

    [Fact]
    public async Task ErrorsAreReportedAtParagraphOffsets()
    {
        using var document = await OpenAsync();
        var spelling = new DocumentSpellCheck(new FakeSpellChecker("Plain"));
        var block = BodyBlock(document);
        var text = ((DocumentParagraph)document.Blocks[block]).PlainText;

        await spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);

        var errors = spelling.ErrorsFor(block, text);
        errors.Count.ShouldBe(1);
        errors[0].Start.ShouldBe(0);
        errors[0].Word.ShouldBe("Plain");
        text.Substring(errors[0].Start, errors[0].Length).ShouldBe("Plain");
    }

    [Fact]
    public async Task ADisabledOrUnavailableCheckerReportsNothing()
    {
        using var document = await OpenAsync();
        var checker = new FakeSpellChecker("Plain");
        var spelling = new DocumentSpellCheck(checker) { IsEnabled = false };

        await spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);

        checker.Checks.ShouldBe(0);
        spelling.ErrorsFor(BodyBlock(document), "Plain body text").ShouldBeEmpty();
    }

    [Fact]
    public async Task InvalidatingFromABlockDropsThatBlockAndEverythingAfterIt()
    {
        using var document = await OpenAsync();
        var checker = new FakeSpellChecker("Plain");
        var spelling = new DocumentSpellCheck(checker);
        var block = BodyBlock(document);
        var text = ((DocumentParagraph)document.Blocks[block]).PlainText;

        await spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);
        spelling.ErrorsFor(block, text).ShouldNotBeEmpty();

        spelling.InvalidateFrom(block);
        spelling.ErrorsFor(block, text).ShouldBeEmpty();
    }
}

public class DocumentEditorSpellingTests
{
    sealed class Fixed : Shiny.Controls.Office.Text.ITextMeasurer
    {
        public Shiny.Controls.Office.Text.TextMetrics Measure(ReadOnlySpan<char> text, Shiny.Controls.Office.Text.TextStyle style)
            => new(text.Length * 8, style.FontSize * 0.8, style.FontSize * 0.2);

        public Shiny.Controls.Office.Text.TextMetrics LineMetrics(Shiny.Controls.Office.Text.TextStyle style)
            => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    static async Task<(WordDocument Document, DocumentEditorController Controller, int Block)> SetupAsync(FakeSpellChecker checker)
    {
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);
        var controller = new DocumentEditorController(document, new Fixed(), checker);
        controller.Resize(800, 4000);

        var block = document.Blocks.ToList().FindIndex(x => x is DocumentParagraph p && p.PlainText.StartsWith("Plain body text"));
        await controller.Spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);

        return (document, controller, block);
    }

    [Fact]
    public async Task TheErrorUnderTheCaretIsFound()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        controller.SpellingErrorAt(new DocumentPosition(block, 2))!.Value.Word.ShouldBe("Plain");
        controller.SpellingErrorAt(new DocumentPosition(block, 40)).ShouldBeNull();
    }

    [Fact]
    public async Task SuggestionsComeFromTheCheckerForTheWordUnderTheCaret()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        var suggestions = await controller.SuggestAtAsync(new DocumentPosition(block, 2));
        suggestions.ShouldBe(["PLAIN", "Plain!"]);
    }

    [Fact]
    public async Task AcceptingASuggestionReplacesTheWordAsOneUndoStep()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        var before = ((DocumentParagraph)document.Blocks[block]).PlainText;

        controller.ApplySuggestion(new DocumentPosition(block, 2), "PLAIN").ShouldBeTrue();
        ((DocumentParagraph)document.Blocks[block]).PlainText.ShouldBe("PLAIN" + before[5..]);

        // Delete-then-insert is two commands; one Ctrl+Z has to undo the correction, not half of it.
        document.Undo.Undo();
        ((DocumentParagraph)document.Blocks[block]).PlainText.ShouldBe(before);
    }

    [Fact]
    public async Task AcceptingASuggestionLeavesTheCaretAfterTheNewWord()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        controller.ApplySuggestion(new DocumentPosition(block, 2), "Clear");

        controller.Selection.Focus.Block.ShouldBe(block);
        controller.Selection.Focus.Offset.ShouldBe("Clear".Length);
    }

    [Fact]
    public async Task ThereIsNothingToApplyWhereThereIsNoError()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        controller.ApplySuggestion(new DocumentPosition(block, 40), "anything").ShouldBeFalse();
    }

    [Fact]
    public async Task IgnoringAWordClearsItsUnderlineEverywhere()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, block) = await SetupAsync(checker);
        using var _ = document;

        controller.IgnoreSpelling("Plain");
        await controller.Spelling.RefreshAsync(document.Blocks, 0, document.Blocks.Count - 1);

        controller.SpellingErrorAt(new DocumentPosition(block, 2)).ShouldBeNull();
    }

    /// <summary>A wrapped misspelling is underlined per line, never as one rectangle across the page.</summary>
    [Fact]
    public async Task MisspellingsProduceUnderlineRectangles()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller, _) = await SetupAsync(checker);
        using var _d = document;

        var rects = controller.SpellingRects().ToList();

        rects.ShouldNotBeEmpty();
        rects.ShouldAllBe(r => r.Width > 0 && r.Height > 0);
    }
}

/// <summary>
/// The editor-facing switches: which underlines get drawn, and what happens when the checker or the
/// enabled flag changes underneath results that have already been found.
/// </summary>
public class SpellingSurfaceTests
{
    sealed class Fixed : Shiny.Controls.Office.Text.ITextMeasurer
    {
        public Shiny.Controls.Office.Text.TextMetrics Measure(ReadOnlySpan<char> text, Shiny.Controls.Office.Text.TextStyle style)
            => new(text.Length * 8, style.FontSize * 0.8, style.FontSize * 0.2);

        public Shiny.Controls.Office.Text.TextMetrics LineMetrics(Shiny.Controls.Office.Text.TextStyle style)
            => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    static async Task<(WordDocument Document, DocumentEditorController Controller)> SetupAsync(ISpellChecker checker)
    {
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);
        var controller = new DocumentEditorController(document, new Fixed(), checker);
        controller.Resize(800, 4000);
        await controller.RefreshSpellingAsync();

        return (document, controller);
    }

    [Fact]
    public async Task EachMisspellingGetsAnUnderlineWithRealDimensions()
    {
        var (document, controller) = await SetupAsync(new FakeSpellChecker("Plain"));
        using var _ = document;

        var rects = controller.SpellingRects().ToList();

        rects.ShouldNotBeEmpty();
        rects.ShouldAllBe(r => r.Width > 0 && r.Height > 0);
    }

    [Fact]
    public async Task ReplacingTheCheckerDropsTheOldOnesFindings()
    {
        var (document, controller) = await SetupAsync(new FakeSpellChecker("Plain"));
        using var _ = document;

        controller.SpellingRects().ShouldNotBeEmpty();

        // A different dictionary disagrees about which words are wrong, so the previous results are
        // not merely stale — they belong to something else entirely.
        controller.SpellChecker = new FakeSpellChecker("nothing-in-this-document");

        controller.SpellingRects().ShouldBeEmpty();
    }

    [Fact]
    public async Task TurningCheckingOffRemovesTheUnderlines()
    {
        var (document, controller) = await SetupAsync(new FakeSpellChecker("Plain"));
        using var _ = document;

        controller.SpellingRects().ShouldNotBeEmpty();

        controller.IsSpellCheckEnabled = false;

        controller.SpellingRects().ShouldBeEmpty();
    }

    [Fact]
    public async Task LearningAWordClearsWhatWasAlreadyFound()
    {
        var checker = new FakeSpellChecker("Plain");
        var (document, controller) = await SetupAsync(checker);
        using var _ = document;

        controller.SpellingRects().ShouldNotBeEmpty();

        controller.LearnSpelling("Plain");

        checker.Learned.ShouldContain("Plain");
        controller.SpellingRects().ShouldBeEmpty();
    }

    [Fact]
    public async Task WithNoCheckerNothingIsUnderlinedAndNothingIsAsked()
    {
        var (document, controller) = await SetupAsync(NullSpellChecker.Instance);
        using var _ = document;

        controller.SpellingRects().ShouldBeEmpty();
    }
}
