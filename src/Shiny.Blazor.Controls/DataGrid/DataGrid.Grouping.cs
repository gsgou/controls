using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Grouping (any number of nested levels) and summary (total) rows.
/// </summary>
/// <remarks>
/// <see cref="DataGrid{TItem}.GroupBy"/> is the whole grouping state: column ids, outermost first. The
/// header's ⊞ button appends to (or removes from) that same list, so interactive grouping and a bound
/// <c>GroupBy</c> are never two competing sources of truth.
/// <para>
/// One set of <c>SummaryRows</c> declarations serves the grid's <c>tfoot</c> <b>and</b> every group -
/// see <see cref="DataGrid{TItem}.GroupSummaryPlacement"/>. A row that declares no cell for a column
/// leaves it blank, which is what lets "Total" sit in one column and the number in the next.
/// </para>
/// </remarks>
public partial class DataGrid<TItem>
{
    /// <summary>How far each nesting level indents its group header.</summary>
    internal const double GroupIndentSize = 18;

    /// <summary>Separates one level's key from the next in a group path - never appears in a key.</summary>
    const char GroupPathSeparator = '\u001f';

    readonly List<string> groupBy = new();
    readonly List<SummaryRow<TItem>> summaryRows = new();
    readonly HashSet<string> toggledGroups = new();

    IReadOnlyList<string>? lastGroupByParam;
    bool? lastInitiallyExpanded;

    /// <summary>Set by Expand/CollapseAllGroups, which move the default rather than listing paths.</summary>
    bool? groupExpandedOverride;
    SummaryRow<TItem>? implicitSummaryRow;
    bool implicitSummaryBuilt;

    // ---- Parameters ----

    /// <summary>Lets the user add/remove grouping levels from the column headers.</summary>
    [Parameter] public bool Groupable { get; set; }

    /// <summary>
    /// The columns to group by, outermost first - each entry a column's property name (or Title).
    /// Grouping is on whenever this has an entry; <see cref="Groupable"/> only controls whether the
    /// user may change it. Supports <c>@bind-GroupBy</c>.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? GroupBy { get; set; }

    [Parameter] public EventCallback<IReadOnlyList<string>> GroupByChanged { get; set; }

    /// <summary>Where each group's summary rows sit. Defaults to a footer under the group's rows.</summary>
    [Parameter] public DataGridGroupSummaryPlacement GroupSummaryPlacement { get; set; } = DataGridGroupSummaryPlacement.Footer;

    /// <summary>Whether a group starts expanded.</summary>
    [Parameter] public bool GroupsInitiallyExpanded { get; set; } = true;

    /// <summary>
    /// How groups are ordered among themselves. <c>None</c> leaves them in row order, which is what a
    /// sort on the grouped column already gives you.
    /// </summary>
    [Parameter] public DataGridSortDirection GroupSortDirection { get; set; } = DataGridSortDirection.Ascending;

    /// <summary>Replaces the default group header content (the caret is still supplied).</summary>
    [Parameter] public RenderFragment<DataGridGroupInfo<TItem>>? GroupHeaderTemplate { get; set; }

    /// <summary>The summary row declarations - <see cref="SummaryRow{TItem}"/> children.</summary>
    [Parameter] public RenderFragment? SummaryRows { get; set; }

    // ---- Grouping state ----

    internal bool IsGrouped => this.GroupColumns.Count > 0;

    internal IReadOnlyList<ColumnBase<TItem>> GroupColumns
    {
        get
        {
            if (this.groupBy.Count == 0)
                return Array.Empty<ColumnBase<TItem>>();

            var cols = new List<ColumnBase<TItem>>(this.groupBy.Count);
            foreach (var id in this.groupBy)
            {
                var col = this.FindColumn(id);
                if (col is not null && col.HasValue && !cols.Contains(col))
                    cols.Add(col);
            }
            return cols;
        }
    }

    ColumnBase<TItem>? FindColumn(string? id)
        => string.IsNullOrEmpty(id)
            ? null
            : this.columns.FirstOrDefault(c =>
                string.Equals(c.Id, id, StringComparison.Ordinal) ||
                string.Equals(c.Title, id, StringComparison.Ordinal));

