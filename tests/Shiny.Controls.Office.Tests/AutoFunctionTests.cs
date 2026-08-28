using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// AutoSum and its siblings. Everything worth testing is in the range and the destination the planner
/// picks — the formula itself is one string concatenation.
/// </summary>
public class AutoFunctionTests
{
    static Workbook Numbers(params (string Reference, double Value)[] cells)
    {
        var workbook = Workbook.Create("Sheet1");
        foreach (var (reference, value) in cells)
            workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse(reference), CellValue.FromNumber(value)));

        return workbook;
    }

    static SpreadsheetController Controller(Workbook workbook) => new(workbook, workbook.Sheets[0]);

    static string? FormulaAt(Workbook workbook, string reference)
        => workbook.Sheets[0].GetFormula(CellRef.Parse(reference));

    [Fact]
    public void SingleCell_TotalsTheRunAbove()
    {
        using var workbook = Numbers(("A1", 1), ("A2", 2), ("A3", 3));
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("A4"));
        controller.ApplyAutoFunction(AutoFunction.Sum).ShouldBeTrue();

        FormulaAt(workbook, "A4").ShouldBe("SUM(A1:A3)");
    }

    [Fact]
    public void SingleCell_StopsAtABlank()
    {
        using var workbook = Numbers(("A1", 1), ("A3", 3), ("A4", 4));
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("A5"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "A5").ShouldBe("SUM(A3:A4)", "the blank A2 ends the run");
    }

    [Fact]
    public void SingleCell_StopsAtAnExistingTotal()
    {
        // The classic AutoSum mistake: a second total under the first silently counts everything twice,
        // and the numbers give no sign of it.
        using var workbook = Numbers(("B1", 1), ("B2", 2));
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("B3"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        controller.Selection.MoveTo(CellRef.Parse("B4"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "B4").ShouldBeNull("there is no run to total: the only cell above is a SUM");
    }

    [Fact]
    public void SingleCell_FallsBackToTheRunOnTheLeft()
    {
        using var workbook = Numbers(("A2", 1), ("B2", 2), ("C2", 3));
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("D2"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "D2").ShouldBe("SUM(A2:C2)");
    }

    [Fact]
    public void NothingToTotal_WritesNothing()
    {
        using var workbook = Numbers();
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("C5"));
        controller.ApplyAutoFunction(AutoFunction.Sum).ShouldBeFalse();

        workbook.Undo.CanUndo.ShouldBeFalse("a no-op must not occupy an undo step");
    }

    [Fact]
    public void SelectedColumn_TotalsIntoTheRowBelow()
    {
        using var workbook = Numbers(("A1", 5), ("A2", 6));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:A2"));
        controller.ApplyAutoFunction(AutoFunction.Average);

        FormulaAt(workbook, "A3").ShouldBe("AVERAGE(A1:A2)");
    }

    [Fact]
    public void SelectionEndingInABlank_TotalsIntoThatBlank()
    {
        // Selecting the numbers *and* the empty cell under them is the standard gesture, and it means
        // "put it here" rather than "total the blank too and spill into the next row".
        using var workbook = Numbers(("A1", 5), ("A2", 6));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:A3"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "A3").ShouldBe("SUM(A1:A2)");
        FormulaAt(workbook, "A4").ShouldBeNull();
    }

    [Fact]
    public void SelectedRow_TotalsIntoTheColumnToTheRight()
    {
        using var workbook = Numbers(("A1", 1), ("B1", 2), ("C1", 3));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:C1"));
        controller.ApplyAutoFunction(AutoFunction.Max);

        FormulaAt(workbook, "D1").ShouldBe("MAX(A1:C1)");
    }

    [Fact]
    public void SelectedBlock_TotalsEachColumn()
    {
        using var workbook = Numbers(("A1", 1), ("B1", 2), ("A2", 3), ("B2", 4));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:B2"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "A3").ShouldBe("SUM(A1:A2)");
        FormulaAt(workbook, "B3").ShouldBe("SUM(B1:B2)");
    }

    [Fact]
    public void ABlock_IsOneUndoStep()
    {
        using var workbook = Numbers(("A1", 1), ("B1", 2), ("A2", 3), ("B2", 4));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:B2"));
        controller.ApplyAutoFunction(AutoFunction.Sum);
        controller.Undo();

        FormulaAt(workbook, "A3").ShouldBeNull();
        FormulaAt(workbook, "B3").ShouldBeNull();
    }

    [Fact]
    public void WholeColumnSelection_IsClampedToTheData()
    {
        // A column-header click selects a million rows. Totalling A1:A1048576 into A1048577 is not a
        // cell that exists, and the operation has to mean the populated part.
        using var workbook = Numbers(("A1", 1), ("A2", 2));
        var controller = Controller(workbook);

        controller.Selection.SelectColumn(0);
        controller.ApplyAutoFunction(AutoFunction.Sum);

        FormulaAt(workbook, "A3").ShouldBe("SUM(A1:A2)");
    }

    [Fact]
    public void TheResult_IsSelectedAfterwards()
    {
        using var workbook = Numbers(("A1", 1), ("A2", 2));
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:A2"));
        controller.ApplyAutoFunction(AutoFunction.Sum);

        controller.Selection.Active.ShouldBe(CellRef.Parse("A3"));
    }

    [Fact]
    public void TheFormula_Calculates()
    {
        using var workbook = Numbers(("A1", 10), ("A2", 20), ("A3", 30));
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("A4"));
        controller.ApplyAutoFunction(AutoFunction.Average);

        workbook.GetEffectiveValue("Sheet1", CellRef.Parse("A4")).AsNumber().ShouldBe(20);
    }
}
