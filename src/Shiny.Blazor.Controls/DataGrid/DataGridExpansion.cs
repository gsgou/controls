using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Row expansion - the detail ("breakdown") row and the hierarchical tree mode.
/// </summary>
/// <remarks>
/// Both features are the same mechanism seen from two sides: one set of expanded data items, and a
/// flatten step that decides what an expanded row reveals. A detail row renders
/// <see cref="RowDetailTemplate"/> in a full-width <c>&lt;tr&gt;</c> under its parent; a tree row emits
/// its children as ordinary rows one level deeper. A grid can do both at once. Expansion is keyed on
/// the *data item*, so it survives the re-render that sorting, filtering or paging causes.
/// </remarks>
public partial class DataGrid<TItem>
{
    /// <summary>Matches the width the expander column is given in CSS - the first-paint sticky offset.</summary>
    const double ExpanderWidthPx = 44;

    readonly HashSet<TItem> expandedItems = new();
    readonly Dictionary<TItem, IReadOnlyList<TItem>> loadedChildren = new();
    readonly HashSet<TItem> loadingChildren = new();
    readonly HashSet<TItem> loadedDetails = new();
    readonly HashSet<TItem> loadingDetails = new();
    bool isBusy;

    /// <summary>
    /// Content shown in a full-width row underneath an expanded row - the "breakdown" view. The
    /// context is the row's data item, so it can host any components you like. Setting it adds an
    /// expander column at the leading edge.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? RowDetailTemplate { get; set; }

    /// <summary>
    /// Fetches whatever the detail row needs, the first time that row is expanded. The caret turns
    /// into a spinner while it runs and the detail row shows <see cref="RowDetailLoadingTemplate"/>;
    /// <see cref="RowDetailTemplate"/> is not rendered until it completes, so the template can assume
    /// its data has arrived. Each item loads once - call <see cref="InvalidateRowDetail"/> to refetch.
    /// </summary>
    /// <remarks>
    /// The loader returns no value: where the data lands is the app's business. Stash it on the item
    /// or in a lookup keyed by it and let the template read it, which keeps the template's context the
    /// row's item rather than some wrapper.
    /// </remarks>
    [Parameter] public Func<TItem, Task>? RowDetailLoader { get; set; }

    /// <summary>
    /// Shown in the detail row while <see cref="RowDetailLoader"/> runs - a skeleton, say. Defaults to
    /// a spinner.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? RowDetailLoadingTemplate { get; set; }

    /// <summary>
    /// True while any row is waiting on <see cref="ChildrenLoader"/> or <see cref="RowDetailLoader"/>.
    /// Bind a page-level busy indicator to it; the per-row spinners are drawn by the grid either way.
    /// Distinct from <see cref="Loading"/>, which you set to cover the grid while *its* data loads.
    /// </summary>
    public bool IsBusy => this.isBusy;

    /// <summary>Fires whenever <see cref="IsBusy"/> flips - use it with a read-only busy binding.</summary>
    [Parameter] public EventCallback<bool> IsBusyChanged { get; set; }

    /// <summary>Whether one row or many can be expanded at a time (default <see cref="DataGridExpandMode.Multiple"/>).</summary>
    [Parameter] public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

    /// <summary>
    /// Clicking anywhere on a row toggles its expansion. Off by default - the caret is the affordance,
    /// so a click stays free for selection/editing.
    /// </summary>
    [Parameter] public bool ExpandOnRowClick { get; set; }

    /// <summary>
    /// Per-item veto on expansion. Return false and the row shows no caret and cannot be expanded -
    /// use it for rows with nothing to break down.
    /// </summary>
    [Parameter] public Func<TItem, bool>? IsRowExpandable { get; set; }

    /// <summary>
    /// Turns the grid into a tree: returns the child items of a row, or null/empty for a leaf.
    /// Children go through the same filter/sort pipeline as their parents, one level at a time.
    /// </summary>
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>
    /// Lazily loads a row's children the first time it is expanded (a spinner glyph shows meanwhile).
    /// Results are cached for the lifetime of the component - call <see cref="InvalidateChildren"/> to
    /// drop them. <see cref="ChildrenSelector"/> gets first refusal: the loader only runs for items it
    /// returns null for, so a tree can mix in-memory branches with fetched ones.
    /// </summary>
    [Parameter] public Func<TItem, Task<IEnumerable<TItem>>>? ChildrenLoader { get; set; }

