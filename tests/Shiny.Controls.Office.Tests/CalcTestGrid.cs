using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Calc;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// An in-memory grid implementing <see cref="ICalcContext"/>.
/// </summary>
/// <remarks>
/// Lets the calculation layer be tested without a package, a workbook or any OOXML — which is what
/// makes it practical to run thousands of formula assertions in milliseconds.
/// </remarks>
public sealed class CalcTestGrid : ICalcContext
{
    readonly Dictionary<CellAddress, CellValue> values = new();
    readonly HashSet<string> sheets = new(StringComparer.OrdinalIgnoreCase) { "Sheet1" };

    public CalcEngine Engine { get; } = new();

    public string CurrentSheet { get; set; } = "Sheet1";
    public CellRef CurrentCell { get; set; }
    public DateTime Now { get; set; } = new(2026, 8, 24, 13, 30, 0);

    public bool SheetExists(string sheet) => this.sheets.Contains(sheet);

    public void AddSheet(string name) => this.sheets.Add(name);

    public CellValue GetValue(string? sheet, CellRef cell)
    {
        var address = new CellAddress(sheet ?? this.CurrentSheet, cell.Relative());
        if (this.Engine.TryGetComputed(address, out var computed))
            return computed;

        return this.values.GetValueOrDefault(address, CellValue.Blank);
    }

    public CalcTestGrid Set(string reference, double value) => this.Set(reference, CellValue.FromNumber(value));
    public CalcTestGrid Set(string reference, string value) => this.Set(reference, CellValue.FromText(value));
    public CalcTestGrid Set(string reference, bool value) => this.Set(reference, CellValue.FromBoolean(value));
    public CalcTestGrid SetError(string reference, CellError error) => this.Set(reference, CellValue.FromError(error));

    public CalcTestGrid Set(string reference, CellValue value)
    {
        this.values[this.Address(reference)] = value;
        return this;
    }

    /// <summary>Registers a formula in a cell and recalculates everything it affects.</summary>
    public CalcTestGrid SetFormula(string reference, string formula)
    {
        var address = this.Address(reference);
        this.Engine.SetFormula(address, formula);
        this.Engine.RecalculateAll(this);
        return this;
    }

    public CellValue Get(string reference)
    {
        var address = this.Address(reference);
        return this.Engine.TryGetComputed(address, out var computed)
            ? computed
            : this.values.GetValueOrDefault(address, CellValue.Blank);
    }

    /// <summary>Evaluates an expression as though it were entered in <paramref name="origin"/>.</summary>
    public CellValue Eval(string formula, string origin = "A1")
    {
        var address = this.Address(origin);
        var context = new RebasedCalcContext(this, address.Sheet, address.Cell);
        return this.Engine.EvaluateOnce(formula, context);
    }

    public double Number(string formula, string origin = "A1") => this.Eval(formula, origin).AsNumber();
    public string Text(string formula, string origin = "A1") => this.Eval(formula, origin).AsText();
    public bool Bool(string formula, string origin = "A1") => this.Eval(formula, origin).AsBoolean();
    public CellError Error(string formula, string origin = "A1") => this.Eval(formula, origin).AsError();

    CellAddress Address(string reference)
    {
        var bang = reference.LastIndexOf('!');
        var sheet = bang < 0 ? this.CurrentSheet : reference[..bang];
        var cell = bang < 0 ? reference : reference[(bang + 1)..];
        this.sheets.Add(sheet);
        return new CellAddress(sheet, CellRef.Parse(cell).Relative());
    }
}
