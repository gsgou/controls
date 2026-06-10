using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Shiny.Blazor.Controls;

public partial class TreeView<TItem>
{
    readonly List<BlazorTreeNode<TItem>> rootNodes = new();
    BlazorTreeNode<TItem>? focusedNode;
    BlazorTreeNode<TItem>? dragSource;
    IEnumerable<TItem>? lastItemsSource;
    bool isLoadingRoot;
    bool rootLoaderInvoked;
    Exception? rootError;

    // ------------- Data -------------
    [Parameter] public IEnumerable<TItem>? ItemsSource { get; set; }
    [Parameter] public Func<Task<IEnumerable<TItem>>>? RootLoader { get; set; }
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }
    [Parameter] public Func<TItem, Task<IEnumerable<TItem>>>? ChildrenLoader { get; set; }
    [Parameter] public Func<TItem, bool>? HasChildrenSelector { get; set; }
    [Parameter] public Func<TItem, bool>? CanExpandSelector { get; set; }
    [Parameter] public Func<TItem, bool>? CanSelectSelector { get; set; }

    // ------------- Templates -------------
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }
    [Parameter] public RenderFragment? ExpandedIcon { get; set; }
    [Parameter] public RenderFragment? CollapsedIcon { get; set; }
    [Parameter] public RenderFragment? RetryIcon { get; set; }
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    // ------------- Selection -------------
    [Parameter] public BlazorTreeSelectionMode SelectionMode { get; set; } = BlazorTreeSelectionMode.Single;
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }
    [Parameter] public IList<TItem>? SelectedItems { get; set; }
    [Parameter] public EventCallback<IList<TItem>> SelectedItemsChanged { get; set; }

    // ------------- Events -------------
    [Parameter] public EventCallback<TreeItemEventArgs<TItem>> ItemSelected { get; set; }
    [Parameter] public EventCallback<TreeItemEventArgs<TItem>> ItemExpanded { get; set; }
    [Parameter] public EventCallback<TreeItemEventArgs<TItem>> ItemCollapsed { get; set; }
    [Parameter] public EventCallback<TreeLoadFailedEventArgs<TItem>> LoadFailed { get; set; }
    [Parameter] public EventCallback<TreeItemDroppedEventArgs<TItem>> ItemDropped { get; set; }

    // ------------- Layout / visuals -------------
    [Parameter] public double IndentSize { get; set; } = 20;
    [Parameter] public double ChevronSize { get; set; } = 14;
    [Parameter] public string ChevronColor { get; set; } = "#666";
    [Parameter] public bool ShowGuideLines { get; set; } = false;
    [Parameter] public bool EnableDragDrop { get; set; } = false;
    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (RootLoader != null)
        {
            if (!rootLoaderInvoked)
                await EnsureRootLoadedAsync();
        }
        // only rebuild when the source actually changes — parameters re-set on
        // every parent render (lambdas are fresh instances), and rebuilding
        // recreates the nodes, wiping all expansion/selection state
        else if (!ReferenceEquals(ItemsSource, lastItemsSource))
        {
            lastItemsSource = ItemsSource;
            RebuildRootNodes();
        }
    }

    void RebuildRootNodes()
    {
        rootNodes.Clear();
        focusedNode = null;
        if (ItemsSource == null) return;
        foreach (var item in ItemsSource)
            rootNodes.Add(new BlazorTreeNode<TItem>(item, null, 0));
    }

    async Task EnsureRootLoadedAsync()
    {
        if (RootLoader == null || rootLoaderInvoked) return;
        rootLoaderInvoked = true;
        isLoadingRoot = true;
        rootError = null;
        StateHasChanged();
        try
        {
            var items = await RootLoader();
            rootNodes.Clear();
            foreach (var item in items)
                rootNodes.Add(new BlazorTreeNode<TItem>(item, null, 0));
        }
        catch (Exception ex)
        {
            rootError = ex;
        }
        finally
        {
            isLoadingRoot = false;
            StateHasChanged();
        }
    }

    public async Task ReloadAsync()
    {
        if (RootLoader != null)
        {
            rootLoaderInvoked = false;
            await EnsureRootLoadedAsync();
        }
        else
        {
            RebuildRootNodes();
            StateHasChanged();
        }
    }

    // ------------- Predicates -------------
    bool HasChildren(TItem item)
    {
        if (HasChildrenSelector != null) return HasChildrenSelector(item);
        if (ChildrenLoader != null) return true;
        var kids = ChildrenSelector?.Invoke(item);
        return kids != null && kids.Any();
    }

    bool CanExpand(TItem item) => CanExpandSelector?.Invoke(item) ?? true;

    bool CanSelect(TItem item)
    {
        if (SelectionMode == BlazorTreeSelectionMode.None) return false;
        return CanSelectSelector?.Invoke(item) ?? true;
    }

    // ------------- Expansion -------------
    async Task OnChevronClick(MouseEventArgs e, BlazorTreeNode<TItem> node, bool hasChildren, bool canExpand)
    {
        if (node.LoadState == BlazorTreeLoadState.Error)
        {
            await RetryAsync(node);
            return;
        }
        if (!hasChildren || !canExpand) return;
        await ToggleExpandAsync(node);
    }

    async Task ToggleExpandAsync(BlazorTreeNode<TItem> node)
    {
        if (node.IsExpanded)
        {
            node.IsExpanded = false;
            await ItemCollapsed.InvokeAsync(new TreeItemEventArgs<TItem>(node));
            StateHasChanged();
            return;
        }

        if (node.Children == null)
        {
            var syncKids = ChildrenSelector?.Invoke(node.Item);
            if (syncKids != null)
            {
                node.Children = new List<BlazorTreeNode<TItem>>();
                foreach (var item in syncKids)
                    node.Children.Add(new BlazorTreeNode<TItem>(item, node, node.Depth + 1));
                node.LoadState = BlazorTreeLoadState.Loaded;
            }
            else if (ChildrenLoader != null)
            {
                node.LoadState = BlazorTreeLoadState.Loading;
                StateHasChanged();
                try
                {
                    var kids = await ChildrenLoader(node.Item);
                    node.Children = new List<BlazorTreeNode<TItem>>();
                    foreach (var item in kids)
                        node.Children.Add(new BlazorTreeNode<TItem>(item, node, node.Depth + 1));
                    node.LoadState = BlazorTreeLoadState.Loaded;
                }
                catch (Exception ex)
                {
                    node.LoadError = ex;
                    node.LoadState = BlazorTreeLoadState.Error;
                    await LoadFailed.InvokeAsync(new TreeLoadFailedEventArgs<TItem>(node, ex));
                    StateHasChanged();
                    return;
                }
            }
            else
            {
                node.Children = new List<BlazorTreeNode<TItem>>();
                node.LoadState = BlazorTreeLoadState.Loaded;
            }
        }

        node.IsExpanded = true;
        await ItemExpanded.InvokeAsync(new TreeItemEventArgs<TItem>(node));
        StateHasChanged();
    }

    async Task RetryAsync(BlazorTreeNode<TItem> node)
    {
        node.LoadState = BlazorTreeLoadState.NotLoaded;
        node.Children = null;
        node.LoadError = null;
        await ToggleExpandAsync(node);
    }

    public async Task ExpandAsync(TItem item)
    {
        var n = FindNode(item);
        if (n != null && !n.IsExpanded) await ToggleExpandAsync(n);
    }

    public async Task CollapseAsync(TItem item)
    {
        var n = FindNode(item);
        if (n != null && n.IsExpanded) await ToggleExpandAsync(n);
    }

    public async Task ExpandAllAsync()
    {
        foreach (var n in rootNodes) await ExpandRecursive(n);
        StateHasChanged();
    }

    async Task ExpandRecursive(BlazorTreeNode<TItem> node)
    {
        if (!HasChildren(node.Item) || !CanExpand(node.Item)) return;
        if (!node.IsExpanded) await ToggleExpandAsync(node);
        if (node.Children != null)
            foreach (var c in node.Children)
                await ExpandRecursive(c);
    }

    public void CollapseAll()
    {
        foreach (var n in rootNodes) CollapseRecursive(n);
        StateHasChanged();
    }

    void CollapseRecursive(BlazorTreeNode<TItem> node)
    {
        node.IsExpanded = false;
        if (node.Children != null)
            foreach (var c in node.Children) CollapseRecursive(c);
    }

    public async Task RefreshAsync(TItem item)
    {
        var node = FindNode(item);
        if (node == null) return;
        var wasExpanded = node.IsExpanded;
        node.IsExpanded = false;
        node.Children = null;
        node.LoadState = BlazorTreeLoadState.NotLoaded;
        node.LoadError = null;
        if (wasExpanded) await ToggleExpandAsync(node);
        else StateHasChanged();
    }

    BlazorTreeNode<TItem>? FindNode(TItem item)
    {
        foreach (var n in rootNodes)
        {
            var f = FindRecursive(n, item);
            if (f != null) return f;
        }
        return null;
    }

    static BlazorTreeNode<TItem>? FindRecursive(BlazorTreeNode<TItem> node, TItem item)
    {
        if (EqualityComparer<TItem>.Default.Equals(node.Item, item)) return node;
        if (node.Children == null) return null;
        foreach (var c in node.Children)
        {
            var f = FindRecursive(c, item);
            if (f != null) return f;
        }
        return null;
    }

    // ------------- Selection -------------
    async Task OnRowClick(MouseEventArgs e, BlazorTreeNode<TItem> node, bool canSelect)
    {
        focusedNode = node;
        if (!canSelect) return;

        switch (SelectionMode)
        {
            case BlazorTreeSelectionMode.None:
                return;
            case BlazorTreeSelectionMode.Single:
                foreach (var n in EnumerateAll(rootNodes)) n.IsSelected = false;
                node.IsSelected = true;
                SelectedItem = node.Item;
                await SelectedItemChanged.InvokeAsync(node.Item);
                break;
            case BlazorTreeSelectionMode.Multiple:
                node.IsSelected = !node.IsSelected;
                SelectedItems ??= new List<TItem>();
                if (node.IsSelected && !SelectedItems.Contains(node.Item))
                    SelectedItems.Add(node.Item);
                else if (!node.IsSelected && SelectedItems.Contains(node.Item))
                    SelectedItems.Remove(node.Item);
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
                if (node.IsSelected)
                {
                    SelectedItem = node.Item;
                    await SelectedItemChanged.InvokeAsync(node.Item);
                }
                break;
        }
        await ItemSelected.InvokeAsync(new TreeItemEventArgs<TItem>(node));
    }

    IEnumerable<BlazorTreeNode<TItem>> EnumerateAll(IEnumerable<BlazorTreeNode<TItem>> nodes)
    {
        foreach (var n in nodes)
        {
            yield return n;
            if (n.Children != null)
                foreach (var c in EnumerateAll(n.Children))
                    yield return c;
        }
    }

    // ------------- Drag/drop -------------
    void OnDragStart(DragEventArgs e, BlazorTreeNode<TItem> node)
    {
        if (!EnableDragDrop) return;
        dragSource = node;
    }

    void OnDragOver(DragEventArgs e, BlazorTreeNode<TItem> node)
    {
        if (!EnableDragDrop) return;
        // visual handled via CSS hover on .shiny-treeview-row
    }

    async Task OnDrop(DragEventArgs e, BlazorTreeNode<TItem> target)
    {
        if (!EnableDragDrop || dragSource == null || ReferenceEquals(dragSource, target))
        {
            dragSource = null;
            return;
        }
        // Disallow dropping onto descendants
        var probe = target;
        while (probe != null)
        {
            if (ReferenceEquals(probe, dragSource)) { dragSource = null; return; }
            probe = probe.Parent;
        }
        await ItemDropped.InvokeAsync(new TreeItemDroppedEventArgs<TItem>(dragSource, target, BlazorTreeDropPosition.Below));
        dragSource = null;
    }

    // ------------- Keyboard nav -------------
    async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (focusedNode == null)
        {
            focusedNode = rootNodes.FirstOrDefault();
            if (focusedNode == null) return;
            StateHasChanged();
            return;
        }

        var flat = EnumerateAll(rootNodes).Where(n => IsVisible(n)).ToList();
        var idx = flat.IndexOf(focusedNode);

        switch (e.Key)
        {
            case "ArrowDown":
                if (idx < flat.Count - 1) { focusedNode = flat[idx + 1]; StateHasChanged(); }
                break;
            case "ArrowUp":
                if (idx > 0) { focusedNode = flat[idx - 1]; StateHasChanged(); }
                break;
            case "ArrowRight":
                if (!focusedNode.IsExpanded && HasChildren(focusedNode.Item) && CanExpand(focusedNode.Item))
                    await ToggleExpandAsync(focusedNode);
                else if (focusedNode.IsExpanded && focusedNode.Children?.Count > 0)
                { focusedNode = focusedNode.Children[0]; StateHasChanged(); }
                break;
            case "ArrowLeft":
                if (focusedNode.IsExpanded)
                    await ToggleExpandAsync(focusedNode);
                else if (focusedNode.Parent != null)
                { focusedNode = focusedNode.Parent; StateHasChanged(); }
                break;
            case "Enter":
            case " ":
                await OnRowClick(new MouseEventArgs(), focusedNode, CanSelect(focusedNode.Item));
                break;
            case "Home":
                if (flat.Count > 0) { focusedNode = flat[0]; StateHasChanged(); }
                break;
            case "End":
                if (flat.Count > 0) { focusedNode = flat[^1]; StateHasChanged(); }
                break;
        }
    }

    static bool IsVisible(BlazorTreeNode<TItem> n)
    {
        var p = n.Parent;
        while (p != null)
        {
            if (!p.IsExpanded) return false;
            p = p.Parent;
        }
        return true;
    }
}