    /// <summary>
    /// Reports whether a row has children without materializing them - required with
    /// <see cref="ChildrenLoader"/> if you want leaves to render without a caret before the first load.
    /// </summary>
    [Parameter] public Func<TItem, bool>? HasChildrenSelector { get; set; }

    /// <summary>Pixels of indent per hierarchy level in tree mode (default 20).</summary>
    [Parameter] public double TreeIndentSize { get; set; } = 20;

    [Parameter] public IReadOnlyCollection<TItem> ExpandedItems { get; set; } = Array.Empty<TItem>();
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> ExpandedItemsChanged { get; set; }

    [Parameter] public EventCallback<TItem> RowExpanded { get; set; }
    [Parameter] public EventCallback<TItem> RowCollapsed { get; set; }

    /// <summary>Raised when <see cref="ChildrenLoader"/> throws; the row is collapsed again.</summary>
    [Parameter] public EventCallback<DataGridLoadFailedEventArgs<TItem>> ChildrenLoadFailed { get; set; }

    /// <summary>Raised when <see cref="RowDetailLoader"/> throws; the row is collapsed again.</summary>
    [Parameter] public EventCallback<DataGridLoadFailedEventArgs<TItem>> RowDetailLoadFailed { get; set; }

    // ---- Public API ----

    public bool IsRowExpanded(TItem item) => this.expandedItems.Contains(item);

    /// <summary>Expands a row - loading its children first when <see cref="ChildrenLoader"/> is set.</summary>
    public async Task ExpandRowAsync(TItem item)
    {
        if (!this.CanExpand(item) || !this.expandedItems.Add(item))
            return;

        if (this.ExpandMode == DataGridExpandMode.Single)
        {
            foreach (var other in this.expandedItems.Where(i => !EqualityComparer<TItem>.Default.Equals(i, item)).ToList())
                this.expandedItems.Remove(other);
        }

        await this.RowExpanded.InvokeAsync(item);
        await this.RaiseExpandedItemsAsync();

        // A row can owe both - a tree node with a breakdown of its own - so neither short-circuits the
        // other, and each clears its own spinner when it lands.
        var children = this.NeedsChildrenLoad(item) ? this.LoadChildrenAsync(item) : null;
        var detail = this.NeedsDetailLoad(item) ? this.LoadDetailAsync(item) : null;

        if (children is null && detail is null)
        {
            this.StateHasChanged();
            return;
        }

        if (children is not null)
            await children;
        if (detail is not null)
            await detail;
    }

    /// <summary>True while this row is waiting on a children or detail load.</summary>
    public bool IsRowBusy(TItem item)
        => this.loadingChildren.Contains(item) || this.loadingDetails.Contains(item);

    internal bool IsRowDetailLoading(TItem item) => this.loadingDetails.Contains(item);

    public async Task CollapseRowAsync(TItem item)
    {
        if (!this.expandedItems.Remove(item))
            return;

        await this.RowCollapsed.InvokeAsync(item);
        await this.RaiseExpandedItemsAsync();
        this.StateHasChanged();
    }

    public Task ToggleExpandAsync(TItem item)
        => this.IsRowExpanded(item) ? this.CollapseRowAsync(item) : this.ExpandRowAsync(item);

    /// <summary>
    /// Expands every row the grid currently knows about. In tree mode that means every already-loaded
    /// level - rows still waiting on <see cref="ChildrenLoader"/> are not fetched, since the depth of a
    /// lazily loaded tree is unbounded.
    /// </summary>
    public async Task ExpandAllAsync()
    {
        if (this.ExpandMode == DataGridExpandMode.Single)
            return;

        var pending = new List<TItem>();
        foreach (var item in this.EnumerateKnownItems())
        {
            if (!this.CanExpand(item) || this.NeedsChildrenLoad(item))
                continue;

            this.expandedItems.Add(item);
            if (this.NeedsDetailLoad(item))
                pending.Add(item);
        }

        await this.RaiseExpandedItemsAsync();
        this.StateHasChanged();

        // One load per expanded row, so this is bounded by what is on screen - unlike a lazy tree,
        // whose depth is not.
        foreach (var item in pending)
            await this.LoadDetailAsync(item);
    }

    public async Task CollapseAllAsync()
    {
        if (this.expandedItems.Count == 0)
            return;

        this.expandedItems.Clear();
        await this.RaiseExpandedItemsAsync();
        this.StateHasChanged();
    }

    /// <summary>Drops cached lazily-loaded children so the next expand re-fetches them.</summary>
    public void InvalidateChildren(TItem? item = default)
    {
        if (item is null)
            this.loadedChildren.Clear();
        else
            this.loadedChildren.Remove(item);

        this.StateHasChanged();
    }

