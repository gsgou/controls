using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class WorkbookCalcTests
{
    static async Task<Workbook> OpenAsync()
    {
        using var source = new MemoryStream(WorkbookFixture.Build(), writable: false);
        return await Workbook.OpenAsync(source);
    }

    [Fact]
    public async Task FormulasInTheFileAreIndexedAndComputed()
    {
        using var workbook = await OpenAsync();

        // The fixture has C1 = B1*2 with B1 = 42.
        workbook.GetEffectiveValue("Data", CellRef.Parse("C1")).AsNumber().ShouldBe(84);
        workbook.Calc.FormulaCount.ShouldBe(1);
    }

    [Fact]
    public async Task EditingAnInputRecomputesTheFormulaThatDependsOnIt()
    {
        using var workbook = await OpenAsync();
        var c1 = CellRef.Parse("C1");

        workbook.GetEffectiveValue("Data", c1).AsNumber().ShouldBe(84);

        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(100)));

        workbook.GetEffectiveValue("Data", c1).AsNumber().ShouldBe(200);
    }

    [Fact]
    public async Task UndoRecomputesToo()
    {
        using var workbook = await OpenAsync();
        var c1 = CellRef.Parse("C1");

        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(100)));
        workbook.GetEffectiveValue("Data", c1).AsNumber().ShouldBe(200);

        workbook.Undo.Undo();

        workbook.GetEffectiveValue("Data", c1).AsNumber().ShouldBe(84);
    }

    [Fact]
    public async Task EnteringANewFormulaComputesImmediately()
    {
        using var workbook = await OpenAsync();
        var target = CellRef.Parse("E1");

        workbook.Execute(new SetCellFormulaCommand("Data", target, "SUM(B1,B1)"));

        workbook.GetEffectiveValue("Data", target).AsNumber().ShouldBe(84);
    }

    [Fact]
    public async Task ReplacingAFormulaWithALiteralStopsItRecomputing()
    {
        using var workbook = await OpenAsync();
        var c1 = CellRef.Parse("C1");

        workbook.Execute(new SetCellValueCommand("Data", c1, CellValue.FromNumber(7)));
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(1000)));

        workbook.GetEffectiveValue("Data", c1).AsNumber().ShouldBe(7, "C1 is a literal now and must not track B1");
    }

    [Fact]
    public async Task SavingWritesFreshCachedValuesIntoFormulaCells()
    {
        // Every reader other than Excel shows the cached value. Leaving it stale means the saved file
        // displays numbers that are simply wrong.
        using var workbook = await OpenAsync();
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(50)));

        var saved = workbook.ToArray();
        var xml = System.Text.Encoding.UTF8.GetString(PackageComparer.ReadEntry(saved, "xl/worksheets/sheet1.xml"));

        xml.ShouldContain("100", Case.Sensitive, "C1's cached result should have been rewritten to 50*2");

        using var reopened = await Workbook.OpenAsync(new MemoryStream(saved));
        reopened["Data"].GetValue(CellRef.Parse("C1")).AsNumber().ShouldBe(100);
        reopened["Data"].GetFormula(CellRef.Parse("C1")).ShouldBe("B1*2", "the formula itself must survive");
    }

    [Fact]
    public async Task GetDisplayValuePrefersTheComputedResult()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(3)));

        sheet.GetDisplayValue(CellRef.Parse("C1")).AsNumber().ShouldBe(6);
    }

    [Fact]
    public async Task EvaluateRunsAnExpressionWithoutStoringIt()
    {
        using var workbook = await OpenAsync();

        workbook.Evaluate("B1*3", "Data", CellRef.Parse("Z1")).AsNumber().ShouldBe(126);
        workbook.Calc.IsFormula(new CellAddress("Data", CellRef.Parse("Z1"))).ShouldBeFalse();
    }

    [Fact]
    public async Task ACircularReferenceIsReportedAsUnsupportedRatherThanCrashing()
    {
        var collector = new Shiny.Controls.Office.Packaging.UnsupportedFeatureCollector();
        using var source = new MemoryStream(WorkbookFixture.Build(), writable: false);
        using var workbook = await Workbook.OpenAsync(source, collector);

        workbook.Execute(new SetCellFormulaCommand("Data", CellRef.Parse("F1"), "G1+1"));
        workbook.Execute(new SetCellFormulaCommand("Data", CellRef.Parse("G1"), "F1+1"));

        workbook.Calc.CircularCells.ShouldNotBeEmpty();
        workbook.GetEffectiveValue("Data", CellRef.Parse("F1")).AsNumber().ShouldBe(0);
    }
}
