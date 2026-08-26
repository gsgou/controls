using System.Text;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// Rewrites the sheet name in every sheet-qualified reference of a formula.
/// </summary>
/// <remarks>
/// <para>
/// Renaming a sheet without this silently breaks every formula that pointed at it: <c>Sales!B2</c>
/// stops resolving the moment <c>Sales</c> becomes <c>Q1</c>, and Excel shows <c>#REF!</c> in a file
/// that was correct before it was opened here. Excel rewrites those references itself on rename, and
/// so must we.
/// </para>
/// <para>
/// This works on the text rather than on a parsed tree because the text is what has to come back out.
/// The engine's AST is lossy on purpose — it normalises function names, drops whitespace and forgets
/// how a number was written — so printing it back would rewrite formulas nobody asked to change. A
/// scanner that touches only the sheet prefixes leaves every other byte exactly where it was.
/// </para>
/// </remarks>
public static class FormulaSheetRenamer
{
    /// <summary>
    /// Returns <paramref name="formula"/> with every reference to <paramref name="oldName"/> pointing
    /// at <paramref name="newName"/>. Returns the original instance when nothing matched.
    /// </summary>
    public static string Rename(string formula, string oldName, string newName)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(oldName);
        ArgumentNullException.ThrowIfNull(newName);

        if (formula.Length == 0 || string.Equals(oldName, newName, StringComparison.Ordinal))
            return formula;

        var builder = new StringBuilder(formula.Length + 8);
        var position = 0;
        var rewritten = false;

        while (position < formula.Length)
        {
            var c = formula[position];

            // A string literal can contain anything at all, including a bang and something that looks
            // exactly like a sheet name, so it is copied across without being read.
            if (c == '"')
            {
                var start = position;
                SkipStringLiteral(formula, ref position);
                builder.Append(formula, start, position - start);
                continue;
            }

            // #REF! and #NULL! end in a bang. Left to the name scanner below, the REF in #REF! would
            // read as a sheet called REF - and renaming a sheet to "REF" would then corrupt errors.
            if (c == '#')
            {
                var length = MatchErrorLiteral(formula, position);
                builder.Append(formula, position, length);
                position += length;
                continue;
            }

            if (c == '\'' || char.IsLetter(c) || c is '_' or '$')
            {
                var start = position;
                if (!TryReadName(formula, ref position, out var name))
                {
                    // An unterminated quote: nothing sensible to rewrite, so copy the rest verbatim.
                    builder.Append(formula, start, formula.Length - start);
                    break;
                }

                // Sheet1!A1 - the bang is what makes the name a sheet reference rather than a
                // function name, a defined name or a cell.
                if (position < formula.Length && formula[position] == '!')
                {
                    position++;
                    Append(builder, name, oldName, newName, ref rewritten);
                    continue;
                }

                // Sheet1:Sheet3!A1 - a 3-D range, where the bang sits after the second name and the
                // first is a sheet name all the same. Without this the left end of the span keeps
                // pointing at a sheet that no longer exists.
                if (TryReadThreeDimensionalTail(formula, position, out var endName, out var after))
                {
                    position = after;
                    Append(builder, name, oldName, newName, ref rewritten, bang: false);
                    builder.Append(':');
                    Append(builder, endName, oldName, newName, ref rewritten);
                    continue;
                }

                builder.Append(formula, start, position - start);
                continue;
            }

            builder.Append(c);
            position++;
        }

        return rewritten ? builder.ToString() : formula;
    }

    /// <summary>True when a sheet name has to be written inside apostrophes to be read back correctly.</summary>
    public static bool RequiresQuoting(string name)
    {
        if (name.Length == 0)
            return true;

        // A name starting with a digit would lex as a number, and one shaped like A1 would lex as a
        // cell reference; both need the quotes to be read as a name at all.
        if (!char.IsLetter(name[0]) && name[0] is not ('_' or '\\'))
            return true;

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c is not ('_' or '.'))
                return true;
        }

        return CellRef.TryParse(name, out _);
    }

    /// <summary>Writes a sheet name as it must appear in a formula, quoting and escaping if needed.</summary>
    public static string Quote(string name)
        => RequiresQuoting(name)
            ? $"'{name.Replace("'", "''", StringComparison.Ordinal)}'"
            : name;

    static void Append(StringBuilder builder, string name, string oldName, string newName, ref bool rewritten, bool bang = true)
    {
        // Sheet names compare the way Excel compares them: 'sales' and 'Sales' are the same sheet.
        if (string.Equals(name, oldName, StringComparison.OrdinalIgnoreCase))
        {
            builder.Append(Quote(newName));
            rewritten = true;
        }
        else
        {
            builder.Append(Quote(name));
        }

        if (bang)
            builder.Append('!');
    }

    static void SkipStringLiteral(string formula, ref int position)
    {
        position++;

        while (position < formula.Length)
        {
            if (formula[position] != '"')
            {
                position++;
                continue;
            }

            // "" is an escaped quote inside the literal, not the end of it.
            if (position + 1 < formula.Length && formula[position + 1] == '"')
            {
                position += 2;
                continue;
            }

            position++;
            return;
        }
    }

    /// <summary>Reads a bare or quoted name, leaving <paramref name="position"/> on whatever follows it.</summary>
    static bool TryReadName(string formula, ref int position, out string name)
    {
        if (formula[position] == '\'')
        {
            var builder = new StringBuilder();
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
                    name = builder.ToString();
                    return true;
                }

                builder.Append(formula[position]);
                position++;
            }

            name = builder.ToString();
            return false;
        }

        var start = position;
        while (position < formula.Length)
        {
            var c = formula[position];
            if (!char.IsLetterOrDigit(c) && c is not ('_' or '.' or '$'))
                break;

            position++;
        }

        name = formula[start..position];
        return true;
    }

    /// <summary>
    /// Recognises the <c>:Sheet3!</c> half of a 3-D range, without consuming anything if that is not
    /// what follows — <c>A1:B2</c> starts identically and is not a sheet span at all.
    /// </summary>
    static bool TryReadThreeDimensionalTail(string formula, int position, out string endName, out int after)
    {
        endName = string.Empty;
        after = position;

        if (position >= formula.Length || formula[position] != ':')
            return false;

        var cursor = position + 1;
        if (cursor >= formula.Length)
            return false;

        var c = formula[cursor];
        if (c != '\'' && !char.IsLetter(c) && c is not ('_' or '$'))
            return false;

        if (!TryReadName(formula, ref cursor, out endName))
            return false;

        if (cursor >= formula.Length || formula[cursor] != '!')
            return false;

        after = cursor + 1;
        return true;
    }

    static int MatchErrorLiteral(string formula, int position)
    {
        foreach (var candidate in ErrorLiterals)
        {
            if (formula.AsSpan(position).StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                return candidate.Length;
        }

        return 1;
    }

    static readonly string[] ErrorLiterals =
    [
        // Longest first, so #NULL! is never matched as a prefix of something shorter.
        "#DIV/0!", "#VALUE!", "#NAME?", "#NULL!", "#NUM!", "#REF!", "#N/A"
    ];
}