    /// <summary>
    /// Pulls a changed <c>GroupBy</c> parameter in. Compared by content against the last value we
    /// were handed, not against our own list: an interactive toggle on an unbound grid must not be
    /// undone by the next render replaying the original parameter.
    /// </summary>
    void SyncGroupByParameter()
    {
        if (this.lastInitiallyExpanded != this.GroupsInitiallyExpanded)
        {
            this.lastInitiallyExpanded = this.GroupsInitiallyExpanded;
            this.ResetGroupState();
        }

        if (this.GroupBy is null)
            return;

        if (this.lastGroupByParam is not null && this.GroupBy.SequenceEqual(this.lastGroupByParam, StringComparer.Ordinal))
            return;

        this.lastGroupByParam = this.GroupBy.ToList();
        this.groupBy.Clear();
        this.groupBy.AddRange(this.GroupBy);
        this.ResetGroupState();
    }

    void ResetGroupState()
    {
        this.groupExpandedOverride = null;
        this.toggledGroups.Clear();
    }

    internal bool EffectiveGroupable(ColumnBase<TItem> col)
        => this.Groupable && (col.Groupable ?? col.HasValue) && col.HasValue;

    internal bool IsGroupedBy(ColumnBase<TItem> col) => this.GroupLevelOf(col) >= 0;

    /// <summary>The 0-based grouping level this column sits at, or -1 when it is not grouped by.</summary>
    internal int GroupLevelOf(ColumnBase<TItem> col)
    {
        var cols = this.GroupColumns;
        for (var i = 0; i < cols.Count; i++)
        {
            if (ReferenceEquals(cols[i], col))
                return i;
        }
        return -1;
    }

    /// <summary>The ⊞ glyph - numbered once more than one level is in play.</summary>
    internal string GroupGlyph(ColumnBase<TItem> col)
    {
        var level = this.GroupLevelOf(col);
        return level >= 0 && this.GroupColumns.Count > 1 ? $"⊞{level + 1}" : "⊞";
    }

    /// <summary>Adds this column as the innermost grouping level, or removes it if it is already one.</summary>
    internal Task ToggleGroupByAsync(ColumnBase<TItem> col)
    {
        var existing = this.groupBy.FirstOrDefault(id => ReferenceEquals(this.FindColumn(id), col));
        if (existing is not null)
            this.groupBy.Remove(existing);
        else
            this.groupBy.Add(col.Id);

        return this.CommitGroupByAsync();
    }

    /// <summary>Drops every grouping level.</summary>
    public Task ClearGroupingAsync()
    {
        if (this.groupBy.Count == 0)
            return Task.CompletedTask;

        this.groupBy.Clear();
        return this.CommitGroupByAsync();
    }

    async Task CommitGroupByAsync()
    {
        this.ResetGroupState();
        this.lastGroupByParam = this.groupBy.ToList();
        await this.GroupByChanged.InvokeAsync(this.groupBy.ToList());
        this.StateHasChanged();
    }

    public void ExpandAllGroups() => this.SetAllGroups(expanded: true);

    public void CollapseAllGroups() => this.SetAllGroups(expanded: false);

    /// <summary>
    /// Moves the default rather than listing paths. Listing them could only reach the groups currently
    /// rendered - a nested group inside a collapsed one is not in the tree, so "collapse all" would
    /// leave it expanded, ready to spring open the moment its parent was reopened.
    /// </summary>
    void SetAllGroups(bool expanded)
    {
        this.groupExpandedOverride = expanded;
        this.toggledGroups.Clear();
        this.StateHasChanged();
    }

    bool GroupDefaultExpanded => this.groupExpandedOverride ?? this.GroupsInitiallyExpanded;

    internal bool IsGroupCollapsed(string path)
        => this.GroupDefaultExpanded ? this.toggledGroups.Contains(path) : !this.toggledGroups.Contains(path);

    internal void ToggleGroupCollapse(string path)
    {
        if (!this.toggledGroups.Add(path))
            this.toggledGroups.Remove(path);
        this.StateHasChanged();
    }

