using System.Text;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// Moves the cell references inside a formula — for a copied formula landing somewhere else, and for
/// the cells a row or column insert pushed out from under an existing one.
/// </summary>
/// <remarks>
/// <para>
/// Like <see cref="FormulaSheetRenamer"/> this works on the text rather than on a parsed tree, and for
/// the same reason: the text is what has to come back out. Printing the engine's AST back would
/// normalise function names, drop whitespace and forget how a number was written, rewriting formulas
/// nobody asked to change. A scanner that touches only the references leaves every other byte alone.
/// </para>
/// <para>
/// The two jobs look similar and are not. <see cref="Translate"/> is a copy: it moves the
/// <em>relative</em> half of a reference and leaves <c>$</c>-pinned parts where they are, which is the
/// whole point of an absolute reference. An insert moves the cells themselves, so it shifts absolute
/// and relative alike — a formula reading <c>$A$5</c> has to read <c>$A$6</c> once a row is pushed in
/// above it, or it silently starts pointing at a different number.
/// </para>
/// </remarks>
public static class FormulaReferenceShifter
{
    /// <summary>What a reference becomes when the cells it named no longer exist.</summary>
    public const string RefError = "#REF!";

    /// <summary>
    /// Rebases a copied formula by a cell offset, moving relative references and leaving absolute ones.
    /// </summary>
    public static string Translate(string formula, int columnDelta, int rowDelta)
    {
        ArgumentNullException.ThrowIfNull(formula);

        if (columnDelta == 0 && rowDelta == 0)
            return formula;

        return Rewrite(formula, (_, reference) =>
        {
            var column = reference.ColumnAbsolute ? reference.Column : reference.Column + columnDelta;
            var row = reference.RowAbsolute ? reference.Row : reference.Row + rowDelta;

            if (column < 0 || column > CellRef.MaxColumn || row < 0 || row > CellRef.MaxRow)
                return null;

            return reference with { Column = column, Row = row };
        });
    }

    /// <summary>Shifts every reference at or below <paramref name="at"/> down by <paramref name="count"/> rows.</summary>
    /// <param name="formulaSheet">The sheet the formula lives on, which is what an unqualified reference means.</param>
    /// <param name="editedSheet">The sheet the rows were inserted into.</param>
    public static string ForInsertedRows(string formula, string formulaSheet, string editedSheet, int at, int count)
        => ShiftAxis(formula, formulaSheet, editedSheet, at, count, rows: true, inserting: true);

    /// <summary>Shifts every reference below the deleted band up, and turns references into it into <c>#REF!</c>.</summary>
    public static string ForDeletedRows(string formula, string formulaSheet, string editedSheet, int at, int count)
        => ShiftAxis(formula, formulaSheet, editedSheet, at, count, rows: true, inserting: false);

    /// <summary>Shifts every reference at or right of <paramref name="at"/> right by <paramref name="count"/> columns.</summary>
    public static string ForInsertedColumns(string formula, string formulaSheet, string editedSheet, int at, int count)
        => ShiftAxis(formula, formulaSheet, editedSheet, at, count, rows: false, inserting: true);

    /// <summary>Shifts every reference right of the deleted band left, and turns references into it into <c>#REF!</c>.</summary>
    public static string ForDeletedColumns(string formula, string formulaSheet, string editedSheet, int at, int count)
        => ShiftAxis(formula, formulaSheet, editedSheet, at, count, rows: false, inserting: false);

    static string ShiftAxis(
        string formula,
        string formulaSheet,
        string editedSheet,
        int at,
        int count,
        bool rows,
        bool inserting)
    {
        ArgumentNullException.ThrowIfNull(formula);

        if (count <= 0)
            return formula;

        return Rewrite(formula, (sheet, reference) =>
        {
            // An unqualified reference means the sheet the formula is written on, so a formula on
            // another sheet is only affected when it names this one explicitly.
            var target = sheet ?? formulaSheet;
            if (!string.Equals(target, editedSheet, StringComparison.OrdinalIgnoreCase))
                return reference;

            var index = rows ? reference.Row : reference.Column;

            if (inserting)
            {
                if (index < at)
                    return reference;

                index += count;
                var limit = rows ? CellRef.MaxRow : CellRef.MaxColumn;
                if (index > limit)
                    return null;
            }
            else
            {
                if (index < at)
                    return reference;

                if (index < at + count)
                    return null;

                index -= count;
            }

            return rows ? reference with { Row = index } : reference with { Column = index };
        });
    }

    /// <summary>
    /// Walks the formula, handing every cell reference to <paramref name="map"/> along with the sheet
    /// it names, and writing back whatever comes out. A null result becomes <c>#REF!</c>.
    /// </summary>
    static string Rewrite(string formula, Func<string?, CellRef, CellRef?> map)
    {
        if (formula.Length == 0)
            return formula;

        var builder = new StringBuilder(formula.Length + 8);
        var position = 0;
        var rewritten = false;

        // The sheet the reference being read belongs to. Set by a Sheet1! prefix and normally spent on
        // the next reference — except across a colon, where Sheet1!A1:B2 puts both ends on Sheet1.
        string? sheet = null;

        while (position < formula.Length)
        {
            var c = formula[position];

            // A string literal can hold anything, including something shaped exactly like a reference.
            if (c == '"')
            {
                var start = position;
                SkipStringLiteral(formula, ref position);
                builder.Append(formula, start, position - start);
                sheet = null;
                continue;
            }

            // #REF! ends in a bang and #NAME? in a question mark; read as names they would both be
            // mistaken for sheet prefixes, and an already-broken reference would break differently.
            if (c == '#')
            {
                var length = MatchErrorLiteral(formula, position);
                builder.Append(formula, position, length);
                position += length;
                sheet = null;
                continue;
            }

            if (c == '\'' || char.IsLetter(c) || c is '_' or '$')
            {
                var start = position;
                if (!TryReadName(formula, ref position, out var name))
                {
                    // An unterminated quote: nothing safe to rewrite, so the rest is copied verbatim.
                    builder.Append(formula, start, formula.Length - start);
                    break;
                }

                // Checked before the reference test on purpose: a sheet really can be called AB1, and
                // the bang is the only thing that says which of the two this is.
                if (position < formula.Length && formula[position] == '!')
                {
                    position++;
                    builder.Append(formula, start, position - start);
                    sheet = name;
                    continue;
                }

                // A name followed by an open bracket is a function, never a cell — LOG10( is not L1.
                if (position < formula.Length && formula[position] == '(')
                {
                    builder.Append(formula, start, position - start);
                    sheet = null;
                    continue;
                }

                if (CellRef.TryParse(name, out var reference))
                {
                    var mapped = map(sheet, reference);

                    if (mapped is null)
                    {
                        builder.Append(RefError);
                        rewritten = true;
                    }
                    else if (mapped.Value == reference)
                    {
                        builder.Append(formula, start, position - start);
                    }
                    else
                    {
                        builder.Append(mapped.Value.ToString());
                        rewritten = true;
                    }

                    // The prefix carries over an A1:B2 span and is spent on anything else.
                    if (position >= formula.Length || formula[position] != ':')
                        sheet = null;

                    continue;
                }

                // A defined name, a boolean, or a function referred to without brackets.
                builder.Append(formula, start, position - start);
                sheet = null;
                continue;
            }

            builder.Append(c);
            position++;

            if (c != ':')
                sheet = null;
        }

        return rewritten ? builder.ToString() : formula;
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
