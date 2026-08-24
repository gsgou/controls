using Shiny.Controls.Office.Editing;

namespace Shiny.Controls.Office.Spreadsheet.Commands;

/// <summary>
/// Writes a literal value into one cell.
/// </summary>
/// <remarks>
/// The command records the sheet by name rather than by reference so that it stays serialisable — a
/// prerequisite for replaying history and for any future collaborative editing.
/// </remarks>
public sealed class SetCellValueCommand(string sheetName, CellRef cell, CellValue value)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public CellRef Cell { get; } = cell.Relative();
    public CellValue Value { get; } = value;

    public string Name => "Edit Cell";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];

        // Capture the whole prior state: a cell holding a formula must come back as that formula, not
        // as the value the formula happened to produce.
        var previousFormula = sheet.GetFormula(this.Cell);
        var previousValue = sheet.GetValue(this.Cell);

        sheet.WriteValue(this.Cell, this.Value);

        return previousFormula is null
            ? new SetCellValueCommand(this.SheetName, this.Cell, previousValue)
            : new SetCellFormulaCommand(this.SheetName, this.Cell, previousFormula);
    }
}

/// <summary>Writes a formula into one cell.</summary>
public sealed class SetCellFormulaCommand(string sheetName, CellRef cell, string formula)
    : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public CellRef Cell { get; } = cell.Relative();
    public string Formula { get; } = formula;

    public string Name => "Edit Formula";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var sheet = context[this.SheetName];
        var previousFormula = sheet.GetFormula(this.Cell);
        var previousValue = sheet.GetValue(this.Cell);

        sheet.WriteFormula(this.Cell, this.Formula);

        return previousFormula is null
            ? new SetCellValueCommand(this.SheetName, this.Cell, previousValue)
            : new SetCellFormulaCommand(this.SheetName, this.Cell, previousFormula);
    }
}
