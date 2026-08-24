namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// A per-cell visual override returned by <see cref="DataGridColumn.CellStyle"/>. Every member is
/// optional - leave one <c>null</c> and the grid's own themed value is used for it.
/// </summary>
/// <remarks>
/// The delegate is evaluated when a row binds (including when the virtualized list recycles a row
/// onto a different item), not when a property on the item changes. Raise the grid's item source
/// change / re-bind if a style needs to follow live edits.
/// </remarks>
public sealed class DataGridCellStyle
{
    /// <summary>Text colour for the cell. <c>null</c> keeps the themed <c>OnSurfaceVariant</c>.</summary>
    public Color? TextColor { get; set; }

    /// <summary>Background behind the cell. <c>null</c> keeps the row background (stripe/selection) showing through.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>Font attributes (bold/italic) for the cell. <c>null</c> keeps <see cref="FontAttributes.None"/>.</summary>
    public FontAttributes? FontAttributes { get; set; }
}
