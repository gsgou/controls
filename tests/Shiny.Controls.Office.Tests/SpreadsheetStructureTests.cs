using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Inserting and removing rows and columns, and the reference arithmetic that has to follow them.
/// </summary>
/// <remarks>
/// Moving the cells is the easy half. The half that goes wrong quietly is everything that <em>named</em>
/// them: a formula on another sheet, a merged range that straddles the insertion point, a column width
/// left behind on the column that took the moved one's place.
/// </remarks>
public class SpreadsheetStructureTests
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

    // ---- the reference shifter, on its own ----

    [Theory]
    [InlineData("B1*2", 0, 2, "B3*2")]
    [InlineData("B1*2", 1, 0, "C1*2")]
    [InlineData("SUM(A1:A3)", 0, 1, "SUM(A2:A4)")]
    [InlineData("$B$1*2", 0, 2, "$B$1*2")]
    [InlineData("$B1*2", 0, 2, "$B3*2")]
    [InlineData("B$1*2", 0, 2, "B$1*2")]
    public void ACopiedFormulaMovesItsRelativeReferencesOnly(string formula, int columns, int rows, string expected)
        => FormulaReferenceShifter.Translate(formula, columns, rows).ShouldBe(expected);

    [Theory]
    // A function name is not a cell reference, however much LOG10 looks like one.
    [InlineData("LOG10(A1)", "LOG10(A2)")]
    // Nor is anything inside a string literal, however much it looks like an address.
    [InlineData("\"A1\"&A1", "\"A1\"&A2")]
    // Nor is the REF in #REF!, which would otherwise read as a sheet called REF.
    [InlineData("#REF!+A1", "#REF!+A2")]
    // TRUE lexes as a name, not as column TRU row E.
    [InlineData("IF(TRUE,A1,0)", "IF(TRUE,A2,0)")]
    public void TheScannerOnlyTouchesThingsThatAreActuallyReferences(string formula, string expected)
        => FormulaReferenceShifter.Translate(formula, 0, 1).ShouldBe(expected);

    [Fact]
    public void ASheetPrefixCarriesAcrossTheColonOfARangeAndNoFurther()
    {
        // Both ends belong to Sheet1; the trailing B2 is local and shifts all the same.
        FormulaReferenceShifter.Translate("SUM(Sheet1!A1:C3)+B2", 0, 1)
            .ShouldBe("SUM(Sheet1!A2:C4)+B3");
    }

    [Fact]
    public void AReferenceMovedOffTheSheetBecomesRefError()
        => FormulaReferenceShifter.Translate("A1*2", 0, -1).ShouldBe("#REF!*2");

    [Fact]
    public void AnInsertMovesAbsoluteReferencesToo()
    {
        // Unlike a copy: the cells themselves moved, so a $-pinned reference has to follow or it
        // silently starts reading the blank row the insert left behind.
        FormulaReferenceShifter.ForInsertedRows("$A$5", "Data", "Data", 2, 1).ShouldBe("$A$6");
    }

    [Fact]
    public void AnInsertLeavesReferencesAboveItAlone()
        => FormulaReferenceShifter.ForInsertedRows("A1+A9", "Data", "Data", 4, 1).ShouldBe("A1+A10");

    [Fact]
    public void ARangeStraddlingAnInsertGrowsRatherThanMoves()
        => FormulaReferenceShifter.ForInsertedRows("SUM(A1:A10)", "Data", "Data", 4, 1).ShouldBe("SUM(A1:A11)");

    [Fact]
    public void AFormulaOnAnotherSheetOnlyShiftsWhenItNamesTheEditedOne()
    {
        FormulaReferenceShifter.ForInsertedRows("A5", "Summary", "Data", 0, 1).ShouldBe("A5");
        FormulaReferenceShifter.ForInsertedRows("Data!A5", "Summary", "Data", 0, 1).ShouldBe("Data!A6");
    }

    [Fact]
    public void AReferenceIntoDeletedRowsBecomesRefError()
        => FormulaReferenceShifter.ForDeletedRows("A5+A9", "Data", "Data", 4, 1).ShouldBe("#REF!+A8");

    // ---- inserting rows ----

    [Fact]
    public async Task InsertingARowPushesTheCellsBelowItDown()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();

        Text(controller.Sheet, "A1").ShouldBeNull();
        Text(controller.Sheet, "A2").ShouldBe("Widget");
        Text(controller.Sheet, "B2").ShouldBe("42");
    }

    [Fact]
    public async Task InsertingARowRepointsTheFormulasThatNamedTheMovedCells()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();

        // C1's =B1*2 moved to C2 and now has to read B2, or it totals the blank row above it.
        controller.Sheet.GetFormula(CellRef.Parse("C2")).ShouldBe("B2*2");
    }

    [Fact]
    public async Task AnInsertOnOneSheetRepointsFormulasOnEveryOtherSheet()
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.BuildMultiSheet()));
        using var _ = workbook;

        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(600, 400);

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();

        // Summary!A1 held =Data!B1*2, and Data!B1 has just become Data!B2.
        workbook["Summary"].GetFormula(CellRef.Parse("A1")).ShouldBe("Data!B2*2");
    }

    [Fact]
    public async Task InsertingAColumnPushesTheCellsRight()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertColumns();

        Text(controller.Sheet, "A1").ShouldBeNull();
        Text(controller.Sheet, "B1").ShouldBe("Widget");
        controller.Sheet.GetFormula(CellRef.Parse("D1")).ShouldBe("C1*2");
    }

    [Fact]
    public async Task InsertingSeveralRowsAtOnceOpensThatManyGaps()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows(3);

        Text(controller.Sheet, "A4").ShouldBe("Widget");
    }

    [Fact]
    public async Task AnInsertIsOneUndoStep()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();
        controller.Undo();

        Text(controller.Sheet, "A1").ShouldBe("Widget");
        controller.Sheet.GetFormula(CellRef.Parse("C1")).ShouldBe("B1*2");
    }

    [Fact]
    public async Task RedoingAnInsertPutsItBack()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();
        controller.Undo();
        controller.Redo();

        Text(controller.Sheet, "A2").ShouldBe("Widget");
    }

    [Fact]
    public async Task AnInsertAbandonsAPendingCopy()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.Copy();
        controller.InsertRows();

        // The capture describes addresses the sheet no longer has; pasting it would put the cells back
        // one row out.
        controller.CanPaste.ShouldBeFalse();
    }

    // ---- deleting ----

    [Fact]
    public async Task DeletingARowClosesTheGap()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.DeleteRows();

        Text(controller.Sheet, "A1").ShouldBe("1234.5");
    }

    [Fact]
    public async Task UndoingADeleteRestoresTheContentsAndTheFormulasThatNamedThem()
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.BuildMultiSheet()));
        using var _ = workbook;

        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(600, 400);

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.DeleteRows();

        // Summary named Data!B1, which is gone rather than moved. The sheet prefix stays, which is
        // what Excel writes too: =Sheet2!#REF!, not a bare #REF!.
        workbook["Summary"].GetFormula(CellRef.Parse("A1")).ShouldBe("Data!#REF!*2");

        controller.Undo();

        Text(workbook["Data"], "A1").ShouldBe("Widget");
        workbook["Summary"].GetFormula(CellRef.Parse("A1")).ShouldBe("Data!B1*2");
    }

    // ---- the grid's own geometry ----

    [Fact]
    public void InsertingMovesTheSizeOverridesAlongWithTheRows()
    {
        var axis = new AxisMetrics(20, 100);
        axis.SetSize(5, 60);

        axis.Shift(3, 2);

        axis.SizeOf(5).ShouldBe(20);
        axis.SizeOf(7).ShouldBe(60);
    }

    [Fact]
    public void DeletingTakesTheSizesInTheBandWithItAndPullsTheRestBack()
    {
        var axis = new AxisMetrics(20, 100);
        axis.SetSize(3, 40);
        axis.SetSize(6, 60);

        axis.Shift(3, -2);

        axis.SizeOf(3).ShouldBe(20);
        axis.SizeOf(4).ShouldBe(60);
        axis.OverrideCount.ShouldBe(1);
    }

    [Fact]
    public void AShiftMovesHiddenEntriesToo()
    {
        var axis = new AxisMetrics(20, 100);
        axis.SetHidden(4, true);

        axis.Shift(0, 1);

        axis.IsHidden(4).ShouldBeFalse();
        axis.IsHidden(5).ShouldBeTrue();
    }

    [Fact]
    public void EntriesPushedOffTheEndOfTheAxisAreDropped()
    {
        var axis = new AxisMetrics(20, 10);
        axis.SetSize(9, 60);

        axis.Shift(0, 1);

        axis.OverrideCount.ShouldBe(0);
    }

    [Fact]
    public async Task AColumnWidthFollowsTheColumnItBelongsTo()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.SelectColumn(1);
        controller.SetColumnWidth(140);
        var width = controller.Sheet.GetColumnWidth(1);

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertColumns();

        controller.Sheet.GetColumnWidth(2).ShouldBe(width);
        controller.Sheet.GetColumnWidth(1).ShouldBeNull();
    }

    // ---- the file stays openable ----

    [Fact]
    public async Task RowsStayInAscendingOrderAfterAnInsert()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A2"));
        controller.InsertRows(2);

        // Excel reports a file whose rows are out of order as corrupt rather than repairing it, so
        // this is read off the element tree rather than through the model that could hide it.
        var indexes = workbook["Data"].Part.Worksheet!
            .GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!
            .Elements<DocumentFormat.OpenXml.Spreadsheet.Row>()
            .Select(x => x.RowIndex!.Value)
            .ToList();

        indexes.ShouldNotBeEmpty();
        indexes.ShouldBe(indexes.OrderBy(x => x).ToList());
    }

    [Fact]
    public async Task AnInsertSurvivesASaveAndReopen()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.InsertRows();

        using var buffer = new MemoryStream();
        await workbook.SaveToAsync(buffer);
        buffer.Position = 0;

        using var reopened = await Workbook.OpenAsync(buffer);
        Text(reopened["Data"], "A2").ShouldBe("Widget");
        reopened["Data"].GetFormula(CellRef.Parse("C2")).ShouldBe("B2*2");
    }
}
