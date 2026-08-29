using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// Sorted insert/lookup over a worksheet's <c>&lt;sheetData&gt;</c>.
/// </summary>
/// <remarks>
/// Rows must appear in ascending <c>r</c> order and cells in ascending column order within their row.
/// Excel does not repair a file that violates this — it reports it as corrupt — so every insert has to
/// find its ordered position rather than appending.
/// </remarks>
sealed class SheetDataEditor
{
    readonly SheetData sheetData;
    readonly Dictionary<uint, Row> rowIndex = new();

    public SheetDataEditor(SheetData sheetData)
    {
        this.sheetData = sheetData;
        foreach (var row in sheetData.Elements<Row>())
        {
            if (row.RowIndex?.Value is { } index)
                this.rowIndex[index] = row;
        }
    }

    public Row? FindRow(int rowIndexZeroBased)
        => this.rowIndex.TryGetValue((uint)(rowIndexZeroBased + 1), out var row) ? row : null;

    public Cell? FindCell(CellRef reference)
    {
        var row = this.FindRow(reference.Row);
        if (row is null)
            return null;

        foreach (var cell in row.Elements<Cell>())
        {
            var column = ColumnOf(cell);
            if (column == reference.Column)
                return cell;

            if (column > reference.Column)
                break;
        }

        return null;
    }

    public Row GetOrCreateRow(int rowIndexZeroBased)
    {
        var oneBased = (uint)(rowIndexZeroBased + 1);
        if (this.rowIndex.TryGetValue(oneBased, out var existing))
            return existing;

        var row = new Row { RowIndex = oneBased };

        // Find the first row that sorts after the new one and insert before it.
        Row? successor = null;
        foreach (var candidate in this.sheetData.Elements<Row>())
        {
            if (candidate.RowIndex?.Value > oneBased)
            {
                successor = candidate;
                break;
            }
        }

        if (successor is null)
            this.sheetData.AppendChild(row);
        else
            this.sheetData.InsertBefore(row, successor);

        this.rowIndex[oneBased] = row;
        return row;
    }

    public Cell GetOrCreateCell(CellRef reference)
    {
        var row = this.GetOrCreateRow(reference.Row);

        Cell? successor = null;
        foreach (var cell in row.Elements<Cell>())
        {
            var column = ColumnOf(cell);
            if (column == reference.Column)
                return cell;

            if (column > reference.Column)
            {
                successor = cell;
                break;
            }
        }

        var created = new Cell { CellReference = reference.Relative().ToString() };
        if (successor is null)
            row.AppendChild(created);
        else
            row.InsertBefore(created, successor);

        return created;
    }

    /// <summary>
    /// Removes a cell entirely. Empty rows are left in place — they may carry height, style or
    /// outline-level attributes that are not ours to discard.
    /// </summary>
    public void RemoveCell(CellRef reference)
    {
        var cell = this.FindCell(reference);
        cell?.Remove();
    }

    /// <summary>
    /// Moves every row at or below <paramref name="at"/> down by <paramref name="count"/>.
    /// </summary>
    /// <remarks>
    /// The elements keep their relative order because they all move by the same amount, so this only
    /// has to renumber them — no re-sorting, and no risk of transiently violating the ascending order
    /// Excel treats as corruption. Rows pushed past the last one Excel has are dropped, which is what
    /// Excel itself does rather than refusing the insert.
    /// </remarks>
    public void InsertRows(int at, int count)
    {
        foreach (var row in this.sheetData.Elements<Row>().ToList())
        {
            var index = IndexOf(row);
            if (index < at)
                continue;

            if (index + count > CellRef.MaxRow)
                row.Remove();
            else
                Renumber(row, index + count);
        }

        this.Reindex();
    }

    /// <summary>Removes the rows in the band and pulls everything below it up by <paramref name="count"/>.</summary>
    public void RemoveRows(int at, int count)
    {
        foreach (var row in this.sheetData.Elements<Row>().ToList())
        {
            var index = IndexOf(row);
            if (index < at)
                continue;

            if (index < at + count)
                row.Remove();
            else
                Renumber(row, index - count);
        }

        this.Reindex();
    }

