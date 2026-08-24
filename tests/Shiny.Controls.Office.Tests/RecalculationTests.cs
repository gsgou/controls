using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class RecalculationTests
{
    [Fact]
    public void AChainRecomputesInDependencyOrder()
    {
        var grid = new CalcTestGrid().Set("A1", 2d);
        grid.SetFormula("B1", "A1*2");
        grid.SetFormula("C1", "B1+1");
        grid.SetFormula("D1", "C1*10");

        grid.Get("D1").AsNumber().ShouldBe(50);
    }

    [Fact]
    public void EditingAnInputPropagatesThroughTheWholeChain()
    {
        var grid = new CalcTestGrid().Set("A1", 2d);
        grid.SetFormula("B1", "A1*2");
        grid.SetFormula("C1", "B1+1");

        grid.Set("A1", 10d);
        grid.Engine.Recalculate([new CellAddress("Sheet1", CellRef.Parse("A1"))], grid);

        grid.Get("B1").AsNumber().ShouldBe(20);
        grid.Get("C1").AsNumber().ShouldBe(21);
    }

    [Fact]
    public void OnlyAffectedCellsAreRecalculated()
    {
        var grid = new CalcTestGrid().Set("A1", 1d).Set("D1", 100d);
        grid.SetFormula("B1", "A1+1");
        grid.SetFormula("E1", "D1+1");

        var recalculated = grid.Engine.Recalculate([new CellAddress("Sheet1", CellRef.Parse("A1"))], grid);

        recalculated.ShouldContain(new CellAddress("Sheet1", CellRef.Parse("B1")));
        recalculated.ShouldNotContain(new CellAddress("Sheet1", CellRef.Parse("E1")));
    }

    [Fact]
    public void ARangeDependencyInvalidatesOnAnyCellInIt()
    {
        var grid = new CalcTestGrid().Set("A1", 1d).Set("A2", 2d).Set("A3", 3d);
        grid.SetFormula("B1", "SUM(A1:A3)");
        grid.Get("B1").AsNumber().ShouldBe(6);

        grid.Set("A2", 20d);
        grid.Engine.Recalculate([new CellAddress("Sheet1", CellRef.Parse("A2"))], grid);

        grid.Get("B1").AsNumber().ShouldBe(24);
    }

    [Fact]
    public void CircularReferencesAreReportedRatherThanOverflowingTheStack()
    {
        // The failure mode this prevents is a StackOverflowException, which cannot be caught and takes
        // the whole process down.
        var grid = new CalcTestGrid();
        grid.SetFormula("A1", "B1+1");
        grid.SetFormula("B1", "A1+1");

        grid.Engine.CircularCells.ShouldNotBeEmpty();
        grid.Engine.CircularCells.ShouldContain(new CellAddress("Sheet1", CellRef.Parse("A1")));
        grid.Get("A1").AsNumber().ShouldBe(0);
    }

    [Fact]
    public void ASelfReferenceIsCircular()
    {
        var grid = new CalcTestGrid();
        grid.SetFormula("A1", "A1+1");
        grid.Engine.CircularCells.ShouldContain(new CellAddress("Sheet1", CellRef.Parse("A1")));
    }

    [Fact]
    public void ACycleDoesNotStopUnrelatedCellsCalculating()
    {
        var grid = new CalcTestGrid().Set("D1", 5d);
        grid.SetFormula("A1", "B1");
        grid.SetFormula("B1", "A1");
        grid.SetFormula("E1", "D1*2");

        grid.Get("E1").AsNumber().ShouldBe(10);
    }

    [Fact]
    public void BreakingACycleClearsIt()
    {
        var grid = new CalcTestGrid();
        grid.SetFormula("A1", "B1+1");
        grid.SetFormula("B1", "A1+1");
        grid.Engine.CircularCells.ShouldNotBeEmpty();

        grid.SetFormula("B1", "10");
        grid.Engine.RecalculateAll(grid);

        grid.Engine.CircularCells.ShouldBeEmpty();
        grid.Get("A1").AsNumber().ShouldBe(11);
    }

    [Fact]
    public void RemovingAFormulaDropsItsDependencies()
    {
        var grid = new CalcTestGrid().Set("A1", 1d);
        var b1 = new CellAddress("Sheet1", CellRef.Parse("B1"));
        grid.SetFormula("B1", "A1+1");

        grid.Engine.Dependencies.DirectDependents(new CellAddress("Sheet1", CellRef.Parse("A1"))).ShouldContain(b1);

        grid.Engine.RemoveFormula(b1);

        grid.Engine.Dependencies.DirectDependents(new CellAddress("Sheet1", CellRef.Parse("A1"))).ShouldNotContain(b1);
    }

    [Fact]
    public void DiamondDependenciesEvaluateEachCellOnce()
    {
        //     A1
        //    /  \
        //   B1  C1
        //    \  /
        //     D1
        var grid = new CalcTestGrid().Set("A1", 3d);
        grid.SetFormula("B1", "A1*2");
        grid.SetFormula("C1", "A1*3");
        grid.SetFormula("D1", "B1+C1");

        grid.Get("D1").AsNumber().ShouldBe(15);

        var order = grid.Engine.Dependencies.AllInEvaluationOrder(out var cycle);
        cycle.ShouldBeEmpty();
        order.Count.ShouldBe(3);
        order.IndexOf(new CellAddress("Sheet1", CellRef.Parse("D1"))).ShouldBe(2);
    }

    [Fact]
    public void AVeryLargeRangeRegistersOnlyItsOriginToBoundMemory()
    {
        // A whole-column reference would otherwise register a million dependency entries per formula.
        var reads = DependencyGraph.Collect(FormulaParser.Parse("SUM(A1:A1048576)"), "Sheet1").ToList();
        reads.Count.ShouldBe(1);
    }

    [Fact]
    public void FormulasThatFailToParseBecomeNameErrorsRatherThanVanishing()
    {
        var grid = new CalcTestGrid();
        var address = new CellAddress("Sheet1", CellRef.Parse("A1"));

        grid.Engine.SetFormula(address, "1+").ShouldBeFalse();

        grid.Engine.TryGetComputed(address, out var value).ShouldBeTrue();
        value.AsError().ShouldBe(CellError.Name);
    }

    [Fact]
    public void NowAndTodayComeFromTheInjectedClock()
    {
        var grid = new CalcTestGrid { Now = new DateTime(2020, 3, 1, 6, 0, 0) };

        grid.Number("YEAR(TODAY())").ShouldBe(2020);
        grid.Number("MONTH(TODAY())").ShouldBe(3);
        grid.Number("DAY(TODAY())").ShouldBe(1);
        grid.Number("HOUR(NOW())").ShouldBe(6);
    }
}

