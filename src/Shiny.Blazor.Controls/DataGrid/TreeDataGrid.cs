using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// A <see cref="DataGrid{TItem}"/> in hierarchy mode - rows nest, and the first column carries an
/// indent and an expand caret. It is the same component with a name that says what it is: set
/// <see cref="DataGrid{TItem}.ChildrenSelector"/> (or <see cref="DataGrid{TItem}.ChildrenLoader"/> for
/// lazy levels) and every other grid feature - columns, sorting, filtering, frozen columns, selection,
/// editing - works exactly as it does on a flat grid.
/// </summary>
/// <remarks>
/// Sorting and filtering are applied per level, so children stay under their parent, and a row whose
/// descendant matches a filter is kept so the match is reachable. Paging pages the *roots*.
/// Hierarchy and <see cref="DataGrid{TItem}.Groupable"/> are mutually exclusive - grouping wins.
/// </remarks>
[CascadingTypeParameter(nameof(TItem))]
public class TreeDataGrid<TItem> : DataGrid<TItem>
{
}
