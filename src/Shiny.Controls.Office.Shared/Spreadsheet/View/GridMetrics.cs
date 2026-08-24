namespace Shiny.Controls.Office.Spreadsheet.View;

public readonly record struct GridRect(double X, double Y, double Width, double Height)
{
    public double Right => this.X + this.Width;
    public double Bottom => this.Y + this.Height;

    public bool Contains(double x, double y)
        => x >= this.X && x < this.Right && y >= this.Y && y < this.Bottom;
}

/// <summary>
/// Column and row geometry for one sheet, in device-independent pixels.
/// </summary>
public sealed class GridMetrics
{
    /// <summary>
    /// Excel stores column width in characters of the default font's widest digit. For Calibri 11 that
    /// digit is 7px, and the stored value already includes padding, which is why the conversion is not
    /// a plain multiply.
    /// </summary>
    public const double MaxDigitWidth = 7d;

    public const double DefaultColumnWidthCharacters = 8.43;
    public const double DefaultRowHeightPoints = 15d;

    public GridMetrics()
    {
        this.Columns = new AxisMetrics(WidthToPixels(DefaultColumnWidthCharacters), CellRef.MaxColumn + 1);
        this.Rows = new AxisMetrics(PointsToPixels(DefaultRowHeightPoints), CellRef.MaxRow + 1);
    }

    public AxisMetrics Columns { get; }
    public AxisMetrics Rows { get; }

    /// <summary>Width of the row-number gutter down the left edge.</summary>
    public double RowHeaderWidth { get; set; } = 46;

    /// <summary>Height of the column-letter strip across the top.</summary>
    public double ColumnHeaderHeight { get; set; } = 22;

    /// <summary>The first non-frozen cell. Everything above and to the left of it stays pinned.</summary>
    public CellRef FrozenPane { get; set; }

    public bool HasFrozenColumns => this.FrozenPane.Column > 0;
    public bool HasFrozenRows => this.FrozenPane.Row > 0;

    public static double WidthToPixels(double characters)
        => Math.Truncate((characters * MaxDigitWidth + 5) / MaxDigitWidth * 256) / 256 * MaxDigitWidth;

    public static double PixelsToWidth(double pixels)
        => Math.Truncate((pixels / MaxDigitWidth * 256 + 0.5) / 256 * 100 + 0.5) / 100;

    public static double PointsToPixels(double points) => points * 96d / 72d;

    public static double PixelsToPoints(double pixels) => pixels * 72d / 96d;

    /// <summary>The rectangle a cell occupies in sheet space, ignoring scrolling and headers.</summary>
    public GridRect CellBounds(CellRef cell) => new(
        this.Columns.OffsetOf(cell.Column),
        this.Rows.OffsetOf(cell.Row),
        this.Columns.SizeOf(cell.Column),
        this.Rows.SizeOf(cell.Row));

    /// <summary>The rectangle a range occupies in sheet space — used for merged cells and selection.</summary>
    public GridRect RangeBounds(CellRange range) => new(
        this.Columns.OffsetOf(range.Left),
        this.Rows.OffsetOf(range.Top),
        this.Columns.SizeOfRange(range.Left, range.Right + 1),
        this.Rows.SizeOfRange(range.Top, range.Bottom + 1));

    /// <summary>Reads column widths, row heights and the frozen pane out of a sheet.</summary>
    public static GridMetrics FromWorksheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var metrics = new GridMetrics();

        foreach (var (first, last, width, hidden) in sheet.ColumnDefinitions())
        {
            for (var column = first; column <= last && column <= CellRef.MaxColumn; column++)
            {
                if (hidden)
                    metrics.Columns.SetHidden(column, true);
                else if (width is { } value)
                    metrics.Columns.SetSize(column, WidthToPixels(value));
            }
        }

        foreach (var (row, height, hidden) in sheet.RowDefinitions())
        {
            if (hidden)
                metrics.Rows.SetHidden(row, true);
            else if (height is { } value)
                metrics.Rows.SetSize(row, PointsToPixels(value));
        }

        if (sheet.FrozenPane is { } frozen)
            metrics.FrozenPane = frozen;

        return metrics;
    }
}
