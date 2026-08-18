namespace Shiny.Maui.Controls.DataGrid;

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
    Menu,
    Row,
    Toolbar
}

public enum DataGridEditMode
{
    None,
    Cell,
    Form
}

public enum DataGridEditTrigger
{
    Manual,
    OnRowClick
}

public enum DataGridFilterOperator
{
    Contains,
    NotContains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    Empty,
    NotEmpty,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
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

/// <summary>
/// Which edge a column is frozen (pinned) to while the grid scrolls horizontally.
/// Only meaningful when <see cref="DataGrid.HorizontalScroll"/> is on - without sideways
/// scrolling there is nothing to pin against.
/// </summary>
public enum DataGridFrozen
{
    None,

    /// <summary>Pinned to the leading edge (left in LTR).</summary>
    Start,

    /// <summary>Pinned to the trailing edge (right in LTR).</summary>
    End
}
