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

/// <summary>
/// Which edge a column is frozen (pinned) to while the grid scrolls horizontally.
/// </summary>
public enum DataGridFrozen
{
    None,

    /// <summary>Pinned to the leading edge (left in LTR).</summary>
    Start,

    /// <summary>Pinned to the trailing edge (right in LTR).</summary>
    End
}

/// <summary>
/// How many rows can be expanded (detail row / tree children) at once.
/// </summary>
public enum DataGridExpandMode
{
    /// <summary>Expanding a row collapses whatever was expanded before it.</summary>
    Single,

    /// <summary>Any number of rows can be expanded at the same time.</summary>
    Multiple
}

/// A display preset for a databound column - the common formats you would otherwise reach for a
/// custom cell template to get. Set <c>StringFormat</c> instead (or as well)
/// for a raw .NET format string; an explicit format string always wins over the preset.
/// </summary>
public enum DataGridColumnFormat
{
    /// <summary>No preset - <c>StringFormat</c> if given, otherwise <c>ToString()</c>.</summary>
    None,

    /// <summary>Plain text. Same output as <see cref="None"/>, but pins left alignment under <see cref="DataGridCellAlignment.Auto"/>.</summary>
    Text,

    /// <summary>Grouped number ("N"), e.g. <c>1,234.5</c>. <c>Decimals</c> sets the places.</summary>
    Number,

    /// <summary>Currency ("C") in the column's culture, e.g. <c>$1,234.00</c>.</summary>
    Currency,

    /// <summary>Percent ("P"). Note .NET multiplies by 100 - <c>0.15</c> renders as <c>15%</c>.</summary>
    Percent,

    /// <summary>Short date ("d").</summary>
    Date,

    /// <summary>Short time ("t").</summary>
    Time,

    /// <summary>General date + short time ("g").</summary>
    DateTime,

    /// <summary>Byte count scaled to B/KB/MB/GB/TB, e.g. <c>1.2 MB</c>.</summary>
    FileSize,

    /// <summary>A glyph (or <c>TrueText</c>/<c>FalseText</c>) instead of "True"/"False".</summary>
    Boolean,

    /// <summary>The enum member's <c>[Description]</c>, else its name split on PascalCase ("InProgress" -> "In Progress").</summary>
    Enum
}

/// <summary>Horizontal alignment of a column's cells, header and footer.</summary>
public enum DataGridCellAlignment
{
    /// <summary>Right-align quantities (number/currency/percent/file-size and numeric CLR types), left-align everything else.</summary>
    Auto,

    /// <summary>Leading edge (left in LTR).</summary>
    Start,

    Center,

    /// <summary>Trailing edge (right in LTR).</summary>
    End
}
