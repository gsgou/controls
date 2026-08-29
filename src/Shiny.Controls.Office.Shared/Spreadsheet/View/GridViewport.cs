namespace Shiny.Controls.Office.Spreadsheet.View;

/// <summary>Which pane of a frozen split a point falls in.</summary>
[Flags]
public enum PaneKind
{
    Scrollable = 0,
    FrozenColumns = 1,
    FrozenRows = 2,
    Corner = FrozenColumns | FrozenRows
}

public readonly record struct HitTestResult(bool IsCell, CellRef Cell, PaneKind Pane, HitTarget Target);

public enum HitTarget
{
    None,
    Cell,
    ColumnHeader,
    RowHeader,
    SelectAllCorner,

    /// <summary>The draggable divider between two column headers.</summary>
    ColumnResize,
    RowResize,

    /// <summary>One of the two grab handles drawn on a touch selection.</summary>
    SelectionHandle
}

/// <summary>Which end of the selection a grab handle moves.</summary>
public enum SelectionHandle
{
    /// <summary>The top-left handle, which moves the anchor.</summary>
    Start,

    /// <summary>The bottom-right handle, which moves the active cell.</summary>
    End
}

/// <summary>
/// Maps between sheet coordinates and viewport coordinates for a scrolled, optionally frozen grid.
/// </summary>
/// <remarks>
/// Frozen panes are the reason this is not a single translation. The pinned bands do not scroll, so a
/// point's meaning depends on which pane it lands in, and the scrollable pane's origin is offset by the
/// pinned bands' width rather than starting at the headers.
/// </remarks>
public sealed class GridViewport(GridMetrics metrics)
{
    /// <summary>How close to a header divider counts as a resize grab.</summary>
    public const double ResizeGripPixels = 4;

    /// <summary>
    /// How far from a touch handle's centre still counts as grabbing it.
    /// </summary>
    /// <remarks>
    /// Deliberately larger than the handle is drawn. The handle marks the corner of the selection
    /// precisely, which is what makes it readable, but a corner is the hardest thing on the grid to
    /// land a fingertip on - and a miss does not do nothing, it pans the sheet out from under the
    /// selection the user was trying to adjust.
    /// </remarks>
    public const double HandleGripPixels = 22;

    public GridMetrics Metrics { get; } = metrics;

    public double ScrollX { get; private set; }
    public double ScrollY { get; private set; }

    public double Width { get; set; } = 800;
    public double Height { get; set; } = 600;

    /// <summary>Where the scrollable pane begins, past the headers and any pinned bands.</summary>
    public double ContentOriginX
        => this.Metrics.RowHeaderWidth + this.Metrics.Columns.SizeOfRange(0, this.Metrics.FrozenPane.Column);

    public double ContentOriginY
        => this.Metrics.ColumnHeaderHeight + this.Metrics.Rows.SizeOfRange(0, this.Metrics.FrozenPane.Row);

    /// <summary>
    /// How far past the content a scroll may go, or null for no limit.
    /// </summary>
    /// <remarks>
    /// A grid is a million rows by sixteen thousand columns whether or not anything is in them, so
    /// there is nothing in the metrics to stop a scroll. That is survivable with a wheel, which moves
    /// a notch at a time; a finger flings, and a sheet that scrolls into an unbounded field of blank
    /// cells with no way back but flinging the other way is indistinguishable from having lost the
    /// data. The host sets this from the used range plus a screen of slack, so there is somewhere to
    /// go past the last cell without there being everywhere.
    /// </remarks>
    public double? MaxScrollX { get; set; }

    /// <inheritdoc cref="MaxScrollX"/>
    public double? MaxScrollY { get; set; }

    public void ScrollTo(double x, double y)
    {
        this.ScrollX = Clamp(x, this.MaxScrollX);
        this.ScrollY = Clamp(y, this.MaxScrollY);

        static double Clamp(double value, double? max)
            => max is { } limit ? Math.Clamp(value, 0, Math.Max(0, limit)) : Math.Max(0, value);
    }

    public void ScrollBy(double dx, double dy) => this.ScrollTo(this.ScrollX + dx, this.ScrollY + dy);

    /// <summary>The first column of the scrollable pane.</summary>
    public int FirstVisibleColumn
    {
        get
        {
            var frozen = this.Metrics.FrozenPane.Column;
            var index = this.Metrics.Columns.IndexAt(this.Metrics.Columns.OffsetOf(frozen) + this.ScrollX);
            return Math.Max(frozen, index);
        }
    }

