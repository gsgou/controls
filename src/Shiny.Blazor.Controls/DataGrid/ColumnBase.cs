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
    [Parameter] public bool? Resizable { get; set; }

    /// <summary>CSS width, e.g. <c>"120px"</c> or <c>"20%"</c>.</summary>
    [Parameter] public string? Width { get; set; }
    [Parameter] public bool StickyLeft { get; set; }
    [Parameter] public bool StickyRight { get; set; }

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

    /// <summary>Writes a value back (inline editing). No-op when the column isn't bound to a property.</summary>
    internal virtual void SetValue(TItem item, object? value) { }

    /// <summary>CLR type of the column value — drives filter operators and editor selection.</summary>
    internal virtual Type GetDataType() => typeof(string);

    /// <summary>True when this column can sort/filter/group/edit by value (false for template-only columns).</summary>
    internal virtual bool HasValue => true;

    bool registered;

    protected override void OnInitialized()
    {
        this.Grid?.AddColumn(this);
        this.registered = true;
    }

    protected override void OnParametersSet()
    {
        if (this.registered)
            this.Grid?.NotifyColumnsChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Columns are declarative metadata; the grid renders cells. Nothing to render here.
    }

    public void Dispose() => this.Grid?.RemoveColumn(this);
}
