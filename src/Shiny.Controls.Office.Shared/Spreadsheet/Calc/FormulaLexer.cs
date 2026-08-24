namespace Shiny.Controls.Office.Spreadsheet.Calc;

public enum TokenKind
{
    Number,
    Text,
    Boolean,
    Error,

    /// <summary>A bare name: a function when followed by <c>(</c>, otherwise a cell reference or defined name.</summary>
    Identifier,

    /// <summary>A sheet-qualified name, e.g. <c>Sheet1!A1</c> or <c>'My Sheet'!A1</c>.</summary>
    QualifiedIdentifier,

    OpenParen,
    CloseParen,
    Comma,
    Colon,
    Operator,
    Percent,
    End
}

public readonly record struct FormulaToken(TokenKind Kind, string Text, int Position)
{
    public override string ToString() => $"{this.Kind}('{this.Text}')";
}

public sealed class FormulaSyntaxException(string message, int position)
    : Exception($"{message} (at position {position})")
{
    public int Position { get; } = position;
}

/// <summary>
/// Turns formula text into tokens.
/// </summary>
/// <remarks>
/// The lexer does not decide what a name means. <c>LOG10</c> and <c>A1</c> are lexically identical, and
/// only the parser — which can see whether a <c>(</c> follows — can tell a function from a reference.
/// </remarks>
public static class FormulaLexer
{
    public static List<FormulaToken> Tokenize(string formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        var tokens = new List<FormulaToken>();
        var position = 0;

        // A leading = is part of how a formula is written, not part of the expression.
        if (position < formula.Length && formula[position] == '=')
            position++;

        while (position < formula.Length)
        {
            var c = formula[position];

            if (char.IsWhiteSpace(c))
            {
                position++;
                continue;
            }

            var start = position;

            if (char.IsAsciiDigit(c) || (c == '.' && position + 1 < formula.Length && char.IsAsciiDigit(formula[position + 1])))
            {
                position = ScanNumber(formula, position);
                tokens.Add(new FormulaToken(TokenKind.Number, formula[start..position], start));
                continue;
            }

            if (c == '"')
            {
                var text = ScanText(formula, ref position);
                tokens.Add(new FormulaToken(TokenKind.Text, text, start));
                continue;
            }

            if (c == '#')
            {
                position = ScanError(formula, position);
                tokens.Add(new FormulaToken(TokenKind.Error, formula[start..position], start));
                continue;
            }

            if (c == '\'' || char.IsLetter(c) || c == '_' || c == '$')
            {
                var (text, qualified) = ScanName(formula, ref position);
                var kind = qualified ? TokenKind.QualifiedIdentifier : TokenKind.Identifier;

                if (!qualified && (text.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || text.Equals("FALSE", StringComparison.OrdinalIgnoreCase)))
                    kind = TokenKind.Boolean;

                tokens.Add(new FormulaToken(kind, text, start));
                continue;
            }

            switch (c)
            {
                case '(':
                    tokens.Add(new FormulaToken(TokenKind.OpenParen, "(", start));
                    position++;
                    continue;
                case ')':
                    tokens.Add(new FormulaToken(TokenKind.CloseParen, ")", start));
                    position++;
                    continue;
                case ',':
                    tokens.Add(new FormulaToken(TokenKind.Comma, ",", start));
                    position++;
                    continue;
                case ':':
                    tokens.Add(new FormulaToken(TokenKind.Colon, ":", start));
                    position++;
                    continue;
                case '%':
                    tokens.Add(new FormulaToken(TokenKind.Percent, "%", start));
                    position++;
                    continue;
            }

            var op = ScanOperator(formula, ref position);
            if (op is null)
                throw new FormulaSyntaxException($"Unexpected character '{c}'", start);

            tokens.Add(new FormulaToken(TokenKind.Operator, op, start));
        }

        tokens.Add(new FormulaToken(TokenKind.End, string.Empty, formula.Length));
        return tokens;
    }

    static int ScanNumber(string formula, int position)
    {
        while (position < formula.Length && char.IsAsciiDigit(formula[position]))
            position++;

        if (position < formula.Length && formula[position] == '.')
        {
            position++;
            while (position < formula.Length && char.IsAsciiDigit(formula[position]))
                position++;
        }

        if (position < formula.Length && (formula[position] is 'e' or 'E'))
        {
            var exponent = position + 1;
            if (exponent < formula.Length && formula[exponent] is '+' or '-')
                exponent++;

            // Only consume the E when real digits follow it, so E1 stays a cell reference.
            if (exponent < formula.Length && char.IsAsciiDigit(formula[exponent]))
            {
                position = exponent;
                while (position < formula.Length && char.IsAsciiDigit(formula[position]))
                    position++;
            }
        }

        return position;
    }

    static string ScanText(string formula, ref int position)
    {
        var start = position;
        position++; // opening quote
        var builder = new System.Text.StringBuilder();

        while (position < formula.Length)
        {
            if (formula[position] == '"')
            {
                // A doubled quote is an escaped quote, not the end of the string.
                if (position + 1 < formula.Length && formula[position + 1] == '"')
                {
                    builder.Append('"');
                    position += 2;
                    continue;
                }

                position++;
                return builder.ToString();
            }

            builder.Append(formula[position]);
            position++;
        }

        throw new FormulaSyntaxException("Unterminated string", start);
    }

    static int ScanError(string formula, int position)
    {
        foreach (var candidate in ErrorLiterals)
        {
            if (formula.AsSpan(position).StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                return position + candidate.Length;
        }

        throw new FormulaSyntaxException("Unrecognised error literal", position);
    }

    static readonly string[] ErrorLiterals =
    [
        // #NULL! must be tested before #N/A would ever match, and #REF! before #N/A for the same reason;
        // ordering by length keeps any prefix relationship from mattering.
        "#DIV/0!", "#VALUE!", "#NAME?", "#NULL!", "#NUM!", "#REF!", "#N/A"
    ];

    static (string Text, bool Qualified) ScanName(string formula, ref int position)
    {
        var qualified = false;
        var builder = new System.Text.StringBuilder();

        if (formula[position] == '\'')
        {
            var start = position;
            position++;
            while (position < formula.Length)
            {
                if (formula[position] == '\'')
                {
                    if (position + 1 < formula.Length && formula[position + 1] == '\'')
                    {
                        builder.Append('\'');
                        position += 2;
                        continue;
                    }

                    position++;
                    break;
                }

                builder.Append(formula[position]);
                position++;
            }

            if (position >= formula.Length || formula[position] != '!')
                throw new FormulaSyntaxException("Quoted sheet name must be followed by '!'", start);

            builder.Append('!');
            position++;
            qualified = true;
        }

        while (position < formula.Length)
        {
            var c = formula[position];
            if (char.IsLetterOrDigit(c) || c is '_' or '.' or '$')
            {
                builder.Append(c);
                position++;
                continue;
            }

            if (c == '!' && !qualified)
            {
                builder.Append('!');
                position++;
                qualified = true;
                continue;
            }

            break;
        }

        return (builder.ToString(), qualified);
    }

    static string? ScanOperator(string formula, ref int position)
    {
        var remaining = formula.AsSpan(position);

        foreach (var candidate in TwoCharOperators)
        {
            if (remaining.StartsWith(candidate, StringComparison.Ordinal))
            {
                position += 2;
                return candidate;
            }
        }

        var c = formula[position];
        if (SingleCharOperators.Contains(c))
        {
            position++;
            return c.ToString();
        }

        return null;
    }

    static readonly string[] TwoCharOperators = ["<=", ">=", "<>"];
    const string SingleCharOperators = "+-*/^&=<>";
}
