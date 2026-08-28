namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>The aggregates a toolbar's AutoSum button offers.</summary>
public enum AutoFunction
{
    Sum,
    Average,

    /// <summary>Excel's <c>COUNT</c>: how many of the cells hold a number.</summary>
    Count,
    Min,
    Max
}

/// <summary>One formula an auto-function would write: where it goes, and what it totals.</summary>
public readonly record struct AutoFunctionEntry(CellRef Target, CellRange Source);

/// <summary>
/// Works out where an auto-function's totals belong, and what each one should cover.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of AutoSum. The formula itself is trivial — <c>SUM(D2:D9)</c> — and everything
/// that makes the button feel like it read your mind is in choosing that range and that destination
/// from nothing but the current selection and what happens to be on the sheet.
/// </para>
/// <para>
/// The rules follow Excel's, which are worth stating because they are not obvious:
/// </para>
/// <list type="bullet">
/// <item>One cell selected: total the run of numbers immediately above it, or failing that the run to
/// its left, and put the result in the selected cell.</item>
/// <item>A single row or column selected: put the total just past the end of it — or in its last cell
/// when that cell is empty, which is what selecting the numbers <em>and</em> the blank below them
/// means.</item>
/// <item>A block selected: one total per column, in the row underneath.</item>
/// </list>
/// </remarks>
public static class AutoFunctions
{
    /// <summary>The Excel function name — what actually goes into the formula.</summary>
    public static string NameOf(AutoFunction function) => function switch
    {
        AutoFunction.Sum => "SUM",
        AutoFunction.Average => "AVERAGE",
        AutoFunction.Count => "COUNT",
        AutoFunction.Min => "MIN",
        AutoFunction.Max => "MAX",
        _ => "SUM"
    };

    /// <summary>A label for a menu. Separate from <see cref="NameOf"/>, which must stay a formula name.</summary>
    public static string DisplayName(AutoFunction function) => function switch
    {
        AutoFunction.Sum => "Sum",
        AutoFunction.Average => "Average",
        AutoFunction.Count => "Count numbers",
        AutoFunction.Min => "Minimum",
        AutoFunction.Max => "Maximum",
        _ => "Sum"
    };

    /// <summary>The formula text, without its leading <c>=</c>.</summary>
    public static string Formula(AutoFunction function, CellRange source)
        => $"{NameOf(function)}({source})";

    /// <summary>
    /// The formulas to write for a selection, or an empty list when there is nothing to total.
    /// </summary>
    /// <remarks>
    /// Returning nothing is a real answer, and the reason this returns a plan rather than writing
    /// anything: pressing AutoSum on an empty sheet should do nothing, not leave <c>=SUM()</c> behind
    /// for the user to clean up.
    /// </remarks>
    public static IReadOnlyList<AutoFunctionEntry> Plan(Worksheet sheet, CellRange selection)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (selection.IsSingleCell)
            return PlanFromNeighbours(sheet, selection.TopLeft);

        // A header click selects the whole sheet in one direction, which says nothing about where a
        // total belongs. The populated part of it does.
        if (Clamp(sheet, selection) is not { } range)
            return Array.Empty<AutoFunctionEntry>();

        if (range.RowCount == 1 && range.ColumnCount == 1)
            return PlanFromNeighbours(sheet, range.TopLeft);

        if (range.RowCount == 1)
            return PlanAcross(sheet, range);

        if (range.ColumnCount == 1)
            return PlanDown(sheet, range, range.Left);

        // A block: one total per column, all on the same row underneath it.
        var entries = new List<AutoFunctionEntry>(range.ColumnCount);
        for (var column = range.Left; column <= range.Right; column++)
            entries.AddRange(PlanDown(sheet, new CellRange(new CellRef(column, range.Top), new CellRef(column, range.Bottom)), column));

