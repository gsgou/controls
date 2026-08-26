using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class FormulaSheetRenamerTests
{
    [Theory]
    [InlineData("Data!A1", "Raw!A1")]
    [InlineData("Data!A1:B2", "Raw!A1:B2")]
    [InlineData("SUM(Data!A1:A9)+Data!B1", "SUM(Raw!A1:A9)+Raw!B1")]
    [InlineData("'Data'!A1", "Raw!A1")]
    [InlineData("data!A1", "Raw!A1")]
    [InlineData("IF(Data!A1>0,Data!A1,0)", "IF(Raw!A1>0,Raw!A1,0)")]
    public void RewritesEveryReferenceToTheSheet(string formula, string expected)
        => FormulaSheetRenamer.Rename(formula, "Data", "Raw").ShouldBe(expected);

    [Theory]
    [InlineData("Other!A1")]
    [InlineData("A1+B1")]
    [InlineData("SUM(A1:A9)")]
    [InlineData("DataPoint!A1")]
    [InlineData("MyData!A1")]
    public void LeavesEverythingElseAlone(string formula)
        => FormulaSheetRenamer.Rename(formula, "Data", "Raw").ShouldBeSameAs(formula);

    [Fact]
    public void DoesNotReachInsideStringLiterals()
    {
        // The text is what the cell displays; rewriting it would change the document's content, not
        // just its references.
        FormulaSheetRenamer.Rename("\"Data!A1\"&Data!A1", "Data", "Raw").ShouldBe("\"Data!A1\"&Raw!A1");
        FormulaSheetRenamer.Rename("\"say \"\"Data!\"\"\"", "Data", "Raw").ShouldBeSameAs("\"say \"\"Data!\"\"\"");
    }

    [Fact]
    public void DoesNotMistakeAnErrorLiteralForASheetName()
    {
        // #REF! ends in a bang; read naively the REF part is indistinguishable from a sheet reference.
        FormulaSheetRenamer.Rename("IFERROR(REF!A1,#REF!)", "REF", "Raw").ShouldBe("IFERROR(Raw!A1,#REF!)");
        FormulaSheetRenamer.Rename("#DIV/0!", "DIV", "Raw").ShouldBeSameAs("#DIV/0!");
    }

    [Fact]
    public void QuotesTheNewNameOnlyWhenItNeedsIt()
    {
        FormulaSheetRenamer.Rename("Data!A1", "Data", "Raw").ShouldBe("Raw!A1");
        FormulaSheetRenamer.Rename("Data!A1", "Data", "Q1_Sales").ShouldBe("Q1_Sales!A1");

        FormulaSheetRenamer.Rename("Data!A1", "Data", "Q1 Sales").ShouldBe("'Q1 Sales'!A1");
        FormulaSheetRenamer.Rename("Data!A1", "Data", "It's").ShouldBe("'It''s'!A1");

        // Names shaped like a cell reference are quoted too. Excel does the same, and it is the safe
        // way round: the quotes are always legal, while leaving them off a name that reads as a cell
        // is only safe until someone opens the file in something stricter.
        FormulaSheetRenamer.Rename("Data!A1", "Data", "Q1").ShouldBe("'Q1'!A1");
        FormulaSheetRenamer.Rename("Data!A1", "Data", "AB12").ShouldBe("'AB12'!A1");
        FormulaSheetRenamer.Rename("Data!A1", "Data", "2024").ShouldBe("'2024'!A1");
    }

    [Fact]
    public void ReadsAQuotedNameWithEscapedApostrophes()
        => FormulaSheetRenamer.Rename("'It''s'!A1", "It's", "Mine").ShouldBe("Mine!A1");

    [Fact]
    public void RewritesBothEndsOfAThreeDimensionalRange()
    {
        // Sheet1:Sheet3!A1 spans sheets. Only the right-hand name carries the bang, so the left one is
        // easy to miss - and missing it leaves the span starting at a sheet that no longer exists.
        FormulaSheetRenamer.Rename("SUM(Data:Summary!A1)", "Data", "Raw").ShouldBe("SUM(Raw:Summary!A1)");
        FormulaSheetRenamer.Rename("SUM(Data:Summary!A1)", "Summary", "Totals").ShouldBe("SUM(Data:Totals!A1)");
    }

    [Fact]
    public void DoesNotTreatAPlainRangeAsASheetSpan()
        => FormulaSheetRenamer.Rename("SUM(A1:B2)", "A1", "Raw").ShouldBeSameAs("SUM(A1:B2)");

    [Theory]
    [InlineData("Sheet1", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("a/b", false)]
    [InlineData("a:b", false)]
    [InlineData("a[b]", false)]
    [InlineData("'quoted'", false)]
    [InlineData("History", false)]
    [InlineData("Q1 Sales & Costs", true)]
    public void SheetNameRulesMatchExcel(string name, bool valid)
        => SheetNames.IsValid(name, out _).ShouldBe(valid);

    [Fact]
    public void RejectsANameOverThirtyOneCharacters()
    {
        SheetNames.IsValid(new string('x', 31), out _).ShouldBeTrue();
        SheetNames.IsValid(new string('x', 32), out _).ShouldBeFalse();
    }

    [Fact]
    public void NextDefaultCountsPastWhatIsAlreadyThere()
    {
        SheetNames.NextDefault(["Sheet1", "Sheet2"]).ShouldBe("Sheet3");
        SheetNames.NextDefault(["Data", "Sheet3"]).ShouldBe("Sheet4");
        SheetNames.NextDefault([]).ShouldBe("Sheet1");
    }

    [Fact]
    public void MakeUniqueUsesExcelsCopySuffixAndStaysInsideTheLimit()
    {
        SheetNames.MakeUnique("Sales", ["Sales"]).ShouldBe("Sales (2)");
        SheetNames.MakeUnique("Sales", ["Sales", "Sales (2)"]).ShouldBe("Sales (3)");
        SheetNames.MakeUnique("Sales", ["Data"]).ShouldBe("Sales");

        var long_ = new string('x', 31);
        SheetNames.MakeUnique(long_, [long_]).Length.ShouldBe(SheetNames.MaxLength);
    }
}
