using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Base class for DataGrid columns. Concrete columns are <see cref="PropertyColumn{TItem, TProperty}"/>
/// and <see cref="TemplateColumn{TItem}"/>. A column renders nothing itself — it registers with the
/// parent <see cref="DataGrid{TItem}"/> which reads its parameters to render header/cells/footer.
/// </summary>
public abstract class ColumnBase<TItem> : ComponentBase, IDisposable
{
    string? id;

    [CascadingParameter] internal DataGrid<TItem> Grid { get; set; } = default!;

    [Parameter] public string? Title { get; set; }
    [Parameter] public bool? Sortable { get; set; }
    [Parameter] public bool? Filterable { get; set; }
    [Parameter] public bool? Groupable { get; set; }
    [Parameter] public bool? Editable { get; set; }
    [Parameter] public bool Hidden { get; set; }
    /// <summary>
    /// Whether a resize handle is offered for this column. Only has an effect when the grid sets
    /// <see cref="DataGrid{TItem}.ColumnResizeMode"/>. Defaults to true.
    /// </summary>
    [Parameter] public bool? Resizable { get; set; }

    /// <summary>CSS width, e.g. <c>"120px"</c> or <c>"20%"</c>.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>
    /// CSS <c>min-width</c> for this column, e.g. <c>"80px"</c>. Falls back to
    /// <see cref="DataGrid{TItem}.MinColumnWidth"/>. An absolute pixel value also floors interactive
    /// resizing; any other unit is emitted as CSS but leaves the drag clamped by the grid default,
    /// because a drag works in pixels and only the browser knows what a <c>%</c> or <c>em</c> is worth.
    /// </summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>
    /// CSS <c>max-width</c> for this column, e.g. <c>"400px"</c>. Falls back to
    /// <see cref="DataGrid{TItem}.MaxColumnWidth"/>, which is unbounded by default. Same pixel caveat
    /// as <see cref="MinWidth"/>.
    /// </summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>
    /// Freezes (pins) this column to the leading or trailing edge so it stays put while the grid
    /// scrolls horizontally. Only a contiguous run of columns at each edge can be frozen - see
    /// <see cref="DataGrid{TItem}.FrozenColumns"/> / <see cref="DataGrid{TItem}.FrozenEndColumns"/>
    /// for the count-based form.
    /// </summary>
    [Parameter] public DataGridFrozen Frozen { get; set; }

    /// <summary>Legacy alias for <c>Frozen="DataGridFrozen.Start"</c>.</summary>
    [Parameter] public bool StickyLeft { get; set; }

    /// <summary>Legacy alias for <c>Frozen="DataGridFrozen.End"</c>.</summary>
    [Parameter] public bool StickyRight { get; set; }

    /// <summary><see cref="Frozen"/> with the legacy sticky flags folded in.</summary>
    internal DataGridFrozen EffectiveFrozen
        => this.Frozen != DataGridFrozen.None ? this.Frozen
            : this.StickyLeft ? DataGridFrozen.Start
            : this.StickyRight ? DataGridFrozen.End
            : DataGridFrozen.None;

    /// <summary>Horizontal alignment of this column's cells and footer. <c>Auto</c> right-aligns quantities.</summary>
    [Parameter] public DataGridCellAlignment Alignment { get; set; }

    /// <summary>Horizontal alignment of the header. <c>Auto</c> follows <see cref="Alignment"/> so the header sits over its own values.</summary>
    [Parameter] public DataGridCellAlignment HeaderAlignment { get; set; }

    /// <summary>Let cell text wrap instead of truncating on one line. Pair with <see cref="MaxLines"/> to cap the height.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Maximum wrapped lines before an ellipsis. <c>0</c> means unlimited; only meaningful with <see cref="Wrap"/>.</summary>
    [Parameter] public int MaxLines { get; set; }

    /// <summary>
    /// Per-cell colour/weight driven by the row item, e.g. red negatives or an amber "overdue" cell.
    /// Return <c>null</c> (or a <see cref="DataGridCellStyle"/> with null members) to keep the themed default.
    /// </summary>
    [Parameter] public Func<TItem, DataGridCellStyle?>? CellStyle { get; set; }

