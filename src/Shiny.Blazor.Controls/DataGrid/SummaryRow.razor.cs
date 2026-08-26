using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// One summary (total) row. Holds <see cref="SummaryCell{TItem}"/> children, each pointing at a column -
/// a cell either aggregates that column's values or simply fills its slot with a label. Stack several
/// rows for a subtotal / tax / total block.
/// </summary>
/// <remarks>
/// The same declarations render in the grid's <c>tfoot</c> <b>and</b> inside every group (see
/// <see cref="DataGrid{TItem}.GroupSummaryPlacement"/>); <see cref="Scope"/> narrows a row to one of the two.
/// </remarks>
public partial class SummaryRow<TItem> : IDisposable
{
    readonly List<SummaryCell<TItem>> cells = new();

    [CascadingParameter] internal DataGrid<TItem> Grid { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether this row is shown under the grid, inside each group, or both (the default).</summary>
    [Parameter] public DataGridSummaryScope Scope { get; set; }

    /// <summary>Drop the row without removing its declaration.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Extra CSS classes for the row's <c>tr</c>.</summary>
    [Parameter] public string? Class { get; set; }

    internal IReadOnlyList<SummaryCell<TItem>> Cells => this.cells;

    internal bool AppliesTo(bool group)
        => !this.Hidden && this.Scope switch
        {
            DataGridSummaryScope.Grid => !group,
            DataGridSummaryScope.Group => group,
            _ => true
        };

    internal SummaryCell<TItem>? CellFor(ColumnBase<TItem> column)
        => this.cells.FirstOrDefault(c => c.Matches(column));

    internal void AddCell(SummaryCell<TItem> cell)
    {
        if (!this.cells.Contains(cell))
        {
            this.cells.Add(cell);
            this.Grid?.NotifySummaryChanged();
        }
    }

    internal void RemoveCell(SummaryCell<TItem> cell)
    {
        if (this.cells.Remove(cell))
            this.Grid?.NotifySummaryChanged();
    }

    protected override void OnInitialized() => this.Grid?.AddSummaryRow(this);

    public void Dispose() => this.Grid?.RemoveSummaryRow(this);
}
