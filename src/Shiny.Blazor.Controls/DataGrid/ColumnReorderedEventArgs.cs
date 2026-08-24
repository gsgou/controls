namespace Shiny.Blazor.Controls;

/// <summary>
/// Raised by <see cref="DataGrid{TItem}.ColumnReordered"/> after a column is dropped in a new position.
/// Persist <see cref="Order"/> to restore a user's column layout on the next visit.
/// </summary>
/// <param name="ColumnId">The column that moved (its property name, else its title).</param>
/// <param name="Order">Every visible column's id, left to right, after the move.</param>
public record ColumnReorderedEventArgs(string ColumnId, IReadOnlyList<string> Order);