    /// <summary>Moves every cell at or right of <paramref name="at"/> right by <paramref name="count"/>.</summary>
    public void InsertColumns(int at, int count)
    {
        foreach (var row in this.sheetData.Elements<Row>())
        {
            foreach (var (cell, column) in Columns(row))
            {
                if (column < at)
                    continue;

                if (column + count > CellRef.MaxColumn)
                    cell.Remove();
                else
                    cell.CellReference = new CellRef(column + count, IndexOf(row)).ToString();
            }
        }
    }

    /// <summary>Removes the cells in the band and pulls everything right of it left by <paramref name="count"/>.</summary>
    public void RemoveColumns(int at, int count)
    {
        foreach (var row in this.sheetData.Elements<Row>())
        {
            foreach (var (cell, column) in Columns(row))
            {
                if (column < at)
                    continue;

                if (column < at + count)
                    cell.Remove();
                else
                    cell.CellReference = new CellRef(column - count, IndexOf(row)).ToString();
            }
        }
    }

    /// <summary>
    /// Every cell in a row with the column it currently sits at, read before anything moves.
    /// </summary>
    /// <remarks>
    /// Materialised, and the columns resolved up front, because <see cref="ColumnOf"/> falls back to
    /// sibling position when a cell carries no <c>r</c> attribute — and half-renumbering a row would
    /// then make that fallback answer a different question for every cell after the first.
    /// </remarks>
    static List<(Cell Cell, int Column)> Columns(Row row)
    {
        var result = new List<(Cell, int)>();
        foreach (var cell in row.Elements<Cell>())
            result.Add((cell, ColumnOf(cell)));

        return result;
    }

    static int IndexOf(Row row) => (int)(row.RowIndex?.Value ?? 0) - 1;

    /// <summary>Points a row and every cell in it at a new row number.</summary>
    static void Renumber(Row row, int index)
    {
        row.RowIndex = (uint)(index + 1);

        foreach (var (cell, column) in Columns(row))
            cell.CellReference = new CellRef(column, index).ToString();
    }

    /// <summary>Rebuilds the row lookup after the elements were renumbered underneath it.</summary>
    void Reindex()
    {
        this.rowIndex.Clear();
        foreach (var row in this.sheetData.Elements<Row>())
        {
            if (row.RowIndex?.Value is { } index)
                this.rowIndex[index] = row;
        }
    }

    /// <summary>Parses the column index out of a cell's <c>r</c> attribute, falling back to sibling order.</summary>
    public static int ColumnOf(Cell cell)
    {
        var reference = cell.CellReference?.Value;
        if (reference is not null && CellRef.TryParse(reference, out var parsed))
            return parsed.Column;

        // The r attribute is optional in the schema. When it is missing, position within the row is
        // the only thing that defines the column.
        var index = 0;
        foreach (var sibling in cell.Parent?.Elements<Cell>() ?? Enumerable.Empty<Cell>())
        {
            if (ReferenceEquals(sibling, cell))
                return index;

            index++;
        }

        return index;
    }

    /// <summary>The bounding box of every cell present in the sheet, or null when the sheet is empty.</summary>
    public CellRange? UsedRange()
    {
        var minColumn = int.MaxValue;
        var minRow = int.MaxValue;
        var maxColumn = -1;
        var maxRow = -1;

        foreach (var row in this.sheetData.Elements<Row>())
        {
            var rowIndexZeroBased = (int)(row.RowIndex?.Value ?? 0) - 1;
            if (rowIndexZeroBased < 0)
                continue;

            var any = false;
            foreach (var cell in row.Elements<Cell>())
            {
                // A styled-but-valueless cell still counts: it is why a blank-looking sheet can have a
                // used range, and Excel's own dimension element includes them.
                var column = ColumnOf(cell);
                any = true;
                if (column < minColumn) minColumn = column;
                if (column > maxColumn) maxColumn = column;
            }

            if (!any)
                continue;

            if (rowIndexZeroBased < minRow) minRow = rowIndexZeroBased;
            if (rowIndexZeroBased > maxRow) maxRow = rowIndexZeroBased;
        }

        if (maxColumn < 0 || maxRow < 0)
            return null;

        return new CellRange(new CellRef(minColumn, minRow), new CellRef(maxColumn, maxRow));
    }
}