public class ExcelDateTests
{
    [Theory]
    [InlineData(1, 1900, 1, 1)]
    [InlineData(59, 1900, 2, 28)]
    [InlineData(61, 1900, 3, 1)]
    [InlineData(367, 1901, 1, 1)]
    [InlineData(44927, 2023, 1, 1)]
    [InlineData(46081, 2026, 2, 28)]
    public void SerialsMapToDates(double serial, int year, int month, int day)
    {
        ExcelDate.TryToDateTime(serial, out var date).ShouldBeTrue();
        date.Year.ShouldBe(year);
        date.Month.ShouldBe(month);
        date.Day.ShouldBe(day);
    }

    [Fact]
    public void SerialSixtyIsThePhantomLeapDay()
    {
        // 29 February 1900 never existed. Excel believes in it for Lotus compatibility, and every date
        // function has to agree with the spreadsheet rather than with the calendar.
        var grid = new CalcTestGrid();
        grid.Number("YEAR(60)").ShouldBe(1900);
        grid.Number("MONTH(60)").ShouldBe(2);
        grid.Number("DAY(60)").ShouldBe(29);
    }

    [Fact]
    public void DatesRoundTripThroughSerials()
    {
        var original = new DateTime(2026, 8, 24);
        var serial = ExcelDate.FromDateTime(original);

        ExcelDate.TryToDateTime(serial, out var restored).ShouldBeTrue();
        restored.Date.ShouldBe(original);
    }

    [Theory]
    [InlineData("DATE(2026,8,24)", 46258)]
    [InlineData("DATE(2026,13,1)", 46388)]   // month 13 rolls into the next year
    [InlineData("DAYS(DATE(2026,1,10),DATE(2026,1,1))", 9)]
    public void DateArithmetic(string formula, double expected)
        => new CalcTestGrid().Number(formula).ShouldBe(expected);

    [Fact]
    public void EomonthFindsTheLastDayOfTheMonth()
    {
        var grid = new CalcTestGrid();
        grid.Number("DAY(EOMONTH(DATE(2024,2,10),0))").ShouldBe(29); // 2024 is a real leap year
        grid.Number("DAY(EOMONTH(DATE(2026,2,10),0))").ShouldBe(28);
    }

    [Fact]
    public void TimePartsSurviveRounding()
    {
        var grid = new CalcTestGrid();
        grid.Number("HOUR(0.5)").ShouldBe(12);
        grid.Number("MINUTE(TIME(9,45,0))").ShouldBe(45);
    }
}

public class FormulaSelfRecalculationTests
{
    [Fact]
    public void ANewFormulaComputesItselfNotJustItsDependents()
    {
        // The trap: recalculation walks the dependency graph outward from what changed, and a brand new
        // formula has no dependents yet - so seeding only the dependents leaves it holding nothing.
        var grid = new CalcTestGrid().Set("A1", 5d).Set("A2", 7d);

        var address = new CellAddress("Sheet1", CellRef.Parse("A3"));
        grid.Engine.SetFormula(address, "SUM(A1:A2)");
        grid.Engine.Recalculate([address], grid);

        grid.Engine.TryGetComputed(address, out var value).ShouldBeTrue();
        value.AsNumber().ShouldBe(12);
    }

    [Fact]
    public void EditingAnExistingFormulaRecomputesIt()
    {
        var grid = new CalcTestGrid().Set("A1", 5d);
        var address = new CellAddress("Sheet1", CellRef.Parse("B1"));

        grid.Engine.SetFormula(address, "A1*2");
        grid.Engine.Recalculate([address], grid);
        grid.Engine.TryGetComputed(address, out var first).ShouldBeTrue();
        first.AsNumber().ShouldBe(10);

        grid.Engine.SetFormula(address, "A1*10");
        grid.Engine.Recalculate([address], grid);

        grid.Engine.TryGetComputed(address, out var second).ShouldBeTrue();
        second.AsNumber().ShouldBe(50);
    }
}
