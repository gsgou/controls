using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Skia;
using Shiny.Controls.Office.Text;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Turning ordinary paragraphs into lists, and moving items between levels.
/// </summary>
/// <remarks>
/// A Word paragraph does not carry its own bullet — it points at a definition in
/// <c>numbering.xml</c> — so almost everything that can go wrong here is invisible in the model and
/// only shows up as a paragraph that has a list reference and draws nothing in front of it. These
/// assert on the rendered label for exactly that reason.
/// </remarks>
public class ListEditingTests
{
    static async Task<WordDocument> OpenAsync()
        => await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()), editable: true);

    /// <summary>A document with no numbering part at all, which is the harder starting point.</summary>
    static async Task<WordDocument> OpenPlainAsync()
        => await WordDocument.OpenAsync(new MemoryStream(PlainFixture()), editable: true);

    static byte[] PlainFixture()
    {
        using var buffer = new MemoryStream();
        using (var document = WordprocessingDocument.Create(buffer, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, autoSave: false))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body(
                new Paragraph(new Run(new W.Text("Alpha"))),
                new Paragraph(new Run(new W.Text("Beta"))),
                new Paragraph(new Run(new W.Text("Gamma")))));

            main.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    static DocumentEditorController Controller(WordDocument document)
    {
        var controller = new DocumentEditorController(document, new SkiaTextMeasurer());
        controller.Resize(800, 600);
        return controller;
    }

    static DocumentParagraph ParagraphAt(WordDocument document, int block)
        => (DocumentParagraph)document.Blocks[block];

    static string? LabelAt(WordDocument document, int block)
        => ParagraphAt(document, block).List?.Text;

    static int BlockOf(WordDocument document, string startsWith) => document.Blocks
        .ToList()
        .FindIndex(x => x is DocumentParagraph p && p.PlainText.StartsWith(startsWith));

    // ---- creating a list where the document had none ----

    [Fact]
    public async Task BulletingAParagraphInADocumentWithNoNumberingPartDrawsABullet()
    {
        // The whole point: nothing to point at exists yet, so the command has to create the part, the
        // abstract definition and the instance before the paragraph reference means anything.
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleBulletList();

        ParagraphAt(document, 0).List.ShouldNotBeNull();
        LabelAt(document, 0).ShouldBe("•");
        ParagraphAt(document, 0).List!.IsBullet.ShouldBeTrue();
    }

    [Fact]
    public async Task ANewListItemGetsTheLevelsHangingIndent()
    {
        // The label is drawn in the hanging part of the indent. Without it the item is not indented
        // and the bullet is painted on top of the first letter — which every assertion on the label
        // text happily passes through, because the text is right and only its position is wrong.
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleBulletList();

        var paragraph = ParagraphAt(document, 0);

        // 720 twips is half an inch, which is 48px at 96dpi; the hanging 360 is a quarter of an inch.
        paragraph.List!.Indent.ShouldBe(48, 0.5);
        paragraph.List!.HangingIndent.ShouldBe(24, 0.5);
        paragraph.Format.IndentLeft.ShouldBe(48, 0.5);
    }

    [Fact]
    public async Task ANestedItemIsIndentedFurtherThanItsParent()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(1, 0));
        controller.ToggleBulletList();

        controller.Selection.MoveTo(new DocumentPosition(1, 0));
        controller.HandleTab();

        ParagraphAt(document, 1).Format.IndentLeft
            .ShouldBeGreaterThan(ParagraphAt(document, 0).Format.IndentLeft);
    }

    [Fact]
    public async Task NumberingParagraphsCountsThem()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(2, 0));
        controller.ToggleNumberedList();

        new[] { LabelAt(document, 0), LabelAt(document, 1), LabelAt(document, 2) }
            .ShouldBe(["1.", "2.", "3."]);
    }

    [Fact]
    public async Task PressingTheSameButtonAgainTakesTheParagraphOutOfTheList()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleBulletList();
        controller.ToggleBulletList();

        ParagraphAt(document, 0).List.ShouldBeNull();
    }

    [Fact]
    public async Task SwitchingFromBulletsToNumbersKeepsTheText()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleBulletList();
        controller.ToggleNumberedList();

        LabelAt(document, 0).ShouldBe("1.");
        ParagraphAt(document, 0).PlainText.ShouldBe("Alpha");
    }

    [Fact]
    public async Task CreatingAListIsOneUndoStep()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleNumberedList();
        controller.Undo();

        ParagraphAt(document, 0).List.ShouldBeNull();
        ParagraphAt(document, 0).PlainText.ShouldBe("Alpha");
    }

    // ---- nesting ----

    [Fact]
    public async Task TabNestsAnItemAndTheLabelCompoundsWithItsParent()
    {
        // The behaviour the whole feature was asked for: the second level reads as 1a, not as a bare
        // "a" that says nothing about which item it belongs under.
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(2, 0));
        controller.ToggleNumberedList();

        controller.Selection.MoveTo(new DocumentPosition(1, 0));
        controller.HandleTab().ShouldBeTrue();

        new[] { LabelAt(document, 0), LabelAt(document, 1), LabelAt(document, 2) }
            .ShouldBe(["1.", "1a.", "2."]);
    }

    [Fact]
    public async Task NestedItemsCountSeparatelyUnderEachParent()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(2, 0));
        controller.ToggleNumberedList();

        controller.Selection.MoveTo(new DocumentPosition(1, 0));
        controller.Selection.ExtendTo(new DocumentPosition(2, 0));
        controller.HandleTab();

        new[] { LabelAt(document, 0), LabelAt(document, 1), LabelAt(document, 2) }
            .ShouldBe(["1.", "1a.", "1b."]);
    }

    [Fact]
    public async Task ShiftTabBringsAnItemBackOut()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(1, 0));
        controller.ToggleNumberedList();

        controller.Selection.MoveTo(new DocumentPosition(1, 0));
        controller.HandleTab();
        controller.HandleTab(shift: true);

        LabelAt(document, 1).ShouldBe("2.");
    }

    [Fact]
    public async Task NestedBulletsChangeGlyph()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(1, 0));
        controller.ToggleBulletList();

        controller.Selection.MoveTo(new DocumentPosition(1, 0));
        controller.HandleTab();

        // Level 0 is the Symbol bullet and level 1 is Courier's lowercase o, both mapped to glyphs a
        // text font can draw. Two levels that drew the same mark would make the nesting invisible.
        LabelAt(document, 0).ShouldBe("•");
        LabelAt(document, 1).ShouldBe("◦");
    }

    [Fact]
    public async Task TabOutsideAListInsertsATabRatherThanNesting()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.HandleTab().ShouldBeTrue();

        // The reader projects a tab as four spaces, so that is what the offset space has to show.
        ParagraphAt(document, 0).PlainText.ShouldBe("    Alpha");
        controller.Selection.Focus.Offset.ShouldBe(4);
    }

    [Fact]
    public async Task AnInsertedTabUndoesWhole()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.HandleTab();
        controller.Undo();

        // A tab is four characters wide but one element; an inverse that removed one character would
        // leave three spaces behind.
        ParagraphAt(document, 0).PlainText.ShouldBe("Alpha");
    }

    [Fact]
    public async Task TabSkipsParagraphsThatAreNotListItems()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleBulletList();

        // A selection covering the bulleted first paragraph and the plain second one.
        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(1, 0));
        controller.HandleTab();

        ParagraphAt(document, 1).List.ShouldBeNull();
        ParagraphAt(document, 1).PlainText.ShouldBe("Beta");
    }

    // ---- autoformat ----

    [Theory]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("+")]
    public async Task TypingAMarkerAndASpaceStartsABulletedList(string marker)
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.InsertText(marker);
        controller.InsertText(" ");

        LabelAt(document, 0).ShouldBe("•");

        // The marker and the space that triggered it both go: leaving either behind puts them in the
        // item's text, which is what a list is meant to replace.
        ParagraphAt(document, 0).PlainText.ShouldBe("Alpha");
        controller.Selection.Focus.Offset.ShouldBe(0);
    }

    [Theory]
    [InlineData("1.")]
    [InlineData("1)")]
    [InlineData("12.")]
    public async Task TypingANumberAndASpaceStartsANumberedList(string marker)
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));

        foreach (var c in marker)
            controller.InsertText(c.ToString());

        controller.InsertText(" ");

        LabelAt(document, 0).ShouldBe("1.");
    }

    [Fact]
    public async Task AutoFormatUndoesBackToTheTypedCharacters()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.InsertText("-");
        controller.InsertText(" ");
        controller.Undo();

        // One step, not two: an undo that removed the list and left a bulleted-looking "-" behind
        // would be the worst of both.
        ParagraphAt(document, 0).List.ShouldBeNull();
        ParagraphAt(document, 0).PlainText.ShouldBe("-Alpha");
    }

    [Fact]
    public async Task AHyphenPartWayThroughALineIsJustAHyphen()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 5));
        controller.InsertText("-");
        controller.InsertText(" ");

        ParagraphAt(document, 0).List.ShouldBeNull();
        ParagraphAt(document, 0).PlainText.ShouldBe("Alpha- ");
    }

    [Fact]
    public async Task AutoFormatCanBeTurnedOff()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);
        controller.IsAutoFormatListEnabled = false;

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.InsertText("-");
        controller.InsertText(" ");

        ParagraphAt(document, 0).List.ShouldBeNull();
        ParagraphAt(document, 0).PlainText.ShouldBe("- Alpha");
    }

    [Fact]
    public void TheDetectorRefusesThingsThatMerelyLookLikeMarkers()
    {
        ListAutoFormat.Detect("a.").ShouldBe(ListStyle.None);
        ListAutoFormat.Detect("Note.").ShouldBe(ListStyle.None);
        ListAutoFormat.Detect("--").ShouldBe(ListStyle.None);
        ListAutoFormat.Detect("").ShouldBe(ListStyle.None);
        ListAutoFormat.Detect(null).ShouldBe(ListStyle.None);
    }

    // ---- Enter on an empty item ----

    [Fact]
    public async Task EnterOnAnEmptyNestedItemBringsItOutOneLevel()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleNumberedList();
        controller.HandleTab();

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(0, 5));
        controller.DeleteBackward();
        controller.InsertParagraph();

        ParagraphAt(document, 0).List.ShouldNotBeNull();
        LabelAt(document, 0).ShouldBe("1.");
    }

    [Fact]
    public async Task EnterOnAnEmptyTopLevelItemEndsTheList()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleNumberedList();

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.Selection.ExtendTo(new DocumentPosition(0, 5));
        controller.DeleteBackward();
        controller.InsertParagraph();

        ParagraphAt(document, 0).List.ShouldBeNull();
        document.Blocks.Count.ShouldBe(3);
    }

    [Fact]
    public async Task EnterOnAnItemWithTextStillMakesANewItem()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(0, 0));
        controller.ToggleNumberedList();

        controller.Selection.MoveTo(new DocumentPosition(0, 5));
        controller.InsertParagraph();

        LabelAt(document, 0).ShouldBe("1.");
        LabelAt(document, 1).ShouldBe("2.");
    }

    // ---- toolbar state ----

    [Fact]
    public async Task TheCaretReportsWhichListItIsIn()
    {
        using var document = await OpenAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(BlockOf(document, "Nested item"), 0));

        controller.CaretFormat.List.ShouldBe(ListStyle.Numbered);
        controller.CaretFormat.ListLevel.ShouldBe(1);
    }

    [Fact]
    public async Task TheCaretReportsNoListForOrdinaryText()
    {
        using var document = await OpenAsync();
        var controller = Controller(document);

        controller.Selection.MoveTo(new DocumentPosition(BlockOf(document, "Plain body text"), 0));

        controller.CaretFormat.List.ShouldBe(ListStyle.None);
    }

    // ---- the package ----

    [Fact]
    public async Task ANewListSurvivesASaveAndReopen()
    {
        using var buffer = new MemoryStream();

        using (var document = await OpenPlainAsync())
        {
            var controller = Controller(document);
            controller.Selection.MoveTo(new DocumentPosition(0, 0));
            controller.Selection.ExtendTo(new DocumentPosition(1, 0));
            controller.ToggleNumberedList();

            controller.Selection.MoveTo(new DocumentPosition(1, 0));
            controller.HandleTab();

            await document.SaveToAsync(buffer);
        }

        buffer.Position = 0;

        // The definitions live in their own part. Saving only the main document writes paragraphs
        // pointing at a numbering.xml the file does not contain, and the list vanishes on reopen.
        using var reopened = await WordDocument.OpenAsync(buffer);

        LabelAt(reopened, 0).ShouldBe("1.");
        LabelAt(reopened, 1).ShouldBe("1a.");
    }

    [Fact]
    public async Task RepeatedTogglesReuseOneDefinition()
    {
        using var document = await OpenPlainAsync();
        var controller = Controller(document);

        for (var i = 0; i < 3; i++)
        {
            controller.Selection.MoveTo(new DocumentPosition(i, 0));
            controller.ToggleBulletList();
        }

        // A definition created per press would leave three abstract lists behind, and — worse — three
        // separate sequences, so a numbered list built the same way would restart at 1 on every item.
        var numbering = document.Main!.NumberingDefinitionsPart!.Numbering!;
        numbering.Elements<AbstractNum>().Count().ShouldBe(1);
        numbering.Elements<NumberingInstance>().Count().ShouldBe(1);
    }
}
