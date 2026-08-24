using Shiny.Controls.Office.Spreadsheet.Commands;

namespace Shiny.Controls.Office.Spreadsheet.View;

public enum EditCommitDirection
{
    None,
    Down,
    Up,
    Right,
    Left
}

/// <summary>
/// Host-independent interaction logic for the grid: selection, dragging, resizing and cell editing.
/// </summary>
/// <remarks>
/// Both hosts forward raw pointer and key events here rather than implementing behaviour themselves.
/// That is what keeps MAUI and Blazor genuinely identical — the only thing either host owns is turning
/// a platform event into a call on this class, and putting a text box on screen when editing starts.
/// </remarks>
public sealed class SpreadsheetController
{
    Worksheet sheet;
    DragMode drag = DragMode.None;
    int resizeIndex = -1;
    double resizeOrigin;
    double resizeStartSize;

    enum DragMode
    {
        None,
        SelectingCells,
        SelectingColumns,
        SelectingRows,
        ResizingColumn,
        ResizingRow
    }

    public SpreadsheetController(Workbook workbook, Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(sheet);

        this.Workbook = workbook;
        this.sheet = sheet;
        this.Metrics = GridMetrics.FromWorksheet(sheet);
        this.Viewport = new GridViewport(this.Metrics);
        this.Selection = new SpreadsheetSelection();
        this.Selection.Changed += (_, _) => this.RaiseChanged();
    }

    public Workbook Workbook { get; }

    public Worksheet Sheet => this.sheet;

    public GridMetrics Metrics { get; private set; }

    public GridViewport Viewport { get; private set; }

    public SpreadsheetSelection Selection { get; }

    /// <summary>The cell currently being edited, or null when no editor is open.</summary>
    public CellRef? EditingCell { get; private set; }

    /// <summary>The text the editor should show. Meaningful only while <see cref="EditingCell"/> is set.</summary>
    public string EditingText { get; private set; } = string.Empty;

    /// <summary>Raised whenever the host needs to repaint.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when an editor should be opened or closed over the active cell.</summary>
    public event EventHandler<CellRef?>? EditingChanged;

    public void SwitchSheet(Worksheet target)
    {
        ArgumentNullException.ThrowIfNull(target);
        this.CancelEdit();

        this.sheet = target;
        this.Metrics = GridMetrics.FromWorksheet(target);
        this.Viewport = new GridViewport(this.Metrics) { Width = this.Viewport.Width, Height = this.Viewport.Height };
        this.Selection.MoveTo(new CellRef(0, 0));
        this.RaiseChanged();
    }

    public void Resize(double width, double height)
    {
        this.Viewport.Width = width;
        this.Viewport.Height = height;
        this.RaiseChanged();
    }

    /// <summary>The text a formula bar should show: the formula when there is one, otherwise the literal.</summary>
    public string ActiveCellText => this.CellText(this.Selection.Active);

    string CellText(CellRef cell)
    {
        var formula = this.sheet.GetFormula(cell);
        if (formula is not null)
            return "=" + formula;

        var value = this.sheet.GetValue(cell);
        return value.IsBlank ? string.Empty : Calc.Coercion.ToText(value);
    }

    // ---- pointer ----

    public void PointerDown(double x, double y, bool extend = false)
    {
        this.CommitEdit(EditCommitDirection.None);

        var hit = this.Viewport.HitTest(x, y);
        switch (hit.Target)
        {
            case HitTarget.SelectAllCorner:
                this.Selection.SelectAll();
                return;

            case HitTarget.ColumnResize:
                this.drag = DragMode.ResizingColumn;
                this.resizeIndex = hit.Cell.Column;
                this.resizeOrigin = x;
                this.resizeStartSize = this.Metrics.Columns.SizeOf(hit.Cell.Column);
                return;

            case HitTarget.RowResize:
                this.drag = DragMode.ResizingRow;
                this.resizeIndex = hit.Cell.Row;
                this.resizeOrigin = y;
                this.resizeStartSize = this.Metrics.Rows.SizeOf(hit.Cell.Row);
                return;

            case HitTarget.ColumnHeader:
                this.drag = DragMode.SelectingColumns;
                this.Selection.SelectColumn(hit.Cell.Column);
                return;

            case HitTarget.RowHeader:
                this.drag = DragMode.SelectingRows;
                this.Selection.SelectRow(hit.Cell.Row);
                return;

            case HitTarget.Cell:
                this.drag = DragMode.SelectingCells;
                if (extend)
                    this.Selection.ExtendTo(hit.Cell);
                else
                    this.Selection.MoveTo(hit.Cell);

                return;
        }
    }

    public void PointerMove(double x, double y)
    {
        switch (this.drag)
        {
            case DragMode.None:
                return;

            case DragMode.ResizingColumn:
                // Below a few pixels a column reads as hidden rather than narrow, so clamp above zero.
                this.Metrics.Columns.SetSize(this.resizeIndex, Math.Max(2, this.resizeStartSize + (x - this.resizeOrigin)));
                this.RaiseChanged();
                return;

            case DragMode.ResizingRow:
                this.Metrics.Rows.SetSize(this.resizeIndex, Math.Max(2, this.resizeStartSize + (y - this.resizeOrigin)));
                this.RaiseChanged();
                return;
        }

        var hit = this.Viewport.HitTest(x, y);
        if (hit.Cell is { } cell)
        {
            switch (this.drag)
            {
                case DragMode.SelectingColumns:
                    this.Selection.ExtendTo(new CellRef(cell.Column, CellRef.MaxRow));
                    break;

                case DragMode.SelectingRows:
                    this.Selection.ExtendTo(new CellRef(CellRef.MaxColumn, cell.Row));
                    break;

                default:
                    this.Selection.ExtendTo(cell);
                    break;
            }
        }
    }

