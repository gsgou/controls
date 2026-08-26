using System.Collections.ObjectModel;
using System.Globalization;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// One summary (total) row. Add <see cref="DataGridSummaryCell"/> children, each pointing at a column -
/// a cell either aggregates that column's values or simply fills its slot with a label. Stack several
/// rows for a subtotal / tax / total block.
/// </summary>
/// <remarks>
/// The same definitions render at the bottom of the grid <b>and</b> inside every group (see
/// <see cref="DataGrid.GroupSummaryPlacement"/>); <see cref="Scope"/> narrows a row to one of the two.
/// </remarks>
[ContentProperty(nameof(Cells))]
public class DataGridSummaryRow : BindableObject
{
    public static readonly BindableProperty ScopeProperty = BindableProperty.Create(
        nameof(Scope), typeof(DataGridSummaryScope), typeof(DataGridSummaryRow), DataGridSummaryScope.Both);

    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(DataGridSummaryRow), true);

    /// <summary>The cells, one per column that has something to show. Columns with no cell are blank.</summary>
    public ObservableCollection<DataGridSummaryCell> Cells { get; } = new();

    /// <summary>Whether this row is shown under the whole grid, inside each group, or both (the default).</summary>
    public DataGridSummaryScope Scope
    {
        get => (DataGridSummaryScope)this.GetValue(ScopeProperty);
        set => this.SetValue(ScopeProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }

    internal bool AppliesTo(bool group)
        => this.IsVisible && this.Scope switch
        {
            DataGridSummaryScope.Grid => !group,
            DataGridSummaryScope.Group => group,
            _ => true
        };

    internal DataGridSummaryCell? CellFor(DataGridColumn column)
        => this.Cells.FirstOrDefault(c => c.Matches(column));
}


