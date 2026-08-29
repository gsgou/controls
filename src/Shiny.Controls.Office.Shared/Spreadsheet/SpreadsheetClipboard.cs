namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>What was put on the clipboard, which decides how a paste lands.</summary>
public enum SpreadsheetClipboardKind
{
    /// <summary>A rectangle of cells, pasted at the selection's top-left corner.</summary>
    Cells,

    /// <summary>Whole rows, pasted over whole rows — heights and row formatting travel with them.</summary>
    Rows,

    /// <summary>Whole columns, pasted over whole columns — widths and column formatting travel with them.</summary>
    Columns
}

public enum SpreadsheetClipboardOperation
{
    Copy,
    Cut
}

/// <summary>One captured cell, positioned relative to the top-left corner of what was copied.</summary>
/// <remarks>
/// Relative rather than absolute so the same capture can be pasted anywhere, any number of times,
/// without the content having to be re-read from a source that may since have been cut away.
/// </remarks>
public sealed record SpreadsheetClipboardCell(int Column, int Row, string? Formula, CellValue Value, uint? StyleIndex);

/// <summary>One captured row or column: what the whole band said about itself, rather than about a cell.</summary>
/// <param name="Offset">Position within the copied band, counted from its first row or column.</param>
/// <param name="StyleIndex">The style the whole band carries, which every cell in it inherits.</param>
/// <param name="Size">Row height in points or column width in characters — the units the file uses.</param>
public sealed record SpreadsheetClipboardBand(int Offset, uint? StyleIndex, double? Size);

/// <summary>
/// A snapshot of what was cut or copied, held by the controller until it is pasted or abandoned.
/// </summary>
/// <remarks>
/// <para>
/// A snapshot rather than a live reference to the source range, because a cut has to survive its own
/// source being cleared — and because pasting the same block twice must produce the same thing both
/// times even if the sheet changed in between.
/// </para>
/// <para>
/// This is the control's own clipboard, not the operating system's. Nothing here reaches another
/// application, and nothing another application copied reaches here.
/// </para>
/// </remarks>
public sealed record SpreadsheetClipboardContent
{
    /// <summary>
    /// The point past which a whole-row or whole-column capture stops taking the selection literally.
    /// </summary>
    /// <remarks>
    /// Selecting a row selects 16,384 cells and selecting a column selects 1,048,576, virtually all of
    /// them blank. The capture is intersected with the used range first, so what gets held is the data
    /// the row actually has rather than a million empty entries.
    /// </remarks>
    const long MaxCapturedCells = 250_000;

    public required string SheetName { get; init; }

    /// <summary>The range the user selected, which is what the marching-ants border is drawn around.</summary>
    public required CellRange Source { get; init; }

    /// <summary>The rectangle actually captured — the selection narrowed to the cells that hold something.</summary>
    public required CellRange Captured { get; init; }

    public required SpreadsheetClipboardKind Kind { get; init; }

    public required SpreadsheetClipboardOperation Operation { get; init; }

    public required IReadOnlyList<SpreadsheetClipboardCell> Cells { get; init; }

    /// <summary>Row or column properties, for a <see cref="SpreadsheetClipboardKind.Rows"/> or <see cref="SpreadsheetClipboardKind.Columns"/> capture.</summary>
    public required IReadOnlyList<SpreadsheetClipboardBand> Bands { get; init; }

    /// <summary>How many rows the paste occupies.</summary>
    public int RowCount => this.Kind == SpreadsheetClipboardKind.Rows ? this.Source.RowCount : this.Captured.RowCount;

    /// <summary>How many columns the paste occupies.</summary>
    public int ColumnCount => this.Kind == SpreadsheetClipboardKind.Columns ? this.Source.ColumnCount : this.Captured.ColumnCount;

    /// <summary>
    /// Reads a selection off a sheet.
    /// </summary>
    /// <remarks>
    /// The kind is inferred from the shape of the selection rather than asked for, because that is how
    /// the user expressed it: a click on a row header selects every column of that row and means "this
    /// row", and there is no other way to say it. Select All spans both axes at once and means neither,
    /// so it is captured as plain cells over whatever the sheet is using.
    /// </remarks>
    public static SpreadsheetClipboardContent Capture(Worksheet sheet, CellRange range, SpreadsheetClipboardOperation operation)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var wholeRows = range.Left == 0 && range.Right >= CellRef.MaxColumn;
        var wholeColumns = range.Top == 0 && range.Bottom >= CellRef.MaxRow;

        var kind = (wholeRows, wholeColumns) switch
        {
            (true, false) => SpreadsheetClipboardKind.Rows,
            (false, true) => SpreadsheetClipboardKind.Columns,
            _ => SpreadsheetClipboardKind.Cells
        };

        var captured = Narrow(sheet, range, kind);
        var cells = new List<SpreadsheetClipboardCell>();

        if (captured is { } rect && rect.CellCount <= MaxCapturedCells)
        {
            foreach (var cell in rect.Cells())
            {
                var formula = sheet.GetFormula(cell);
                var value = sheet.GetValue(cell);

                // A band capture leaves inherited formatting to the band, so only the cell's own style
                // is read; a plain rectangle has no band to fall back on and takes the resolved one,
                // which is what makes copying out of a formatted column keep looking formatted.
                var style = kind == SpreadsheetClipboardKind.Cells
                    ? sheet.GetEffectiveStyleIndex(cell)
                    : sheet.GetStyleIndex(cell);

                if (formula is null && value.IsBlank && style is null)
                    continue;

                cells.Add(new SpreadsheetClipboardCell(
                    cell.Column - rect.Left,
                    cell.Row - rect.Top,
                    formula,
                    value,
                    style));
            }
        }

        return new SpreadsheetClipboardContent
        {
            SheetName = sheet.Name,
            Source = range,
            Captured = captured ?? new CellRange(range.TopLeft),
            Kind = kind,
            Operation = operation,
            Cells = cells,
            Bands = ReadBands(sheet, range, kind)
        };
    }

    /// <summary>The rectangle worth reading: the selection, clipped to the cells the sheet is using.</summary>
    static CellRange? Narrow(Worksheet sheet, CellRange range, SpreadsheetClipboardKind kind)
    {
        if (kind == SpreadsheetClipboardKind.Cells && range.CellCount <= MaxCapturedCells)
            return range;

        if (sheet.UsedRange is not { } used || !used.Intersects(range))
            return null;

        return new CellRange(
            new CellRef(Math.Max(range.Left, used.Left), Math.Max(range.Top, used.Top)),
            new CellRef(Math.Min(range.Right, used.Right), Math.Min(range.Bottom, used.Bottom)));
    }

    static IReadOnlyList<SpreadsheetClipboardBand> ReadBands(Worksheet sheet, CellRange range, SpreadsheetClipboardKind kind)
    {
        var bands = new List<SpreadsheetClipboardBand>();

        switch (kind)
        {
            case SpreadsheetClipboardKind.Rows:
                for (var row = range.Top; row <= range.Bottom; row++)
                    bands.Add(new SpreadsheetClipboardBand(row - range.Top, sheet.GetRowStyleIndex(row), sheet.GetRowHeight(row)));

                break;

            case SpreadsheetClipboardKind.Columns:
                for (var column = range.Left; column <= range.Right; column++)
                    bands.Add(new SpreadsheetClipboardBand(column - range.Left, sheet.GetColumnStyleIndex(column), sheet.GetColumnWidth(column)));

                break;
        }

        return bands;
    }
}
