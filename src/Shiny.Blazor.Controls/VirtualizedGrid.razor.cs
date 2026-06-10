using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

public partial class VirtualizedGrid<TItem>
{
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment? FooterTemplate { get; set; }
    [Parameter] public RenderFragment? EmptyViewTemplate { get; set; }
    [Parameter] public int ColumnCount { get; set; } = 1;
    [Parameter] public double ItemSpacing { get; set; } = 8;
    [Parameter] public double CellPaddingLeft { get; set; }
    [Parameter] public double CellPaddingRight { get; set; }
    [Parameter] public double CellPaddingTop { get; set; }
    [Parameter] public double CellPaddingBottom { get; set; }

    // Grouping
    [Parameter] public bool IsGroupingEnabled { get; set; }
    [Parameter] public IReadOnlyList<VirtualizedGridGroup<TItem>>? GroupedItems { get; set; }
    [Parameter] public RenderFragment<object>? GroupHeaderTemplate { get; set; }
    [Parameter] public bool HasStickyHeaders { get; set; } = true;

    // Virtualization
    [Parameter] public bool EnableVirtualization { get; set; }
    [Parameter] public int LoadMoreThreshold { get; set; } = 5;

    // Load more
    [Parameter] public bool ShowLoadMoreButton { get; set; }
    [Parameter] public bool IsLoadingMore { get; set; }
    [Parameter] public EventCallback IsLoadingMoreChanged { get; set; }
    [Parameter] public RenderFragment? LoadMoreButtonTemplate { get; set; }
    [Parameter] public EventCallback LoadMoreRequested { get; set; }

    // Selection
    [Parameter] public EventCallback<TItem> ItemSelected { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    // Virtualize requires a stable collection and items that stack vertically, so we
    // virtualize rows (chunks of ColumnCount) rather than individual grid cells
    List<TItem[]> itemRows = new();
    IReadOnlyList<TItem>? lastRowSource;
    int lastRowColumns;
    int lastRowCount;

    protected override void OnParametersSet()
    {
        if (!EnableVirtualization)
            return;

        var columns = Math.Max(1, ColumnCount);
        var count = Items?.Count ?? 0;

        if (ReferenceEquals(lastRowSource, Items) && lastRowColumns == columns && lastRowCount == count)
            return;

        lastRowSource = Items;
        lastRowColumns = columns;
        lastRowCount = count;
        itemRows = Items is null ? new() : Items.Chunk(columns).ToList();
    }

    string ContainerStyle => $"--vgrid-spacing:{ItemSpacing}px;--vgrid-columns:{ColumnCount};";

    string GridStyle =>
        $"display:grid;grid-template-columns:repeat({ColumnCount},1fr);gap:{ItemSpacing}px;";

    string CellStyle
    {
        get
        {
            if (CellPaddingLeft == 0 && CellPaddingRight == 0 &&
                CellPaddingTop == 0 && CellPaddingBottom == 0)
                return "";
            return $"padding:{CellPaddingTop}px {CellPaddingRight}px {CellPaddingBottom}px {CellPaddingLeft}px;";
        }
    }

    async Task OnItemClicked(TItem item)
    {
        if (ItemSelected.HasDelegate)
            await ItemSelected.InvokeAsync(item);
    }

    async Task OnLoadMoreClicked()
    {
        IsLoadingMore = true;
        if (IsLoadingMoreChanged.HasDelegate)
            await IsLoadingMoreChanged.InvokeAsync();

        if (LoadMoreRequested.HasDelegate)
            await LoadMoreRequested.InvokeAsync();

        IsLoadingMore = false;
        if (IsLoadingMoreChanged.HasDelegate)
            await IsLoadingMoreChanged.InvokeAsync();
    }
}

public class VirtualizedGridGroup<TItem>
{
    public VirtualizedGridGroup(object key, IReadOnlyList<TItem> items)
    {
        Key = key;
        Items = items;
    }

    public object Key { get; }
    public IReadOnlyList<TItem> Items { get; }
}
