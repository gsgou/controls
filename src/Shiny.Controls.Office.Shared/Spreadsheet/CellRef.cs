using System.Diagnostics.CodeAnalysis;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// A single cell address. Indices are zero-based internally; A1 notation is one-based, so
/// <c>A1</c> is <c>(0, 0)</c>.
/// </summary>
/// <remarks>
/// The absolute flags are carried rather than normalised away because they survive a round-trip:
/// a formula written <c>$A$1</c> must be written back as <c>$A$1</c>, not <c>A1</c>.
/// </remarks>
public readonly record struct CellRef(int Column, int Row, bool ColumnAbsolute = false, bool RowAbsolute = false)
{
    /// <summary>Highest column index Excel supports (XFD).</summary>
    public const int MaxColumn = 16383;

    /// <summary>Highest row index Excel supports.</summary>
    public const int MaxRow = 1048575;

    public bool IsValid => this.Column >= 0 && this.Column <= MaxColumn && this.Row >= 0 && this.Row <= MaxRow;

    /// <summary>Converts a zero-based column index to letters: 0 =&gt; A, 25 =&gt; Z, 26 =&gt; AA.</summary>
    public static string ColumnName(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(column, MaxColumn);

        // Bijective base-26. The -1 on each step is what makes Z/AA roll over correctly.
        Span<char> buffer = stackalloc char[3];
        var index = 3;
        var value = column;
        do
        {
            buffer[--index] = (char)('A' + value % 26);
            value = value / 26 - 1;
        }
        while (value >= 0);

        return new string(buffer[index..]);
    }

    /// <summary>Converts column letters to a zero-based index. Returns -1 if the text is not a valid column.</summary>
    public static int ParseColumnName(ReadOnlySpan<char> name)
    {
        if (name.Length is 0 or > 3)
            return -1;

        var value = 0;
        foreach (var c in name)
        {
            var upper = (char)(c & ~0x20);
            if (upper is < 'A' or > 'Z')
                return -1;

            value = value * 26 + (upper - 'A' + 1);
        }

        var index = value - 1;
        return index > MaxColumn ? -1 : index;
    }

    public static CellRef Parse(ReadOnlySpan<char> text)
        => TryParse(text, out var result) ? result : throw new FormatException($"'{text}' is not a valid cell reference.");

    public static bool TryParse(ReadOnlySpan<char> text, out CellRef result)
    {
        result = default;
        if (text.IsEmpty)
            return false;

        var position = 0;
        var columnAbsolute = text[position] == '$';
        if (columnAbsolute && ++position == text.Length)
            return false;

        var letterStart = position;
        while (position < text.Length && char.IsAsciiLetter(text[position]))
            position++;

        if (position == letterStart)
            return false;

        var column = ParseColumnName(text[letterStart..position]);
        if (column < 0)
            return false;

        var rowAbsolute = position < text.Length && text[position] == '$';
        if (rowAbsolute && ++position == text.Length)
            return false;

        var digitStart = position;
        while (position < text.Length && char.IsAsciiDigit(text[position]))
            position++;

        if (position == digitStart || position != text.Length)
            return false;

        if (!int.TryParse(text[digitStart..position], out var oneBasedRow) || oneBasedRow < 1 || oneBasedRow - 1 > MaxRow)
            return false;

        result = new CellRef(column, oneBasedRow - 1, columnAbsolute, rowAbsolute);
        return true;
    }

    /// <summary>Returns the address in A1 notation, including any absolute markers.</summary>
    public override string ToString()
    {
        var column = ColumnName(this.Column);
        var row = (this.Row + 1).ToString();
        return (this.ColumnAbsolute, this.RowAbsolute) switch
        {
            (true, true) => $"${column}${row}",
            (true, false) => $"${column}{row}",
            (false, true) => $"{column}${row}",
            _ => column + row
        };
    }

    /// <summary>The address with both absolute markers stripped — the form used as a dictionary key.</summary>
    public CellRef Relative() => new(this.Column, this.Row);

    public CellRef Offset(int columns, int rows) => this with { Column = this.Column + columns, Row = this.Row + rows };
}