    /// <summary>
    /// The grouped body, flattened: a header, then either the next level's headers or a run of rows,
    /// then the group's summary rows. Flat because a nested render tree would need recursion in
    /// markup and buys nothing - the indent is a style, not a DOM nesting.
    /// </summary>
    internal IReadOnlyList<GroupRenderNode<TItem>> GroupRenderNodes()
    {
        var nodes = new List<GroupRenderNode<TItem>>();
        var cols = this.GroupColumns;
        if (cols.Count > 0)
            this.AppendGroupNodes(nodes, this.ProcessedItems(), 0, string.Empty);

        return nodes;
    }

    void AppendGroupNodes(List<GroupRenderNode<TItem>> nodes, IReadOnlyList<TItem> items, int level, string parentPath)
    {
        var cols = this.GroupColumns;
        var col = cols[level];

        foreach (var group in this.OrderGroups(items.GroupBy(col.GetValue)))
        {
            var groupItems = (IReadOnlyList<TItem>)group.ToList();
            var keyText = col.FormatValue(group.Key);
            var path = parentPath + GroupPathSeparator + (keyText ?? group.Key?.ToString() ?? "\u0000");
            var collapsed = this.IsGroupCollapsed(path);

            var info = new DataGridGroupInfo<TItem>(
                group.Key,
                keyText,
                col.HeaderText,
                groupItems,
                level,
                path,
                !collapsed
            );
            nodes.Add(new GroupRenderNode<TItem> { Header = info, Level = level });

            if (this.GroupSummaryPlacement is DataGridGroupSummaryPlacement.Header or DataGridGroupSummaryPlacement.Both)
                AppendSummaries(info);

            if (collapsed)
                continue;

            if (level + 1 < cols.Count)
                this.AppendGroupNodes(nodes, groupItems, level + 1, path);
            else
                nodes.Add(new GroupRenderNode<TItem> { Rows = groupItems, Level = level });

            if (this.GroupSummaryPlacement is DataGridGroupSummaryPlacement.Footer or DataGridGroupSummaryPlacement.Both)
                AppendSummaries(info);
        }

        void AppendSummaries(DataGridGroupInfo<TItem> info)
        {
            foreach (var row in this.EffectiveSummaryRows(group: true))
                nodes.Add(new GroupRenderNode<TItem> { Summary = row, Group = info, Level = info.Level });
        }
    }

    IEnumerable<IGrouping<object?, TItem>> OrderGroups(IEnumerable<IGrouping<object?, TItem>> groups)
        => this.GroupSortDirection switch
        {
            DataGridSortDirection.Ascending => groups.OrderBy(g => g.Key, DataGridValueComparer.Instance),
            DataGridSortDirection.Descending => groups.OrderByDescending(g => g.Key, DataGridValueComparer.Instance),
            _ => groups
        };

    // ---- Summary rows ----

    internal void AddSummaryRow(SummaryRow<TItem> row)
    {
        if (!this.summaryRows.Contains(row))
        {
            this.summaryRows.Add(row);
            this.StateHasChanged();
        }
    }

    internal void RemoveSummaryRow(SummaryRow<TItem> row)
    {
        if (this.summaryRows.Remove(row))
            this.StateHasChanged();
    }

    internal void NotifySummaryChanged() => this.StateHasChanged();

    /// <summary>
    /// The rows to render for one scope. With nothing declared, the legacy per-column
    /// <c>Aggregate</c>/<c>FooterTemplate</c> still produce the single footer row they always did.
    /// </summary>
    internal IReadOnlyList<SummaryRow<TItem>> EffectiveSummaryRows(bool group)
    {
        if (this.summaryRows.Count > 0)
            return this.summaryRows.Where(r => r.AppliesTo(group)).ToList();

        if (!this.implicitSummaryBuilt)
        {
            this.implicitSummaryRow = this.BuildImplicitSummaryRow();
            this.implicitSummaryBuilt = true;
        }

        return this.implicitSummaryRow is null
            ? Array.Empty<SummaryRow<TItem>>()
            : new[] { this.implicitSummaryRow };
    }

    /// <summary>Column-level aggregates can change with any render, so the synthesized row is rebuilt
    /// with the component rather than cached for the life of the grid.</summary>
    void InvalidateImplicitSummaryRow()
    {
        this.implicitSummaryRow = null;
        this.implicitSummaryBuilt = false;
    }