/// <summary>
/// One slot of a <see cref="DataGridSummaryRow"/>. Set <see cref="Text"/> for a plain label (the
/// right-aligned "Total" that sits beside the number), <see cref="Aggregate"/> to compute over the
/// column's values, or <see cref="CellTemplate"/> for anything else.
/// </summary>
public class DataGridSummaryCell : BindableObject
{
    public static readonly BindableProperty ColumnProperty = BindableProperty.Create(
        nameof(Column), typeof(string), typeof(DataGridSummaryCell), null);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(DataGridSummaryCell), null);

    public static readonly BindableProperty AggregateProperty = BindableProperty.Create(
        nameof(Aggregate), typeof(DataGridAggregateType), typeof(DataGridSummaryCell), DataGridAggregateType.None);

    public static readonly BindableProperty StringFormatProperty = BindableProperty.Create(
        nameof(StringFormat), typeof(string), typeof(DataGridSummaryCell), null);

    public static readonly BindableProperty AlignmentProperty = BindableProperty.Create(
        nameof(Alignment), typeof(DataGridCellAlignment), typeof(DataGridSummaryCell), DataGridCellAlignment.Auto);

    public static readonly BindableProperty BoldProperty = BindableProperty.Create(
        nameof(Bold), typeof(bool), typeof(DataGridSummaryCell), true);

    /// <summary>The column this cell fills, by <c>PropertyName</c> (or <c>Title</c> for a template column).</summary>
    public string? Column
    {
        get => (string?)this.GetValue(ColumnProperty);
        set => this.SetValue(ColumnProperty, value);
    }

    /// <summary>A literal label - "Total". Wins over <see cref="Aggregate"/>.</summary>
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>What to compute over the rows this summary covers (the whole grid, or one group).</summary>
    public DataGridAggregateType Aggregate
    {
        get => (DataGridAggregateType)this.GetValue(AggregateProperty);
        set => this.SetValue(AggregateProperty, value);
    }

    /// <summary>
    /// Format for the aggregate. Accepts both dialects - <c>"C0"</c> and <c>"{0:C0}"</c>. When unset,
    /// a Sum/Average/Min/Max is formatted the way the column's own cells are, so a currency column
    /// totals as currency without repeating the format here.
    /// </summary>
    public string? StringFormat
    {
        get => (string?)this.GetValue(StringFormatProperty);
        set => this.SetValue(StringFormatProperty, value);
    }

    /// <summary><c>Auto</c> follows the column, so a total lines up under its own values.</summary>
    public DataGridCellAlignment Alignment
    {
        get => (DataGridCellAlignment)this.GetValue(AlignmentProperty);
        set => this.SetValue(AlignmentProperty, value);
    }

    public bool Bold
    {
        get => (bool)this.GetValue(BoldProperty);
        set => this.SetValue(BoldProperty, value);
    }

    /// <summary>Full control over the slot. Its BindingContext is a <see cref="DataGridSummaryContext"/>.</summary>
    public DataTemplate? CellTemplate { get; set; }

    /// <summary>Used when <see cref="Aggregate"/> is <c>Custom</c> - produce the text from the rows.</summary>
    public Func<IEnumerable<object>, string>? CustomAggregate { get; set; }

    /// <summary>
    /// Set when this cell was synthesized from the legacy <see cref="DataGridColumn.Aggregate"/>, so the
    /// definition's <c>DisplayTemplate</c> / <c>Format</c> keep working unchanged.
    /// </summary>
    internal DataGridAggregateDefinition? Definition { get; set; }

    /// <summary>Set when this cell was synthesized from the legacy <see cref="DataGridColumn.FooterTemplate"/>.</summary>
    internal DataTemplate? LegacyTemplate { get; set; }

    internal bool Matches(DataGridColumn column)
        => !string.IsNullOrEmpty(this.Column) &&
           (string.Equals(this.Column, column.PropertyName, StringComparison.Ordinal) ||
            string.Equals(this.Column, column.Title, StringComparison.Ordinal) ||
            string.Equals(this.Column, column.Id, StringComparison.Ordinal));

    /// <summary>The text for this slot over <paramref name="items"/>.</summary>
    internal string ComputeText(DataGridColumn column, IReadOnlyList<object> items)
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

        var culture = column.Culture ?? CultureInfo.CurrentCulture;
        double result;
        if (type == DataGridAggregateType.Count)
        {
            // A count is a count, never the column's own preset - a currency column would otherwise
            // report "12 rows" as "$12.00".
            result = items.Count;
            return this.FormatResult(result, column, culture, countLike: true);
        }

        var nums = items
            .Select(i => ToDouble(column.GetCellValue(i), culture))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        result = type switch
        {
            DataGridAggregateType.Sum => nums.Sum(),
            DataGridAggregateType.Average => nums.Count > 0 ? nums.Average() : 0,
            DataGridAggregateType.Min => nums.Count > 0 ? nums.Min() : 0,
            DataGridAggregateType.Max => nums.Count > 0 ? nums.Max() : 0,
            _ => 0
        };

        return this.FormatResult(result, column, culture, countLike: false);
    }

    string FormatResult(double result, DataGridColumn column, CultureInfo culture, bool countLike)
    {
        if (this.Definition?.DisplayTemplate is not null)
            return this.Definition.DisplayTemplate(result);

        var format = this.StringFormat ?? this.Definition?.Format;
        if (!string.IsNullOrEmpty(format))
            return DataGridValueFormatter.Format(result, new DataGridFormatSpec { StringFormat = format, Culture = culture }) ?? string.Empty;

        // No explicit format: a total reads best in the column's own dress (currency, percent, the
        // suffix) - which is also exactly what the cells above it are showing.
        return countLike
            ? result.ToString("N0", culture)
            : column.FormatValue(result) ?? string.Empty;
    }

    static double? ToDouble(object? value, CultureInfo culture)
    {
        if (value is null)
            return null;
        try
        {
            return Convert.ToDouble(value, culture);
        }
        catch
        {
            return null;
        }
    }
}


/// <summary>The BindingContext handed to a <see cref="DataGridSummaryCell.CellTemplate"/>.</summary>
public sealed class DataGridSummaryContext
{
    internal DataGridSummaryContext(IReadOnlyList<object> items, bool isGroup, object? groupKey, string? groupText, int level)
    {
        this.Items = items;
        this.IsGroup = isGroup;
        this.GroupKey = groupKey;
        this.GroupText = groupText;
        this.Level = level;
    }

    /// <summary>The rows this summary covers - every processed row, or one group's rows.</summary>
    public IReadOnlyList<object> Items { get; }

    /// <summary>True for a group's summary, false for the grid's own.</summary>
    public bool IsGroup { get; }

    public object? GroupKey { get; }

    /// <summary>The group's key run through its column's formatting.</summary>
    public string? GroupText { get; }

    /// <summary>Nesting depth of the group (0 for the outermost, and for a grid-level summary).</summary>
    public int Level { get; }

    public int Count => this.Items.Count;
}
