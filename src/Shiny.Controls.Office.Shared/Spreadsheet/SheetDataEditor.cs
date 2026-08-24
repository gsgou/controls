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
