using Shiny.Controls.Office.Editing;

namespace Shiny.Controls.Office.Spreadsheet.Commands;

/// <summary>Points one cell at a style index, or clears it back to the default with null.</summary>
public sealed class SetCellStyleCommand(string sheetName, CellRef cell, uint? styleIndex)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public CellRef Cell { get; } = cell.Relative();
    public uint? StyleIndex { get; } = styleIndex;

    public string Name => "Format";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        var previous = sheet.GetStyleIndex(this.Cell);
        sheet.WriteStyleIndex(this.Cell, this.StyleIndex);

        return new SetCellStyleCommand(this.SheetName, this.Cell, previous);
    }
}

/// <summary>Points a whole row at a style index, which every cell in it inherits.</summary>
public sealed class SetRowStyleCommand(string sheetName, int row, uint? styleIndex)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int Row { get; } = row;
    public uint? StyleIndex { get; } = styleIndex;

    public string Name => "Format Row";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        var previous = sheet.GetRowStyleIndex(this.Row);
        sheet.WriteRowStyle(this.Row, this.StyleIndex);

        return new SetRowStyleCommand(this.SheetName, this.Row, previous);
    }
}

/// <summary>
/// Points a span of columns at a style index, which every cell in them inherits.
/// </summary>
/// <remarks>
/// This is what "format the column" means in a file: one attribute on one <c>&lt;col&gt;</c> element,
/// applying to all 1,048,576 rows — including the ones that do not exist yet, which is why a column
/// formatted as currency still shows currency for a value typed into it tomorrow.
/// </remarks>
public sealed class SetColumnStyleCommand(string sheetName, int first, int last, uint? styleIndex)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int First { get; } = first;
    public int Last { get; } = last;
    public uint? StyleIndex { get; } = styleIndex;

    public string Name => "Format Column";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];

        // Restored run by run: the span being set can straddle several existing spans with different
        // styles, and only one command per run reproduces them.
        var restore = new List<IEditCommand<Workbook>>();
        foreach (var (first, last, previous) in sheet.ColumnStyleRuns(this.First, this.Last))
        {
            if (previous != this.StyleIndex)
                restore.Add(new SetColumnStyleCommand(this.SheetName, first, last, previous));
        }

        sheet.WriteColumnStyle(this.First, this.Last, this.StyleIndex);
        return new CompositeCommand<Workbook>(this.Name, restore);
    }
}

/// <summary>Sets a span of columns to a width in characters, or back to the sheet default with null.</summary>
public sealed class SetColumnWidthCommand(string sheetName, int first, int last, double? characters)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int First { get; } = first;
    public int Last { get; } = last;

    /// <summary>Width in characters of the default font's widest digit — the unit the file uses.</summary>
    public double? Characters { get; } = characters;

    public string Name => "Column Width";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];

        var restore = new List<IEditCommand<Workbook>>();
        foreach (var (first, last, previous) in sheet.ColumnWidthRuns(this.First, this.Last))
        {
            if (previous != this.Characters)
                restore.Add(new SetColumnWidthCommand(this.SheetName, first, last, previous));
        }

        sheet.WriteColumnWidth(this.First, this.Last, this.Characters);
        return new CompositeCommand<Workbook>(this.Name, restore);
    }
}

/// <summary>Sets a row's height in points, or back to the sheet default with null.</summary>
public sealed class SetRowHeightCommand(string sheetName, int row, double? points)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int Row { get; } = row;

    /// <summary>Height in points — the unit the file uses, not the pixels the grid is laid out in.</summary>
    public double? Points { get; } = points;

    public string Name => "Row Height";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        var previous = sheet.GetRowHeight(this.Row);
        sheet.WriteRowHeight(this.Row, this.Points);

        return new SetRowHeightCommand(this.SheetName, this.Row, previous);
    }
}

/// <summary>Hides or shows a span of columns.</summary>
public sealed class SetColumnHiddenCommand(string sheetName, int first, int last, bool hidden)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int First { get; } = first;
    public int Last { get; } = last;
    public bool Hidden { get; } = hidden;

    public string Name => this.Hidden ? "Hide Columns" : "Show Columns";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        sheet.WriteColumnHidden(this.First, this.Last, this.Hidden);

        return new SetColumnHiddenCommand(this.SheetName, this.First, this.Last, !this.Hidden);
    }
}

