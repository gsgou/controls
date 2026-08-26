namespace Shiny.Blazor.Controls;

/// <summary>
/// A per-cell visual override, and the base of <see cref="DataGridHighlight{TItem}"/>. Returned by <c>ColumnBase&lt;TItem&gt;.CellStyle</c>, set as a whole
/// column's <c>Highlight</c>, returned by the grid's <c>RowHighlight</c>, or carried by a
/// <see cref="DataGridHighlight{TItem}"/> rule. Every member is optional - leave one <c>null</c> and
/// the grid's own themed value is used for it. Colours are CSS values, so a theme token
/// (<c>"var(--shiny-color-error)"</c>) is as valid as <c>"#c62828"</c>.
/// </summary>
public class DataGridCellStyle
{
    /// <summary>CSS <c>color</c> for the cell.</summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// CSS <c>background-color</c> for the cell. This <b>replaces</b> the cell's background, so the
    /// row's stripe and selection tint stop showing through it. Use <see cref="Fill"/> instead to
    /// highlight a cell without losing those.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// A highlighter wash laid over the cell - painted <b>behind the text</b> and over whatever the
    /// row already had, so neither the content nor the row's stripe/selection is lost. Any CSS colour,
    /// including a theme token. See <see cref="FillOpacity"/> for how strongly it is laid on.
    /// </summary>
    public string? Fill { get; set; }

    /// <summary>
    /// How much of <see cref="Fill"/> to lay on, 0-1. Defaults to <see cref="DefaultFillOpacity"/>
    /// (0.25) - a wash rather than a repaint, which is what keeps dark text on a strong fill readable.
    /// <c>1</c> gives a solid fill.
    /// </summary>
    public double? FillOpacity { get; set; }

    /// <summary>CSS colour of the highlight stroke. Needs a <see cref="BorderStyle"/> other than <c>None</c> to draw.</summary>
    public string? BorderColor { get; set; }

    /// <summary>The stroke's line style. <c>None</c> (the default) draws nothing.</summary>
    public DataGridBorderStyle BorderStyle { get; set; }

    /// <summary>CSS width of the stroke. Defaults to <see cref="DefaultBorderWidth"/> (2px).</summary>
    public string? BorderWidth { get; set; }

    /// <summary>
    /// Which sides to stroke. <c>null</c> (the default) lets the grid trace the perimeter of the
    /// highlighted region - a highlighted row is outlined as a row, not as a run of boxed cells.
    /// </summary>
    public DataGridBorderEdges? BorderEdges { get; set; }

    /// <summary>Bold the cell text.</summary>
    public bool? Bold { get; set; }

    /// <summary>
    /// Extra CSS class(es) on the <c>&lt;td&gt;</c>. Note these are <b>not</b> scoped to the grid's own
    /// stylesheet - declare them in your app's CSS, not in an isolated <c>.razor.css</c>.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>Raw CSS appended to the cell's <c>style</c> attribute, for anything the members above don't cover.</summary>
    public string? Style { get; set; }

    /// <summary>The wash strength used when <see cref="FillOpacity"/> is not set.</summary>
    public const double DefaultFillOpacity = 0.25;

    /// <summary>The stroke width used when <see cref="BorderWidth"/> is not set.</summary>
    public const string DefaultBorderWidth = "2px";

    internal bool HasFill => !string.IsNullOrWhiteSpace(this.Fill);

    internal bool HasBorder
        => this.BorderStyle != DataGridBorderStyle.None && !string.IsNullOrWhiteSpace(this.BorderColor);

    internal bool HasText => !string.IsNullOrWhiteSpace(this.TextColor) || this.Bold is not null;

    /// <summary>
    /// Lays <paramref name="over"/> on top of this style and returns the result, leaving both operands
    /// alone. Members are taken in <b>groups</b> rather than one at a time: fill, stroke and text each
    /// come wholesale from the most specific style that speaks to them, so a cell rule that sets only a
    /// stroke keeps the row rule's fill instead of inheriting half of each. <c>CssClass</c> and
    /// <c>Style</c> accumulate, since those are escape hatches and dropping one silently would be worse.
    /// </summary>
    internal static DataGridCellStyle? Merge(DataGridCellStyle? under, DataGridCellStyle? over)
    {
        if (under is null)
            return over;
        if (over is null)
            return under;

        var merged = new DataGridCellStyle
        {
            CssClass = Join(under.CssClass, over.CssClass, " "),
            Style = Join(under.Style, over.Style, ";")
        };

        var fill = over.HasFill ? over : under;
        merged.Fill = fill.Fill;
        merged.FillOpacity = fill.FillOpacity;
        merged.BackgroundColor = over.BackgroundColor ?? under.BackgroundColor;

        var border = over.HasBorder ? over : under;
        merged.BorderColor = border.BorderColor;
        merged.BorderStyle = border.BorderStyle;
        merged.BorderWidth = border.BorderWidth;
        merged.BorderEdges = border.BorderEdges;

        var text = over.HasText ? over : under;
        merged.TextColor = text.TextColor;
        merged.Bold = text.Bold;

        return merged;
    }

    static string? Join(string? a, string? b, string separator)
    {
        if (string.IsNullOrWhiteSpace(a))
            return b;
        if (string.IsNullOrWhiteSpace(b))
            return a;
        return a.Trim().TrimEnd(';') + separator + b.Trim();
    }

    /// <summary>A copy with <see cref="BorderEdges"/> pinned - used when the grid derives them from the scope.</summary>
    internal DataGridCellStyle WithEdges(DataGridBorderEdges edges)
        => new()
        {
            TextColor = this.TextColor,
            BackgroundColor = this.BackgroundColor,
            Fill = this.Fill,
            FillOpacity = this.FillOpacity,
            BorderColor = this.BorderColor,
            BorderStyle = this.BorderStyle,
            BorderWidth = this.BorderWidth,
            BorderEdges = edges,
            Bold = this.Bold,
            CssClass = this.CssClass,
            Style = this.Style
        };
}
