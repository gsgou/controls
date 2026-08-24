using System.Globalization;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// Precedence-climbing parser over <see cref="FormulaLexer"/> output.
/// </summary>
public sealed class FormulaParser
{
    readonly List<FormulaToken> tokens;
    int index;

    FormulaParser(List<FormulaToken> tokens) => this.tokens = tokens;

    public static FormulaNode Parse(string formula)
    {
        var parser = new FormulaParser(FormulaLexer.Tokenize(formula));
        var node = parser.ParseExpression(0);
        parser.Expect(TokenKind.End);
        return node;
    }

    public static bool TryParse(string formula, out FormulaNode? node, out string? error)
    {
        try
        {
            node = Parse(formula);
            error = null;
            return true;
        }
        catch (FormulaSyntaxException ex)
        {
            node = null;
            error = ex.Message;
            return false;
        }
    }

    FormulaToken Current => this.tokens[this.index];

    FormulaToken Advance() => this.tokens[this.index++];

    void Expect(TokenKind kind)
    {
        if (this.Current.Kind != kind)
            throw new FormulaSyntaxException($"Expected {kind} but found {this.Current.Kind}", this.Current.Position);

        if (kind != TokenKind.End)
            this.index++;
    }

    /// <summary>
    /// Binding powers, lowest first. Comparison binds loosest, so <c>1+2=3</c> parses as <c>(1+2)=3</c>.
    /// </summary>
    static int Precedence(string op) => op switch
    {
        "=" or "<>" or "<" or "<=" or ">" or ">=" => 1,
        "&" => 2,
        "+" or "-" => 3,
        "*" or "/" => 4,
        "^" => 5,
        _ => 0
    };

    static bool IsRightAssociative(string op) => op == "^";

    static BinaryOperator MapBinary(string op) => op switch
    {
        "+" => BinaryOperator.Add,
        "-" => BinaryOperator.Subtract,
        "*" => BinaryOperator.Multiply,
        "/" => BinaryOperator.Divide,
        "^" => BinaryOperator.Power,
        "&" => BinaryOperator.Concat,
        "=" => BinaryOperator.Equal,
        "<>" => BinaryOperator.NotEqual,
        "<" => BinaryOperator.LessThan,
        "<=" => BinaryOperator.LessThanOrEqual,
        ">" => BinaryOperator.GreaterThan,
        ">=" => BinaryOperator.GreaterThanOrEqual,
        _ => throw new FormulaSyntaxException($"Unknown operator '{op}'", 0)
    };

    FormulaNode ParseExpression(int minPrecedence)
    {
        var left = this.ParseUnary();

        while (this.Current.Kind == TokenKind.Operator)
        {
            var op = this.Current.Text;
            var precedence = Precedence(op);
            if (precedence == 0 || precedence < minPrecedence)
                break;

            this.Advance();
            var nextMinimum = IsRightAssociative(op) ? precedence : precedence + 1;
            var right = this.ParseExpression(nextMinimum);
            left = new BinaryNode(MapBinary(op), left, right);
        }

        return left;
    }

    FormulaNode ParseUnary()
    {
        if (this.Current.Kind == TokenKind.Operator && this.Current.Text is "-" or "+")
        {
            var op = this.Advance().Text;

            // Unary binds tighter than the arithmetic operators but looser than ^, which is why
            // -2^2 is -4 in Excel.
            var operand = this.ParseExpression(5);
            return new UnaryNode(op == "-" ? UnaryOperator.Negate : UnaryOperator.Plus, operand);
        }

        return this.ParsePostfix();
    }

    FormulaNode ParsePostfix()
    {
        var node = this.ParsePrimary();

        while (this.Current.Kind == TokenKind.Percent)
        {
            this.Advance();
            node = new UnaryNode(UnaryOperator.Percent, node);
        }

        return node;
    }

    FormulaNode ParsePrimary()
    {
        var token = this.Current;

        switch (token.Kind)
        {
            case TokenKind.Number:
                this.Advance();
                return new LiteralNode(CellValue.FromNumber(double.Parse(token.Text, CultureInfo.InvariantCulture)));

            case TokenKind.Text:
                this.Advance();
                return new LiteralNode(CellValue.FromText(token.Text));

            case TokenKind.Boolean:
                this.Advance();
                return new LiteralNode(CellValue.FromBoolean(token.Text.Equals("TRUE", StringComparison.OrdinalIgnoreCase)));

            case TokenKind.Error:
                this.Advance();
                return new LiteralNode(CellValue.TryParseError(token.Text.ToUpperInvariant(), out var error)
                    ? CellValue.FromError(error)
                    : CellValue.FromError(CellError.Value));

            case TokenKind.OpenParen:
                this.Advance();
                var inner = this.ParseExpression(0);
                this.Expect(TokenKind.CloseParen);
                return inner;

            case TokenKind.Identifier or TokenKind.QualifiedIdentifier:
                return this.ParseIdentifier();

            default:
                throw new FormulaSyntaxException($"Unexpected {token.Kind}", token.Position);
        }
    }

    FormulaNode ParseIdentifier()
    {
        var token = this.Advance();

        // A name followed by '(' is a call; nothing else can be.
        if (this.Current.Kind == TokenKind.OpenParen)
            return this.ParseFunctionCall(token.Text);

        var (sheet, name) = SplitSheet(token.Text);

        if (!CellRef.TryParse(name, out var cell))
            return new UnknownNameNode(token.Text);

        // A colon after a reference makes it a range. The sheet on the left governs both ends:
        // Sheet1!A1:B2 is one range on Sheet1, not a reference to two sheets.
        if (this.Current.Kind == TokenKind.Colon)
        {
            this.Advance();
            var end = this.Current;
            if (end.Kind is not (TokenKind.Identifier or TokenKind.QualifiedIdentifier))
                throw new FormulaSyntaxException("Expected a cell reference after ':'", end.Position);

            this.Advance();
            var (_, endName) = SplitSheet(end.Text);
            if (!CellRef.TryParse(endName, out var endCell))
                throw new FormulaSyntaxException($"'{end.Text}' is not a cell reference", end.Position);

            return new RangeNode(sheet, new CellRange(cell, endCell));
        }

        return new ReferenceNode(sheet, cell);
    }

    FormulaNode ParseFunctionCall(string name)
    {
        this.Expect(TokenKind.OpenParen);
        var arguments = new List<FormulaNode>();

        if (this.Current.Kind == TokenKind.CloseParen)
        {
            this.Advance();
            return new FunctionNode(NormaliseFunctionName(name), arguments);
        }

        while (true)
        {
            arguments.Add(this.Current.Kind is TokenKind.Comma or TokenKind.CloseParen
                ? MissingArgumentNode.Instance
                : this.ParseExpression(0));

            if (this.Current.Kind == TokenKind.Comma)
            {
                this.Advance();
                continue;
            }

            break;
        }

        this.Expect(TokenKind.CloseParen);
        return new FunctionNode(NormaliseFunctionName(name), arguments);
    }

    /// <summary>Strips the future-function prefix Excel writes into the file for newer functions.</summary>
    static string NormaliseFunctionName(string name)
    {
        const string futurePrefix = "_xlfn.";
        if (name.StartsWith(futurePrefix, StringComparison.OrdinalIgnoreCase))
            name = name[futurePrefix.Length..];

        return name.ToUpperInvariant();
    }

    static (string? Sheet, string Name) SplitSheet(string text)
    {
        var bang = text.LastIndexOf('!');
        return bang < 0 ? (null, text) : (text[..bang], text[(bang + 1)..]);
    }
}
