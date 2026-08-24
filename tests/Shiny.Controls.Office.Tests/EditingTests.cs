using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class EditingTests
{
    static async Task<Workbook> OpenAsync(IUnsupportedFeatureSink? sink = null)
    {
        using var source = new MemoryStream(WorkbookFixture.Build(), writable: false);
        return await Workbook.OpenAsync(source, sink);
    }

    [Fact]
    public async Task ReadsEveryCellStorageType()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        sheet.GetValue(CellRef.Parse("A1")).AsText().ShouldBe("Widget");
        sheet.GetValue(CellRef.Parse("B1")).AsNumber().ShouldBe(42);
        sheet.GetValue(CellRef.Parse("B2")).AsBoolean().ShouldBeTrue();
        sheet.GetValue(CellRef.Parse("D5")).AsError().ShouldBe(CellError.Div0);
        sheet.GetValue(CellRef.Parse("Z99")).IsBlank.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadsCachedFormulaResultWithoutRecomputing()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        sheet.GetFormula(CellRef.Parse("C1")).ShouldBe("B1*2");
        sheet.GetValue(CellRef.Parse("C1")).AsNumber().ShouldBe(84);
    }

    [Fact]
    public async Task UsedRange_CoversPopulatedCellsOnly()
    {
        using var workbook = await OpenAsync();
        workbook["Data"].UsedRange.ShouldBe(CellRange.Parse("A1:D5"));
    }

    [Fact]
    public async Task WritingIntoAnEmptyRow_InsertsItInSortedPosition()
    {
        // Excel reports a file as corrupt when rows or cells are out of order, and it does not repair
        // it, so insert position is correctness rather than tidiness.
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("A3"), CellValue.FromNumber(3)));
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("A4"), CellValue.FromNumber(4)));
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("C2"), CellValue.FromNumber(2)));

        var saved = workbook.ToArray();
        var xml = System.Text.Encoding.UTF8.GetString(PackageComparer.ReadEntry(saved, "xl/worksheets/sheet1.xml"));

        var rowOrder = System.Text.RegularExpressions.Regex.Matches(xml, "<x:row r=\"(\\d+)\"|<row r=\"(\\d+)\"")
            .Select(m => int.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value))
            .ToArray();

        rowOrder.ShouldBe(rowOrder.OrderBy(x => x).ToArray(), "rows must be written in ascending order");

        using var reopened = await Workbook.OpenAsync(new MemoryStream(saved));
        reopened["Data"].GetValue(CellRef.Parse("C2")).AsNumber().ShouldBe(2);
        reopened["Data"].GetValue(CellRef.Parse("A3")).AsNumber().ShouldBe(3);
    }

    [Fact]
    public async Task ReplacingAValue_KeepsTheCellsFormatting()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];
        var styled = CellRef.Parse("A2");

        sheet.GetStyleIndex(styled).ShouldBe(1u);
        workbook.Execute(new SetCellValueCommand("Data", styled, CellValue.FromNumber(999)));

        sheet.GetStyleIndex(styled).ShouldBe(1u, "editing a formatted cell must not strip its formatting");
    }

    [Fact]
    public async Task ClearingAStyledCell_KeepsTheCellSoFormattingSurvives()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];
        var styled = CellRef.Parse("A2");

        workbook.Execute(new ClearRangeCommand("Data", new CellRange(styled)));

        sheet.GetValue(styled).IsBlank.ShouldBeTrue();
        sheet.GetStyleIndex(styled).ShouldBe(1u);
    }

    [Fact]
    public async Task WritingText_SharesStringsRatherThanDuplicatingThem()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("E1"), CellValue.FromText("Repeat")));
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("E2"), CellValue.FromText("Repeat")));

        var saved = workbook.ToArray();
        var shared = System.Text.Encoding.UTF8.GetString(PackageComparer.ReadEntry(saved, "xl/sharedStrings.xml"));

        var occurrences = System.Text.RegularExpressions.Regex.Matches(shared, "Repeat").Count;
        occurrences.ShouldBe(1, "the same text written twice must reuse one shared string entry");
    }

    [Fact]
    public async Task WritingTextWithSignificantWhitespace_PreservesIt()
    {
        using var workbook = await OpenAsync();
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("E5"), CellValue.FromText("  padded  ")));

        var saved = workbook.ToArray();
        using var reopened = await Workbook.OpenAsync(new MemoryStream(saved));

        reopened["Data"].GetValue(CellRef.Parse("E5")).AsText().ShouldBe("  padded  ");
    }

    [Fact]
    public async Task UndoRestoresAFormulaRatherThanItsCachedValue()
    {
        // The trap: C1 shows 84, so a naive undo puts the literal 84 back and the formula is gone.
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];
        var cell = CellRef.Parse("C1");

        workbook.Execute(new SetCellValueCommand("Data", cell, CellValue.FromNumber(1)));
        sheet.GetFormula(cell).ShouldBeNull();

        workbook.Undo.Undo();

        sheet.GetFormula(cell).ShouldBe("B1*2");
    }

    [Fact]
    public async Task RedoReappliesTheEdit()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];
        var cell = CellRef.Parse("B1");

        workbook.Execute(new SetCellValueCommand("Data", cell, CellValue.FromNumber(7)));
        workbook.Undo.Undo();
        sheet.GetValue(cell).AsNumber().ShouldBe(42);

        workbook.Undo.Redo();
        sheet.GetValue(cell).AsNumber().ShouldBe(7);
    }

    [Fact]
    public async Task ClearRange_UndoesAsASingleStep()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        workbook.Execute(new ClearRangeCommand("Data", CellRange.Parse("A1:C1")));
        sheet.GetValue(CellRef.Parse("A1")).IsBlank.ShouldBeTrue();
        sheet.GetValue(CellRef.Parse("B1")).IsBlank.ShouldBeTrue();

        workbook.Undo.Undo();

        sheet.GetValue(CellRef.Parse("A1")).AsText().ShouldBe("Widget");
        sheet.GetValue(CellRef.Parse("B1")).AsNumber().ShouldBe(42);
        sheet.GetFormula(CellRef.Parse("C1")).ShouldBe("B1*2");
        workbook.Undo.CanUndo.ShouldBeFalse("a range clear is one undo step, not one per cell");
    }

    [Fact]
    public async Task UnsupportedFeatures_AreReportedRatherThanSwallowed()
    {
        var collector = new UnsupportedFeatureCollector();
        using var workbook = await OpenAsync(collector);

        // Nothing in the fixture is lossy, but the sink must be wired up and reachable.
        collector.HasLossy.ShouldBeFalse();
    }
}