    /// <summary>
    /// Highlights the whole column - a fill, a stroke, or both, applied to every one of its cells.
    /// Row-scoped and cell-scoped highlights are laid over it, and the column's own
    /// <see cref="CellStyle"/> wins over all of them.
    /// </summary>
    [Parameter] public DataGridCellStyle? Highlight { get; set; }

    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment<CellContext<TItem>>? CellTemplate { get; set; }
    [Parameter] public RenderFragment<CellContext<TItem>>? EditTemplate { get; set; }
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>Footer/group aggregate for this column.</summary>
    [Parameter] public AggregateDefinition<TItem>? Aggregate { get; set; }

    /// <summary>Optional custom value comparer for sorting.</summary>
    [Parameter] public IComparer<object?>? Comparer { get; set; }

    /// <summary>Stable identity used in sort/filter/group state.</summary>
    internal string Id => this.id ??= this.ComputeId();

    protected abstract string ComputeId();

    /// <summary>The text shown in the column header (explicit Title, else a derived name).</summary>
    internal virtual string HeaderText => this.Title ?? string.Empty;

    /// <summary>The raw value for sorting/filtering/grouping/aggregation. Null for template-only columns.</summary>
    internal abstract object? GetValue(TItem item);

    /// <summary>The display text for the default cell rendering.</summary>
    internal abstract string? GetText(TItem item);

    /// <summary>
    /// Formats a bare value the way this column's cells are formatted. Used for group headers, so a
    /// header reads the same as the cells under it ("Salary: $45,000", not "Salary: 45000").
    /// </summary>
    internal virtual string? FormatValue(object? value) => value?.ToString();

    /// <summary>Writes a value back (inline editing). No-op when the column isn't bound to a property.</summary>
    internal virtual void SetValue(TItem item, object? value) { }

    /// <summary>CLR type of the column value — drives filter operators and editor selection.</summary>
    internal virtual Type GetDataType() => typeof(string);

    /// <summary>True when this column can sort/filter/group/edit by value (false for template-only columns).</summary>
    internal virtual bool HasValue => true;

    /// <summary>The column's display preset, if it has one. Drives <see cref="DataGridCellAlignment.Auto"/>.</summary>
    internal virtual DataGridColumnFormat DisplayFormat => DataGridColumnFormat.None;

    /// <summary>Resolves <c>Auto</c> against the preset and CLR type - quantities right, everything else left.</summary>
    internal DataGridCellAlignment EffectiveAlignment
        => this.Alignment != DataGridCellAlignment.Auto ? this.Alignment
            : this.HasValue && DataGridValueFormatter.IsNumericAlignment(this.DisplayFormat, this.GetDataType())
                ? DataGridCellAlignment.End
                : DataGridCellAlignment.Start;

    /// <summary>Header alignment - <c>Auto</c> follows the cells so a header sits over its own values.</summary>
    internal DataGridCellAlignment EffectiveHeaderAlignment
        => this.HeaderAlignment != DataGridCellAlignment.Auto ? this.HeaderAlignment : this.EffectiveAlignment;

    bool registered;

    // Snapshot of the stable (non-delegate) params that affect how the grid lays the column out. We only
    // re-notify the grid when one of these actually changes — notifying on every OnParametersSet would loop:
    // the grid's StateHasChanged re-renders this column, re-firing OnParametersSet, ad infinitum. Template /
    // comparer / aggregate params are excluded on purpose — they get a fresh delegate identity on every parent
    // render, so including them would re-introduce the loop (and the grid re-renders them as part of its tree).
    (string?, bool?, bool?, bool?, bool?, bool, bool?, string?, string?, string?, bool, bool, DataGridFrozen,
        DataGridCellAlignment, DataGridCellAlignment, bool, int) layoutSnapshot;
    bool hasSnapshot;

    protected override void OnInitialized()
    {
        this.Grid?.AddColumn(this);
        this.registered = true;
    }

    protected override void OnParametersSet()
    {
        if (!this.registered)
            return;

        var snapshot = (this.Title, this.Sortable, this.Filterable, this.Groupable, this.Editable,
            this.Hidden, this.Resizable, this.Width, this.MinWidth, this.MaxWidth,
            this.StickyLeft, this.StickyRight, this.Frozen,
            this.Alignment, this.HeaderAlignment, this.Wrap, this.MaxLines);

        if (!this.hasSnapshot || !snapshot.Equals(this.layoutSnapshot))
        {
            this.layoutSnapshot = snapshot;
            this.hasSnapshot = true;
            this.Grid?.NotifyColumnsChanged();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Columns are declarative metadata; the grid renders cells. Nothing to render here.
    }

    public void Dispose() => this.Grid?.RemoveColumn(this);
}