    public void PointerUp()
    {
        this.drag = DragMode.None;
        this.resizeIndex = -1;
    }

    public void DoubleClick(double x, double y)
    {
        var hit = this.Viewport.HitTest(x, y);
        if (hit.Target == HitTarget.Cell)
        {
            this.Selection.MoveTo(hit.Cell);
            this.BeginEdit();
        }
    }

    public void Scroll(double dx, double dy)
    {
        this.Viewport.ScrollBy(dx, dy);
        this.RaiseChanged();
    }

    // ---- keyboard ----

    public void Move(MoveDirection direction, bool extend = false, bool toEdge = false)
    {
        this.CommitEdit(EditCommitDirection.None);

        if (toEdge)
            this.Selection.MoveToEdge(direction, cell => !this.sheet.GetValue(cell).IsBlank, extend);
        else
            this.Selection.Move(direction, extend);

        this.Viewport.ScrollIntoView(this.Selection.Active);
        this.RaiseChanged();
    }

    public void Advance(bool byRow, bool backwards = false)
    {
        this.Selection.Advance(byRow, backwards);
        this.Viewport.ScrollIntoView(this.Selection.Active);
        this.RaiseChanged();
    }

    /// <summary>Clears the contents of the selection, leaving formatting intact.</summary>
    public void ClearSelection()
    {
        this.Workbook.Execute(new ClearRangeCommand(this.sheet.Name, this.Selection.Range));
        this.RaiseChanged();
    }

    public void Undo()
    {
        this.CancelEdit();
        this.Workbook.Undo.Undo();
        this.RaiseChanged();
    }

    public void Redo()
    {
        this.CancelEdit();
        this.Workbook.Undo.Redo();
        this.RaiseChanged();
    }

    // ---- editing ----

    /// <summary>Opens the editor on the active cell, seeded with its current content.</summary>
    public void BeginEdit(string? initialText = null)
    {
        if (this.EditingCell is not null)
            return;

        var cell = this.Selection.Active;
        this.EditingCell = cell;

        // Typing over a cell replaces it; F2 or a double-click edits what is there.
        this.EditingText = initialText ?? this.CellText(cell);

        this.Workbook.Undo.BreakCoalescing();
        this.EditingChanged?.Invoke(this, cell);
        this.RaiseChanged();
    }

    public void UpdateEditingText(string text)
    {
        if (this.EditingCell is null)
            return;

        this.EditingText = text ?? string.Empty;
    }

    public void CancelEdit()
    {
        if (this.EditingCell is null)
            return;

        this.EditingCell = null;
        this.EditingText = string.Empty;
        this.EditingChanged?.Invoke(this, null);
        this.RaiseChanged();
    }

    /// <summary>Writes the editor's content into the cell and closes the editor.</summary>
    public void CommitEdit(EditCommitDirection direction)
    {
        if (this.EditingCell is not { } cell)
            return;

        var text = this.EditingText;
        this.EditingCell = null;
        this.EditingText = string.Empty;
        this.EditingChanged?.Invoke(this, null);

        this.Apply(cell, text);

        switch (direction)
        {
            case EditCommitDirection.Down:
                this.Advance(byRow: true);
                break;
            case EditCommitDirection.Up:
                this.Advance(byRow: true, backwards: true);
                break;
            case EditCommitDirection.Right:
                this.Advance(byRow: false);
                break;
            case EditCommitDirection.Left:
                this.Advance(byRow: false, backwards: true);
                break;
            default:
                this.RaiseChanged();
                break;
        }
    }

    /// <summary>
    /// Interprets typed text the way Excel does: a leading <c>=</c> is a formula, otherwise the text is
    /// parsed as a number, boolean, error or date before falling back to text.
    /// </summary>
    void Apply(CellRef cell, string text)
    {
        var sheetName = this.sheet.Name;

        if (text.Length == 0)
        {
            this.Workbook.Execute(new SetCellValueCommand(sheetName, cell, CellValue.Blank));
            return;
        }

        if (text.StartsWith('=') && text.Length > 1)
        {
            this.Workbook.Execute(new SetCellFormulaCommand(sheetName, cell, text[1..]));
            return;
        }

        this.Workbook.Execute(new SetCellValueCommand(sheetName, cell, ParseInput(text)));
    }

    /// <summary>Converts typed text into the value Excel would store.</summary>
    public static CellValue ParseInput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return CellValue.Blank;

        var trimmed = text.Trim();

        if (trimmed.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            return CellValue.FromBoolean(true);

        if (trimmed.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return CellValue.FromBoolean(false);

        if (CellValue.TryParseError(trimmed.ToUpperInvariant(), out var error))
            return CellValue.FromError(error);

        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var number))
            return CellValue.FromNumber(number);

        // A percentage typed into a cell stores the fraction; the display format supplies the sign.
        if (trimmed.EndsWith('%') &&
            double.TryParse(trimmed[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var percent))
            return CellValue.FromNumber(percent / 100d);

        // Text that merely looks like a date is left as text; converting it would change what the user
        // typed, and only an explicit date format should do that.
        return CellValue.FromText(text);
    }

    void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
}