    SummaryRow<TItem>? BuildImplicitSummaryRow()
    {
        var cols = this.VisibleColumns
            .Where(c => c.Aggregate is not null || c.FooterTemplate is not null)
            .ToList();

        if (cols.Count == 0)
            return null;

        var row = new SummaryRow<TItem>();
        foreach (var col in cols)
        {
            row.AddCell(new SummaryCell<TItem>
            {
                Column = col.Id,
                Definition = col.Aggregate,
                LegacyTemplate = col.FooterTemplate
            });
        }
        return row;
    }

    internal bool HasFooter => this.EffectiveSummaryRows(group: false).Count > 0;

    /// <summary>The content of one summary slot: a template, a legacy column footer, or the text.</summary>
    internal RenderFragment SummaryCellContent(
        SummaryRow<TItem> row,
        ColumnBase<TItem> col,
        IReadOnlyList<TItem> items,
        DataGridGroupInfo<TItem>? group
    )
    {
        var cell = row.CellFor(col);
        if (cell is null)
            return _ => { };

        if (cell.ChildContent is not null)
        {
            var context = new SummaryContext<TItem>(items, group is not null, group?.Key, group?.KeyText, group?.Level ?? 0);
            return cell.ChildContent(context);
        }

        if (cell.LegacyTemplate is not null)
            return cell.LegacyTemplate;

        var text = cell.ComputeText(col, items);
        return builder => builder.AddContent(0, text);
    }

    internal string SummaryCellCssClass(SummaryRow<TItem> row, ColumnBase<TItem> col)
    {
        var cell = row.CellFor(col);
        var alignment = cell?.EffectiveAlignment(col) ?? col.EffectiveAlignment;
        return "shiny-dg-footer-cell"
            + this.FrozenCssClass(col)
            + AlignCssClass(alignment)
            + (cell?.Bold == false ? " shiny-dg-summary-plain" : null)
            + (string.IsNullOrWhiteSpace(cell?.Class) ? null : " " + cell!.Class!.Trim());
    }

    internal static string SummaryRowCssClass(SummaryRow<TItem> row, bool group)
        => (group ? "shiny-dg-group-footer" : "shiny-dg-summary-row")
            + (string.IsNullOrWhiteSpace(row.Class) ? null : " " + row.Class!.Trim());
}


/// <summary>One group as the grid sees it. Also the context of <c>DataGrid.GroupHeaderTemplate</c>.</summary>
public sealed class DataGridGroupInfo<TItem>
{
    internal DataGridGroupInfo(
        object? key,
        string? keyText,
        string title,
        IReadOnlyList<TItem> items,
        int level,
        string path,
        bool isExpanded
    )
    {
        this.Key = key;
        this.KeyText = keyText;
        this.Title = title;
        this.Items = items;
        this.Level = level;
        this.Path = path;
        this.IsExpanded = isExpanded;
    }

    public object? Key { get; }

    /// <summary>The key run through the grouped column's formatting, so a header reads the same as
    /// the cells under it ("Salary: $45,000", not "Salary: 45000").</summary>
    public string? KeyText { get; }

    /// <summary>The grouped column's header text.</summary>
    public string Title { get; }

    /// <summary>The rows in this group, nested groups included.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>Nesting depth - 0 for the outermost grouping level.</summary>
    public int Level { get; }

    /// <summary>
    /// Identity of this group among its siblings <i>and</i> its ancestors. Collapse state is tracked
    /// by it, so two nested groups that happen to share a key stay independent.
    /// </summary>
    public string Path { get; }

    public bool IsExpanded { get; }

    public int Count => this.Items.Count;
}


/// <summary>One line of the flattened grouped body - a header, a run of rows, or a summary row.</summary>
public sealed class GroupRenderNode<TItem>
{
    /// <summary>Set for a group title row.</summary>
    public DataGridGroupInfo<TItem>? Header { get; init; }

    /// <summary>Set for a summary row; <see cref="Group"/> says which group it totals.</summary>
    public SummaryRow<TItem>? Summary { get; init; }

    public DataGridGroupInfo<TItem>? Group { get; init; }

    /// <summary>Set for a run of data rows.</summary>
    public IReadOnlyList<TItem>? Rows { get; init; }

    public int Level { get; init; }
}