        return entries;
    }

    /// <summary>One cell selected: look up, then left, for a run of numbers to total.</summary>
    static IReadOnlyList<AutoFunctionEntry> PlanFromNeighbours(Worksheet sheet, CellRef target)
    {
        if (RunAbove(sheet, target) is { } above)
            return [new AutoFunctionEntry(target, above)];

        if (RunLeft(sheet, target) is { } left)
            return [new AutoFunctionEntry(target, left)];

        return Array.Empty<AutoFunctionEntry>();
    }

    /// <summary>A vertical selection: total it into the blank cell at its foot, or the row below.</summary>
    static IReadOnlyList<AutoFunctionEntry> PlanDown(Worksheet sheet, CellRange range, int column)
    {
        var bottom = range.Bottom;

        // Selecting the numbers and the empty cell under them is the standard gesture, and it means
        // "put it here" - not "total the blank as well and overflow into the next row".
        if (IsBlank(sheet, new CellRef(column, bottom)) && bottom > range.Top)
            return [new AutoFunctionEntry(new CellRef(column, bottom), new CellRange(new CellRef(column, range.Top), new CellRef(column, bottom - 1)))];

        if (bottom >= CellRef.MaxRow)
            return Array.Empty<AutoFunctionEntry>();

        return [new AutoFunctionEntry(new CellRef(column, bottom + 1), new CellRange(new CellRef(column, range.Top), new CellRef(column, bottom)))];
    }

    /// <summary>A horizontal selection: the same, one column to the right.</summary>
    static IReadOnlyList<AutoFunctionEntry> PlanAcross(Worksheet sheet, CellRange range)
    {
        var row = range.Top;
        var right = range.Right;

        if (IsBlank(sheet, new CellRef(right, row)) && right > range.Left)
            return [new AutoFunctionEntry(new CellRef(right, row), new CellRange(new CellRef(range.Left, row), new CellRef(right - 1, row)))];

        if (right >= CellRef.MaxColumn)
            return Array.Empty<AutoFunctionEntry>();

        return [new AutoFunctionEntry(new CellRef(right + 1, row), new CellRange(new CellRef(range.Left, row), new CellRef(right, row)))];
    }

    static CellRange? RunAbove(Worksheet sheet, CellRef target)
    {
        var row = target.Row - 1;
        while (row >= 0 && IsNumeric(sheet, new CellRef(target.Column, row)))
            row--;

        var first = row + 1;
        return first > target.Row - 1 ? null : new CellRange(new CellRef(target.Column, first), new CellRef(target.Column, target.Row - 1));
    }

    static CellRange? RunLeft(Worksheet sheet, CellRef target)
    {
        var column = target.Column - 1;
        while (column >= 0 && IsNumeric(sheet, new CellRef(column, target.Row)))
            column--;

        var first = column + 1;
        return first > target.Column - 1 ? null : new CellRange(new CellRef(first, target.Row), new CellRef(target.Column - 1, target.Row));
    }

    /// <summary>
    /// Whether a cell counts as part of the run being totalled.
    /// </summary>
    /// <remarks>
    /// A cell that already holds the same kind of aggregate ends the run. Without that, adding a second
    /// total under an existing one silently double-counts everything above it — the classic AutoSum
    /// mistake, and one the numbers give no sign of.
    /// </remarks>
    static bool IsNumeric(Worksheet sheet, CellRef cell)
    {
        if (sheet.GetFormula(cell) is { } formula && IsAggregate(formula))
            return false;

        return sheet.GetDisplayValue(cell).Kind == CellValueKind.Number;
    }

    static bool IsAggregate(string formula)
    {
        var trimmed = formula.AsSpan().TrimStart();
        foreach (var function in Enum.GetValues<AutoFunction>())
        {
            var name = NameOf(function);
            if (trimmed.StartsWith(name, StringComparison.OrdinalIgnoreCase) &&
                trimmed.Length > name.Length &&
                trimmed[name.Length] == '(')
            {
                return true;
            }
        }

        return false;
    }

    static bool IsBlank(Worksheet sheet, CellRef cell)
        => sheet.GetFormula(cell) is null && sheet.GetValue(cell).IsBlank;

    /// <summary>Cuts a whole-column or whole-row selection back to the part of it that holds data.</summary>
    static CellRange? Clamp(Worksheet sheet, CellRange range)
    {
        if (range.Bottom < CellRef.MaxRow && range.Right < CellRef.MaxColumn)
            return range;

        if (sheet.UsedRange is not { } used || !used.Intersects(range))
            return null;

        return new CellRange(
            new CellRef(Math.Max(range.Left, used.Left), Math.Max(range.Top, used.Top)),
            new CellRef(Math.Min(range.Right, used.Right), Math.Min(range.Bottom, used.Bottom)));
    }
}
