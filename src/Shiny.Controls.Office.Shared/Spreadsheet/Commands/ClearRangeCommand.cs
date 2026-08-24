using Shiny.Controls.Office.Editing;

namespace Shiny.Controls.Office.Spreadsheet.Commands;

/// <summary>
/// Clears the contents of a range, leaving formatting in place — the behaviour of pressing Delete in
/// Excel, as opposed to Clear All.
/// </summary>
public sealed class ClearRangeCommand(string sheetName, CellRange range) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public CellRange Range { get; } = range;

    public string Name => "Clear";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        var restore = new List<IEditCommand<Workbook>>();

        foreach (var cell in this.Range.Cells())
        {
            var formula = sheet.GetFormula(cell);
            var value = sheet.GetValue(cell);

            // Only record cells that actually held something, so undoing a clear over a mostly-empty
            // selection does not replay a million no-ops.
            if (formula is null && value.IsBlank)
                continue;

            restore.Add(formula is null
                ? new SetCellValueCommand(this.SheetName, cell, value)
                : new SetCellFormulaCommand(this.SheetName, cell, formula));

            sheet.WriteValue(cell, CellValue.Blank);
        }

        return new CompositeCommand<Workbook>(this.Name, restore);
    }
}
