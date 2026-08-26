namespace Shiny.Blazor.Controls;

/// <summary>
/// One highlighting rule in the grid's <c>Highlights</c> collection: what to cover, and how to paint
/// it. The scope is <b>derived</b> from which targeting members are set rather than declared -
/// name a row and you have highlighted a row, name a column and you have highlighted a column, name
/// both and you have highlighted the one cell where they cross, name neither and the whole grid is
/// washed. The paint itself comes from <see cref="DataGridCellStyle"/>, which this derives from.
/// </summary>
/// <remarks>
/// <para>
/// Later entries in the collection win over earlier ones at the same scope, and a more specific scope
/// always wins over a less specific one - grid, then column, then row, then cell - member group by
/// member group (see <see cref="DataGridCellStyle.Merge"/>). A column's own <c>CellStyle</c> delegate
/// is the most specific thing there is and is applied last of all.
/// </para>
/// <para>
/// The stroke traces the <b>perimeter of the region</b>, not of each cell: a highlighted row draws a
/// line above and below every cell in it but only one leading and one trailing edge. Set
/// <see cref="DataGridCellStyle.BorderEdges"/> to override that.
/// </para>
/// </remarks>
public sealed class DataGridHighlight<TItem> : DataGridCellStyle
{
    /// <summary>
    /// The row to cover. Matched by <see cref="EqualityComparer{T}.Default"/>, so a record or a
    /// value type matches by value and a class matches by reference unless it says otherwise.
    /// Ignored when <see cref="RowPredicate"/> is set.
    /// </summary>
    public TItem? Item
    {
        get => this.item;
        set
        {
            this.item = value;
            // Tracked explicitly rather than compared against default: for a value-type TItem
            // `default` is a perfectly good row (0, DateTime.MinValue), so "was it set?" cannot be
            // answered by looking at the value.
            this.hasItem = true;
        }
    }

    TItem? item;
    bool hasItem;

    /// <summary>
    /// The rows to cover, tested per row. Wins over <see cref="Item"/> when both are set - use this
    /// for "every overdue invoice" and <see cref="Item"/> for "that one".
    /// </summary>
    public Func<TItem, bool>? RowPredicate { get; set; }

    /// <summary>
    /// The column to cover, by <c>Property</c> name or by <c>Title</c>, case-insensitively.
    /// <c>null</c> covers every column.
    /// </summary>
    public string? Column { get; set; }

    /// <summary>Set false to leave the rule in the collection but stop it painting.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>What this rule covers, derived from which targeting members are set.</summary>
    public DataGridHighlightScope Scope
    {
        get
        {
            var row = this.HasRowTarget;
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

    bool HasRowTarget => this.RowPredicate is not null || this.hasItem;

    /// <summary>True when this rule covers <paramref name="item"/>'s row.</summary>
    internal bool MatchesRow(TItem item)
    {
        if (this.RowPredicate is not null)
            return this.RowPredicate(item);

        return !this.hasItem || EqualityComparer<TItem>.Default.Equals(this.item, item);
    }

    /// <summary>True when this rule covers <paramref name="column"/>.</summary>
    internal bool MatchesColumn(ColumnBase<TItem> column)
    {
        if (string.IsNullOrWhiteSpace(this.Column))
            return true;

        var name = this.Column.Trim();
        return string.Equals(column.Id, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(column.Title, name, StringComparison.OrdinalIgnoreCase);
    }
}
