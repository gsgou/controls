using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Text;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class WordViewerTests
{
    static async Task<WordDocument> OpenAsync(IUnsupportedFeatureSink? sink = null)
    {
        using var source = new MemoryStream(DocumentFixture.Build(), writable: false);
        return await WordDocument.OpenAsync(source, sink);
    }

    [Fact]
    public async Task ReadsBlocksInDocumentOrder()
    {
        using var document = await OpenAsync();

        document.Blocks.Count.ShouldBeGreaterThan(5);
        document.Blocks[0].ShouldBeOfType<DocumentParagraph>().PlainText.ShouldBe("Quarterly Report");
        document.Blocks.OfType<DocumentTable>().Count().ShouldBe(1);
    }

    [Fact]
    public async Task ResolvesTheStyleChainIncludingBasedOn()
    {
        // "Accent" only sets a colour; everything else comes from "Base". A resolver that ignores
        // basedOn renders it in the default font at the default size.
        using var document = await OpenAsync();

        var paragraph = document.Blocks
            .OfType<DocumentParagraph>()
            .First(x => x.PlainText.StartsWith("Text using a style"));

        var style = paragraph.Runs[0].Style;
        style.FontFamily.ShouldBe("Georgia");
        style.Italic.ShouldBeTrue();
        style.FontSize.ShouldBe(OoxmlUnits.HalfPointsToPixels(28), 0.01);
        style.Color.R.ShouldBe((byte)0xC0);
    }

    [Fact]
    public async Task HeadingsCarryTheirOutlineLevel()
    {
        using var document = await OpenAsync();

        var outline = document.Outline().ToList();
        outline.ShouldHaveSingleItem();
        outline[0].Level.ShouldBe(1);
        outline[0].Text.ShouldBe("Quarterly Report");
    }

    [Fact]
    public async Task DirectFormattingLayersOverTheStyle()
    {
        using var document = await OpenAsync();

        var paragraph = document.Blocks
            .OfType<DocumentParagraph>()
            .First(x => x.PlainText.StartsWith("Bold "));

        paragraph.Format.Alignment.ShouldBe(TextAlignment.Center);
        paragraph.Runs[0].Style.Bold.ShouldBeTrue();
        paragraph.Runs[1].Style.Italic.ShouldBeTrue();
        paragraph.Runs[2].Style.Underline.ShouldBe(UnderlineStyle.Single);
        paragraph.Runs[2].Style.Color.R.ShouldBe((byte)0xFF);

        paragraph.Runs[1].Style.Bold.ShouldBeFalse("formatting must not leak between sibling runs");
    }

    [Fact]
    public async Task NumberedListsProduceRunningLabels()
    {
        using var document = await OpenAsync();

        var items = document.Blocks
            .OfType<DocumentParagraph>()
            .Where(x => x.List is not null)
            .ToList();

        items.Count.ShouldBe(3);
        items[0].List!.Text.ShouldBe("1.");
        items[1].List!.Text.ShouldBe("2.");

        // The nested level's template is "%1.%2.", so it composes the outer counter with its own.
        items[2].List!.Text.ShouldBe("2.a.");
    }

    [Fact]
    public async Task TableStructureSurvivesSpansAndMerges()
    {
        using var document = await OpenAsync();
        var table = document.Blocks.OfType<DocumentTable>().Single();

        table.Rows.Count.ShouldBe(4);
        table.ColumnWidths.Count.ShouldBe(3);
        table.Rows[0].IsHeader.ShouldBeTrue();
        table.Rows[0].Cells[0].Shading.ShouldNotBeNull();

        table.Rows[2].Cells[0].IsVerticalContinuation.ShouldBeTrue("the second data row continues the merge above");
        table.Rows[3].Cells[1].ColumnSpan.ShouldBe(2);
    }

    [Fact]
    public async Task PageSetupComesFromTheSectionProperties()
    {
        using var document = await OpenAsync();

        document.Page.Width.ShouldBe(OoxmlUnits.TwipsToPixels(12240), 0.01);
        document.Page.MarginLeft.ShouldBe(OoxmlUnits.TwipsToPixels(1440), 0.01);
        document.Page.ContentWidth.ShouldBe(document.Page.Width - document.Page.MarginLeft - document.Page.MarginRight, 0.01);
    }

    [Fact]
    public async Task PlainTextExtractsEveryParagraph()
    {
        using var document = await OpenAsync();

        document.PlainText.ShouldContain("Quarterly Report");
        document.PlainText.ShouldContain("Nested item");
    }

    [Fact]
    public async Task OpeningIsNonDestructive()
    {
        // A viewer must never rewrite what it opened.
        var original = DocumentFixture.Build();

        using var source = new MemoryStream(original, writable: false);
        using var document = await WordDocument.OpenAsync(source);

        PackageComparer.Compare(original, document.ToArray()).IsIdentical.ShouldBeTrue();
    }
}

public class DocumentLayoutTests
{
    /// <summary>
    /// A measurer with fixed metrics, so line-breaking assertions are exact rather than dependent on
    /// whichever fonts the machine running the tests happens to have.
    /// </summary>
    sealed class FakeMeasurer : ITextMeasurer
    {
        public const double CharWidth = 10;

        public TextMetrics Measure(ReadOnlySpan<char> text, TextStyle style)
            => new(text.Length * CharWidth, style.FontSize * 0.8, style.FontSize * 0.2);

