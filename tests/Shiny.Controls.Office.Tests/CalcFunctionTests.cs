using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class CalcFunctionTests
{
    static CalcTestGrid Sample()
    {
        // A1:A5 numbers, B1:B5 labels, C1:C5 mixed with a blank and text.
        var grid = new CalcTestGrid();
        grid.Set("A1", 10d).Set("A2", 20d).Set("A3", 30d).Set("A4", 40d).Set("A5", 50d);
        grid.Set("B1", "apple").Set("B2", "banana").Set("B3", "apricot").Set("B4", "cherry").Set("B5", "avocado");
        grid.Set("C1", 1d).Set("C2", "text").Set("C4", 3d).Set("C5", 5d);
        return grid;
    }

    [Theory]
    [InlineData("SUM(A1:A5)", 150)]
    [InlineData("AVERAGE(A1:A5)", 30)]
    [InlineData("MIN(A1:A5)", 10)]
    [InlineData("MAX(A1:A5)", 50)]
    [InlineData("COUNT(A1:A5)", 5)]
    [InlineData("COUNT(C1:C5)", 3)]
    [InlineData("COUNTA(C1:C5)", 4)]
    [InlineData("COUNTBLANK(C1:C5)", 1)]
    [InlineData("MEDIAN(A1:A5)", 30)]
    [InlineData("PRODUCT(A1:A3)", 6000)]
    [InlineData("LARGE(A1:A5,2)", 40)]
    [InlineData("SMALL(A1:A5,2)", 20)]
    [InlineData("SUM(A1:A5,100)", 250)]
    public void Aggregates(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected);

    [Fact]
    public void MedianOfAnEvenCountAveragesTheMiddleTwo()
        => Sample().Number("MEDIAN(A1:A4)").ShouldBe(25);

    [Fact]
    public void MinOfNothingIsZeroNotAnError()
        => Sample().Number("MIN(Z1:Z9)").ShouldBe(0);

    [Fact]
    public void AverageOfNothingIsDivideByZero()
        => Sample().Error("AVERAGE(Z1:Z9)").ShouldBe(CellError.Div0);

    [Theory]
    [InlineData("ROUND(2.5,0)", 3)]      // half away from zero, not banker's
    [InlineData("ROUND(-2.5,0)", -3)]
    [InlineData("ROUND(2.675,2)", 2.68)] // the classic binary floating point trap
    [InlineData("ROUND(1234.5678,-2)", 1200)]
    [InlineData("ROUNDUP(1.1,0)", 2)]
    [InlineData("ROUNDUP(-1.1,0)", -2)]
    [InlineData("ROUNDDOWN(1.9,0)", 1)]
    [InlineData("ROUNDDOWN(-1.9,0)", -1)]
    [InlineData("INT(-1.5)", -2)]        // INT floors; TRUNC does not
    [InlineData("TRUNC(-1.5)", -1)]
    public void Rounding(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected);

    [Theory]
    [InlineData("MOD(5,3)", 2)]
    [InlineData("MOD(-5,3)", 1)]   // Excel's MOD takes the divisor's sign, unlike C#'s %
    [InlineData("MOD(5,-3)", -1)]
    [InlineData("ABS(-4)", 4)]
    [InlineData("SIGN(-9)", -1)]
    [InlineData("POWER(2,10)", 1024)]
    [InlineData("SQRT(16)", 4)]
    [InlineData("LOG10(1000)", 3)]
    [InlineData("LOG(8,2)", 3)]
    [InlineData("CEILING(4.2,1)", 5)]
    [InlineData("FLOOR(4.8,1)", 4)]
    public void Arithmetic(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected, 1e-9);

    [Fact]
    public void ModByZeroIsDivideByZero()
        => Sample().Error("MOD(5,0)").ShouldBe(CellError.Div0);

    [Fact]
    public void SqrtOfANegativeIsANumError()
        => Sample().Error("SQRT(-1)").ShouldBe(CellError.Num);

    [Theory]
    [InlineData("LEN(\"hello\")", 5)]
    [InlineData("FIND(\"l\",\"hello\")", 3)]
    [InlineData("SEARCH(\"L\",\"hello\")", 3)]  // SEARCH is case-insensitive, FIND is not
    [InlineData("CODE(\"A\")", 65)]
    public void TextNumbers(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected);

    [Theory]
    [InlineData("LEFT(\"hello\",2)", "he")]
    [InlineData("RIGHT(\"hello\",2)", "lo")]
    [InlineData("MID(\"hello\",2,3)", "ell")]
    [InlineData("LEFT(\"hi\",99)", "hi")]
    [InlineData("MID(\"hi\",5,2)", "")]
    [InlineData("UPPER(\"aBc\")", "ABC")]
    [InlineData("LOWER(\"aBc\")", "abc")]
    [InlineData("PROPER(\"hello world\")", "Hello World")]
    [InlineData("TRIM(\"  a   b  \")", "a b")]
    [InlineData("CONCAT(\"a\",\"b\",\"c\")", "abc")]
    [InlineData("REPT(\"ab\",3)", "ababab")]
    [InlineData("SUBSTITUTE(\"a-b-c\",\"-\",\"+\")", "a+b+c")]
    [InlineData("SUBSTITUTE(\"a-b-c\",\"-\",\"+\",2)", "a-b+c")]
    [InlineData("REPLACE(\"abcdef\",2,3,\"X\")", "aXef")]
    [InlineData("CHAR(65)", "A")]
    [InlineData("TEXTJOIN(\",\",TRUE,\"a\",\"b\")", "a,b")]
    public void TextFunctions(string formula, string expected)
        => Sample().Text(formula).ShouldBe(expected);

    [Fact]
    public void TrimCollapsesInternalRunsUnlikeStringTrim()
        => Sample().Text("TRIM(\"  lots   of    space  \")").ShouldBe("lots of space");

    [Fact]
    public void FindIsCaseSensitiveAndFailsLoudly()
        => Sample().Error("FIND(\"L\",\"hello\")").ShouldBe(CellError.Value);

    [Fact]
    public void SearchSupportsWildcards()
        => Sample().Number("SEARCH(\"h?llo\",\"say hello\")").ShouldBe(5);

    [Theory]
    [InlineData("VLOOKUP(30,A1:B5,2,FALSE)", "apricot")]
    [InlineData("VLOOKUP(35,A1:B5,2,TRUE)", "apricot")]
    [InlineData("INDEX(B1:B5,2)", "banana")]
    public void Lookups(string formula, string expected)
        => Sample().Text(formula).ShouldBe(expected);

    [Fact]
    public void VlookupExactMissIsNotAvailable()
        => Sample().Error("VLOOKUP(35,A1:B5,2,FALSE)").ShouldBe(CellError.NotAvailable);

    [Fact]
    public void VlookupColumnOutOfRangeIsARefError()
        => Sample().Error("VLOOKUP(30,A1:B5,9,FALSE)").ShouldBe(CellError.Ref);

    [Theory]
    [InlineData("MATCH(30,A1:A5,0)", 3)]
    [InlineData("MATCH(35,A1:A5,1)", 3)]
    [InlineData("ROWS(A1:A5)", 5)]
    [InlineData("COLUMNS(A1:B5)", 2)]
    public void MatchAndShape(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected);

    [Fact]
    public void MatchExactMissIsNotAvailable()
        => Sample().Error("MATCH(99,A1:A5,0)").ShouldBe(CellError.NotAvailable);

    [Fact]
    public void RowAndColumnDefaultToTheEvaluatingCell()
    {
        var grid = Sample();
        grid.Number("ROW()", "C7").ShouldBe(7);
        grid.Number("COLUMN()", "C7").ShouldBe(3);
        grid.Number("ROW(B4)").ShouldBe(4);
    }

    [Theory]
    [InlineData("SUMIF(A1:A5,\">25\")", 120)]
    [InlineData("SUMIF(A1:A5,\">25\",A1:A5)", 120)]
    [InlineData("COUNTIF(A1:A5,\">25\")", 3)]
    [InlineData("COUNTIF(B1:B5,\"a*\")", 3)]
    [InlineData("COUNTIF(B1:B5,\"ap*\")", 2)]
    [InlineData("COUNTIF(B1:B5,\"?pple\")", 1)]
    [InlineData("AVERAGEIF(A1:A5,\">25\")", 40)]
    [InlineData("SUMIFS(A1:A5,B1:B5,\"a*\")", 90)]
    [InlineData("COUNTIFS(A1:A5,\">15\",B1:B5,\"a*\")", 2)]
    public void ConditionalAggregates(string formula, double expected)
        => Sample().Number(formula).ShouldBe(expected);

    [Fact]
    public void CriteriaMatchTextCaseInsensitively()
        => Sample().Number("COUNTIF(B1:B5,\"APPLE\")").ShouldBe(1);

    [Fact]
    public void SumProductMultipliesElementwise()
    {
        var grid = new CalcTestGrid();
        grid.Set("A1", 2d).Set("A2", 3d).Set("B1", 4d).Set("B2", 5d);
        grid.Number("SUMPRODUCT(A1:A2,B1:B2)").ShouldBe(23);
    }

    [Fact]
    public void SumProductRejectsMismatchedShapes()
    {
        var grid = new CalcTestGrid();
        grid.Set("A1", 1d).Set("A2", 2d).Set("B1", 1d);
        grid.Error("SUMPRODUCT(A1:A2,B1:B1)").ShouldBe(CellError.Value);
    }

    [Theory]
    [InlineData("AND(TRUE,TRUE)", true)]
    [InlineData("AND(TRUE,FALSE)", false)]
    [InlineData("OR(FALSE,TRUE)", true)]
    [InlineData("NOT(TRUE)", false)]
    [InlineData("XOR(TRUE,TRUE)", false)]
    [InlineData("XOR(TRUE,FALSE)", true)]
    [InlineData("ISNUMBER(1)", true)]
    [InlineData("ISTEXT(\"a\")", true)]
    [InlineData("ISBLANK(Z99)", true)]
    [InlineData("ISEVEN(4)", true)]
    [InlineData("ISODD(4)", false)]
    public void Logical(string formula, bool expected)
        => Sample().Bool(formula).ShouldBe(expected);

    [Fact]
    public void ChooseSelectsByOneBasedIndex()
        => Sample().Text("CHOOSE(2,\"a\",\"b\",\"c\")").ShouldBe("b");

    [Fact]
    public void SwitchFallsBackToATrailingDefault()
    {
        Sample().Text("SWITCH(3,1,\"one\",2,\"two\",\"other\")").ShouldBe("other");
        Sample().Text("SWITCH(2,1,\"one\",2,\"two\",\"other\")").ShouldBe("two");
    }

    [Fact]
    public void IfsReturnsTheFirstMatch()
        => Sample().Text("IFS(FALSE,\"a\",TRUE,\"b\",TRUE,\"c\")").ShouldBe("b");

    [Fact]
    public void IfsWithNoMatchIsNotAvailable()
        => Sample().Error("IFS(FALSE,\"a\",FALSE,\"b\")").ShouldBe(CellError.NotAvailable);

    [Fact]
    public void TextFormatsUsingAnExcelFormatCode()
        => Sample().Text("TEXT(1234.5,\"#,##0.00\")").ShouldBe("1,234.50");

    [Fact]
    public void ValueParsesNumericText()
        => Sample().Number("VALUE(\"1234\")").ShouldBe(1234);

    [Fact]
    public void TheRegistryCoversASubstantialFunctionSet()
        => FunctionRegistry.Default.Count.ShouldBeGreaterThan(70);
}
