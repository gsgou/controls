namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// One highlighting rule in <see cref="DataGrid.Highlights"/>: what to cover, and how to paint it.
/// The scope is <b>derived</b> from which targeting members are set rather than declared - name a row
/// and you have highlighted a row, name a column and you have highlighted a column, name both and you
/// have highlighted the one cell where they cross, name neither and the whole grid is washed. The
/// paint itself comes from <see cref="DataGridCellStyle"/>, which this derives from.
/// </summary>
/// <remarks>
/// <para>
/// Later entries in the collection win over earlier ones at the same scope, and a more specific scope
/// always wins over a less specific one - grid, then column, then row, then cell - member group by
/// member group. A column's own <see cref="DataGridColumn.CellStyle"/> delegate is the most specific
/// thing there is and is applied last of all.
/// </para>
/// <para>
/// The stroke traces the <b>perimeter of the region</b>, not of each cell: a highlighted row draws a
/// line above and below every cell in it but only one leading and one trailing edge. Set
/// <see cref="DataGridCellStyle.BorderEdges"/> to override that.
/// </para>
/// <example>
/// <code language="xml">
/// &lt;shiny:DataGrid.Highlights&gt;
///     &lt;shiny:DataGridHighlight Column="Salary" Fill="Gold" /&gt;
///     &lt;shiny:DataGridHighlight Item="{Binding SelectedOrder}"
///                              BorderColor="Red" BorderStyle="Dashed" /&gt;
/// &lt;/shiny:DataGrid.Highlights&gt;
/// </code>
/// </example>
/// </remarks>
public class DataGridHighlight : DataGridCellStyle
{
    object? item;
    bool hasItem;

    /// <summary>
    /// The row to cover, matched against the grid's items with <see cref="object.Equals(object?)"/>.
    /// Ignored when <see cref="RowPredicate"/> is set.
    /// </summary>
    public object? Item
    {
        get => this.item;
        set
        {
            this.item = value;
            // Tracked explicitly rather than compared against null: binding a null selection is a
            // real state, and it should stop the rule matching rather than turn it into a grid-wide one.
            this.hasItem = true;
        }
    }

    /// <summary>
    /// The rows to cover, tested per row. Wins over <see cref="Item"/> when both are set - use this
    /// for "every overdue invoice" and <see cref="Item"/> for "that one".
    /// </summary>
    public Func<object, bool>? RowPredicate { get; set; }

    /// <summary>
    /// The column to cover, by <see cref="DataGridColumn.PropertyName"/> or by
    /// <see cref="DataGridColumn.Title"/>, case-insensitively. <c>null</c> covers every column.
    /// </summary>
    public string? Column { get; set; }

    /// <summary>Set false to leave the rule in the collection but stop it painting.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>What this rule covers, derived from which targeting members are set.</summary>
    public DataGridHighlightScope Scope
    {
        get
        {
            var row = this.RowPredicate is not null || this.hasItem;
            var column = !string.IsNullOrWhiteSpace(this.Column);
            return (row, column) switch
            {
                (true, true) => DataGridHighlightScope.Cell,
                (true, false) => DataGridHighlightScope.Row,
                (false, true) => DataGridHighlightScope.Column,
                _ => DataGridHighlightScope.Grid
            };
        }
    }

    /// <summary>True when this rule covers <paramref name="data"/>'s row.</summary>
    internal bool MatchesRow(object data)
    {
        if (this.RowPredicate is not null)
            return this.RowPredicate(data);

        return !this.hasItem || Equals(this.item, data);
    }

    /// <summary>True when this rule covers <paramref name="column"/>.</summary>
    internal bool MatchesColumn(DataGridColumn column)
    {
        if (string.IsNullOrWhiteSpace(this.Column))
            return true;

        var name = this.Column.Trim();
        return string.Equals(column.PropertyName, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(column.Title, name, StringComparison.OrdinalIgnoreCase);
    }
}
