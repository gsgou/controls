namespace Shiny.Blazor.Controls;

public enum DataGridSortDirection
{
    None,
    Ascending,
    Descending
}

public enum DataGridSortMode
{
    None,
    Single,
    Multiple
}

public enum DataGridSelectionMode
{
    None,
    Single,
    Multiple
}

public enum DataGridFilterMode
{
    /// <summary>A filter icon on each column header opens a per-column filter menu.</summary>
    Menu,

    /// <summary>An inline row of filter inputs under the header.</summary>
    Row,

    /// <summary>A single quick-filter search box above the grid.</summary>
    Toolbar
}

public enum DataGridEditMode
{
    None,

    /// <summary>Edit a single cell in place.</summary>
    Cell,

    /// <summary>Edit the whole row via a form.</summary>
    Form
}

public enum DataGridEditTrigger
{
    Manual,
    OnRowClick
}

public enum DataGridColumnResizeMode
{
    None,
    Column,
    Container
}

public enum DataGridFilterOperator
{
    // string
    Contains,
    NotContains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    Empty,
    NotEmpty,

    // numeric / date
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,

    // bool / enum
    Is,
    IsNot
}

public enum DataGridAggregateType
{
    None,
    Count,
    Sum,
    Average,
    Min,
    Max,
    Custom
}
