namespace Shiny.Blazor.Controls;

/// <summary>
/// The context passed to a column's <c>CellTemplate</c>/<c>EditTemplate</c>. Mirrors MudBlazor's
/// <c>CellContext</c> — use <c>context.Item</c> to reach the row's data.
/// </summary>
public sealed class CellContext<TItem>
{
    public CellContext(TItem item, bool selected, CellActions actions)
    {
        this.Item = item;
        this.Selected = selected;
        this.Actions = actions;
    }

    public TItem Item { get; }
    public bool Selected { get; }
    public CellActions Actions { get; }

    public sealed class CellActions
    {
        public Action<bool>? SetSelectedItem { get; init; }
        public Action? StartEditingItem { get; init; }
        public Action? CancelEditingItem { get; init; }
    }
}
