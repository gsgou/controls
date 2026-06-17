namespace Shiny.Blazor.Controls;

/// <summary>A column footer/group aggregate (Count/Sum/Average/Min/Max or a custom function).</summary>
public sealed class AggregateDefinition<TItem>
{
    public DataGridAggregateType Type { get; set; } = DataGridAggregateType.Count;

    /// <summary>Format for the numeric result (e.g. "N0", "C2").</summary>
    public string? Format { get; set; }

    /// <summary>Optional display template, e.g. <c>v =&gt; $"Total: {v}"</c>.</summary>
    public Func<double, string>? DisplayTemplate { get; set; }

    /// <summary>Used when <see cref="Type"/> is <c>Custom</c> — produce the footer text from the items.</summary>
    public Func<IEnumerable<TItem>, string>? CustomAggregate { get; set; }
}
