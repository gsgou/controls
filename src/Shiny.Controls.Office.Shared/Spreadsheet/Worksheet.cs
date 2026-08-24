using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SheetElement = DocumentFormat.OpenXml.Spreadsheet.Worksheet;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// One sheet of a <see cref="Workbook"/>, read and written directly against its OOXML element tree.
/// </summary>
public sealed class Worksheet
{
    readonly Workbook workbook;
    readonly WorksheetPart part;
    readonly SheetDataEditor editor;

    internal Worksheet(Workbook workbook, WorksheetPart part, string name, uint sheetId, bool visible)
    {
        this.workbook = workbook;
        this.part = part;
        this.Name = name;
        this.SheetId = sheetId;
        this.IsVisible = visible;

        var sheetElement = part.Worksheet ??= new SheetElement();
        var sheetData = sheetElement.GetFirstChild<SheetData>();
        if (sheetData is null)
        {
            sheetData = new SheetData();
            sheetElement.AppendChild(sheetData);
        }

        this.editor = new SheetDataEditor(sheetData);
    }

    public string Name { get; }
    public uint SheetId { get; }
    public bool IsVisible { get; }

    /// <summary>The bounding box of every populated cell, or null for an empty sheet.</summary>
    public CellRange? UsedRange => this.editor.UsedRange();

    public CellValue GetValue(CellRef reference)
    {
        var cell = this.editor.FindCell(reference);
        return cell is null ? CellValue.Blank : this.ReadCell(cell);
    }

    /// <summary>The formula text without its leading <c>=</c>, or null when the cell holds a literal.</summary>
    public string? GetFormula(CellRef reference)
        => this.editor.FindCell(reference)?.CellFormula?.Text;

    /// <summary>The style index (into cellXfs) applied to the cell, or null when it uses the default.</summary>
    public uint? GetStyleIndex(CellRef reference)
        => this.editor.FindCell(reference)?.StyleIndex?.Value;

