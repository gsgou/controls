using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class DataGrid<TItem> : IAsyncDisposable
{
    readonly List<ColumnBase<TItem>> columns = new();
    readonly HashSet<TItem> selected = new();
    IReadOnlyList<TItem>? serverItems;
    int? serverTotal;
    bool serverLoaded;

    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>The column declarations — <see cref="PropertyColumn{TItem, TProperty}"/> / <see cref="TemplateColumn{TItem}"/>.</summary>
    [Parameter] public RenderFragment? Columns { get; set; }

    // --- Selection ---
    [Parameter] public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.None;
    [Parameter] public TItem? SelectedItem { get; set; }
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }
    [Parameter] public IReadOnlyCollection<TItem> SelectedItems { get; set; } = Array.Empty<TItem>();
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    // --- Styling ---
    [Parameter] public bool Dense { get; set; }
    [Parameter] public bool Striped { get; set; }
    [Parameter] public bool Bordered { get; set; }
    [Parameter] public bool Hover { get; set; } = true;
    [Parameter] public bool Outlined { get; set; } = true;
    [Parameter] public bool FixedHeader { get; set; }
    [Parameter] public string? Height { get; set; }
    [Parameter] public bool ShowColumnHeaders { get; set; } = true;

    // --- State ---
    [Parameter] public bool Loading { get; set; }
    [Parameter] public RenderFragment? LoadingContent { get; set; }
    [Parameter] public string NoRecordsText { get; set; } = "No records";
    [Parameter] public RenderFragment? NoRecordsContent { get; set; }

    // --- Events ---
    [Parameter] public EventCallback<TItem> RowClick { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    internal IReadOnlyList<ColumnBase<TItem>> VisibleColumns
    {
        get
        {
            var visible = this.columns.Where(c => !c.Hidden);
            if (this.columnOrder.Count == 0)
                return visible.ToList();
            return visible
                .OrderBy(c => { var i = this.columnOrder.IndexOf(c.Id); return i < 0 ? int.MaxValue : i; })
                .ToList();
        }
    }

    // ---- Virtualization / resize / reorder ----
    [Parameter] public bool Virtualize { get; set; }
    [Parameter] public DataGridColumnResizeMode ColumnResizeMode { get; set; } = DataGridColumnResizeMode.None;
    [Parameter] public bool DragDropColumnReordering { get; set; }

    readonly Dictionary<string, string> columnWidths = new();
    readonly List<string> columnOrder = new();
    string? resizingColumnId;
    double resizeStartX;
    double resizeStartWidth;
    string? dragColumnId;

    internal bool CanVirtualize => this.Virtualize && !this.IsGrouped && !this.Paging && this.serverItems is null;

    internal void OnResizeStart(ColumnBase<TItem> col, PointerEventArgs e)
    {
        this.resizingColumnId = col.Id;
        this.resizeStartX = e.ClientX;
        this.resizeStartWidth = this.columnWidths.TryGetValue(col.Id, out var w) && w.EndsWith("px")
            && double.TryParse(w[..^2], out var px) ? px : 150;
    }

    internal void OnResizeMove(PointerEventArgs e)
    {
        if (this.resizingColumnId is null)
            return;
        var width = Math.Max(48, this.resizeStartWidth + (e.ClientX - this.resizeStartX));
        this.columnWidths[this.resizingColumnId] = $"{width:0}px";
        this.StateHasChanged();
    }

    internal void OnResizeEnd() => this.resizingColumnId = null;

    internal void OnColumnDragStart(ColumnBase<TItem> col) => this.dragColumnId = col.Id;

    internal void OnColumnDrop(ColumnBase<TItem> target)
    {
        if (this.dragColumnId is null || this.dragColumnId == target.Id)
            return;

        if (this.columnOrder.Count == 0)
            this.columnOrder.AddRange(this.columns.Where(c => !c.Hidden).Select(c => c.Id));

        this.columnOrder.Remove(this.dragColumnId);
        var targetIdx = this.columnOrder.IndexOf(target.Id);
        this.columnOrder.Insert(targetIdx < 0 ? this.columnOrder.Count : targetIdx, this.dragColumnId);
        this.dragColumnId = null;
        this.StateHasChanged();
    }

    internal bool HasMultiSelect => this.SelectionMode == DataGridSelectionMode.Multiple;

    // ---- Frozen (pinned) columns ----

    /// <summary>Freezes the first N visible columns to the leading edge. Overridden upward by any
    /// leading columns that set <see cref="ColumnBase{TItem}.Frozen"/> themselves.</summary>
    [Parameter] public int FrozenColumns { get; set; }

    /// <summary>Freezes the last N visible columns to the trailing edge.</summary>
    [Parameter] public int FrozenEndColumns { get; set; }

    readonly Dictionary<string, FrozenCell> frozenCells = new();

    internal int FrozenStartCount { get; private set; }

    internal int FrozenEndCount { get; private set; }

    internal bool HasFrozenColumns => this.FrozenStartCount > 0 || this.FrozenEndCount > 0;

    /// <summary>The multi-select checkbox column is always leftmost, so it pins with the start block.</summary>
    internal bool FrozenCheckColumn => this.HasMultiSelect && this.FrozenStartCount > 0;

    readonly record struct FrozenCell(DataGridFrozen Position, bool Edge, double? Offset);

    /// <summary>
    /// Recomputes which columns are pinned and (where the widths are known up front) how far in.
    /// Called once at the top of the render so the per-cell helpers below are plain lookups.
    /// </summary>
    void RefreshFrozenLayout()
    {
        var cols = this.VisibleColumns;

        var start = 0;
        while (start < cols.Count && cols[start].EffectiveFrozen == DataGridFrozen.Start)
            start++;
        start = Math.Clamp(Math.Max(start, this.FrozenColumns), 0, cols.Count);

        var end = 0;
        while (end < cols.Count && cols[cols.Count - 1 - end].EffectiveFrozen == DataGridFrozen.End)
            end++;
        end = Math.Clamp(Math.Max(end, this.FrozenEndColumns), 0, cols.Count - start);

        this.FrozenStartCount = start;
        this.FrozenEndCount = end;

        this.frozenCells.Clear();
        if (start == 0 && end == 0)
            return;

        // Best-effort offsets so the pinning is right on the very first paint (and without JS at
        // all when every pinned column declares a px width). datagrid.js re-measures and corrects
        // whatever we could not work out here.
        double? offset = this.HasMultiSelect ? null : 0;
        for (var i = 0; i < start; i++)
        {
            this.frozenCells[cols[i].Id] = new FrozenCell(DataGridFrozen.Start, i == start - 1, offset);
            var w = this.PxWidth(cols[i]);
            offset = offset is null || w is null ? null : offset + w;
        }

        offset = 0;
        for (var i = 0; i < end; i++)
        {
            var col = cols[cols.Count - 1 - i];
            this.frozenCells[col.Id] = new FrozenCell(DataGridFrozen.End, i == end - 1, offset);
            var w = this.PxWidth(col);
            offset = offset is null || w is null ? null : offset + w;
        }
    }

    double? PxWidth(ColumnBase<TItem> col)
    {
        var w = this.columnWidths.TryGetValue(col.Id, out var resized) ? resized : col.Width;
        if (string.IsNullOrWhiteSpace(w))
            return null;

        w = w.Trim();
        return w.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(w[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var px)
            ? px
            : null;
    }

    /// <summary>The <c>data-dg-frozen</c> marker datagrid.js keys its measurements off.</summary>
    internal string? FrozenAttr(ColumnBase<TItem>? col)
    {
        if (col is null)
            return this.FrozenCheckColumn ? "start" : null;

        return this.frozenCells.TryGetValue(col.Id, out var f)
            ? f.Position == DataGridFrozen.Start ? "start" : "end"
            : null;
    }

    /// <summary>Pass a null column for the multi-select checkbox cell.</summary>
    internal string FrozenCssClass(ColumnBase<TItem>? col)
    {
        if (col is null)
            return this.FrozenCheckColumn ? " shiny-dg-frozen shiny-dg-frozen-start" : string.Empty;

        if (!this.frozenCells.TryGetValue(col.Id, out var f))
            return string.Empty;

        var side = f.Position == DataGridFrozen.Start ? "start" : "end";
        return f.Edge
            ? $" shiny-dg-frozen shiny-dg-frozen-{side} shiny-dg-frozen-{side}-edge"
            : $" shiny-dg-frozen shiny-dg-frozen-{side}";
    }

    internal string? FrozenOffsetStyle(ColumnBase<TItem>? col)
    {
        if (col is null)
            return this.FrozenCheckColumn ? "left:0;" : null;

        if (!this.frozenCells.TryGetValue(col.Id, out var f) || f.Offset is not { } offset)
            return null;

        var edge = f.Position == DataGridFrozen.Start ? "left" : "right";
        return string.Create(CultureInfo.InvariantCulture, $"{edge}:{offset:0.##}px;");
    }

    // ---- Column registration ----
    internal void AddColumn(ColumnBase<TItem> column)
    {
        if (!this.columns.Contains(column))
        {
            this.columns.Add(column);
            this.StateHasChanged();
        }
    }

    internal void RemoveColumn(ColumnBase<TItem> column)
    {
        if (this.columns.Remove(column))
            this.StateHasChanged();
    }

    internal void NotifyColumnsChanged() => this.StateHasChanged();

    protected override void OnParametersSet()
    {
        // Keep the internal selection set in sync with bound parameters.
        if (this.SelectionMode == DataGridSelectionMode.Multiple)
        {
            this.selected.Clear();
            foreach (var item in this.SelectedItems)
                this.selected.Add(item);
        }
        else if (this.SelectionMode == DataGridSelectionMode.Single)
        {
            this.selected.Clear();
            if (this.SelectedItem is not null)
                this.selected.Add(this.SelectedItem);
        }
    }

    // ---- Sorting / filtering / paging pipeline ----
    [Parameter] public DataGridSortMode SortMode { get; set; } = DataGridSortMode.Single;

    /// <summary>A quick filter predicate applied across all rows (toolbar search).</summary>
    [Parameter] public Func<TItem, bool>? QuickFilter { get; set; }

    [Parameter] public int RowsPerPage { get; set; } = 10;
    [Parameter] public EventCallback<int> RowsPerPageChanged { get; set; }
    [Parameter] public int CurrentPage { get; set; }
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }

    /// <summary>Pager slot — place a <see cref="DataGridPager{TItem}"/> here to enable paging.</summary>
    [Parameter] public RenderFragment? PagerContent { get; set; }

    readonly List<SortDefinition> sortDefs = new();
    readonly List<FilterDefinition> filterDefs = new();

    internal bool Paging => this.PagerContent is not null;

    internal bool EffectiveSortable(ColumnBase<TItem> col)
        => col.Sortable ?? (this.SortMode != DataGridSortMode.None && col.HasValue);

    internal IReadOnlyList<SortDefinition> SortDefinitions => this.sortDefs;
    internal IReadOnlyList<FilterDefinition> FilterDefinitions => this.filterDefs;

    internal SortDefinition? GetSortDefinition(ColumnBase<TItem> col)
        => this.sortDefs.FirstOrDefault(d => d.ColumnId == col.Id);

    internal int SortOrderBadge(ColumnBase<TItem> col)
    {
        if (this.SortMode != DataGridSortMode.Multiple || this.sortDefs.Count < 2)
            return 0;
        var def = this.GetSortDefinition(col);
        return def is null ? 0 : def.Order + 1;
    }

    internal async Task ToggleSortAsync(ColumnBase<TItem> col)
    {
        if (!this.EffectiveSortable(col))
            return;

        var existing = this.GetSortDefinition(col);
        var next = existing is null
            ? DataGridSortDirection.Ascending
            : existing.Direction == DataGridSortDirection.Ascending
                ? DataGridSortDirection.Descending
                : DataGridSortDirection.None;

        if (this.SortMode != DataGridSortMode.Multiple)
            this.sortDefs.Clear();
        else
            this.sortDefs.RemoveAll(d => d.ColumnId == col.Id);

        if (next != DataGridSortDirection.None)
            this.sortDefs.Add(new SortDefinition(col.Id, next, this.sortDefs.Count));

        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    // ---- Filtering UI ----
    [Parameter] public DataGridFilterMode FilterMode { get; set; } = DataGridFilterMode.Menu;

    string quickSearch = string.Empty;
    string? openFilterColumnId;

    internal DataGridFilterMode EffectiveFilterMode => this.FilterMode;

    internal bool EffectiveFilterable(ColumnBase<TItem> col)
        => (col.Filterable ?? col.HasValue) && col.HasValue;

    internal bool AnyFilterable => this.VisibleColumns.Any(this.EffectiveFilterable);

    internal string QuickSearch => this.quickSearch;

    internal async Task SetQuickSearchAsync(string? value)
    {
        this.quickSearch = value ?? string.Empty;
        this.CurrentPage = 0;
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    internal bool IsFilterMenuOpen(ColumnBase<TItem> col) => this.openFilterColumnId == col.Id;

    internal void ToggleFilterMenu(ColumnBase<TItem> col)
    {
        this.openFilterColumnId = this.openFilterColumnId == col.Id ? null : col.Id;
        this.StateHasChanged();
    }

    internal FilterDefinition GetOrCreateFilter(ColumnBase<TItem> col)
    {
        var def = this.filterDefs.FirstOrDefault(d => d.ColumnId == col.Id);
        if (def is null)
        {
            def = new FilterDefinition { ColumnId = col.Id, Operator = DefaultOperator(col.GetDataType()) };
            this.filterDefs.Add(def);
        }
        return def;
    }

    internal bool HasActiveFilter(ColumnBase<TItem> col)
        => this.filterDefs.Any(d => d.ColumnId == col.Id &&
            (d.Value is not null || d.Operator is DataGridFilterOperator.Empty or DataGridFilterOperator.NotEmpty));

    internal async Task ApplyColumnFilterAsync(ColumnBase<TItem> col, DataGridFilterOperator op, object? value)
    {
        var def = this.GetOrCreateFilter(col);
        def.Operator = op;
        def.Value = value;
        this.openFilterColumnId = null;
        this.CurrentPage = 0;
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    internal async Task ClearColumnFilterAsync(ColumnBase<TItem> col)
    {
        this.filterDefs.RemoveAll(d => d.ColumnId == col.Id);
        this.openFilterColumnId = null;
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    internal static IReadOnlyList<DataGridFilterOperator> OperatorsFor(Type type)
    {
        if (type == typeof(string))
            return new[] { DataGridFilterOperator.Contains, DataGridFilterOperator.NotContains, DataGridFilterOperator.Equals, DataGridFilterOperator.NotEquals, DataGridFilterOperator.StartsWith, DataGridFilterOperator.EndsWith, DataGridFilterOperator.Empty, DataGridFilterOperator.NotEmpty };
        if (type == typeof(bool))
            return new[] { DataGridFilterOperator.Is };
        if (type.IsEnum)
            return new[] { DataGridFilterOperator.Is, DataGridFilterOperator.IsNot };
        // numeric / date
        return new[] { DataGridFilterOperator.Equals, DataGridFilterOperator.NotEquals, DataGridFilterOperator.GreaterThan, DataGridFilterOperator.GreaterThanOrEqual, DataGridFilterOperator.LessThan, DataGridFilterOperator.LessThanOrEqual };
    }

    internal static string OperatorLabel(DataGridFilterOperator op) => op switch
    {
        DataGridFilterOperator.Contains => "contains",
        DataGridFilterOperator.NotContains => "not contains",
        DataGridFilterOperator.Equals => "equals",
        DataGridFilterOperator.NotEquals => "not equals",
        DataGridFilterOperator.StartsWith => "starts with",
        DataGridFilterOperator.EndsWith => "ends with",
        DataGridFilterOperator.Empty => "is empty",
        DataGridFilterOperator.NotEmpty => "is not empty",
        DataGridFilterOperator.GreaterThan => ">",
        DataGridFilterOperator.GreaterThanOrEqual => "≥",
        DataGridFilterOperator.LessThan => "<",
        DataGridFilterOperator.LessThanOrEqual => "≤",
        DataGridFilterOperator.Is => "is",
        DataGridFilterOperator.IsNot => "is not",
        _ => op.ToString()
    };

    static DataGridFilterOperator DefaultOperator(Type type)
        => type == typeof(string) ? DataGridFilterOperator.Contains
            : type == typeof(bool) || type.IsEnum ? DataGridFilterOperator.Is
            : DataGridFilterOperator.Equals;

    internal IReadOnlyList<TItem> ProcessedItems()
    {
        if (this.serverItems is not null)
            return this.serverItems;

        IEnumerable<TItem> q = this.Items ?? Enumerable.Empty<TItem>();

        if (this.QuickFilter is not null)
            q = q.Where(this.QuickFilter);

        if (!string.IsNullOrEmpty(this.quickSearch))
        {
            var term = this.quickSearch;
            q = q.Where(item => this.VisibleColumns.Any(c =>
                c.HasValue && (c.GetText(item)?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)));
        }

        q = this.ApplyColumnFilters(q);
        return this.ApplySort(q);
    }

    IReadOnlyList<TItem> ApplySort(IEnumerable<TItem> source)
    {
        if (this.sortDefs.Count == 0)
            return source as IReadOnlyList<TItem> ?? source.ToList();

        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var def in this.sortDefs.OrderBy(d => d.Order))
        {
            var col = this.columns.FirstOrDefault(c => c.Id == def.ColumnId);
            if (col is null)
                continue;

            var comparer = col.Comparer ?? DataGridValueComparer.Instance;
            Func<TItem, object?> key = col.GetValue;
            var asc = def.Direction == DataGridSortDirection.Ascending;

            ordered = ordered is null
                ? (asc ? source.OrderBy(key, comparer) : source.OrderByDescending(key, comparer))
                : (asc ? ordered.ThenBy(key, comparer) : ordered.ThenByDescending(key, comparer));
        }
        return ((IEnumerable<TItem>?)ordered ?? source).ToList();
    }

    internal int TotalItems => this.serverTotal ?? this.ProcessedItems().Count;

    internal int TotalPages => this.Paging
        ? Math.Max(1, (int)Math.Ceiling(this.TotalItems / (double)Math.Max(1, this.RowsPerPage)))
        : 1;

    internal async Task SetPageAsync(int page)
    {
        var clamped = Math.Clamp(page, 0, this.TotalPages - 1);
        if (clamped == this.CurrentPage && this.serverItems is not null)
            return;
        this.CurrentPage = clamped;
        await this.CurrentPageChanged.InvokeAsync(this.CurrentPage);
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    internal async Task SetRowsPerPageAsync(int rows)
    {
        this.RowsPerPage = rows;
        this.CurrentPage = 0;
        await this.RowsPerPageChanged.InvokeAsync(rows);
        await this.CurrentPageChanged.InvokeAsync(0);
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    /// <summary>The rows to render after filter → sort → page.</summary>
    internal IReadOnlyList<TItem> GetRenderedItems()
    {
        var processed = this.ProcessedItems();
        if (!this.Paging || this.serverItems is not null)
            return processed;

        var start = this.CurrentPage * this.RowsPerPage;
        if (start >= processed.Count && processed.Count > 0)
        {
            this.CurrentPage = this.TotalPages - 1;
            start = this.CurrentPage * this.RowsPerPage;
        }
        return processed.Skip(start).Take(this.RowsPerPage).ToList();
    }

    // ---- Column filters (operators applied in Phase 3 via FilterDefinitions) ----
    IEnumerable<TItem> ApplyColumnFilters(IEnumerable<TItem> source)
    {
        if (this.filterDefs.Count == 0)
            return source;

        var result = source;
        foreach (var def in this.filterDefs)
        {
            var col = this.columns.FirstOrDefault(c => c.Id == def.ColumnId);
            if (col is null)
                continue;
            var capture = def;
            result = result.Where(item => DataGridFilterEvaluator.Matches(col.GetValue(item), capture.Operator, capture.Value));
        }
        return result;
    }

    // ---- Server-side data ----
    [Parameter] public Func<GridState, Task<GridData<TItem>>>? ServerData { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await this.SyncStickyLayoutAsync();

        if (firstRender && this.ServerData is not null && !this.serverLoaded)
        {
            this.serverLoaded = true;
            await this.ReloadServerDataAsync();
        }
    }

    // ---- Sticky header / frozen column measurement ----
    [Inject] IJSRuntime JS { get; set; } = default!;

    ElementReference rootRef;
    IJSObjectReference? stickyModule;
    bool stickyDisposed;

    bool NeedsStickyLayout => this.FixedHeader || this.HasFrozenColumns;

    async Task SyncStickyLayoutAsync()
    {
        if (this.stickyDisposed)
            return;

        try
        {
            if (!this.NeedsStickyLayout)
            {
                if (this.stickyModule is not null)
                    await this.stickyModule.InvokeVoidAsync("dispose", this.rootRef);
                return;
            }

            this.stickyModule ??= await this.JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Shiny.Blazor.Controls/datagrid.js");

            // init is idempotent: it wires the observer once and re-measures on every call, which is
            // exactly what we want after a re-render changed the columns or their widths.
            await this.stickyModule.InvokeVoidAsync("init", this.rootRef);
        }
        catch (JSDisconnectedException)
        {
            // circuit went away mid-render; nothing to clean up
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        this.stickyDisposed = true;
        if (this.stickyModule is null)
            return;

        try
        {
            await this.stickyModule.InvokeVoidAsync("dispose", this.rootRef);
            await this.stickyModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            this.stickyModule = null;
        }
    }

    async Task ReloadServerDataAsync()
    {
        if (this.ServerData is null)
            return;

        var state = new GridState
        {
            Page = this.CurrentPage,
            PageSize = this.RowsPerPage,
            SortDefinitions = this.sortDefs.ToList(),
            FilterDefinitions = this.filterDefs.ToList()
        };
        var data = await this.ServerData(state);
        this.serverItems = data.Items;
        this.serverTotal = data.TotalItems;
        this.StateHasChanged();
    }

    internal bool IsSelected(TItem item) => this.selected.Contains(item);

    internal bool AllSelected
    {
        get
        {
            var items = this.GetRenderedItems();
            return items.Count > 0 && items.All(this.selected.Contains);
        }
    }

    internal async Task ToggleRowAsync(TItem item)
    {
        switch (this.SelectionMode)
        {
            case DataGridSelectionMode.Single:
                this.selected.Clear();
                this.selected.Add(item);
                this.SelectedItem = item;
                await this.SelectedItemChanged.InvokeAsync(item);
                break;

            case DataGridSelectionMode.Multiple:
                if (!this.selected.Add(item))
                    this.selected.Remove(item);
                await this.RaiseSelectedItemsAsync();
                break;
        }
        this.StateHasChanged();
    }

    internal async Task ToggleSelectAllAsync(bool select)
    {
        this.selected.Clear();
        if (select)
        {
            foreach (var item in this.GetRenderedItems())
                this.selected.Add(item);
        }
        await this.RaiseSelectedItemsAsync();
        this.StateHasChanged();
    }

    async Task RaiseSelectedItemsAsync()
    {
        this.SelectedItems = this.selected.ToList();
        await this.SelectedItemsChanged.InvokeAsync(this.SelectedItems);
    }

    internal async Task RowClickedAsync(TItem item)
    {
        if (this.RowClick.HasDelegate)
            await this.RowClick.InvokeAsync(item);

        if (this.EditMode == DataGridEditMode.Form && this.EditTrigger == DataGridEditTrigger.OnRowClick && this.EditingEnabled)
        {
            await this.StartEditRowAsync(item);
            return;
        }

        if (this.SelectionMode != DataGridSelectionMode.None)
            await this.ToggleRowAsync(item);
    }

    internal Task OnCellClickAsync(TItem item, ColumnBase<TItem> col)
        => this.EditMode == DataGridEditMode.Cell && this.EffectiveEditable(col)
            ? this.StartEditCellAsync(item, col)
            : Task.CompletedTask;

    internal async Task EditorKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await this.CommitEditAsync();
        else if (e.Key == "Escape")
            await this.CancelEditAsync();
    }

    internal Task OnEditorBlurAsync()
        => this.editingCell is not null ? this.CommitEditAsync() : Task.CompletedTask;

    internal CellContext<TItem> CreateCellContext(TItem item)
        => new(
            item,
            this.IsSelected(item),
            new CellContext<TItem>.CellActions
            {
                SetSelectedItem = select => { _ = this.ToggleRowAsync(item); }
            });

    [Parameter] public RenderFragment? ToolbarContent { get; set; }

    // ---- Inline editing ----
    [Parameter] public DataGridEditMode EditMode { get; set; } = DataGridEditMode.None;
    [Parameter] public DataGridEditTrigger EditTrigger { get; set; } = DataGridEditTrigger.OnRowClick;
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public EventCallback<TItem> StartedEditingItem { get; set; }
    [Parameter] public EventCallback<TItem> CommittedItemChanges { get; set; }
    [Parameter] public EventCallback<TItem> CanceledEditingItem { get; set; }

    TItem? editingItem;
    ColumnBase<TItem>? editingCell;
    readonly Dictionary<string, object?> editValues = new();

    internal bool EditingEnabled => this.EditMode != DataGridEditMode.None && !this.ReadOnly;

    internal bool EffectiveEditable(ColumnBase<TItem> col)
        => this.EditingEnabled && (col.Editable ?? col.HasValue) && col.HasValue;

    internal bool IsEditingRow(TItem item) => this.editingItem is not null && EqualityComparer<TItem>.Default.Equals(this.editingItem, item);

    internal bool IsEditingCell(TItem item, ColumnBase<TItem> col)
        => this.IsEditingRow(item) && ReferenceEquals(this.editingCell, col);

    internal object? GetEditValue(ColumnBase<TItem> col) => this.editValues.TryGetValue(col.Id, out var v) ? v : null;

    internal void SetEditValue(ColumnBase<TItem> col, object? value) => this.editValues[col.Id] = value;

    void BeginEdit(TItem item)
    {
        this.editingItem = item;
        this.editValues.Clear();
        foreach (var col in this.columns.Where(c => c.HasValue))
            this.editValues[col.Id] = col.GetValue(item);
    }

    internal async Task StartEditRowAsync(TItem item)
    {
        if (!this.EditingEnabled)
            return;
        this.BeginEdit(item);
        this.editingCell = null;
        await this.StartedEditingItem.InvokeAsync(item);
        this.StateHasChanged();
    }

    internal async Task StartEditCellAsync(TItem item, ColumnBase<TItem> col)
    {
        if (!this.EffectiveEditable(col))
            return;
        this.BeginEdit(item);
        this.editingCell = col;
        await this.StartedEditingItem.InvokeAsync(item);
        this.StateHasChanged();
    }

    internal async Task CommitEditAsync()
    {
        if (this.editingItem is null)
            return;
        var item = this.editingItem;

        foreach (var col in this.columns.Where(this.EffectiveEditable))
        {
            if (this.editValues.TryGetValue(col.Id, out var v))
                col.SetValue(item, v);
        }

        this.editingItem = default;
        this.editingCell = null;
        await this.CommittedItemChanges.InvokeAsync(item);
        await this.ReloadServerDataAsync();
        this.StateHasChanged();
    }

    internal async Task CancelEditAsync()
    {
        var item = this.editingItem;
        this.editingItem = default;
        this.editingCell = null;
        this.editValues.Clear();
        if (item is not null)
            await this.CanceledEditingItem.InvokeAsync(item);
        this.StateHasChanged();
    }

    // ---- Grouping ----
    [Parameter] public bool Groupable { get; set; }

    string? groupColumnId;
    readonly HashSet<object> collapsedGroups = new();

    internal bool IsGrouped => this.groupColumnId is not null;

    internal bool EffectiveGroupable(ColumnBase<TItem> col)
        => this.Groupable && (col.Groupable ?? col.HasValue) && col.HasValue;

    internal bool IsGroupedBy(ColumnBase<TItem> col) => this.groupColumnId == col.Id;

    internal void ToggleGroupBy(ColumnBase<TItem> col)
    {
        this.groupColumnId = this.groupColumnId == col.Id ? null : col.Id;
        this.collapsedGroups.Clear();
        this.StateHasChanged();
    }

    internal ColumnBase<TItem>? GroupColumn
        => this.groupColumnId is null ? null : this.columns.FirstOrDefault(c => c.Id == this.groupColumnId);

    internal IReadOnlyList<(object? Key, IReadOnlyList<TItem> Items)> GetGroups()
    {
        var col = this.GroupColumn;
        if (col is null)
            return Array.Empty<(object?, IReadOnlyList<TItem>)>();

        return this.ProcessedItems()
            .GroupBy(col.GetValue)
            .Select(g => (g.Key, (IReadOnlyList<TItem>)g.ToList()))
            .ToList();
    }

    internal bool IsGroupCollapsed(object? key) => key is not null && this.collapsedGroups.Contains(key);

    internal void ToggleGroupCollapse(object? key)
    {
        if (key is null)
            return;
        if (!this.collapsedGroups.Add(key))
            this.collapsedGroups.Remove(key);
        this.StateHasChanged();
    }

    internal string GroupAggregateText(ColumnBase<TItem> col, IReadOnlyList<TItem> items)
        => col.Aggregate is null ? string.Empty : ComputeAggregate(col, items);

    internal Task OnHeaderClickAsync(ColumnBase<TItem> col, bool sortable)
        => sortable ? this.ToggleSortAsync(col) : Task.CompletedTask;

    internal string ComputeAggregateText(ColumnBase<TItem> col)
        => ComputeAggregate(col, this.ProcessedItems());

    internal static string ComputeAggregate(ColumnBase<TItem> col, IReadOnlyList<TItem> items)
    {
        var agg = col.Aggregate;
        if (agg is null)
            return string.Empty;

        if (agg.Type == DataGridAggregateType.Custom)
            return agg.CustomAggregate?.Invoke(items) ?? string.Empty;

        double result;
        if (agg.Type == DataGridAggregateType.Count)
        {
            result = items.Count;
        }
        else
        {
            var nums = items
                .Select(i => ToDouble(col.GetValue(i)))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            result = agg.Type switch
            {
                DataGridAggregateType.Sum => nums.Sum(),
                DataGridAggregateType.Average => nums.Count > 0 ? nums.Average() : 0,
                DataGridAggregateType.Min => nums.Count > 0 ? nums.Min() : 0,
                DataGridAggregateType.Max => nums.Count > 0 ? nums.Max() : 0,
                _ => 0
            };
        }

        return agg.DisplayTemplate?.Invoke(result)
            ?? result.ToString(agg.Format, System.Globalization.CultureInfo.CurrentCulture);
    }

    static double? ToDouble(object? value)
    {
        if (value is null)
            return null;
        try
        {
            return Convert.ToDouble(value, System.Globalization.CultureInfo.CurrentCulture);
        }
        catch
        {
            return null;
        }
    }

    string RootCssClass
    {
        get
        {
            var sb = new System.Text.StringBuilder("shiny-dg");
            if (this.Dense) sb.Append(" shiny-dg-dense");
            if (this.Striped) sb.Append(" shiny-dg-striped");
            if (this.Bordered) sb.Append(" shiny-dg-bordered");
            if (this.Hover) sb.Append(" shiny-dg-hover");
            if (this.Outlined) sb.Append(" shiny-dg-outlined");
            if (this.FixedHeader) sb.Append(" shiny-dg-fixedheader");
            // Sticky cells lose their borders under border-collapse:collapse, so the sticky
            // variants of the table only switch on when something is actually pinned.
            if (this.FixedHeader || this.HasFrozenColumns) sb.Append(" shiny-dg-sticky");
            if (!string.IsNullOrEmpty(this.Class)) sb.Append(' ').Append(this.Class);
            return sb.ToString();
        }
    }

    // The height caps the scroller (.shiny-dg-scroll), not the root: position:sticky only engages
    // against the ancestor that actually scrolls, and capping the root leaves the scroller unbounded.
    string? RootStyle
        => string.IsNullOrEmpty(this.Height) ? this.Style : $"--shiny-dg-height:{this.Height};{this.Style}";

    internal int ColSpan => this.VisibleColumns.Count + (this.HasMultiSelect ? 1 : 0);

    internal bool HasFooter => this.VisibleColumns.Any(c => c.FooterTemplate is not null || c.Aggregate is not null);

    string? ColumnWidthStyle(ColumnBase<TItem> col)
    {
        var width = this.columnWidths.TryGetValue(col.Id, out var w)
            ? $"width:{w};min-width:{w};max-width:{w};"
            : string.IsNullOrEmpty(col.Width) ? null : $"width:{col.Width};";

        return width + this.FrozenOffsetStyle(col);
    }

    string HeaderCssClass(ColumnBase<TItem> col)
        => "shiny-dg-header" + this.FrozenCssClass(col);

    string RowCssClass(TItem item)
    {
        var sb = new System.Text.StringBuilder("shiny-dg-row");
        if (this.IsSelected(item)) sb.Append(" shiny-dg-selected");
        if (this.SelectionMode != DataGridSelectionMode.None || this.RowClick.HasDelegate)
            sb.Append(" shiny-dg-clickable");
        return sb.ToString();
    }
}
