namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// What the evaluator needs from the world outside a single formula.
/// </summary>
/// <remarks>
/// Kept as an interface so the engine can be tested against an in-memory grid with no workbook, no
/// package and no OOXML — which is what makes a conformance suite of thousands of formulas practical.
/// </remarks>
public interface ICalcContext
{
    /// <summary>The sheet a formula belongs to, used to resolve unqualified references.</summary>
    string CurrentSheet { get; }

    /// <summary>The cell being evaluated, for functions like ROW() and COLUMN() that take no arguments.</summary>
    CellRef CurrentCell { get; }

    /// <summary>Reads a cell's current value. Blank for anything unset or off-sheet.</summary>
    CellValue GetValue(string? sheet, CellRef cell);

    /// <summary>True when the sheet exists; a reference to a missing sheet evaluates to #REF!.</summary>
    bool SheetExists(string sheet);

    /// <summary>Now, injected so that NOW() and TODAY() are deterministic under test.</summary>
    DateTime Now { get; }
}

/// <summary>A context bound to a different cell, used when a formula is evaluated on behalf of another cell.</summary>
public sealed class RebasedCalcContext(ICalcContext inner, string sheet, CellRef cell) : ICalcContext
{
    public string CurrentSheet => sheet;
    public CellRef CurrentCell => cell;
    public DateTime Now => inner.Now;
    public CellValue GetValue(string? s, CellRef c) => inner.GetValue(s ?? sheet, c);
    public bool SheetExists(string s) => inner.SheetExists(s);
}
