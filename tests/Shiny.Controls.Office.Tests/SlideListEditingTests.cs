using Shiny.Controls.Office.Presentation;
using Shiny.Controls.Office.Text;
using Shouldly;
using Xunit;
using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Bullets and numbers inside a shape's text, and the outline levels Tab moves between.
/// </summary>
/// <remarks>
/// PowerPoint expresses a list entirely differently from Word — the mark lives in the paragraph's own
/// properties rather than in a shared part — but the editor's surface is the same on purpose, so
/// these mirror the Word list tests deliberately closely.
/// </remarks>
public class SlideListEditingTests
{
    sealed class Fixed : ITextMeasurer
    {
        public TextMetrics Measure(ReadOnlySpan<char> text, TextStyle style)
            => new(text.Length * 8, style.FontSize * 0.8, style.FontSize * 0.2);

        public TextMetrics LineMetrics(TextStyle style)
            => new(0, style.FontSize * 0.8, style.FontSize * 0.2);
    }

    static async Task<SlideDeck> OpenAsync()
    {
        using var source = new MemoryStream(SlideFixture.Build(), writable: false);
        return await SlideDeck.OpenAsync(source, editable: true);
    }

    /// <summary>
    /// Slide 2's body placeholder, which has a top-level and a nested paragraph — and whose bullet
    /// comes from the master rather than from the paragraphs themselves.
    /// </summary>
    static int BodyShape(SlideDeck deck)
        => deck.Slides[1].Shapes.ToList().FindIndex(x => x.Text?.PlainText.Contains("Top level point") == true);

    /// <summary>
    /// A plain text shape with no bullet of any kind.
    /// </summary>
    /// <remarks>
    /// Not a placeholder, so it inherits the master's "other" style rather than its body style, and
    /// starts out genuinely unmarked. That is the case a toggle has to get right in the direction the
    /// body placeholder cannot test, because that one already has a bullet before anything is pressed.
    /// </remarks>
    static int PlainShape(SlideDeck deck)
        => deck.Slides[1].Shapes.ToList().FindIndex(x => x.Text?.PlainText == "Callout text");

    static ShapeParagraph ParagraphAt(SlideDeck deck, int shape, int paragraph)
        => deck.Slides[1].Shapes[shape].Text!.Paragraphs[paragraph];

    static SlideEditorController Editing(SlideDeck deck, int shape, int paragraph = 0, int offset = 0)
    {
        var controller = new SlideEditorController(deck, new Fixed());
        controller.Resize(960, 540);
        controller.Index = 1;
        controller.Select(shape);
        controller.BeginTextEditing(paragraph, offset);
        controller.MoveCaret(new SlidePosition(1, shape, paragraph, offset));
        return controller;
    }

    // ---- applying a mark ----

