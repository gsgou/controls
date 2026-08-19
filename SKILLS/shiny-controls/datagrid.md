# DataGrid

A feature-rich data grid for **both MAUI and Blazor**, modeled on MudBlazor's `MudDataGrid`. Blazor
renders a semantic HTML `<table>` via a generic `DataGrid<TItem>`; MAUI is a **pure cross-platform
composite** (a `Grid` header over a virtualized `CollectionView` — no native handlers), so it looks and
behaves the same on iOS/Android/Windows/Mac.

Feature surface (both hosts): typed columns (`PropertyColumn` + `TemplateColumn`), sorting (single +
multi), column **filtering** (menu / row / toolbar quick-search), **grouping** with expandable groups,
footer & group **aggregates**, single/multi **selection** with checkboxes, inline **editing**
(cell + form), **detail ("breakdown") rows**, a **tree/hierarchy mode** (`TreeDataGrid`) with lazy child
loading, **paging**, **virtualization**, column **resize / reorder**, **frozen columns** and a
frozen (sticky) header, loading + empty states, a `ServerData` delegate, and density/striped/bordered/hover styling. Colors follow the
theme tokens (`var(--shiny-color-*)` on Blazor, `ShinyThemeKeys.Color.*` on MAUI).

## Blazor

`@typeparam TItem`; columns are child components inside `<Columns>`. `TItem` cascades to columns, so
`PropertyColumn` only needs `Property`.

```razor
<DataGrid TItem="Person" Items="people"
          SelectionMode="DataGridSelectionMode.Multiple"
          SortMode="DataGridSortMode.Multiple"
          FilterMode="DataGridFilterMode.Menu"
          Groupable="true"
          EditMode="DataGridEditMode.Form"
          Dense="true" Striped="true" Hover="true" Bordered="true"
          FixedHeader="true" Height="420px"
          FrozenColumns="1" FrozenEndColumns="1"
          ColumnResizeMode="DataGridColumnResizeMode.Column"
          DragDropColumnReordering="true"
          CommittedItemChanges="OnSaved">
    <Columns>
        <PropertyColumn Property="x => x.FirstName" Title="First" />
        <PropertyColumn Property="x => x.Age" Format="N0" />
        <PropertyColumn Property="x => x.Salary" Format="C0">
            <FooterTemplate>Total: @people.Sum(p => p.Salary).ToString("C0")</FooterTemplate>
        </PropertyColumn>
        <TemplateColumn Title="Status" Sortable="false" Filterable="false">
            <CellTemplate>
                <Pill Text="@(context.Item.Active ? "Active" : "Inactive")"
                      Type="@(context.Item.Active ? PillType.Success : PillType.Caution)" />
            </CellTemplate>
            <EditTemplate>
                <input @bind="context.Item.FirstName" />
            </EditTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <DataGridPager TItem="Person" />
    </PagerContent>
</DataGrid>
```

- **Columns**: `PropertyColumn<TItem,TProperty>` (`Property="x => x.Name"`, `Format`, derives Title) and
  `TemplateColumn<TItem>` (`CellTemplate`/`EditTemplate`/`HeaderTemplate`/`FooterTemplate` with
  `context.Item`). Per-column flags: `Sortable`, `Filterable`, `Groupable`, `Editable`, `Hidden`,
  `Width`, `Resizable`, `Frozen` (`DataGridFrozen.Start`/`End`; `StickyLeft`/`StickyRight` are legacy
  aliases), `Aggregate`.
- **Grid params**: `Items`, `ServerData` (`Func<GridState, Task<GridData<TItem>>>`), `SelectionMode`,
  `SelectedItem(s)`, `SortMode`, `FilterMode`, `QuickFilter`, `Groupable`, `Virtualize`, `EditMode`,
  `EditTrigger`, `ReadOnly`, `RowsPerPage`, `FixedHeader`, `Height`, `Dense`, `Striped`, `Bordered`,
  `Hover`, `Outlined`, `Loading`, `RowClick`, `StartedEditingItem`/`CommittedItemChanges`/
  `CanceledEditingItem`, `ColumnResizeMode`, `DragDropColumnReordering`, `ToolbarContent`,
  `NoRecordsText`/`NoRecordsContent`, `LoadingContent`.
