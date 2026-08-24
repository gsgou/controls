using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class FormulaLexerTests
{
    [Fact]
    public void SkipsTheLeadingEquals()
    {
        var tokens = FormulaLexer.Tokenize("=1+2");
        tokens[0].Kind.ShouldBe(TokenKind.Number);
        tokens[0].Text.ShouldBe("1");
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1.5", "1.5")]
    [InlineData(".5", ".5")]
    [InlineData("1e3", "1e3")]
    [InlineData("1.2E-4", "1.2E-4")]
    public void ScansNumbers(string input, string expected)
        => FormulaLexer.Tokenize(input)[0].Text.ShouldBe(expected);

    [Fact]
    public void DoesNotSwallowEAsAnExponentWhenItStartsACellReference()
    {
        // "1" followed by cell E1 must not lex as the single number 1e1.
        var tokens = FormulaLexer.Tokenize("1+E1");
        tokens[0].Text.ShouldBe("1");
        tokens[2].Kind.ShouldBe(TokenKind.Identifier);
        tokens[2].Text.ShouldBe("E1");
    }

    [Fact]
    public void DoubledQuotesAreAnEscapedQuote()
        => FormulaLexer.Tokenize("\"say \"\"hi\"\"\"")[0].Text.ShouldBe("say \"hi\"");

    [Fact]
    public void UnterminatedStringIsASyntaxError()
        => Should.Throw<FormulaSyntaxException>(() => FormulaLexer.Tokenize("\"open"));

    [Theory]
    [InlineData("#DIV/0!")]
    [InlineData("#VALUE!")]
    [InlineData("#N/A")]
    [InlineData("#REF!")]
    [InlineData("#NAME?")]
    [InlineData("#NULL!")]
    [InlineData("#NUM!")]
    public void ScansErrorLiterals(string input)
    {
        var token = FormulaLexer.Tokenize(input)[0];
        token.Kind.ShouldBe(TokenKind.Error);
        token.Text.ShouldBe(input);
    }

    [Fact]
    public void ScansQuotedSheetNames()
    {
        var token = FormulaLexer.Tokenize("'My Sheet'!A1")[0];
        token.Kind.ShouldBe(TokenKind.QualifiedIdentifier);
        token.Text.ShouldBe("My Sheet!A1");
    }

    [Theory]
    [InlineData("<=")]
    [InlineData(">=")]
    [InlineData("<>")]
    public void TwoCharacterOperatorsBeatSingleOnes(string op)
        => FormulaLexer.Tokenize($"1{op}2")[1].Text.ShouldBe(op);
}

public class FormulaParserTests
{
    static readonly CalcTestGrid Grid = new();

    [Theory]
    [InlineData("1+2*3", 7)]
    [InlineData("(1+2)*3", 9)]
    [InlineData("2^3^2", 512)]        // right associative
    [InlineData("-2^2", -4)]          // unary binds looser than ^
    [InlineData("10-2-3", 5)]         // left associative
    [InlineData("50%", 0.5)]
    [InlineData("2*50%", 1)]
    [InlineData("-(3)", -3)]
    [InlineData("--3", 3)]
    public void OperatorPrecedenceAndAssociativity(string formula, double expected)
        => Grid.Number(formula).ShouldBe(expected);

    [Fact]
    public void ComparisonBindsLoosestOfAll()
        => Grid.Bool("1+2=3").ShouldBeTrue();

    [Fact]
    public void ConcatBindsTighterThanComparisonButLooserThanArithmetic()
        => Grid.Bool("\"a\"&1+1=\"a2\"").ShouldBeTrue();

    [Fact]
    public void ParsesRanges()
    {
        var node = FormulaParser.Parse("SUM(A1:B2)");
        var function = node.ShouldBeOfType<FunctionNode>();
        var range = function.Arguments[0].ShouldBeOfType<RangeNode>();
        range.Range.ShouldBe(CellRange.Parse("A1:B2"));
    }

    [Fact]
    public void SheetQualifierOnTheLeftGovernsTheWholeRange()
    {
        var node = FormulaParser.Parse("SUM(Sheet2!A1:B2)");
        var range = node.ShouldBeOfType<FunctionNode>().Arguments[0].ShouldBeOfType<RangeNode>();
        range.Sheet.ShouldBe("Sheet2");
        range.Range.ShouldBe(CellRange.Parse("A1:B2"));
    }

    [Fact]
    public void DistinguishesFunctionsFromReferences()
    {
        // LOG10 is a function; LOG10 without parentheses would be a defined name, and E1 is a cell.
        FormulaParser.Parse("LOG10(100)").ShouldBeOfType<FunctionNode>();
        FormulaParser.Parse("E1").ShouldBeOfType<ReferenceNode>();
    }

    [Fact]
    public void UnknownNamesSurviveParsingAndBecomeNameErrors()
    {
        FormulaParser.Parse("SomeName").ShouldBeOfType<UnknownNameNode>();
        Grid.Error("SomeName").ShouldBe(CellError.Name);
    }

    [Fact]
    public void StripsTheFutureFunctionPrefix()
    {
        // Excel writes newer functions into the file as _xlfn.NAME.
        var node = FormulaParser.Parse("_xlfn.TEXTJOIN(\",\",TRUE,\"a\")").ShouldBeOfType<FunctionNode>();
        node.Name.ShouldBe("TEXTJOIN");
    }

    [Fact]
    public void EmptyArgumentsArePreserved()
    {
        var node = FormulaParser.Parse("IF(A1,,\"no\")").ShouldBeOfType<FunctionNode>();
        node.Arguments.Count.ShouldBe(3);
        node.Arguments[1].ShouldBeOfType<MissingArgumentNode>();
    }

    [Fact]
    public void ZeroArgumentCalls()
        => FormulaParser.Parse("PI()").ShouldBeOfType<FunctionNode>().Arguments.ShouldBeEmpty();

    [Theory]
    [InlineData("1+")]
    [InlineData("(1")]
    [InlineData("SUM(1))")]
    [InlineData("*3")]
    public void RejectsMalformedFormulas(string formula)
        => FormulaParser.TryParse(formula, out _, out _).ShouldBeFalse();
}
