namespace Shiny.Blazor.Controls;

/// <summary>A sort instruction for one column.</summary>
public sealed class SortDefinition
{
    public SortDefinition(string columnId, DataGridSortDirection direction, int order)
    {
        this.ColumnId = columnId;
        this.Direction = direction;
        this.Order = order;
    }

    public string ColumnId { get; }
    public DataGridSortDirection Direction { get; }

    /// <summary>Priority for multi-sort (0 = primary).</summary>
    public int Order { get; }
}

/// <summary>A filter instruction for one column.</summary>
public sealed class FilterDefinition
{
    public string ColumnId { get; set; } = string.Empty;
    public DataGridFilterOperator Operator { get; set; } = DataGridFilterOperator.Contains;
    public object? Value { get; set; }
}

/// <summary>Snapshot of the grid's sort/filter/page state, handed to a <c>ServerData</c> delegate.</summary>
public sealed class GridState
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<SortDefinition> SortDefinitions { get; set; } = Array.Empty<SortDefinition>();
    public IReadOnlyList<FilterDefinition> FilterDefinitions { get; set; } = Array.Empty<FilterDefinition>();
}

/// <summary>The result a <c>ServerData</c> delegate returns: a page of items plus the total count.</summary>
public sealed class GridData<TItem>
{
    public GridData(IReadOnlyList<TItem> items, int totalItems)
    {
        this.Items = items;
        this.TotalItems = totalItems;
    }

    public IReadOnlyList<TItem> Items { get; }
    public int TotalItems { get; }
}