    [Fact]
    public async Task BulletingAnUnmarkedParagraphPutsAGlyphInFrontOfIt()
    {
        using var deck = await OpenAsync();
        var shape = PlainShape(deck);
        var controller = Editing(deck, shape);

        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.None);
        controller.ToggleBulletList();

        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Bullet);
        ParagraphAt(deck, shape, 0).Bullet.ShouldBe("•");
    }

    [Fact]
    public async Task NumberingParagraphsCountsThem()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.SelectAll();
        controller.ToggleNumberedList();

        // The second paragraph is nested, so it counts as the first item at its own level rather than
        // as the second item overall.
        ParagraphAt(deck, shape, 0).Bullet.ShouldBe("1.");
        ParagraphAt(deck, shape, 1).Bullet.ShouldBe("1.");
    }

    [Fact]
    public async Task NumbersAtOneLevelRunAsASequence()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.SelectAll();
        controller.ToggleNumberedList();

        // Flatten the nested one so both are at level 0 and share a counter.
        controller.MoveCaret(new SlidePosition(1, shape, 1, 0));
        controller.ShiftLevel(-1);

        ParagraphAt(deck, shape, 0).Bullet.ShouldBe("1.");
        ParagraphAt(deck, shape, 1).Bullet.ShouldBe("2.");
    }

    [Fact]
    public async Task TurningOffAnInheritedBulletWritesAnExplicitNoBullet()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        // The bullet comes from the master's body style, so the paragraph starts out marked without
        // carrying anything of its own.
        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Bullet);

        controller.ToggleBulletList();

        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.None);
        ParagraphAt(deck, shape, 0).Bullet.ShouldBeNull();

        // There was no element to remove, so the only way to say "no bullet" is to write one. Leaving
        // the properties alone would let the master's bullet through and the button would look like
        // it had done nothing.
        var element = ParagraphAt(deck, shape, 0).Element!;
        element.ParagraphProperties!.GetFirstChild<D.NoBullet>().ShouldNotBeNull();
    }

    [Fact]
    public async Task ANumberedListUndoesBackToWhatWasInherited()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.ToggleNumberedList();
        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Numbered);

        controller.Undo();

        // Back to the master's bullet, not to nothing: the paragraph never had a mark of its own to
        // return to, and an undo that left it bare would be a second edit rather than the reverse of
        // the first.
        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Bullet);
    }

    // ---- nesting ----

    [Fact]
    public async Task TabNestsAndShiftTabUnNests()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.HandleTab().ShouldBeTrue();
        ParagraphAt(deck, shape, 0).Level.ShouldBe(1);

        controller.HandleTab(shift: true);
        ParagraphAt(deck, shape, 0).Level.ShouldBe(0);
    }

    [Fact]
    public async Task NestingAMixedSelectionMovesEachParagraphRelativeToItself()
    {
        // Reading one level off the first paragraph and applying it to all of them flattened the two
        // onto a single depth, which only shows when a selection spans more than one line.
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.SelectAll();
        controller.ShiftLevel(1);

        ParagraphAt(deck, shape, 0).Level.ShouldBe(1);
        ParagraphAt(deck, shape, 1).Level.ShouldBe(2);
    }

    [Fact]
    public async Task NestingStopsAtTheDeepestLevel()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        for (var i = 0; i < 12; i++)
            controller.HandleTab();

        // DrawingML has nine outline levels and no more; writing a tenth produces a file PowerPoint
        // will not open.
        ParagraphAt(deck, shape, 0).Level.ShouldBe(8);
    }

    // ---- autoformat ----

    [Fact]
    public async Task TypingAMarkerAndASpaceStartsABulletedList()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.SetListStyle(ListStyle.None);
        controller.MoveCaret(new SlidePosition(1, shape, 0, 0));

        controller.InsertText("-");
        controller.InsertText(" ");

        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Bullet);
        ParagraphAt(deck, shape, 0).PlainText.ShouldBe("Top level point");
    }

    [Fact]
    public async Task TypingANumberAndASpaceStartsANumberedList()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.SetListStyle(ListStyle.None);
        controller.MoveCaret(new SlidePosition(1, shape, 0, 0));

        controller.InsertText("1");
        controller.InsertText(".");
        controller.InsertText(" ");

        ParagraphAt(deck, shape, 0).List.ShouldBe(ListStyle.Numbered);
        ParagraphAt(deck, shape, 0).Bullet.ShouldBe("1.");
    }

    [Fact]
    public async Task AParagraphThatAlreadyHasAMarkIsLeftAlone()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.MoveCaret(new SlidePosition(1, shape, 0, 0));

        controller.InsertText("-");
        controller.InsertText(" ");

        // The paragraph already carries the master's bullet, so what was typed is text the user meant
        // to keep rather than a marker asking for the bullet it already has.
        ParagraphAt(deck, shape, 0).PlainText.ShouldBe("- Top level point");
    }

    // ---- toolbar state ----

    [Fact]
    public async Task TheCaretReportsTheListAndLevelItIsIn()
    {
        using var deck = await OpenAsync();
        var shape = BodyShape(deck);
        var controller = Editing(deck, shape);

        controller.ToggleNumberedList();

        controller.CaretFormat.List.ShouldBe(ListStyle.Numbered);
        controller.CaretFormat.Level.ShouldBe(0);

        controller.MoveCaret(new SlidePosition(1, shape, 1, 0));
        controller.CaretFormat.Level.ShouldBe(1);
    }

    // ---- the package ----

    [Fact]
    public async Task AListSurvivesASaveAndReopen()
    {
        using var buffer = new MemoryStream();
        int shape;

        using (var deck = await OpenAsync())
        {
            shape = BodyShape(deck);
            var controller = Editing(deck, shape);

            controller.SelectAll();
            controller.ToggleNumberedList();

            await deck.SaveToAsync(buffer);
        }

        buffer.Position = 0;

        // a:pPr's children are a sequence, so a bullet written in the wrong slot saves without
        // complaint and produces a file PowerPoint reports as corrupt.
        using var reopened = await SlideDeck.OpenAsync(buffer);

        reopened.Slides[1].Shapes[shape].Text!.Paragraphs[0].Bullet.ShouldBe("1.");
    }
}