    /// <summary>
    /// Forgets that a row's detail was loaded, so the next expand runs <see cref="RowDetailLoader"/>
    /// again. Pass null for every row. A row that is expanded right now reloads immediately.
    /// </summary>
    public async Task InvalidateRowDetail(TItem? item = default)
    {
        var affected = item is null
            ? this.loadedDetails.ToList()
            : this.loadedDetails.Contains(item) ? new List<TItem> { item } : new List<TItem>();

        if (item is null)
            this.loadedDetails.Clear();
        else
            this.loadedDetails.Remove(item);

        this.StateHasChanged();
        foreach (var stale in affected.Where(this.IsRowExpanded))
            await this.LoadDetailAsync(stale);
    }

    // ---- Internals ----

    /// <summary>True when a detail row can appear - which is also what puts the expander column in.</summary>
    internal bool HasRowDetail => this.RowDetailTemplate is not null;

    internal bool TreeEnabled => this.ChildrenSelector is not null || this.ChildrenLoader is not null;

    /// <summary>Tree carets live inline in the first column; only the detail row gets its own column.</summary>
    internal bool HasExpanderColumn => this.HasRowDetail;

    internal int LeadColumnCount => (this.HasExpanderColumn ? 1 : 0) + (this.HasMultiSelect ? 1 : 0);

    internal bool ExpansionEnabled => this.HasRowDetail || this.TreeEnabled;

    internal bool CanExpand(TItem item)
    {
        if (!this.ExpansionEnabled)
            return false;
        if (this.IsRowExpandable is not null && !this.IsRowExpandable(item))
            return false;

        return this.HasRowDetail || this.HasChildrenOf(item);
    }

    /// <summary>True when the detail caret should show for this row.</summary>
    internal bool CanShowDetail(TItem item)
        => this.HasRowDetail && (this.IsRowExpandable?.Invoke(item) ?? true);

    internal bool HasChildrenOf(TItem item)
    {
        if (!this.TreeEnabled)
            return false;
        if (this.HasChildrenSelector is not null)
            return this.HasChildrenSelector(item);
        if (this.loadedChildren.TryGetValue(item, out var cached))
            return cached.Count > 0;

        var sync = this.SelectorChildren(item);
        if (sync is not null)
            return sync.Count > 0;

        // The selector passed on this one, so only the loader knows - offer the caret rather than
        // hide a branch that may well have something in it.
        return this.ChildrenLoader is not null;
    }

    /// <summary>What <see cref="ChildrenSelector"/> says, or null for "not mine - ask the loader".</summary>
    IReadOnlyList<TItem>? SelectorChildren(TItem item)
        => this.ChildrenSelector?.Invoke(item)?.ToList();

    IReadOnlyList<TItem> RawChildren(TItem item)
        => this.loadedChildren.TryGetValue(item, out var cached)
            ? cached
            : this.SelectorChildren(item) ?? Array.Empty<TItem>();

    /// <summary>
    /// The loader only covers what the selector declined. Without that the loader would take over
    /// every branch, and a tree could never mix in-memory levels with fetched ones.
    /// </summary>
    bool NeedsChildrenLoad(TItem item)
        => this.ChildrenLoader is not null
            && !this.loadedChildren.ContainsKey(item)
            && this.SelectorChildren(item) is null;

    bool NeedsDetailLoad(TItem item)
        => this.RowDetailLoader is not null
            && this.HasRowDetail
            && !this.loadedDetails.Contains(item)
            && !this.loadingDetails.Contains(item);

    async Task LoadChildrenAsync(TItem item)
    {
        if (this.ChildrenLoader is null)
            return;

        this.loadingChildren.Add(item);
        await this.OnBusyChangedAsync();
        this.StateHasChanged();
        try
        {
            var children = await this.ChildrenLoader(item);
            this.loadedChildren[item] = children is null ? Array.Empty<TItem>() : children.ToList();
        }
        catch (Exception ex)
        {
            this.expandedItems.Remove(item);
            await this.ChildrenLoadFailed.InvokeAsync(new DataGridLoadFailedEventArgs<TItem>(item, ex));
        }
        finally
        {
            this.loadingChildren.Remove(item);
            await this.OnBusyChangedAsync();
            this.StateHasChanged();
        }
    }