    public int FirstVisibleRow
    {
        get
        {
            var frozen = this.Metrics.FrozenPane.Row;
            var index = this.Metrics.Rows.IndexAt(this.Metrics.Rows.OffsetOf(frozen) + this.ScrollY);
            return Math.Max(frozen, index);
        }
    }

    /// <summary>The columns to paint in the scrollable pane, inclusive.</summary>
    public (int First, int Last) VisibleColumns()
    {
        var first = this.FirstVisibleColumn;
        var available = this.Width - this.ContentOriginX;
        var last = this.Metrics.Columns.LastIndexWithin(first, Math.Max(0, available));
        return (first, Math.Max(first, last));
    }

    public (int First, int Last) VisibleRows()
    {
        var first = this.FirstVisibleRow;
        var available = this.Height - this.ContentOriginY;
        var last = this.Metrics.Rows.LastIndexWithin(first, Math.Max(0, available));
        return (first, Math.Max(first, last));
    }

    /// <summary>The on-screen rectangle for a cell, accounting for scrolling and pinned panes.</summary>
    public GridRect CellRect(CellRef cell)
    {
        var frozen = this.Metrics.FrozenPane;

        var x = cell.Column < frozen.Column
            ? this.Metrics.RowHeaderWidth + this.Metrics.Columns.SizeOfRange(0, cell.Column)
            : this.ContentOriginX + this.Metrics.Columns.SizeOfRange(frozen.Column, cell.Column) - this.ScrollX;

        var y = cell.Row < frozen.Row
            ? this.Metrics.ColumnHeaderHeight + this.Metrics.Rows.SizeOfRange(0, cell.Row)
            : this.ContentOriginY + this.Metrics.Rows.SizeOfRange(frozen.Row, cell.Row) - this.ScrollY;

        return new GridRect(x, y, this.Metrics.Columns.SizeOf(cell.Column), this.Metrics.Rows.SizeOf(cell.Row));
    }

    public GridRect RangeRect(CellRange range)
    {
        var topLeft = this.CellRect(range.TopLeft);
        var bottomRight = this.CellRect(range.BottomRight);
        return new GridRect(
            topLeft.X,
            topLeft.Y,
            bottomRight.Right - topLeft.X,
            bottomRight.Bottom - topLeft.Y);
    }

    public PaneKind PaneAt(double x, double y)
    {
        var pane = PaneKind.Scrollable;
        if (this.Metrics.HasFrozenColumns && x < this.ContentOriginX)
            pane |= PaneKind.FrozenColumns;

        if (this.Metrics.HasFrozenRows && y < this.ContentOriginY)
            pane |= PaneKind.FrozenRows;

        return pane;
    }

    /// <summary>Resolves a viewport point to whatever is under it.</summary>
    public HitTestResult HitTest(double x, double y)
    {
        var inRowHeader = x < this.Metrics.RowHeaderWidth;
        var inColumnHeader = y < this.Metrics.ColumnHeaderHeight;

        if (inRowHeader && inColumnHeader)
            return new HitTestResult(false, default, PaneKind.Corner, HitTarget.SelectAllCorner);

        if (inColumnHeader)
        {
            var column = this.ColumnAt(x);
            if (column < 0)
                return new HitTestResult(false, default, PaneKind.Scrollable, HitTarget.None);

            // A grab near the trailing edge is a resize, not a selection.
            var bounds = this.CellRect(new CellRef(column, this.FirstVisibleRow));
            var target = Math.Abs(x - bounds.Right) <= ResizeGripPixels ? HitTarget.ColumnResize : HitTarget.ColumnHeader;
            return new HitTestResult(false, new CellRef(column, 0), this.PaneAt(x, y), target);
        }

        if (inRowHeader)
        {
            var row = this.RowAt(y);
            if (row < 0)
                return new HitTestResult(false, default, PaneKind.Scrollable, HitTarget.None);

            var bounds = this.CellRect(new CellRef(this.FirstVisibleColumn, row));
            var target = Math.Abs(y - bounds.Bottom) <= ResizeGripPixels ? HitTarget.RowResize : HitTarget.RowHeader;
            return new HitTestResult(false, new CellRef(0, row), this.PaneAt(x, y), target);
        }

        var hitColumn = this.ColumnAt(x);
        var hitRow = this.RowAt(y);
        if (hitColumn < 0 || hitRow < 0)
            return new HitTestResult(false, default, PaneKind.Scrollable, HitTarget.None);

        return new HitTestResult(true, new CellRef(hitColumn, hitRow), this.PaneAt(x, y), HitTarget.Cell);
    }