/// <summary>
/// Applies a formatting change across a range, as one undo step.
/// </summary>
/// <remarks>
/// <para>
/// The change is a delta rather than a format to assign, because a selection is rarely uniform:
/// bolding a range that mixes a red heading with black body text has to leave both colours where they
/// are. Each cell's own resolved format is read, the delta folded into it, and the result interned
/// back into a style index.
/// </para>
/// <para>
/// A selection made from a column or row header is recognised and written as a <em>column</em> or
/// <em>row</em> style rather than as a million cell styles — which is both what Excel does and the
/// difference between an instant operation and one that would never finish.
/// </para>
/// </remarks>
public sealed class FormatRangeCommand(string sheetName, CellRange range, CellFormatChange change)
    : IEditCommand<Workbook>
{
    /// <summary>
    /// The point past which a selection is treated as covering data rather than cells.
    /// </summary>
    /// <remarks>
    /// Formatting a blank cell is meaningful — it is how a column gets set up before anything is typed
    /// into it — so a selection is honoured literally where it can be. But a drag across a hundred
    /// thousand blank cells means "the block I am looking at", not "materialise a hundred thousand cell
    /// elements", and doing the latter would balloon the file for nothing.
    /// </remarks>
    const long MaxCellsToWrite = 20_000;

    /// <summary>The same idea for rows, which cost a <c>&lt;row&gt;</c> element each.</summary>
    const int MaxRowsToWrite = 20_000;

    public string SheetName { get; } = sheetName;
    public CellRange Range { get; } = range;
    public CellFormatChange Change { get; } = change;

    public string Name => "Format";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sheet = context[this.SheetName];
        var restore = new List<IEditCommand<Workbook>>();

        var wholeColumns = this.Range.Top == 0 && this.Range.Bottom >= CellRef.MaxRow;
        var wholeRows = this.Range.Left == 0 && this.Range.Right >= CellRef.MaxColumn;

        // Columns first, and Select All takes this path too: a column style is one attribute on one
        // element, where the row equivalent needs a row element per row.
        if (wholeColumns)
            this.ApplyToColumns(context, sheet, restore);
        else if (wholeRows)
            this.ApplyToRows(context, sheet, restore);

        foreach (var cell in this.CellsToTouch(sheet, wholeColumns || wholeRows))
            this.ApplyToCell(context, sheet, cell, restore);

        return new CompositeCommand<Workbook>(this.Name, restore);
    }

    void ApplyToColumns(Workbook context, Worksheet sheet, List<IEditCommand<Workbook>> restore)
    {
        // Materialised before writing: the runs are read from the very spans the writes rewrite.
        foreach (var (first, last, previous) in sheet.ColumnStyleRuns(this.Range.Left, this.Range.Right).ToList())
        {
            var target = Style(context, previous);
            if (previous == target)
                continue;

            restore.Add(new SetColumnStyleCommand(this.SheetName, first, last, previous));
            sheet.WriteColumnStyle(first, last, target);
        }
    }

    void ApplyToRows(Workbook context, Worksheet sheet, List<IEditCommand<Workbook>> restore)
    {
        var bottom = this.Range.Bottom;

        // A row-header drag can select a million rows, and each one formatted is a row element the file
        // did not have. Past the cap only the rows that hold something are given a style.
        if (this.Range.RowCount > MaxRowsToWrite)
            bottom = Math.Max(this.Range.Top, Math.Min(bottom, sheet.UsedRange?.Bottom ?? this.Range.Top));

        for (var row = this.Range.Top; row <= bottom; row++)
        {
            var previous = sheet.GetRowStyleIndex(row);
            var target = Style(context, previous);
            if (previous == target)
                continue;

            restore.Add(new SetRowStyleCommand(this.SheetName, row, previous));
            sheet.WriteRowStyle(row, target);
        }
    }

    void ApplyToCell(Workbook context, Worksheet sheet, CellRef cell, List<IEditCommand<Workbook>> restore)
    {
        var own = sheet.GetStyleIndex(cell);

        // Resolved from the effective index rather than the cell's own: a cell in a column that is
        // already bold has to come out bold and italic, not default and italic.
        var current = context.Styles.Resolve(sheet.GetEffectiveStyleIndex(cell));
        var index = context.StyleWriter.Intern(this.Change.ApplyTo(current));

        // Index 0 is the default format, and normally means "carry no style at all". Not when the cell
        // sits under a formatted row or column: there the attribute has to stay, as the override that
        // stops the cell inheriting the formatting the user just cleared from it.
        var inherits = sheet.GetRowStyleIndex(cell.Row) is not null || sheet.GetColumnStyleIndex(cell.Column) is not null;
        var target = index == 0 && !inherits ? (uint?)null : index;

        if (own == target)
            return;

        restore.Add(new SetCellStyleCommand(this.SheetName, cell, own));
        sheet.WriteStyleIndex(cell, target);
    }

    /// <summary>Folds the change into a style index and interns the result, as an index to write.</summary>
    uint? Style(Workbook context, uint? previous)
    {
        var index = context.StyleWriter.Intern(this.Change.ApplyTo(context.Styles.Resolve(previous)));
        return index == 0 ? null : index;
    }

    /// <summary>
    /// The cells to rewrite individually.
    /// </summary>
    /// <param name="inherited">
    /// True when the range was already handled as whole columns or rows. Only cells carrying a style of
    /// their own are then touched: everything else already inherits the new one, and writing the rest
    /// would mean creating a cell element for every populated row in the column, for no gain.
    /// </param>
    IEnumerable<CellRef> CellsToTouch(Worksheet sheet, bool inherited)
    {
        var range = this.Range;

        if (inherited || range.CellCount > MaxCellsToWrite)
        {
            if (sheet.UsedRange is not { } used || !used.Intersects(range))
                yield break;

            range = new CellRange(
                new CellRef(Math.Max(range.Left, used.Left), Math.Max(range.Top, used.Top)),
                new CellRef(Math.Min(range.Right, used.Right), Math.Min(range.Bottom, used.Bottom)));
        }

        foreach (var cell in range.Cells())
        {
            if (!inherited || sheet.GetStyleIndex(cell) is not null)
                yield return cell;
        }
    }
}