- **Paging**: put `<DataGridPager TItem="..." />` in `<PagerContent>`.
- **Detail rows**: `<RowDetailTemplate>` (context is the item) adds a caret column at the leading edge.
  Grid params: `ExpandMode`, `ExpandOnRowClick`, `IsRowExpandable`, `ExpandedItems`(+`Changed`),
  `RowExpanded`/`RowCollapsed`. Methods: `ExpandRowAsync`/`CollapseRowAsync`/`ToggleExpandAsync`/
  `ExpandAllAsync`/`CollapseAllAsync`/`IsRowExpanded`/`InvalidateChildren`/`InvalidateRowDetail`.
- **Async detail**: `RowDetailLoader` (`Func<TItem, Task>`) + optional `<RowDetailLoadingTemplate>`,
  `RowDetailLoadFailed`. Read-only `IsBusy` (+ `IsBusyChanged`) and `IsRowBusy(item)`.
- **Tree mode**: `ChildrenSelector` (+ `ChildrenLoader`, `HasChildrenSelector`, `TreeIndentSize`,
  `ChildrenLoadFailed`) on `DataGrid` — or use `TreeDataGrid`, the same component under a clearer name.

```razor
<DataGrid TItem="Order" Items="orders" RowDetailLoader="LoadLinesAsync" IsBusyChanged="b => busy = b">
    <Columns>…</Columns>
    <RowDetailTemplate>
        @foreach (var line in lines[context.Id]) { <div>@line.Sku</div> }
    </RowDetailTemplate>
    <RowDetailLoadingTemplate><span class="shiny-dg-busy"></span> Loading…</RowDetailLoadingTemplate>
</DataGrid>
@code {
    Dictionary<int, List<Line>> lines = new();
    async Task LoadLinesAsync(Order o) => lines[o.Id] = await api.GetLinesAsync(o.Id);
}
```

```razor
<TreeDataGrid TItem="CostNode" Items="accounts"
              ChildrenSelector="n => n.Lazy ? null : n.Children"
              ChildrenLoader="LoadChildrenAsync"
              HasChildrenSelector="n => n.Lazy || n.Children.Count > 0"
              TreeIndentSize="18">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Account" />
        <PropertyColumn Property="x => x.Budget" Format="C0" />
    </Columns>
</TreeDataGrid>
```

## MAUI

`shiny:DataGrid` with `shiny:DataGridColumn` / `shiny:DataGridTemplateColumn` children (items are
`object`; no generics — XAML-friendly). Bind a column by `PropertyName`.

```xml
<shiny:DataGrid ItemsSource="{Binding People}"
                SelectionMode="Multiple"
                SortMode="Multiple"
                FilterMode="Menu"
                Groupable="True"
                PageSize="20"
                EditMode="Form"
                AllowColumnResize="True"
                AllowColumnReorder="True"
                HorizontalScroll="True"
                DefaultColumnWidth="140"
                FrozenColumns="1"
                Striped="True" Bordered="True">
    <shiny:DataGridColumn Title="First" PropertyName="FirstName" Width="*" />
    <shiny:DataGridColumn Title="Age" PropertyName="Age" Width="Auto" />
    <shiny:DataGridColumn Title="Salary" PropertyName="Salary" StringFormat="{}{0:C0}" Width="*">
        <shiny:DataGridColumn.Aggregate>
            <shiny:DataGridAggregateDefinition Type="Sum" Format="C0" />
        </shiny:DataGridColumn.Aggregate>
    </shiny:DataGridColumn>
    <shiny:DataGridTemplateColumn Title="Status" Width="110" Editable="False" Frozen="End">
        <shiny:DataGridTemplateColumn.CellTemplate>
            <DataTemplate><shiny:PillView Text="{Binding StatusText}" /></DataTemplate>
        </shiny:DataGridTemplateColumn.CellTemplate>
    </shiny:DataGridTemplateColumn>
</shiny:DataGrid>
```

- **Columns**: `DataGridColumn` (`PropertyName`, `Width` as `GridLength` star/auto/abs, `StringFormat`,
  `CellTemplate`/`HeaderTemplate`/`EditTemplate`/`FooterTemplate`, `Sortable`/`Filterable`/`Groupable`/
  `Editable`/`Resizable`/`IsVisible`, `Frozen`, `Aggregate`). `DataGridTemplateColumn` for custom-only cells.
  Cell templates bind to the data item directly (e.g. `{Binding StatusText}`).
