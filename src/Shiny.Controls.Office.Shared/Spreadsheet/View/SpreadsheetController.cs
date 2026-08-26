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
    /// <summary>
    /// Where each sheet was left: its selection, its scroll position and its column and row sizes.
    /// </summary>
    /// <remarks>
    /// Keyed by name because that is what survives an undo — deleting a sheet and undoing it produces a
    /// different <see cref="Worksheet"/> instance with the same name and the same contents, and coming
    /// back to a restored sheet at the top-left when you left it halfway down reads as data loss.
    /// </remarks>
    readonly Dictionary<string, SheetViewState> sheetState = new(StringComparer.OrdinalIgnoreCase);

    Worksheet sheet;
    DragMode drag = DragMode.None;
    int resizeIndex = -1;
    double resizeOrigin;
    double resizeStartSize;

    /// <summary>What a sheet looked like when it was last showing.</summary>
    sealed record SheetViewState(GridMetrics Metrics, double ScrollX, double ScrollY, CellRef Anchor, CellRef Active);

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

    /// <summary>The sheets a tab strip should offer, in book order. Hidden sheets are left out.</summary>
    public IReadOnlyList<Worksheet> VisibleSheets => this.Workbook.Sheets.Where(x => x.IsVisible).ToList();

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

    /// <summary>
    /// Raised when a different sheet becomes the one on screen — by a tab click, or because a
    /// structural edit took the old one away.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="Changed"/>, which every scroll and keystroke raises. A host binding
    /// its sheet name two-way needs the rare event, not the constant one.
    /// </remarks>
    public event EventHandler<Worksheet>? ActiveSheetChanged;

    public void SwitchSheet(Worksheet target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (ReferenceEquals(target, this.sheet))
            return;

        this.CancelEdit();
        this.Remember();
        this.Adopt(target);
        this.RaiseChanged();
        this.ActiveSheetChanged?.Invoke(this, target);
    }

    /// <summary>Switches to a sheet by name. Unknown or hidden names are ignored.</summary>
    public void SwitchSheet(string name)
    {
        if (this.Workbook.Find(name) is { } target)
            this.SwitchSheet(target);
    }

    /// <summary>Files the current sheet's position away so coming back to it lands where it was left.</summary>
    void Remember()
        => this.sheetState[this.sheet.Name] = new SheetViewState(
            this.Metrics,
            this.Viewport.ScrollX,
            this.Viewport.ScrollY,
            this.Selection.Anchor,
            this.Selection.Active);

    /// <summary>Makes a sheet the current one, restoring where it was left if it has been seen before.</summary>
    void Adopt(Worksheet target)
    {
        var remembered = this.sheetState.GetValueOrDefault(target.Name);

        this.sheet = target;

        // A remembered metrics object carries the column widths the user dragged out by hand. Rebuilding
        // it from the sheet would throw those away every time a tab was clicked.
        this.Metrics = remembered?.Metrics ?? GridMetrics.FromWorksheet(target);
        this.Viewport = new GridViewport(this.Metrics) { Width = this.Viewport.Width, Height = this.Viewport.Height };

        if (remembered is null)
        {
            this.Selection.MoveTo(new CellRef(0, 0));
            return;
        }

        this.Viewport.ScrollTo(remembered.ScrollX, remembered.ScrollY);

        // Anchor first, then extend: that reproduces both the range and which end of it is active.
        this.Selection.MoveTo(remembered.Anchor);
        if (remembered.Active != remembered.Anchor)
            this.Selection.ExtendTo(remembered.Active);
    }

    public void Resize(double width, double height)
    {
        this.Viewport.Width = width;
        this.Viewport.Height = height;
        this.RaiseChanged();
    }

    /// <summary>The text a formula bar should show: the formula when there is one, otherwise the literal.</summary>
    public string ActiveCellText => this.CellText(this.Selection.Active);

    /// <summary>The active cell's address in A1 notation — what a formula bar's name box shows.</summary>
    public string ActiveCellAddress => this.Selection.Active.Relative().ToString();

    /// <summary>
    /// Writes text into the active cell, reading it exactly as typing it into the cell would.
    /// </summary>
    /// <remarks>
    /// For a formula bar, which edits the same cell from somewhere else on screen. It deliberately does
    /// not go through <see cref="BeginEdit"/>: that raises <see cref="EditingChanged"/> and would open
    /// the in-cell editor on top of the grid, leaving two editors live on one cell and each unaware of
    /// what the other holds.
    /// </remarks>
    public void SetActiveCellText(string text) => this.SetCellText(this.Selection.Active, text);

    /// <summary>
    /// Writes text into a named cell, reading it exactly as typing it into that cell would.
    /// </summary>
    /// <remarks>
    /// The cell is named rather than assumed to be the active one because a formula bar loses focus
    /// <em>after</em> the click that moved the selection: committing to whatever is active by then
    /// would put the text in the cell the user clicked on rather than the one they were editing.
    /// </remarks>
    public void SetCellText(CellRef cell, string text)
    {
        this.CancelEdit();
        this.Workbook.Undo.BreakCoalescing();
        this.Apply(cell.Relative(), text ?? string.Empty);
        this.RaiseChanged();
    }

    /// <summary>
    /// Moves the selection to a cell and scrolls it into view — what typing an address into the name
    /// box does.
    /// </summary>
    public void GoTo(CellRef cell)
    {
        this.CancelEdit();
        this.Selection.MoveTo(cell.Relative());
        this.Viewport.ScrollIntoView(this.Selection.Active);
        this.RaiseChanged();
    }

    /// <summary>What a cell would show in a formula bar: its formula when it has one, else its literal.</summary>
    public string CellText(CellRef cell)
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

        var before = this.CurrentSheetNames();
        this.Workbook.Undo.Undo();

        // What was undone may have been a sheet edit, in which case the sheet on screen has just been
        // renamed, hidden or deleted out from under this controller.
        this.AfterSheetsChanged(this.Appeared(before) ?? this.sheet.Name);
    }

    public void Redo()
    {
        this.CancelEdit();

        var before = this.CurrentSheetNames();
        this.Workbook.Undo.Redo();
        this.AfterSheetsChanged(this.Appeared(before) ?? this.sheet.Name);
    }

    HashSet<string> CurrentSheetNames()
        => new(this.Workbook.Sheets.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The sheet an undo or redo brought back, if it brought one back.
    /// </summary>
    /// <remarks>
    /// Undoing a delete has to land on the sheet that returned — staying put would leave the user
    /// looking at a different sheet and wondering whether the undo did anything. The stack cannot say
    /// what a command restored, but a sheet that was not there a moment ago is unambiguous enough.
    /// </remarks>
    string? Appeared(HashSet<string> before)
        => this.Workbook.Sheets.FirstOrDefault(x => x.IsVisible && !before.Contains(x.Name))?.Name;

    // ---- sheets ----
    //
    // Each of these is the pair of things a sheet edit actually is: the command, which changes the
    // workbook and can be undone, and the reconciliation, which decides what should be on screen
    // afterwards. Neither half is any use without the other - a delete that leaves the controller
    // pointing at the sheet it just removed paints a grid that no longer exists.

    /// <summary>
    /// Adds a sheet after the current one and switches to it.
    /// </summary>
    /// <param name="name">A name, or null for the next free <c>SheetN</c>.</param>
    /// <param name="index">Where to put it, or null for immediately after the current sheet.</param>
    public Worksheet AddSheet(string? name = null, int? index = null)
    {
        var chosen = name ?? SheetNames.NextDefault(this.Workbook.Sheets.Select(x => x.Name));
        var position = index ?? this.IndexOf(this.sheet) + 1;

        this.Begin();
        this.Workbook.Execute(new AddSheetCommand(chosen, position));
        this.AfterSheetsChanged(chosen);

        return this.Workbook[chosen];
    }

    /// <summary>
    /// Renames a sheet, repointing every formula that referred to it.
    /// </summary>
    /// <exception cref="ArgumentException">The name is illegal or already taken.</exception>
    public void RenameSheet(Worksheet target, string newName)
    {
        ArgumentNullException.ThrowIfNull(target);

        var previous = target.Name;
        if (string.Equals(previous, newName, StringComparison.Ordinal))
            return;

        this.Begin();
        this.Workbook.Execute(new RenameSheetCommand(previous, newName));

        // The remembered position is filed under the old name; without this the sheet you are looking
        // at jumps back to A1 the moment you rename it.
        if (this.sheetState.Remove(previous, out var state))
            this.sheetState[newName] = state;

        this.AfterSheetsChanged(newName);
    }

    /// <summary>Copies a sheet in beside the original and switches to the copy.</summary>
    public Worksheet DuplicateSheet(Worksheet target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var copyName = SheetNames.MakeUnique(target.Name, this.Workbook.Sheets.Select(x => x.Name));

        this.Begin();
        this.Workbook.Execute(new DuplicateSheetCommand(target.Name, copyName, this.IndexOf(target) + 1));
        this.AfterSheetsChanged(copyName);

        return this.Workbook[copyName];
    }

    /// <summary>Deletes a sheet. There is no confirmation here; that belongs to the host.</summary>
    /// <exception cref="InvalidOperationException">It is the only visible sheet left.</exception>
    public void DeleteSheet(Worksheet target)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Whichever tab Excel would land on: the one to the right, or the one to the left at the end.
        var visible = this.VisibleSheets;
        var at = -1;
        for (var i = 0; i < visible.Count && at < 0; i++)
        {
            if (ReferenceEquals(visible[i], target))
                at = i;
        }

        var next = at < 0 ? null : visible.ElementAtOrDefault(at + 1) ?? visible.ElementAtOrDefault(at - 1);

        this.Begin();
        this.Workbook.Execute(new DeleteSheetCommand(target.Name));

        // The remembered position is deliberately kept: undoing the delete brings the sheet back, and
        // it should come back where it was rather than scrolled to the top.
        this.AfterSheetsChanged(next?.Name);
    }

    /// <summary>Moves a sheet to a position in the tab order.</summary>
    public void MoveSheet(Worksheet target, int index)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (this.IndexOf(target) == index)
            return;

        this.Begin();
        this.Workbook.Execute(new MoveSheetCommand(target.Name, index));
        this.AfterSheetsChanged(this.sheet.Name);
    }

    /// <summary>Hides or shows a sheet.</summary>
    /// <exception cref="InvalidOperationException">Hiding it would leave no visible sheet.</exception>
    public void SetSheetVisible(Worksheet target, bool visible)
    {
        ArgumentNullException.ThrowIfNull(target);

        this.Begin();
        this.Workbook.Execute(new SetSheetVisibilityCommand(target.Name, visible));
        this.AfterSheetsChanged(visible ? target.Name : null);
    }

    /// <summary>False when the sheet is the last one Excel would have a tab for.</summary>
    public bool CanRemoveFromView(Worksheet target)
        => target is not null && (!target.IsVisible || this.VisibleSheets.Count > 1);

    public int IndexOf(Worksheet target)
    {
        for (var i = 0; i < this.Workbook.Sheets.Count; i++)
        {
            if (ReferenceEquals(this.Workbook.Sheets[i], target))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Closes the editor and ends the coalescing run before a structural edit.
    /// </summary>
    /// <remarks>
    /// Without the break, adding a sheet in the middle of a typing run folds into that run's undo step,
    /// and one Ctrl+Z then takes the sheet away along with the characters.
    /// </remarks>
    void Begin()
    {
        this.CancelEdit();
        this.Workbook.Undo.BreakCoalescing();
    }

    /// <summary>Picks what should be on screen after the sheet list changed, and switches to it.</summary>
    void AfterSheetsChanged(string? preferred)
    {
        var target = this.Workbook.Find(preferred) is { IsVisible: true } named
            ? named
            : this.Resolve();

        if (target is null)
        {
            // Nothing visible is left to show. The workbook guards against this, so reaching here means
            // the grid keeps painting the last sheet rather than crashing on a null one.
            this.RaiseChanged();
            return;
        }

        if (ReferenceEquals(target, this.sheet))
        {
            this.RaiseChanged();
            return;
        }

        this.Remember();
        this.Adopt(target);
        this.RaiseChanged();
        this.ActiveSheetChanged?.Invoke(this, target);
    }

    /// <summary>
    /// The sheet the current one has become: itself if it survived, the sheet with its name if it was
    /// restored as a new instance, otherwise the first visible one.
    /// </summary>
    Worksheet? Resolve()
    {
        if (this.Workbook.Sheets.Any(x => ReferenceEquals(x, this.sheet)) && this.sheet.IsVisible)
            return this.sheet;

        if (this.Workbook.Find(this.sheet.Name) is { IsVisible: true } byName)
            return byName;

        return this.Workbook.Sheets.FirstOrDefault(x => x.IsVisible);
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
