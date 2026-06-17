using System.Collections;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>A sort instruction for one column.</summary>
public sealed class DataGridSortDefinition
{
    public DataGridSortDefinition(string columnId, DataGridSortDirection direction, int order)
    {
        this.ColumnId = columnId;
        this.Direction = direction;
        this.Order = order;
    }

    public string ColumnId { get; }
    public DataGridSortDirection Direction { get; }
    public int Order { get; }
}

/// <summary>A filter instruction for one column.</summary>
public sealed class DataGridFilterDefinition
{
    public string ColumnId { get; set; } = string.Empty;
    public DataGridFilterOperator Operator { get; set; } = DataGridFilterOperator.Contains;
    public object? Value { get; set; }
}

/// <summary>Snapshot of the grid's sort/filter/page state, handed to a <c>ServerData</c> delegate.</summary>
public sealed class DataGridGridState
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<DataGridSortDefinition> SortDefinitions { get; set; } = Array.Empty<DataGridSortDefinition>();
    public IReadOnlyList<DataGridFilterDefinition> FilterDefinitions { get; set; } = Array.Empty<DataGridFilterDefinition>();
}

/// <summary>The result a <c>ServerData</c> delegate returns: a page of items plus the total count.</summary>
public sealed class DataGridGridData
{
    public DataGridGridData(IList items, int totalItems)
    {
        this.Items = items;
        this.TotalItems = totalItems;
    }

    public IList Items { get; }
    public int TotalItems { get; }
}
