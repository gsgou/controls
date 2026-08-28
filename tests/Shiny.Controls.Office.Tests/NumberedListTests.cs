using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Spreadsheet;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// A list number belongs to the sequence, not to the paragraph carrying it.
/// </summary>
/// <remarks>
/// The counters used to run as the body was read and were never rewound, so re-reading one paragraph —
/// which every edit does — advanced them again and handed that item the number after the document's
/// last one. It looked like a formatting bug because highlighting a word was where anyone noticed it.
/// </remarks>
public class NumberedListTests
{
    static async Task<WordDocument> OpenAsync()
        => await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);

    /// <summary>The list labels in document order. The fixture's list is 1., 2., 2.a.</summary>
    static List<string> LabelsOf(WordDocument document) => document.Blocks
        .OfType<DocumentParagraph>()
        .Where(x => x.List is not null)
        .Select(x => x.List!.Text)
        .ToList();

    static int ListBlock(WordDocument document, string startsWith) => document.Blocks
        .ToList()
        .FindIndex(x => x is DocumentParagraph { List: not null } p && p.PlainText.StartsWith(startsWith));

    [Fact]
    public async Task ReadingProducesRunningLabels()
    {
        using var document = await OpenAsync();

        // The nested level's template is "%1.%2.", so it composes the outer counter with its own.
        LabelsOf(document).ShouldBe(["1.", "2.", "2.a."]);
    }

    [Fact]
    public async Task FormattingAWordInAnItemLeavesEveryNumberAlone()
    {
        using var document = await OpenAsync();
        var block = ListBlock(document, "Second item");

        document.Execute(new FormatRunsCommand(
            new DocumentRange(new DocumentPosition(block, 0), new DocumentPosition(block, 6)),
            RunFormatChange.Highlight(new ArgbColor(255, 255, 255, 0))));

        LabelsOf(document).ShouldBe(["1.", "2.", "2.a."]);
    }

    [Fact]
    public async Task RepeatedEditsDoNotRatchetTheNumberUpwards()
    {
        // The old bug compounded: each edit re-advanced the counter, so the number climbed away from
        // the truth one edit at a time. One edit alone would not have caught that.
        using var document = await OpenAsync();
        var block = ListBlock(document, "Second item");

        for (var i = 0; i < 5; i++)
        {
            document.Execute(new InsertTextCommand(new DocumentPosition(block, 0), "x"));
            LabelsOf(document).ShouldBe(["1.", "2.", "2.a."]);
        }
    }

    [Fact]
    public async Task UndoRestoresTheNumbersRatherThanAdvancingThem()
    {
        // Undo re-reads the restored paragraph through the same path an edit does, so it used to push
        // the number one further rather than putting it back.
        using var document = await OpenAsync();
        var block = ListBlock(document, "First item");

        document.Execute(new InsertTextCommand(new DocumentPosition(block, 0), "x"));
        document.Undo.Undo();

        LabelsOf(document).ShouldBe(["1.", "2.", "2.a."]);
    }

    [Fact]
    public async Task SplittingAnItemRenumbersTheRestOfTheList()
    {
        // Pressing Enter mid-item makes a new item, and everything after it has to move up. Nothing
        // renumbered before this change, because the number was frozen into the paragraph at read time.
        using var document = await OpenAsync();
        var block = ListBlock(document, "First item");

        document.Execute(new SplitParagraphCommand(new DocumentPosition(block, 5)));

        LabelsOf(document).ShouldBe(["1.", "2.", "3.", "3.a."]);
    }

    [Fact]
    public async Task RemovingAnItemRenumbersTheRestOfTheList()
    {
        using var document = await OpenAsync();
        var block = ListBlock(document, "First item");

        document.Execute(new RemoveBlocksCommand(block, 1));

        LabelsOf(document).ShouldBe(["1.", "1.a."]);
    }
}
