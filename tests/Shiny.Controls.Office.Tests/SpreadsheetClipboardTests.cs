using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Cut, copy, paste and the row and column inserts that share their machinery.
/// </summary>
/// <remarks>
/// The interesting cases are all about what travels with a block and what gets left behind: a formula
/// that has to be rebased on a copy but not on a cut, formatting that lives on a row rather than on
/// its cells, and the references elsewhere in the workbook that an insert silently invalidates if
/// nothing repoints them.
/// </remarks>
public class SpreadsheetClipboardTests
{
    static async Task<(Workbook Workbook, SpreadsheetController Controller)> SetupAsync()
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.Build()));
        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(600, 400);
        return (workbook, controller);
    }

    static string? Text(Worksheet sheet, string address)
    {
        var value = sheet.GetValue(CellRef.Parse(address));
        return value.IsBlank ? null : Coercion.ToText(value);
    }

    // ---- capturing ----

    [Fact]
    public async Task NothingIsOnTheClipboardToStartWith()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Clipboard.ShouldBeNull();
        controller.CanPaste.ShouldBeFalse();
        controller.ClipboardRange.ShouldBeNull();
    }

    [Fact]
    public async Task ARowHeaderSelectionIsCapturedAsRowsRatherThanCells()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRow(0);
        controller.Copy();

        controller.Clipboard!.Kind.ShouldBe(SpreadsheetClipboardKind.Rows);
        controller.ClipboardRange.ShouldBe(controller.Selection.Range);
    }

    [Fact]
    public async Task AColumnHeaderSelectionIsCapturedAsColumns()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectColumn(1);
        controller.Copy();

        controller.Clipboard!.Kind.ShouldBe(SpreadsheetClipboardKind.Columns);
    }

    [Fact]
    public async Task AWholeRowCaptureIsNarrowedToTheCellsTheSheetActuallyUses()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRow(0);
        controller.Copy();

        // Row 1 holds A1:C1. Sixteen thousand blank entries would be the alternative.
        controller.Clipboard!.Cells.Count.ShouldBe(3);
    }

    [Fact]
    public async Task TheBorderIsNotDrawnOverASheetTheCaptureDidNotComeFrom()
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.BuildMultiSheet()));
        using var _ = workbook;

        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(600, 400);
        controller.Selection.SelectRange(CellRange.Parse("A1:B2"));
        controller.Copy();

        controller.SwitchSheet(workbook["Summary"]);

        // Still pasteable, just not drawable: those coordinates mean something else over here.
        controller.CanPaste.ShouldBeTrue();
        controller.ClipboardRange.ShouldBeNull();
    }

    // ---- pasting cells ----

    [Fact]
    public async Task CopyingAndPastingDuplicatesValuesAndLeavesTheSourceAlone()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRange(CellRange.Parse("A1:B1"));
        controller.Copy();

        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste().ShouldBeTrue();

        Text(controller.Sheet, "A8").ShouldBe("Widget");
        Text(controller.Sheet, "B8").ShouldBe("42");
        Text(controller.Sheet, "A1").ShouldBe("Widget");
    }

    [Fact]
    public async Task ACopiedFormulaIsRebasedOntoItsNewPosition()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        // C1 holds =B1*2.
        controller.Selection.MoveTo(CellRef.Parse("C1"));
        controller.Copy();

        controller.Selection.MoveTo(CellRef.Parse("C3"));
        controller.Paste();

        controller.Sheet.GetFormula(CellRef.Parse("C3")).ShouldBe("B3*2");
    }

    [Fact]
    public async Task ACutFormulaKeepsPointingAtTheCellsItAlwaysPointedAt()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("C1"));
        controller.Cut();

        controller.Selection.MoveTo(CellRef.Parse("C3"));
        controller.Paste();

        // Moved bodily, not rebased: the formula is meant to go on totalling the same numbers.
        controller.Sheet.GetFormula(CellRef.Parse("C3")).ShouldBe("B1*2");
        controller.Sheet.GetFormula(CellRef.Parse("C1")).ShouldBeNull();
    }

    [Fact]
    public async Task CuttingRemovesNothingUntilItIsPasted()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRange(CellRange.Parse("A1:B1"));
        controller.Cut();

        Text(controller.Sheet, "A1").ShouldBe("Widget");
    }

    [Fact]
    public async Task PastingOverACellThatHeldSomethingElseReplacesIt()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.SetActiveCellText("stale");

        controller.Selection.SelectRange(CellRange.Parse("A5:A5"));
        controller.Copy();
        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste();

        // A5 is blank, so what lands on A8 is a blank — not the value that was there before.
        Text(controller.Sheet, "A8").ShouldBeNull();
    }

    [Fact]
    public async Task FormattingTravelsWithACopiedCell()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        // A2 carries style 1 in the fixture.
        controller.Selection.MoveTo(CellRef.Parse("A2"));
        controller.Copy();
        controller.Selection.MoveTo(CellRef.Parse("D8"));
        controller.Paste();

        controller.Sheet.GetStyleIndex(CellRef.Parse("D8")).ShouldBe(1u);
    }

    // ---- pasting rows and columns ----

    [Fact]
    public async Task CopyingAWholeRowRepaintsEveryColumnOfTheDestination()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRow(0);
        controller.Copy();

        controller.Selection.SelectRow(7);
        controller.Paste();

        Text(controller.Sheet, "A8").ShouldBe("Widget");
        Text(controller.Sheet, "B8").ShouldBe("42");
    }

    [Fact]
    public async Task ARowPasteLandsOnTheSameColumnsWhateverCellIsSelected()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRow(0);
        controller.Copy();

        // A single cell three columns in: a row put down over there would no longer be that row.
        controller.Selection.MoveTo(CellRef.Parse("D8"));
        controller.Paste();

        Text(controller.Sheet, "A8").ShouldBe("Widget");
    }

    [Fact]
    public async Task AColumnWidthTravelsWithTheColumn()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectColumn(0);
        controller.SetColumnWidth(160);
        var width = controller.Sheet.GetColumnWidth(0);
        width.ShouldNotBeNull();

        controller.Selection.SelectColumn(0);
        controller.Copy();
        controller.Selection.SelectColumn(5);
        controller.Paste();

        controller.Sheet.GetColumnWidth(5).ShouldBe(width);

        // And the grid's own geometry, which nothing else keeps in step with the file.
        controller.Metrics.Columns.SizeOf(5).ShouldBe(GridMetrics.WidthToPixels(width.Value), 0.01);
    }

    [Fact]
    public async Task ColumnFormattingTravelsWithTheColumn()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectColumn(0);
        controller.ToggleBold();

        controller.Selection.SelectColumn(0);
        controller.Copy();
        controller.Selection.SelectColumn(5);
        controller.Paste();

        // Applied to the <col> element rather than to a cell, which is the only way it can reach the
        // rows that do not exist yet.
        controller.Sheet.GetColumnStyleIndex(5).ShouldBe(controller.Sheet.GetColumnStyleIndex(0));
    }

    [Fact]
    public async Task CuttingARowLeavesTheSourceRowEmpty()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRow(0);
        controller.Cut();

        controller.Selection.SelectRow(7);
        controller.Paste();

        Text(controller.Sheet, "A1").ShouldBeNull();
        Text(controller.Sheet, "A8").ShouldBe("Widget");
    }

    [Fact]
    public async Task CopyingAWholeColumnMovesOnlyHorizontally()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectColumn(0);
        controller.Copy();

        controller.Selection.MoveTo(CellRef.Parse("F4"));
        controller.Paste();

        Text(controller.Sheet, "F1").ShouldBe("Widget");
        Text(controller.Sheet, "F2").ShouldBe("1234.5");
    }

    // ---- the undo contract ----

    [Fact]
    public async Task APasteIsOneUndoStep()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRange(CellRange.Parse("A1:C1"));
        controller.Copy();
        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste();

        controller.Undo();

        Text(controller.Sheet, "A8").ShouldBeNull();
        Text(controller.Sheet, "B8").ShouldBeNull();
        Text(controller.Sheet, "C8").ShouldBeNull();
    }

    [Fact]
    public async Task UndoingACutPasteBringsTheSourceBack()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectRange(CellRange.Parse("A1:B1"));
        controller.Cut();
        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste();

        controller.Undo();

        Text(controller.Sheet, "A1").ShouldBe("Widget");
        Text(controller.Sheet, "B1").ShouldBe("42");
        Text(controller.Sheet, "A8").ShouldBeNull();
    }

    [Fact]
    public async Task PastingOverAFormulaUndoesBackToTheFormula()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();
        controller.Selection.MoveTo(CellRef.Parse("C1"));
        controller.Paste();

        controller.Sheet.GetFormula(CellRef.Parse("C1")).ShouldBeNull();

        controller.Undo();
        controller.Sheet.GetFormula(CellRef.Parse("C1")).ShouldBe("B1*2");
    }

    // ---- abandoning the clipboard ----

    [Fact]
    public async Task ACopyStaysOnTheClipboardSoItCanBePastedTwice()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();
        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste();

        controller.CanPaste.ShouldBeTrue();

        controller.Selection.MoveTo(CellRef.Parse("A9"));
        controller.Paste();
        Text(controller.Sheet, "A9").ShouldBe("Widget");
    }

    [Fact]
    public async Task ACutIsSpentByThePasteThatUsesIt()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Cut();
        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.Paste();

        // Pasting it again would move cells that are no longer there.
        controller.CanPaste.ShouldBeFalse();
        controller.ClipboardRange.ShouldBeNull();
    }

    [Fact]
    public async Task TypingSupersedesAPendingCopy()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();

        controller.Selection.MoveTo(CellRef.Parse("A8"));
        controller.SetActiveCellText("something else");

        controller.ClipboardRange.ShouldBeNull();
    }

    [Fact]
    public async Task ClearingTheSelectionAbandonsTheClipboard()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();
        controller.ClearSelection();

        controller.CanPaste.ShouldBeFalse();
    }

    [Fact]
    public async Task AbandoningTheClipboardRaisesTheEventAHostAnimatesOn()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        var raised = 0;
        controller.ClipboardChanged += (_, _) => raised++;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();
        controller.ClearClipboard();

        raised.ShouldBe(2);
    }

    [Fact]
    public async Task PastingWithAnEmptyClipboardDoesNothing()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Paste().ShouldBeFalse();
        controller.CanUndo.ShouldBeFalse();
    }
}