        public TextMetrics LineMetrics(TextStyle style) => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    static readonly TextStyle Style = TextStyle.Default with { FontSize = 10 };

    static TextLayoutEngine Engine() => new(new FakeMeasurer());

    [Fact]
    public void WrapsAtWordBoundaries()
    {
        // "aaa bbb ccc" is 11 chars = 110px; at 80px it must break after "bbb".
        var lines = Engine().Layout([new StyledRun("aaa bbb ccc", Style)], 80);

        lines.Count.ShouldBe(2);
        string.Concat(lines[0].Runs.Select(x => x.Text)).ShouldBe("aaa bbb");
        string.Concat(lines[1].Runs.Select(x => x.Text)).ShouldBe("ccc");
    }

    [Fact]
    public void TrailingWhitespaceHangsRatherThanForcingABreak()
    {
        var lines = Engine().Layout([new StyledRun("aaaaaaaa ", Style)], 80);
        lines.Count.ShouldBe(1);
    }

    [Fact]
    public void AWordLongerThanTheLineIsBrokenRatherThanOverflowing()
    {
        // Without mid-word breaking a single long token paints straight through the margin.
        var lines = Engine().Layout([new StyledRun("abcdefghijklmnop", Style)], 50);

        lines.Count.ShouldBeGreaterThan(1);
        lines.All(x => x.Width <= 50.001).ShouldBeTrue();
    }

    [Fact]
    public void ExplicitBreaksStartANewLine()
    {
        var lines = Engine().Layout(
            [new StyledRun("one", Style), new StyledRun(string.Empty, Style) { IsBreak = true }, new StyledRun("two", Style)],
            1000);

        lines.Count.ShouldBe(2);
    }

    [Fact]
    public void AnEmptyParagraphStillOccupiesALine()
    {
        var lines = Engine().Layout([], 100);
        lines.ShouldHaveSingleItem();
        lines[0].Height.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(TextAlignment.Left, 0)]
    [InlineData(TextAlignment.Center, 35)]
    [InlineData(TextAlignment.Right, 70)]
    public void AlignmentPositionsTheLine(TextAlignment alignment, double expectedX)
    {
        // "abc" is 30px on a 100px measure, so the slack is 70.
        var lines = Engine().Layout([new StyledRun("abc", Style)], 100, alignment);
        lines[0].Runs[0].X.ShouldBe(expectedX, 0.01);
    }

    [Fact]
    public void JustificationSpreadsGapsButNotOnTheLastLine()
    {
        var runs = new[] { new StyledRun("aa bb cc dd ee ff", Style) };

        var justified = Engine().Layout(runs, 100, TextAlignment.Justify);
        justified.Count.ShouldBeGreaterThan(1);

        // The final line is left as-is, which is why its width is under the measure.
        justified[^1].Width.ShouldBeLessThan(100);
    }

    [Fact]
    public void MixedStylesMeasureIndependently()
    {
        var big = Style with { FontSize = 20 };
        var lines = Engine().Layout([new StyledRun("aa", Style), new StyledRun("bb", big)], 1000);

        // Line height follows the tallest run on it.
        lines[0].Ascent.ShouldBe(16, 0.01);
    }

    [Fact]
    public void DocumentLayoutStacksBlocksAndReportsHeight()
    {
        var engine = new DocumentLayoutEngine(new FakeMeasurer());
        var blocks = new DocumentBlock[]
        {
            new DocumentParagraph([new StyledRun("first", Style)], ParagraphFormat.Default with { SpaceAfter = 5 }),
            new DocumentParagraph([new StyledRun("second", Style)], ParagraphFormat.Default)
        };

        var result = engine.Layout(blocks, 200);

        result.Blocks.Count.ShouldBe(2);
        result.Blocks[1].Y.ShouldBeGreaterThan(result.Blocks[0].Y);
        result.Height.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void IndentsNarrowTheMeasure()
    {
        var engine = new DocumentLayoutEngine(new FakeMeasurer());
        var format = ParagraphFormat.Default with { IndentLeft = 50, IndentRight = 50 };

        var result = engine.Layout([new DocumentParagraph([new StyledRun("x", Style)], format)], 200);
        var paragraph = result.Blocks[0].ShouldBeOfType<LaidOutParagraph>();

        paragraph.X.ShouldBe(50);
        paragraph.Width.ShouldBe(100);
    }

    [Fact]
    public void TheViewportSkipsBlocksOutsideTheVisibleBand()
    {
        var engine = new DocumentLayoutEngine(new FakeMeasurer());
        var blocks = Enumerable.Range(0, 200)
            .Select(i => (DocumentBlock)new DocumentParagraph([new StyledRun($"line {i}", Style)], ParagraphFormat.Default))
            .ToList();

        var result = engine.Layout(blocks, 300);
        var viewport = new DocumentViewport { Height = 100, ContentHeight = result.Height };
        viewport.ScrollTo(0);

        // Virtualisation: a 200-paragraph document must not paint 200 paragraphs.
        viewport.Visible(result.Blocks).Count().ShouldBeLessThan(30);
    }

    [Fact]
    public void ScrollingIsClampedToTheContent()
    {
        var viewport = new DocumentViewport { Height = 100, ContentHeight = 500 };

        viewport.ScrollTo(-50);
        viewport.ScrollY.ShouldBe(0);

        viewport.ScrollTo(10_000);
        viewport.ScrollY.ShouldBe(400);
    }
}
