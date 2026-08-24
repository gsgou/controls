namespace Shiny.Blazor.Controls;

/// <summary>
/// Raised by <see cref="DataGrid{TItem}.ColumnResized"/> when a resize drag ends. Persist these to
/// restore a user's column widths on the next visit.
/// </summary>
/// <param name="ColumnId">The column's stable id (its property name, else its title).</param>
/// <param name="Width">The final width in pixels, already held inside the column's min/max.</param>
public record ColumnResizedEventArgs(string ColumnId, double Width);
