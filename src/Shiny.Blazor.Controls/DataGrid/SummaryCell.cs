using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Shiny.Blazor.Controls;

/// <summary>
/// One slot of a <see cref="SummaryRow{TItem}"/>. Set <see cref="Text"/> for a plain label (the
/// right-aligned "Total" that sits beside the number), <see cref="Aggregate"/> to compute over the
/// column's values, or <see cref="ChildContent"/> for anything else.
/// </summary>
public class SummaryCell<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] internal SummaryRow<TItem> Row { get; set; } = default!;

    /// <summary>The column this cell fills, by property name (or <c>Title</c> for a template column).</summary>
    [Parameter] public string? Column { get; set; }

    /// <summary>A literal label - "Total". Wins over <see cref="Aggregate"/>.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>What to compute over the rows this summary covers (the whole grid, or one group).</summary>
    [Parameter] public DataGridAggregateType Aggregate { get; set; }

    /// <summary>
    /// Format for the aggregate. Accepts both dialects - <c>"C0"</c> and <c>"{0:C0}"</c>. When unset,
    /// a Sum/Average/Min/Max is formatted the way the column's own cells are, so a currency column
    /// totals as currency without repeating the format here.
    /// </summary>
    [Parameter] public string? StringFormat { get; set; }

    /// <summary>Alias for <see cref="StringFormat"/>, matching <c>PropertyColumn.Format</c>.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary><c>Auto</c> follows the column, so a total lines up under its own values.</summary>
    [Parameter] public DataGridCellAlignment Alignment { get; set; }

    [Parameter] public bool Bold { get; set; } = true;

    /// <summary>Used when <see cref="Aggregate"/> is <c>Custom</c> - produce the text from the rows.</summary>
    [Parameter] public Func<IEnumerable<TItem>, string>? CustomAggregate { get; set; }

    /// <summary>Full control over the slot; the context carries the rows this summary covers.</summary>
    [Parameter] public RenderFragment<SummaryContext<TItem>>? ChildContent { get; set; }

    /// <summary>Extra CSS classes for this cell's <c>td</c>.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Set when this cell was synthesized from the legacy column-level <c>Aggregate</c>, so that
    /// definition's <c>DisplayTemplate</c> / <c>Format</c> keep working unchanged.
    /// </summary>
    internal AggregateDefinition<TItem>? Definition { get; set; }

    /// <summary>Set when this cell was synthesized from the legacy column-level <c>FooterTemplate</c>.</summary>
    internal RenderFragment? LegacyTemplate { get; set; }

    internal bool Matches(ColumnBase<TItem> column)
        => !string.IsNullOrEmpty(this.Column) &&
           (string.Equals(this.Column, column.Id, StringComparison.Ordinal) ||
            string.Equals(this.Column, column.Title, StringComparison.Ordinal));

    internal DataGridCellAlignment EffectiveAlignment(ColumnBase<TItem> column)
        => this.Alignment != DataGridCellAlignment.Auto ? this.Alignment : column.EffectiveAlignment;

    /// <summary>The text for this slot over <paramref name="items"/>.</summary>
    internal string ComputeText(ColumnBase<TItem> column, IReadOnlyList<TItem> items)
    {
        if (this.Text is not null)
            return this.Text;

        var type = this.Definition?.Type ?? this.Aggregate;
        if (type == DataGridAggregateType.None)
            return string.Empty;

        if (type == DataGridAggregateType.Custom)
        {
            var custom = this.CustomAggregate ?? this.Definition?.CustomAggregate;
            return custom?.Invoke(items) ?? string.Empty;
        }

        if (type == DataGridAggregateType.Count)
        {
            // A count is a count, never the column's own preset - a currency column would otherwise
            // report "12 rows" as "$12.00".
            return this.FormatResult(items.Count, column, countLike: true);
        }

        var nums = items
            .Select(i => ToDouble(column.GetValue(i)))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var result = type switch
        {
            DataGridAggregateType.Sum => nums.Sum(),
            DataGridAggregateType.Average => nums.Count > 0 ? nums.Average() : 0,
            DataGridAggregateType.Min => nums.Count > 0 ? nums.Min() : 0,
            DataGridAggregateType.Max => nums.Count > 0 ? nums.Max() : 0,
            _ => 0
        };

        return this.FormatResult(result, column, countLike: false);
    }

    string FormatResult(double result, ColumnBase<TItem> column, bool countLike)
    {
        if (this.Definition?.DisplayTemplate is not null)
            return this.Definition.DisplayTemplate(result);

        var format = this.StringFormat ?? this.Format ?? this.Definition?.Format;
        if (!string.IsNullOrEmpty(format))
            return DataGridValueFormatter.Format(result, new DataGridFormatSpec { StringFormat = format }) ?? string.Empty;

        // No explicit format: a total reads best in the column's own dress (currency, percent, the
        // suffix) - which is also exactly what the cells above it are showing.
        return countLike
            ? result.ToString("N0", CultureInfo.CurrentCulture)
            : column.FormatValue(result) ?? string.Empty;
    }

    static double? ToDouble(object? value)
    {
        if (value is null)
            return null;
        try
        {
            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }
        catch
        {
            return null;
        }
    }

    bool registered;

    // Same reasoning as ColumnBase: only re-notify the grid when a parameter that changes the
    // rendering actually changes, or the grid's StateHasChanged would re-render this cell, re-fire
    // OnParametersSet and spin the renderer forever.
    (string?, string?, DataGridAggregateType, string?, string?, DataGridCellAlignment, bool, string?) snapshot;
    bool hasSnapshot;

    protected override void OnInitialized()
    {
        this.Row?.AddCell(this);
        this.registered = true;
    }

    protected override void OnParametersSet()
    {
        if (!this.registered)
            return;

        var current = (this.Column, this.Text, this.Aggregate, this.StringFormat, this.Format,
            this.Alignment, this.Bold, this.Class);

        if (!this.hasSnapshot || !current.Equals(this.snapshot))
        {
            this.snapshot = current;
            this.hasSnapshot = true;
            this.Row?.Grid?.NotifySummaryChanged();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Declarative metadata; the grid renders the cell.
    }

    public void Dispose() => this.Row?.RemoveCell(this);
}


/// <summary>The context handed to a <see cref="SummaryCell{TItem}.ChildContent"/>.</summary>
public sealed class SummaryContext<TItem>
{
    internal SummaryContext(IReadOnlyList<TItem> items, bool isGroup, object? groupKey, string? groupText, int level)
    {
        this.Items = items;
        this.IsGroup = isGroup;
        this.GroupKey = groupKey;
        this.GroupText = groupText;
        this.Level = level;
    }

    /// <summary>The rows this summary covers - every processed row, or one group's rows.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>True for a group's summary, false for the grid's own.</summary>
    public bool IsGroup { get; }

    public object? GroupKey { get; }

    /// <summary>The group's key run through its column's formatting.</summary>
    public string? GroupText { get; }

    /// <summary>Nesting depth of the group (0 for the outermost, and for a grid-level summary).</summary>
    public int Level { get; }

    public int Count => this.Items.Count;
}
