using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Spreadsheet.Calc;

namespace Shiny.Controls.Office.Spreadsheet.Commands;

/// <summary>
/// Writes a clipboard capture onto a sheet — values, formulas and formatting — as a single undo step.
/// </summary>
/// <remarks>
/// <para>
/// The work is done by the same one-cell commands a typed edit uses, gathered into one composite. That
/// is deliberate: each of those already knows how to describe its own inverse exactly, so pasting over
/// a formatted formula cell undoes back to that formula and that formatting without this command
/// having to model either. What this class owns is only the arithmetic — which cell goes where, what
/// has to be cleared first, and what a formula has to be rewritten to.
/// </para>
/// <para>
/// A cut clears its source as part of the same step, before anything is written, so cutting a block
/// and pasting it one row down does not wipe out what it has just written.
/// </para>
/// </remarks>
public sealed class PasteClipboardCommand(SpreadsheetClipboardContent content, string sheetName, CellRef target)
    : IEditCommand<Workbook>
{
    /// <summary>The capture being pasted.</summary>
    public SpreadsheetClipboardContent Content { get; } = content;

    /// <summary>The sheet being pasted onto, which need not be the one the capture came from.</summary>
    public string SheetName { get; } = sheetName;

    /// <summary>Where the capture lands: its top-left corner for cells, its first row or column otherwise.</summary>
    public CellRef Target { get; } = target.Relative();

    public string Name => this.Content.Operation == SpreadsheetClipboardOperation.Cut ? "Move" : "Paste";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sheet = context[this.SheetName];
        var origin = this.Origin();
        var columnDelta = origin.Column - this.Content.Captured.Left;
        var rowDelta = origin.Row - this.Content.Captured.Top;

        var commands = new List<IEditCommand<Workbook>>();

        // Source first. A cut into an overlapping destination would otherwise erase the cells it had
        // just written, and the capture is a snapshot so nothing is lost by clearing early.
        if (this.Content.Operation == SpreadsheetClipboardOperation.Cut &&
            context.Find(this.Content.SheetName) is { } source)
        {
            this.AddClear(commands, source, this.Content.Captured, this.Content.SheetName);
        }

        this.AddClear(commands, sheet, Offset(this.Content.Captured, columnDelta, rowDelta), this.SheetName);
        this.AddBands(commands, origin);
        this.AddCells(commands, origin, columnDelta, rowDelta);

        var inverses = new IEditCommand<Workbook>[commands.Count];
        for (var i = 0; i < commands.Count; i++)
            inverses[commands.Count - 1 - i] = commands[i].Apply(context);

        return new CompositeCommand<Workbook>(this.Name, inverses);
    }

    /// <summary>
    /// Where the captured rectangle's top-left lands.
    /// </summary>
    /// <remarks>
    /// A row paste moves only vertically and a column paste only horizontally, whatever cell happens
    /// to be selected: a whole row put down three columns to the right would no longer be that row.
    /// </remarks>
    CellRef Origin() => this.Content.Kind switch
    {
        SpreadsheetClipboardKind.Rows => new CellRef(this.Content.Captured.Left, this.Target.Row),
        SpreadsheetClipboardKind.Columns => new CellRef(this.Target.Column, this.Content.Captured.Top),
        _ => this.Target
    };

    /// <summary>
    /// Empties a region so what lands on it is what was pasted, not a mixture with what was there.
    /// </summary>
    /// <remarks>
    /// A row or column paste replaces the whole band, not just the columns the capture happened to
    /// cover, so the cleared region is widened to everything the sheet is using. Without that, pasting
    /// a three-column row over a ten-column one leaves seven columns of the old row in place.
    /// </remarks>
    void AddClear(List<IEditCommand<Workbook>> commands, Worksheet sheet, CellRange region, string name)
    {
        if (sheet.UsedRange is not { } used)
            return;

        var widened = this.Content.Kind switch
        {
            SpreadsheetClipboardKind.Rows => new CellRange(
                new CellRef(Math.Min(region.Left, used.Left), region.Top),
                new CellRef(Math.Max(region.Right, used.Right), region.Bottom)),

            SpreadsheetClipboardKind.Columns => new CellRange(
                new CellRef(region.Left, Math.Min(region.Top, used.Top)),
                new CellRef(region.Right, Math.Max(region.Bottom, used.Bottom))),

            _ => region
        };

        if (!widened.Intersects(used))
            return;

        var scope = new CellRange(
            new CellRef(Math.Max(widened.Left, used.Left), Math.Max(widened.Top, used.Top)),
            new CellRef(Math.Min(widened.Right, used.Right), Math.Min(widened.Bottom, used.Bottom)));

        commands.Add(new ClearRangeCommand(name, scope));

        // Contents and formatting are separate clears because Delete and Clear All are separate
        // operations everywhere else in the model, and only a paste wants both.
        foreach (var cell in scope.Cells())
        {
            if (sheet.GetStyleIndex(cell) is not null)
                commands.Add(new SetCellStyleCommand(name, cell, null));
        }
    }

    void AddBands(List<IEditCommand<Workbook>> commands, CellRef origin)
    {
        foreach (var band in this.Content.Bands)
        {
            switch (this.Content.Kind)
            {
                case SpreadsheetClipboardKind.Rows:
                    var row = origin.Row + band.Offset;
                    if (row > CellRef.MaxRow)
                        continue;

                    commands.Add(new SetRowStyleCommand(this.SheetName, row, band.StyleIndex));
                    commands.Add(new SetRowHeightCommand(this.SheetName, row, band.Size));
                    break;

                case SpreadsheetClipboardKind.Columns:
                    var column = origin.Column + band.Offset;
                    if (column > CellRef.MaxColumn)
                        continue;

                    commands.Add(new SetColumnStyleCommand(this.SheetName, column, column, band.StyleIndex));
                    commands.Add(new SetColumnWidthCommand(this.SheetName, column, column, band.Size));
                    break;
            }
        }
    }

    void AddCells(List<IEditCommand<Workbook>> commands, CellRef origin, int columnDelta, int rowDelta)
    {
        foreach (var entry in this.Content.Cells)
        {
            var cell = new CellRef(origin.Column + entry.Column, origin.Row + entry.Row);
            if (!cell.IsValid)
                continue;

            if (entry.Formula is { Length: > 0 } formula)
            {
                // Only a copy rebases: a cut moves the formula bodily, and Excel leaves a moved
                // formula pointing at exactly the cells it pointed at before it was moved.
                var text = this.Content.Operation == SpreadsheetClipboardOperation.Copy
                    ? FormulaReferenceShifter.Translate(formula, columnDelta, rowDelta)
                    : formula;

                commands.Add(new SetCellFormulaCommand(this.SheetName, cell, text));
            }
            else if (!entry.Value.IsBlank)
            {
                commands.Add(new SetCellValueCommand(this.SheetName, cell, entry.Value));
            }

            if (entry.StyleIndex is not null || entry.Formula is not null || !entry.Value.IsBlank)
                commands.Add(new SetCellStyleCommand(this.SheetName, cell, entry.StyleIndex));
        }
    }

    static CellRange Offset(CellRange range, int columns, int rows) => new(
        new CellRef(Math.Clamp(range.Left + columns, 0, CellRef.MaxColumn), Math.Clamp(range.Top + rows, 0, CellRef.MaxRow)),
        new CellRef(Math.Clamp(range.Right + columns, 0, CellRef.MaxColumn), Math.Clamp(range.Bottom + rows, 0, CellRef.MaxRow)));
}
