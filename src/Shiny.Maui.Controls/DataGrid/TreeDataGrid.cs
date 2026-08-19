namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// A <see cref="DataGrid"/> in hierarchy mode - rows nest, and the first column carries an indent and
/// an expand caret. It is the same control with a name that says what it is: set
/// <see cref="DataGrid.ChildrenSelector"/> (or <see cref="DataGrid.ChildrenLoader"/> for lazy levels)
/// and every other grid feature - columns, sorting, filtering, frozen columns, selection, editing -
/// works exactly as it does on a flat grid.
/// </summary>
/// <remarks>
/// Sorting and filtering are applied per level, so children stay under their parent, and a row whose
/// descendant matches a filter is kept so the match is reachable. Paging pages the *roots*.
/// Hierarchy and <see cref="DataGrid.Groupable"/> are mutually exclusive - grouping wins.
/// </remarks>
[ContentProperty(nameof(Columns))]
public class TreeDataGrid : DataGrid
{
}