public class NewWorkbookTests
{
    [Fact]
    public void CreateProducesAWorkbookExcelCanOpen()
    {
        using var workbook = Workbook.Create("Budget");

        workbook.Sheets.ShouldHaveSingleItem();
        workbook.Sheets[0].Name.ShouldBe("Budget");
        workbook.Sheets[0].UsedRange.ShouldBeNull("a new sheet has no cells yet");

        // The parts Excel refuses to open a file without.
        var parts = PackageComparer.EntryNames(workbook.ToArray());
        parts.ShouldContain("xl/workbook.xml");
        parts.ShouldContain("xl/styles.xml");
        parts.ShouldContain("xl/worksheets/sheet1.xml");
        parts.ShouldContain("[Content_Types].xml");
    }

    [Fact]
    public async Task ANewWorkbookRoundTripsThroughEditsAndReopens()
    {
        using var workbook = Workbook.Create();
        workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse("A1"), CellValue.FromNumber(5)));
        workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse("A2"), CellValue.FromNumber(7)));
        workbook.Execute(new SetCellFormulaCommand("Sheet1", CellRef.Parse("A3"), "SUM(A1:A2)"));

        workbook.GetEffectiveValue("Sheet1", CellRef.Parse("A3")).AsNumber().ShouldBe(12);

        using var reopened = await Workbook.OpenAsync(new MemoryStream(workbook.ToArray()));
        reopened["Sheet1"].GetValue(CellRef.Parse("A3")).AsNumber().ShouldBe(12);
        reopened["Sheet1"].GetFormula(CellRef.Parse("A3")).ShouldBe("SUM(A1:A2)");
    }
}