- **Grid params**: `ItemsSource`, `ServerData`, `SelectionMode`, `SelectedItem`/`SelectedItems`,
  `SortMode`, `FilterMode`, `Groupable`, `PageSize` (0 = no paging), `EditMode`, `EditTrigger`,
  `ReadOnly`, `AllowColumnResize`, `AllowColumnReorder`, `HorizontalScroll`, `DefaultColumnWidth`,
  `FrozenColumns`/`FrozenEndColumns`, `Dense`, `Striped`, `Bordered`,
  `ShowColumnHeaders`, `IsLoading`, `EmptyText`, `RowHeight`, `SelectionChanged`/`SelectionChangedCommand`,
  `StartedEditingItem`/`CommittedItemChanges`/`CanceledEditingItem` events.
- **Detail rows**: `RowDetailTemplate` (a `DataTemplate` whose BindingContext is the row's item) adds a
  caret column at the leading edge. Also `ExpandMode`, `ExpandOnRowTap`, `IsRowExpandable`,
  `ExpandedItems`, `RowExpanded`/`RowCollapsed` events, and `ExpandRow`/`CollapseRow`/`ToggleRow`/
  `ExpandAll`/`CollapseAll`/`IsRowExpanded`/`InvalidateChildren`/`InvalidateRowDetail`.
- **Async detail**: `RowDetailLoader` (`Func<object, Task>`, a BindableProperty) + optional
  `RowDetailLoadingTemplate`, `RowDetailLoadFailed`. Read-only bindable `IsBusy` (+ `IsBusyChanged`
  event) and `IsRowBusy(item)`.
- **Tree mode**: `ChildrenSelector` (+ `ChildrenLoader`, `HasChildrenSelector`, `TreeIndentSize`,
  `ChildrenLoadFailed`) — or use `shiny:TreeDataGrid`, the same control under a clearer name. All three
  selectors are `BindableProperty`s, so a XAML page can bind them straight to a view model.

```xml
<shiny:TreeDataGrid ItemsSource="{Binding Accounts}"
                    ChildrenSelector="{Binding ChildrenSelector}"
                    ChildrenLoader="{Binding ChildrenLoader}"
                    HasChildrenSelector="{Binding HasChildrenSelector}"
                    TreeIndentSize="18">
    <shiny:DataGridColumn Title="Account" PropertyName="Name" Width="2*" />
    <shiny:DataGridColumn Title="Budget" PropertyName="Budget" StringFormat="{}{0:C0}" Width="1.2*" />
</shiny:TreeDataGrid>
```

## Behavior notes & platform nuances

- **Sorting**: click/tap a header to cycle asc → desc → none. In `Multiple` mode each header adds to the
  sort with an order badge.
- **Filtering**: `Menu` shows a per-column filter popup (type-aware operators); `Row` shows inline
  filter inputs under the header; `Toolbar` shows a single quick-search box that matches any column.
- **Editing**: Blazor `Cell` edits one cell on click (commit on blur/Enter, cancel on Escape); `Form`
  edits the whole row with Save/Cancel. MAUI uses **inline-row editing** (editors for editable columns +
  a Save/Cancel bar) for both modes — the touch-friendly model.
- **Reorder**: Blazor uses native HTML drag-and-drop on headers (`DragDropColumnReordering`); MAUI uses
  ‹ › reorder arrows on headers (`AllowColumnReorder`).
- **Virtualization**: Blazor opt-in via `Virtualize` (uses `<Virtualize>`, best with `FixedHeader`+`Height`,
  not combined with paging/grouping); MAUI gets it free from `CollectionView`.
- **Frozen header**: Blazor needs `FixedHeader="true"` **and** `Height` (the header sticks against the
  scroller, and without a capped height nothing scrolls). MAUI's header is always frozen — it sits in
  its own row above the `CollectionView`.
- **Frozen columns**: pin a contiguous run at each edge, either per-column (`Frozen="Start"` / `"End"`)
  or by count on the grid (`FrozenColumns` / `FrozenEndColumns`, which also pins the multi-select
  checkbox column). Only a leading/trailing run can be pinned; a `Frozen` column in the middle is
  ignored. Frozen cells paint an opaque background and sit above the scrolling ones.
  - **MAUI requires `HorizontalScroll="True"`** — without sideways scrolling there is nothing to pin
    against, so `Frozen` is a no-op. `HorizontalScroll` puts header, rows and footer in one scroller;
    star widths cannot survive its unbounded measure, so each one resolves to
    `DefaultColumnWidth` (150 by default) x its star factor.
  - Blazor needs no extra flag - the table scrolls sideways whenever the columns are wider than the
    grid. A declared non-percentage `Width` is emitted as `width` **and** `min-width`, because a table
    cell's `width` alone is only a suggestion: without it the browser compresses the columns to fit
    and nothing ever overflows (so nothing scrolls, and pinning has nothing to pin against). Use a `%`
    width when you *want* a column to shrink with the container. Give frozen columns an explicit px `Width` and the offsets are right on the first paint;
    otherwise a small JS module measures them after render.
- **Detail (breakdown) rows**: setting `RowDetailTemplate` adds a caret column at the leading edge and
  renders the template in a full-width row under whichever rows are expanded. `ExpandMode="Single"`
  keeps only one open. The detail content is pinned to the leading edge while the columns scroll
  sideways (MAUI translates the pane; Blazor uses `position: sticky`), so a breakdown never slides out
  of view. Expansion is keyed on the *data item*, so it survives sorting, filtering and paging.
- **Async loading**: both loaders put a **spinner in place of that row's caret** while they run — the
  caret is the button, so the progress goes on it. `RowDetailLoader` fetches whatever the breakdown
  needs the first time a row opens; the detail row shows `RowDetailLoadingTemplate` (default: a
  spinner) meanwhile, and `RowDetailTemplate` is **not built until the load completes**, so it can
  assume its data arrived. The loader returns **no value** — fill an observable property on the item
  (or a lookup keyed by it) and let the template bind to it, which keeps the template's context the
  item rather than a wrapper. Each item loads once; `InvalidateRowDetail(item)` refetches (immediately
  if that row is open). A throw collapses the row and raises `RowDetailLoadFailed`.
  - **`IsBusy`** is true while *any* children or detail load is in flight — a read-only bindable on
    MAUI, a property plus `IsBusyChanged` on Blazor. Bind a page-level indicator to it; per-row
    spinners are drawn either way. It is **not** `IsLoading`/`Loading`, which you set yourself to
    cover the grid while its own data loads. `IsRowBusy(item)` is the per-row form.
- **Tree mode**: hand the grid a `ChildrenSelector` and the first visible column grows an indent and a
  caret — no extra column. `ChildrenLoader` fetches a level on first expand (the caret becomes a
  spinner meanwhile) and caches it; **the selector gets first refusal**, so the loader only runs for items the
  selector returns `null` for and a tree can mix in-memory branches with fetched ones. Give
  `HasChildrenSelector` if you want leaves to render caret-free before anything is loaded.
  - Sorting and filtering apply **per level**, so children stay under their parent, and a row is kept
    when a *descendant* matches the filter — otherwise the match would be unreachable.
  - Paging pages the **roots**; footer aggregates are computed over the roots too.
  - Tree and `Groupable` are mutually exclusive — **grouping wins**.
  - `ExpandAll` opens every already-loaded level; it does not fetch, since a lazy tree's depth is
    unbounded.
- **AOT/trimming**: MAUI string-path value access uses reflection (annotated). For full trim/NativeAOT,
  set a column's `ValueGetter`/`ValueSetter`/`Comparer` to avoid reflection.

## Code Generation Guidance

- Prefer `PropertyColumn` (Blazor) / `DataGridColumn` with `PropertyName` (MAUI) for bound data; use
  `TemplateColumn`/`DataGridTemplateColumn` for custom cells, actions, or status badges.
- Enable only the features asked for (sorting/paging/filtering/grouping/editing) — they're independent
  toggles.
- Blazor paging needs `<PagerContent><DataGridPager TItem="..."/></PagerContent>`; MAUI paging is just
  `PageSize`.
- Reach for a **detail row** when the extra information is *about* the row (a breakdown, a chart, a form)
  and for **tree mode** when the extra rows are more of the same thing one level down. They compose —
  a tree row can also have a detail row — but pick the one that matches the data before using both.
- `TreeDataGrid` and `DataGrid` are the same type; prefer `TreeDataGrid` when the grid is hierarchical so
  the markup says so.
- Leave colors unset to inherit the theme; the grid is light/dark aware.
- **Budget columns to the width you actually have.** Header titles ellipsize and clip to their column
  (they no longer spill into the next one), but a phone-width grid only has room for roughly **3–4
  columns**, fewer once `AllowColumnResize`/`AllowColumnReorder`/`Groupable`/`FilterMode="Menu"` add
  their glyphs to each header. On narrow layouts prefer a handful of columns — or fold the extras into a
  single `DataGridTemplateColumn` — instead of declaring six and letting every cell render as `…`.
  A `DataGridColumn` with no `Width` is `*`, so stars split whatever the `Auto` columns leave behind.