    /// <summary>Reads the cached result of a formula cell without recomputing it.</summary>
    internal CellValue ReadCell(Cell cell)
    {
        var raw = cell.CellValue?.Text;
        var type = cell.DataType?.Value;

        if (type == CellValues.SharedString)
        {
            return int.TryParse(raw, out var index)
                ? CellValue.FromText(this.workbook.SharedStrings[index])
                : CellValue.Blank;
        }

        if (type == CellValues.InlineString)
        {
            var inline = cell.GetFirstChild<InlineString>();
            return CellValue.FromText(inline?.Text?.Text ?? inline?.InnerText ?? string.Empty);
        }

        if (type == CellValues.Boolean)
            return CellValue.FromBoolean(raw == "1");

        if (type == CellValues.Error)
            return CellValue.TryParseError(raw, out var error) ? CellValue.FromError(error) : CellValue.FromError(CellError.Value);

        // t="str" is a formula that produced text.
        if (type == CellValues.String)
            return CellValue.FromText(raw ?? string.Empty);

        if (string.IsNullOrEmpty(raw))
            return CellValue.Blank;

        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)
            ? CellValue.FromNumber(number)
            : CellValue.FromText(raw);
    }

    /// <summary>
    /// Writes a literal value, clearing any formula that was there.
    /// </summary>
    /// <remarks>
    /// Style is deliberately untouched: replacing the contents of a formatted cell must not strip its
    /// formatting, which is what a naive create-new-cell implementation does.
    /// </remarks>
    internal void WriteValue(CellRef reference, CellValue value)
    {
        if (value.IsBlank)
        {
            var existing = this.editor.FindCell(reference);
            if (existing is null)
                return;

            // Keep the element when it carries a style, or the formatting silently disappears.
            if (existing.StyleIndex is not null)
            {
                existing.CellFormula = null;
                existing.CellValue = null;
                existing.DataType = null;
                existing.RemoveAllChildren<InlineString>();
            }
            else
            {
                existing.Remove();
            }

            this.workbook.OnCellChanged(this, reference);
            return;
        }

        var cell = this.editor.GetOrCreateCell(reference);
        cell.CellFormula = null;
        cell.RemoveAllChildren<InlineString>();

        switch (value.Kind)
        {
            case CellValueKind.Number:
                cell.DataType = null; // n is the default and Excel omits it
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(
                    value.AsNumber().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                break;

            case CellValueKind.Text:
                cell.DataType = CellValues.SharedString;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(
                    this.workbook.SharedStrings.GetOrAdd(value.AsText()).ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;

            case CellValueKind.Boolean:
                cell.DataType = CellValues.Boolean;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value.AsBoolean() ? "1" : "0");
                break;

            case CellValueKind.Error:
                cell.DataType = CellValues.Error;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(CellValue.ErrorText(value.AsError()));
                break;
        }

        this.workbook.OnCellChanged(this, reference);
    }

    /// <summary>
    /// Writes a formula. The cached value is removed, so the workbook is marked for recalculation on
    /// load — until the calc engine lands, Excel itself computes the result when the file is opened.
    /// </summary>
    internal void WriteFormula(CellRef reference, string formula)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formula);
        var text = formula.StartsWith('=') ? formula[1..] : formula;

        var cell = this.editor.GetOrCreateCell(reference);
        cell.RemoveAllChildren<InlineString>();
        cell.DataType = null;
        cell.CellFormula = new CellFormula(text);
        cell.CellValue = null;

        this.workbook.OnCellChanged(this, reference);
    }

    internal void WriteStyleIndex(CellRef reference, uint? styleIndex)
    {
        var cell = this.editor.GetOrCreateCell(reference);
        cell.StyleIndex = styleIndex is null ? null : UInt32Value.FromUInt32(styleIndex.Value);
        this.workbook.OnContentChanged();
    }


    /// <summary>Every cell on the sheet that holds a formula, with the formula text.</summary>
    internal IEnumerable<(CellRef Cell, string Formula)> Formulas()
    {
        foreach (var row in this.part.Worksheet?.GetFirstChild<SheetData>()?.Elements<Row>() ?? Enumerable.Empty<Row>())
        {
            var rowIndex = (int)(row.RowIndex?.Value ?? 0) - 1;
            if (rowIndex < 0)
                continue;

            foreach (var cell in row.Elements<Cell>())
            {
                var formula = cell.CellFormula?.Text;
                if (!string.IsNullOrEmpty(formula))
                    yield return (new CellRef(SheetDataEditor.ColumnOf(cell), rowIndex), formula);
            }
        }
    }

    /// <summary>
    /// Updates a formula cell's cached result without disturbing the formula itself.
    /// </summary>
    /// <remarks>
    /// Every reader other than Excel — and Excel itself before it recalculates — shows this cached value,
    /// so leaving it stale after an edit means the file displays numbers that are simply wrong.
    /// </remarks>
    internal void WriteCachedValue(CellRef reference, CellValue value)
    {
        var cell = this.editor.FindCell(reference);
        if (cell?.CellFormula is null)
            return;

        cell.RemoveAllChildren<InlineString>();

        switch (value.Kind)
        {
            case CellValueKind.Number:
                cell.DataType = null;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(
                    value.AsNumber().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                break;

            case CellValueKind.Text:
                // t="str" is a formula that produced text, distinct from a shared string.
                cell.DataType = CellValues.String;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value.AsText());
                break;

            case CellValueKind.Boolean:
                cell.DataType = CellValues.Boolean;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value.AsBoolean() ? "1" : "0");
                break;

            case CellValueKind.Error:
                cell.DataType = CellValues.Error;
                cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(CellValue.ErrorText(value.AsError()));
                break;

            default:
                cell.DataType = null;
                cell.CellValue = null;
                break;
        }
    }

    /// <summary>The value to display: the freshly computed result for a formula, otherwise the literal.</summary>
    public CellValue GetDisplayValue(CellRef reference)
        => this.workbook.GetEffectiveValue(this.Name, reference);


    /// <summary>Column width definitions from <c>&lt;cols&gt;</c>, as inclusive zero-based spans.</summary>
    internal IEnumerable<(int First, int Last, double? Width, bool Hidden)> ColumnDefinitions()
    {
        var columns = this.part.Worksheet?.GetFirstChild<Columns>();
        if (columns is null)
            yield break;

        foreach (var column in columns.Elements<Column>())
        {
            var min = (int)(column.Min?.Value ?? 1) - 1;
            var max = (int)(column.Max?.Value ?? 1) - 1;
            if (min < 0)
                continue;

            // customWidth distinguishes "this is the width" from "this is the default, recorded anyway".
            var width = column.CustomWidth?.Value == true ? column.Width?.Value : null;
            yield return (min, max, width, column.Hidden?.Value ?? false);
        }
    }

    /// <summary>Row height definitions, for rows that override the sheet default.</summary>
    internal IEnumerable<(int Row, double? Height, bool Hidden)> RowDefinitions()
    {
        foreach (var row in this.part.Worksheet?.GetFirstChild<SheetData>()?.Elements<Row>() ?? Enumerable.Empty<Row>())
        {
            var index = (int)(row.RowIndex?.Value ?? 0) - 1;
            if (index < 0)
                continue;

            var height = row.CustomHeight?.Value == true ? row.Height?.Value : null;
            var hidden = row.Hidden?.Value ?? false;

            if (height is not null || hidden)
                yield return (index, height, hidden);
        }
    }

    /// <summary>Ranges merged on this sheet. Read-only for now; editing them arrives with structural edits.</summary>
    public IReadOnlyList<CellRange> MergedRanges
    {
        get
        {
            var merges = this.part.Worksheet?.GetFirstChild<MergeCells>();
            if (merges is null)
                return Array.Empty<CellRange>();

            var result = new List<CellRange>();
            foreach (var merge in merges.Elements<MergeCell>())
            {
                if (merge.Reference?.Value is { } text && CellRange.TryParse(text, out var range))
                    result.Add(range);
            }

            return result;
        }
    }

    /// <summary>The frozen pane split, expressed as the first non-frozen cell, or null when nothing is frozen.</summary>
    public CellRef? FrozenPane
    {
        get
        {
            var pane = this.part.Worksheet?
                .GetFirstChild<SheetViews>()?
                .Elements<SheetView>().FirstOrDefault()?
                .GetFirstChild<Pane>();

            if (pane?.State?.Value != PaneStateValues.Frozen)
                return null;

            var columns = (int)(pane.HorizontalSplit?.Value ?? 0);
            var rows = (int)(pane.VerticalSplit?.Value ?? 0);
            return columns == 0 && rows == 0 ? null : new CellRef(columns, rows);
        }
    }
}
