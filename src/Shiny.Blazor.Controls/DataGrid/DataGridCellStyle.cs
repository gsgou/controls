namespace Shiny.Blazor.Controls;

/// <summary>
/// A per-cell visual override returned by <c>ColumnBase&lt;TItem&gt;.CellStyle</c>. Every member is
/// optional - leave one <c>null</c> and the grid's own themed value is used for it. Colours are CSS
/// values, so a theme token (<c>"var(--shiny-color-error)"</c>) is as valid as <c>"#c62828"</c>.
/// </summary>
public sealed class DataGridCellStyle
{
    /// <summary>CSS <c>color</c> for the cell.</summary>
    public string? TextColor { get; set; }

    /// <summary>CSS <c>background</c> for the cell.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Bold the cell text.</summary>
    public bool? Bold { get; set; }

    /// <summary>
    /// Extra CSS class(es) on the <c>&lt;td&gt;</c>. Note these are <b>not</b> scoped to the grid's own
    /// stylesheet - declare them in your app's CSS, not in an isolated <c>.razor.css</c>.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>Raw CSS appended to the cell's <c>style</c> attribute, for anything the members above don't cover.</summary>
    public string? Style { get; set; }
}
