namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// A rectangular block of cells, inclusive of both corners. Always stored normalised, so
/// <c>C3:A1</c> and <c>A1:C3</c> compare equal.
/// </summary>
public readonly record struct CellRange
{
    public CellRange(CellRef a, CellRef b)
    {
        this.Left = Math.Min(a.Column, b.Column);
        this.Top = Math.Min(a.Row, b.Row);
        this.Right = Math.Max(a.Column, b.Column);
        this.Bottom = Math.Max(a.Row, b.Row);
    }

    public CellRange(CellRef single) : this(single, single)
    {
    }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public int ColumnCount => this.Right - this.Left + 1;
    public int RowCount => this.Bottom - this.Top + 1;
    public long CellCount => (long)this.ColumnCount * this.RowCount;
    public bool IsSingleCell => this.Left == this.Right && this.Top == this.Bottom;

    public CellRef TopLeft => new(this.Left, this.Top);
    public CellRef BottomRight => new(this.Right, this.Bottom);

    public bool Contains(CellRef cell)
        => cell.Column >= this.Left && cell.Column <= this.Right && cell.Row >= this.Top && cell.Row <= this.Bottom;

    public bool Intersects(CellRange other)
        => this.Left <= other.Right && other.Left <= this.Right && this.Top <= other.Bottom && other.Top <= this.Bottom;

    /// <summary>Enumerates the range row by row, left to right.</summary>
    public IEnumerable<CellRef> Cells()
    {
        for (var row = this.Top; row <= this.Bottom; row++)
            for (var column = this.Left; column <= this.Right; column++)
                yield return new CellRef(column, row);
    }

    public static CellRange Parse(ReadOnlySpan<char> text)
        => TryParse(text, out var result) ? result : throw new FormatException($"'{text}' is not a valid range.");

    public static bool TryParse(ReadOnlySpan<char> text, out CellRange result)
    {
        result = default;
        var colon = text.IndexOf(':');
        if (colon < 0)
        {
            if (!CellRef.TryParse(text, out var single))
                return false;

            result = new CellRange(single);
            return true;
        }

        if (!CellRef.TryParse(text[..colon], out var from) || !CellRef.TryParse(text[(colon + 1)..], out var to))
            return false;

        result = new CellRange(from, to);
        return true;
    }

    public override string ToString()
        => this.IsSingleCell ? this.TopLeft.ToString() : $"{this.TopLeft}:{this.BottomRight}";
}
