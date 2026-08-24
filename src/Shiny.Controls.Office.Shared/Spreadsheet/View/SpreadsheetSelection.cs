namespace Shiny.Controls.Office.Spreadsheet.View;

public enum MoveDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// The active cell and the selected range.
/// </summary>
/// <remarks>
/// The anchor is kept separately from the range because extending a selection has to grow from where it
/// started, not from wherever the last extension landed — shift-clicking twice must select from the
/// original cell both times.
/// </remarks>
public sealed class SpreadsheetSelection
{
    CellRef anchor;
    CellRef active;

    public SpreadsheetSelection()
    {
        this.anchor = new CellRef(0, 0);
        this.active = new CellRef(0, 0);
        this.Range = new CellRange(this.active);
    }

    /// <summary>The cell that receives typing. Always inside <see cref="Range"/>.</summary>
    public CellRef Active
    {
        get => this.active;
        private set => this.active = value.Relative();
    }

    public CellRef Anchor => this.anchor;

    public CellRange Range { get; private set; }

    public bool IsSingleCell => this.Range.IsSingleCell;

    public event EventHandler? Changed;

    /// <summary>Collapses the selection onto one cell.</summary>
    public void MoveTo(CellRef cell)
    {
        var clamped = Clamp(cell);
        this.anchor = clamped;
        this.Active = clamped;
        this.Range = new CellRange(clamped);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Extends the selection from the anchor to <paramref name="cell"/>.</summary>
    public void ExtendTo(CellRef cell)
    {
        var clamped = Clamp(cell);
        this.Active = clamped;
        this.Range = new CellRange(this.anchor, clamped);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectRange(CellRange range)
    {
        this.anchor = range.TopLeft;
        this.Active = range.TopLeft;
        this.Range = range;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectColumn(int column)
        => this.SelectRange(new CellRange(new CellRef(column, 0), new CellRef(column, CellRef.MaxRow)));

    public void SelectRow(int row)
        => this.SelectRange(new CellRange(new CellRef(0, row), new CellRef(CellRef.MaxColumn, row)));

    public void SelectAll()
        => this.SelectRange(new CellRange(new CellRef(0, 0), new CellRef(CellRef.MaxColumn, CellRef.MaxRow)));

    public void Move(MoveDirection direction, bool extend = false)
    {
        var target = Step(this.Active, direction, 1);

        if (extend)
            this.ExtendTo(target);
        else
            this.MoveTo(target);
    }

    /// <summary>
    /// Ctrl+Arrow: jumps to the edge of the current block of data, the way Excel does — to the last
    /// non-empty cell in a run, or across a gap to the next non-empty cell.
    /// </summary>
    public void MoveToEdge(MoveDirection direction, Func<CellRef, bool> isPopulated, bool extend = false)
    {
        ArgumentNullException.ThrowIfNull(isPopulated);

        var current = this.Active;
        var next = Step(current, direction, 1);

        if (!InBounds(next))
        {
            this.Apply(current, extend);
            return;
        }

        var startedPopulated = isPopulated(current) && isPopulated(next);
        var cursor = current;

        while (true)
        {
            var candidate = Step(cursor, direction, 1);
            if (!InBounds(candidate))
                break;

            var populated = isPopulated(candidate);

            if (startedPopulated)
            {
                // Inside a run: stop on the last populated cell before the gap.
                if (!populated)
                    break;

                cursor = candidate;
                continue;
            }

            // In a gap: skip forward to the first populated cell and stop there.
            cursor = candidate;
            if (populated)
                break;
        }

        this.Apply(cursor, extend);
    }

    void Apply(CellRef target, bool extend)
    {
        if (extend)
            this.ExtendTo(target);
        else
            this.MoveTo(target);
    }

    /// <summary>
    /// Enter and Tab wrap inside a multi-cell selection rather than moving it, which is what makes
    /// typing across a selected block work.
    /// </summary>
    public void Advance(bool byRow, bool backwards = false)
    {
        if (this.IsSingleCell)
        {
            this.Move((byRow, backwards) switch
            {
                (true, false) => MoveDirection.Down,
                (true, true) => MoveDirection.Up,
                (false, false) => MoveDirection.Right,
                _ => MoveDirection.Left
            });

            return;
        }

        var range = this.Range;
        var column = this.Active.Column;
        var row = this.Active.Row;
        var step = backwards ? -1 : 1;

        if (byRow)
        {
            row += step;
            if (row > range.Bottom)
            {
                row = range.Top;
                column = column + 1 > range.Right ? range.Left : column + 1;
            }
            else if (row < range.Top)
            {
                row = range.Bottom;
                column = column - 1 < range.Left ? range.Right : column - 1;
            }
        }
        else
        {
            column += step;
            if (column > range.Right)
            {
                column = range.Left;
                row = row + 1 > range.Bottom ? range.Top : row + 1;
            }
            else if (column < range.Left)
            {
                column = range.Right;
                row = row - 1 < range.Top ? range.Bottom : row - 1;
            }
        }

        this.Active = new CellRef(column, row);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    static CellRef Step(CellRef cell, MoveDirection direction, int distance) => direction switch
    {
        MoveDirection.Up => cell.Offset(0, -distance),
        MoveDirection.Down => cell.Offset(0, distance),
        MoveDirection.Left => cell.Offset(-distance, 0),
        _ => cell.Offset(distance, 0)
    };

    static bool InBounds(CellRef cell)
        => cell.Column >= 0 && cell.Column <= CellRef.MaxColumn && cell.Row >= 0 && cell.Row <= CellRef.MaxRow;

    static CellRef Clamp(CellRef cell) => new(
        Math.Clamp(cell.Column, 0, CellRef.MaxColumn),
        Math.Clamp(cell.Row, 0, CellRef.MaxRow));
}
