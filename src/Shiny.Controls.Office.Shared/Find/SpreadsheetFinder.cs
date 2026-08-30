using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Spreadsheet.View;

/// <summary>One hit in a workbook: the sheet, the cell, and where in that cell's text.</summary>
/// <param name="Sheet">The sheet's name rather than the object, so a match survives a sheet being re-read.</param>
public readonly record struct SpreadsheetFindMatch(string Sheet, CellRef Cell, int Start, int Length)
{
    public int End => this.Start + this.Length;
}


/// <summary>
/// Finds text in a workbook and moves the selection onto each hit.
/// </summary>
/// <remarks>
/// <para>
/// What is searched is the cell's text as the formula bar shows it: the formula when the cell has one,
/// otherwise the literal. That is Excel's own default — "look in: formulas" — and it is the only
/// choice under which searching for <c>SUM</c> finds the cells that total something. A cell's
/// <em>formatted</em> value is deliberately not searched, or a search for <c>1234</c> would miss a
/// cell showing <c>1,234.00</c> and a search for <c>1,234</c> would find one that holds no comma.
/// </para>
/// <para>
/// The active sheet only, unless <see cref="SearchAllSheets"/> is set — again Excel's default. A
/// workbook-wide search moves the user between sheets on every press of "next", which is rarely what
/// they meant when they typed into a box on the sheet they were looking at.
/// </para>
/// </remarks>
public sealed class SpreadsheetFinder(SpreadsheetController controller) : FindController<SpreadsheetFindMatch>
{
    bool allSheets;

    /// <summary>
    /// Search every visible sheet rather than only the active one. Off by default.
    /// </summary>
    /// <remarks>
    /// Hidden sheets stay out either way: they are not on screen, and stepping onto one would show the
    /// user a sheet the workbook has deliberately put away.
    /// </remarks>
    public bool SearchAllSheets
    {
        get => this.allSheets;
        set
        {
            if (this.allSheets == value)
                return;

            this.allSheets = value;
            this.Invalidate();
        }
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<SpreadsheetFindMatch> Collect(string query, FindOptions options)
    {
        var results = new List<SpreadsheetFindMatch>();

        // Book order, always - never "the active sheet first". Ordering the list around the sheet the
        // user happens to be on re-orders it every time "next" crosses a sheet boundary, and stepping
        // then resumes from the moved match's new index: A2 to B1 to B2 and back to A1, forever, with
        // sheet C never reached.
        foreach (var sheet in this.SheetsToSearch())
        {
            foreach (var cell in sheet.PopulatedCells())
            {
                var text = CellText(sheet, cell);
                if (text.Length == 0)
                    continue;

                foreach (var match in TextSearch.Matches(text, query, options))
                    results.Add(new SpreadsheetFindMatch(sheet.Name, cell, match.Start, match.Length));
            }
        }

        return results;
    }

    /// <inheritdoc/>
    protected override void MoveTo(SpreadsheetFindMatch match) => controller.SelectFindMatch(match);

    /// <inheritdoc/>
    protected override int IndexAtOrAfterCaret(IReadOnlyList<SpreadsheetFindMatch> matches)
    {
        var active = controller.Selection.Active;
        var order = this.SheetOrder(controller.Sheet.Name);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var sheet = this.SheetOrder(match.Sheet);

            if (sheet != order)
            {
                if (sheet > order)
                    return i;

                continue;
            }

            if (match.Cell.Row > active.Row || (match.Cell.Row == active.Row && match.Cell.Column >= active.Column))
                return i;
        }

        return matches.Count;
    }

    /// <summary>A sheet's position in the tab strip, which is the order the matches are collected in.</summary>
    int SheetOrder(string name)
    {
        var sheets = controller.VisibleSheets;

        for (var i = 0; i < sheets.Count; i++)
        {
            if (string.Equals(sheets[i].Name, name, StringComparison.Ordinal))
                return i;
        }

        return int.MaxValue;
    }

    IEnumerable<Worksheet> SheetsToSearch()
        => this.allSheets ? controller.VisibleSheets : [controller.Sheet];

    /// <summary>What the formula bar would show for a cell: its formula, else its literal.</summary>
    static string CellText(Worksheet sheet, CellRef cell)
    {
        if (sheet.GetFormula(cell) is { } formula)
            return "=" + formula;

        var value = sheet.GetValue(cell);
        return value.IsBlank ? string.Empty : Calc.Coercion.ToText(value);
    }
}
