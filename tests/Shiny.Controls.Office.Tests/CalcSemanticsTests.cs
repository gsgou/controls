using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// The coercion, error and blank-handling rules. These are the parts that look like details and are
/// actually the difference between agreeing with Excel and quietly disagreeing with it.
/// </summary>
public class CalcSemanticsTests
{
    static CalcTestGrid Grid() => new();

    [Fact]
    public void OperatorsCoerceTextToNumbers()
        => Grid().Number("\"1\"+1").ShouldBe(2);

    [Fact]
    public void AggregatesSkipTextInsteadOfCoercingIt()
    {
        // The asymmetry is deliberate: operators coerce, aggregate functions ignore.
        var grid = Grid().Set("A1", 1d).Set("A2", "not a number").Set("A3", 2d);
        grid.Number("SUM(A1:A3)").ShouldBe(3);
    }

    [Fact]
    public void TextThatIsNotANumberFailsAnOperator()
        => Grid().Error("\"abc\"+1").ShouldBe(CellError.Value);

    [Fact]
    public void BooleansCoerceToOneAndZero()
    {
        Grid().Number("TRUE+0").ShouldBe(1);
        Grid().Number("FALSE+0").ShouldBe(0);
    }

    [Fact]
    public void BooleansInsideARangeAreSkippedBySum()
    {
        var grid = Grid().Set("A1", 5d).Set("A2", true);
        grid.Number("SUM(A1:A2)").ShouldBe(5);
    }

    [Fact]
    public void BlankBehavesAsZeroInArithmetic()
        => Grid().Number("Z99+1").ShouldBe(1);

    [Fact]
    public void BlankEqualsBothZeroAndEmptyText()
    {
        var grid = Grid();
        grid.Bool("Z99=0").ShouldBeTrue();
        grid.Bool("Z99=\"\"").ShouldBeTrue();
    }

    [Fact]
    public void ErrorsPropagateThroughOperators()
    {
        var grid = Grid().SetError("A1", CellError.Div0);
        grid.Error("A1+1").ShouldBe(CellError.Div0);
        grid.Error("A1&\"x\"").ShouldBe(CellError.Div0);
    }

    [Fact]
    public void TheLeftmostErrorWins()
    {
        var grid = Grid().SetError("A1", CellError.Ref).SetError("A2", CellError.Div0);
        grid.Error("A1+A2").ShouldBe(CellError.Ref);
    }

    [Fact]
    public void DivisionByZeroIsAnErrorValueNotAnException()
        => Grid().Error("1/0").ShouldBe(CellError.Div0);

    [Fact]
    public void ComparisonOrdersNumbersBelowTextBelowBooleans()
    {
        var grid = Grid();
        grid.Bool("1<\"a\"").ShouldBeTrue();
        grid.Bool("\"a\"<TRUE").ShouldBeTrue();
    }

    [Fact]
    public void TextComparisonIsCaseInsensitive()
        => Grid().Bool("\"ABC\"=\"abc\"").ShouldBeTrue();

    [Fact]
    public void ExactIsCaseSensitiveUnlikeEquals()
    {
        var grid = Grid();
        grid.Bool("EXACT(\"ABC\",\"abc\")").ShouldBeFalse();
        grid.Bool("\"ABC\"=\"abc\"").ShouldBeTrue();
    }

    [Fact]
    public void ConcatenationStringifiesNumbersWithoutFormatting()
        => Grid().Text("1&2").ShouldBe("12");

    [Fact]
    public void IfDoesNotEvaluateTheBranchItDoesNotTake()
    {
        // The whole point of the guard: without lazy arguments this returns #DIV/0!.
        var grid = Grid().Set("A1", 0d);
        grid.Text("IF(A1=0,\"safe\",1/A1)").ShouldBe("safe");
    }

    [Fact]
    public void IfWithoutAFalseBranchReturnsFalse()
        => Grid().Bool("IF(1=2,\"yes\")").ShouldBeFalse();

    [Fact]
    public void IsErrorInspectsRatherThanPropagates()
    {
        var grid = Grid().SetError("A1", CellError.Div0);
        grid.Bool("ISERROR(A1)").ShouldBeTrue();
        grid.Bool("ISNA(A1)").ShouldBeFalse();
        grid.Bool("ISERR(A1)").ShouldBeTrue();
    }

    [Fact]
    public void IsErrAndIsNaSplitOnNotAvailable()
    {
        var grid = Grid().SetError("A1", CellError.NotAvailable);
        grid.Bool("ISNA(A1)").ShouldBeTrue();
        grid.Bool("ISERR(A1)").ShouldBeFalse();
        grid.Bool("ISERROR(A1)").ShouldBeTrue();
    }

    [Fact]
    public void IfErrorCatchesAnything()
        => Grid().Text("IFERROR(1/0,\"caught\")").ShouldBe("caught");

    [Fact]
    public void IfNaOnlyCatchesNotAvailable()
    {
        Grid().Error("IFNA(1/0,\"caught\")").ShouldBe(CellError.Div0);
        Grid().Text("IFNA(NA(),\"caught\")").ShouldBe("caught");
    }

    [Fact]
    public void UnknownFunctionIsANameError()
        => Grid().Error("NOSUCHFUNC(1)").ShouldBe(CellError.Name);

    [Fact]
    public void ReferenceToAMissingSheetIsARefError()
        => Grid().Error("Nope!A1").ShouldBe(CellError.Ref);

    [Fact]
    public void CrossSheetReferencesResolve()
    {
        var grid = Grid();
        grid.AddSheet("Data");
        grid.Set("Data!A1", 42d);
        grid.Number("Data!A1").ShouldBe(42);
    }

    [Fact]
    public void ARangeUsedWhereAScalarIsExpectedTakesItsTopLeft()
    {
        var grid = Grid().Set("A1", 5d).Set("A2", 9d);
        grid.Number("A1:A2*2").ShouldBe(10);
    }

    [Fact]
    public void WrongArgumentCountIsAValueError()
        => Grid().Error("ABS()").ShouldBe(CellError.Value);
}