    async Task LoadDetailAsync(TItem item)
    {
        if (this.RowDetailLoader is null)
            return;

        this.loadingDetails.Add(item);
        await this.OnBusyChangedAsync();
        this.StateHasChanged();
        try
        {
            await this.RowDetailLoader(item);
            this.loadedDetails.Add(item);
        }
        catch (Exception ex)
        {
            this.expandedItems.Remove(item);
            await this.RowDetailLoadFailed.InvokeAsync(new DataGridLoadFailedEventArgs<TItem>(item, ex));
        }
        finally
        {
            this.loadingDetails.Remove(item);
            await this.OnBusyChangedAsync();
            this.StateHasChanged();
        }
    }

    Task OnBusyChangedAsync()
    {
        var busy = this.loadingChildren.Count > 0 || this.loadingDetails.Count > 0;
        if (busy == this.isBusy)
            return Task.CompletedTask;

        this.isBusy = busy;
        return this.IsBusyChanged.InvokeAsync(busy);
    }

    Task RaiseExpandedItemsAsync()
    {
        this.ExpandedItems = this.expandedItems.ToList();
        return this.ExpandedItemsChanged.InvokeAsync(this.ExpandedItems);
    }

    /// <summary>Every item reachable without a load - the roots plus any already-loaded subtree.</summary>
    IEnumerable<TItem> EnumerateKnownItems()
    {
        foreach (var root in this.ProcessedItems())
        {
            yield return root;
            if (!this.TreeEnabled)
                continue;

            foreach (var descendant in this.Descendants(root))
                yield return descendant;
        }
    }

    IEnumerable<TItem> Descendants(TItem item)
    {
        if (this.NeedsChildrenLoad(item))
            yield break;

        foreach (var child in this.RawChildren(item))
        {
            yield return child;
            foreach (var descendant in this.Descendants(child))
                yield return descendant;
        }
    }

    /// <summary>One rendered row: the item plus where it sits in the hierarchy.</summary>
    internal sealed record RowNode(TItem Item, int Level, bool HasChildren, bool IsExpanded, bool IsLoading)
    {
        /// <summary>Empty while loading - the busy spinner takes the caret's place rather than sitting beside it.</summary>
        public string CaretGlyph
            => this.IsLoading || !this.HasChildren ? string.Empty
                : this.IsExpanded ? "▾" : "▸";

        /// <summary>
        /// First row of the block this node was flattened into - the page, or one group when the grid
        /// is grouped. A column highlight closes its stroke here rather than leaving it running off
        /// the top of the block.
        /// </summary>
        public bool IsFirst { get; init; }

        /// <summary>Last row of the block - the other end of the same stroke.</summary>
        public bool IsLast { get; init; }
    }

    /// <summary>Flattens a set of items (already filtered/sorted) plus every expanded subtree under them.</summary>
    internal IReadOnlyList<RowNode> Flatten(IEnumerable<TItem> items)
    {
        var rows = new List<RowNode>();
        this.Append(rows, items, 0);
        if (rows.Count > 0)
        {
            // Stamped after the fact rather than tracked during the walk: Append recurses into
            // expanded subtrees, so "last" is only known once the whole block is laid out.
            rows[0] = rows[0] with { IsFirst = true };
            rows[^1] = rows[^1] with { IsLast = true };
        }
        return rows;
    }

    void Append(List<RowNode> rows, IEnumerable<TItem> items, int level)
    {
        foreach (var item in items)
        {
            // Grouping wins over hierarchy - a grouped tree has two competing row orders.
            var hasChildren = !this.IsGrouped && this.HasChildrenOf(item);
            var expanded = this.IsRowExpanded(item);
            var loading = this.loadingChildren.Contains(item);
            rows.Add(new RowNode(item, level, hasChildren, expanded, loading));

            if (hasChildren && expanded && !loading)
                this.Append(rows, this.ProcessLevel(this.RawChildren(item)), level + 1);
        }
    }

    /// <summary>The flattened rows for the current page - roots plus expanded descendants.</summary>
    internal IReadOnlyList<RowNode> GetRenderedRows() => this.Flatten(this.GetRenderedItems());

    internal Task OnExpanderClickAsync(TItem item) => this.ToggleExpandAsync(item);
}

/// <summary>Reports a failed <c>ChildrenLoader</c> or <c>RowDetailLoader</c>.</summary>
public sealed class DataGridLoadFailedEventArgs<TItem>
{
    public DataGridLoadFailedEventArgs(TItem item, Exception exception)
    {
        this.Item = item;
        this.Exception = exception;
    }

    public TItem Item { get; }
    public Exception Exception { get; }
}