    /// <summary>The column under a viewport x, or -1 when it falls in the row header.</summary>
    public int ColumnAt(double x)
    {
        if (x < this.Metrics.RowHeaderWidth)
            return -1;

        var frozen = this.Metrics.FrozenPane.Column;
        if (this.Metrics.HasFrozenColumns && x < this.ContentOriginX)
        {
            var frozenOffset = x - this.Metrics.RowHeaderWidth;
            return Math.Min(this.Metrics.Columns.IndexAt(frozenOffset), frozen - 1);
        }

        var offset = x - this.ContentOriginX + this.ScrollX + this.Metrics.Columns.OffsetOf(frozen);
        return this.Metrics.Columns.IndexAt(offset);
    }

    public int RowAt(double y)
    {
        if (y < this.Metrics.ColumnHeaderHeight)
            return -1;

        var frozen = this.Metrics.FrozenPane.Row;
        if (this.Metrics.HasFrozenRows && y < this.ContentOriginY)
        {
            var frozenOffset = y - this.Metrics.ColumnHeaderHeight;
            return Math.Min(this.Metrics.Rows.IndexAt(frozenOffset), frozen - 1);
        }

        var offset = y - this.ContentOriginY + this.ScrollY + this.Metrics.Rows.OffsetOf(frozen);
        return this.Metrics.Rows.IndexAt(offset);
    }

    /// <summary>
    /// Scrolls the minimum amount needed to bring a cell fully into view. A cell inside a frozen band
    /// is always visible, so nothing moves.
    /// </summary>
    /// <summary>Where the two grab handles sit for a selection, in viewport coordinates.</summary>
    public (GridRect Start, GridRect End) SelectionHandles(CellRange range)
    {
        var rect = this.RangeRect(range);
        var size = HandleGripPixels;
        var half = size / 2;

        return (
            new GridRect(rect.X - half, rect.Y - half, size, size),
            new GridRect(rect.Right - half, rect.Bottom - half, size, size));
    }

    /// <summary>Which handle, if either, a point grabs.</summary>
    /// <remarks>
    /// End is tested first. The two handles overlap on a single-cell selection in a small cell, and
    /// End is the one that grows the range - starting a drag by shrinking from the anchor is almost
    /// never what was meant.
    /// </remarks>
    public SelectionHandle? SelectionHandleAt(CellRange range, double x, double y)
    {
        // Handles belong to the content area. Letting one be grabbed over the headers would put a
        // grab target on top of the row and column selection strips.
        if (x < this.Metrics.RowHeaderWidth || y < this.Metrics.ColumnHeaderHeight)
            return null;

        var (start, end) = this.SelectionHandles(range);

        if (end.Contains(x, y))
            return SelectionHandle.End;

        if (start.Contains(x, y))
            return SelectionHandle.Start;

        return null;
    }

    public void ScrollIntoView(CellRef cell)
    {
        var frozen = this.Metrics.FrozenPane;

        if (cell.Column >= frozen.Column)
        {
            var leading = this.Metrics.Columns.SizeOfRange(frozen.Column, cell.Column);
            var size = this.Metrics.Columns.SizeOf(cell.Column);
            var available = Math.Max(0, this.Width - this.ContentOriginX);

            if (leading < this.ScrollX)
                this.ScrollX = leading;
            else if (leading + size > this.ScrollX + available)
                this.ScrollX = leading + size - available;
        }

        if (cell.Row >= frozen.Row)
        {
            var leading = this.Metrics.Rows.SizeOfRange(frozen.Row, cell.Row);
            var size = this.Metrics.Rows.SizeOf(cell.Row);
            var available = Math.Max(0, this.Height - this.ContentOriginY);

            if (leading < this.ScrollY)
                this.ScrollY = leading;
            else if (leading + size > this.ScrollY + available)
                this.ScrollY = leading + size - available;
        }

        this.ScrollTo(this.ScrollX, this.ScrollY);
    }
}
