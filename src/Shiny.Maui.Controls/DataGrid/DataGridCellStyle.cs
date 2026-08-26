namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// A per-cell visual override, and the base of <see cref="DataGridHighlight"/>. Returned by
/// <see cref="DataGridColumn.CellStyle"/>, set as a whole column's <see cref="DataGridColumn.Highlight"/>,
/// returned by the grid's <c>RowHighlight</c>, or carried by a highlighting rule. Every member is
/// optional - leave one <c>null</c> and the grid's own themed value is used for it.
/// </summary>
/// <remarks>
/// The delegate forms are evaluated when a row binds (including when the virtualized list recycles a
/// row onto a different item), not when a property on the item changes. Raise the grid's item source
/// change / re-bind if a style needs to follow live edits.
/// </remarks>
public class DataGridCellStyle
{
    /// <summary>Text colour for the cell. <c>null</c> keeps the themed <c>OnSurfaceVariant</c>.</summary>
    public Color? TextColor { get; set; }

    /// <summary>
    /// An opaque background behind the cell. This <b>replaces</b> what the row was painting, so the
    /// stripe and selection tint stop showing through it. Use <see cref="Fill"/> instead to highlight
    /// a cell without losing those.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// A highlighter wash laid over the cell - painted <b>behind the text</b> and over whatever the
    /// row already had, so neither the content nor the row's stripe/selection is lost. See
    /// <see cref="FillOpacity"/> for how strongly it is laid on.
    /// </summary>
    public Color? Fill { get; set; }

    /// <summary>
    /// How much of <see cref="Fill"/> to lay on, 0-1. Defaults to <see cref="DefaultFillOpacity"/>
    /// (0.25) - a wash rather than a repaint, which is what keeps dark text on a strong fill readable.
    /// <c>1</c> gives a solid fill. A <see cref="Fill"/> that already carries its own alpha is scaled
    /// by this, so a semi-transparent brush stays semi-transparent.
    /// </summary>
    public double? FillOpacity { get; set; }

    /// <summary>Colour of the highlight stroke. Needs a <see cref="BorderStyle"/> other than <c>None</c> to draw.</summary>
    public Color? BorderColor { get; set; }

    /// <summary>The stroke's line style. <c>None</c> (the default) draws nothing.</summary>
    public DataGridBorderStyle BorderStyle { get; set; }

    /// <summary>Stroke thickness in device-independent units. <c>0</c> uses <see cref="DefaultBorderWidth"/> (2).</summary>
    public double BorderWidth { get; set; }

    /// <summary>
    /// Which sides to stroke. <c>null</c> (the default) lets the grid trace the perimeter of the
    /// highlighted region - a highlighted row is outlined as a row, not as a run of boxed cells.
    /// </summary>
    public DataGridBorderEdges? BorderEdges { get; set; }

    /// <summary>Font attributes (bold/italic) for the cell. <c>null</c> keeps <see cref="Microsoft.Maui.Controls.FontAttributes.None"/>.</summary>
    public FontAttributes? FontAttributes { get; set; }

    /// <summary>The wash strength used when <see cref="FillOpacity"/> is not set.</summary>
    public const double DefaultFillOpacity = 0.25;

    /// <summary>The stroke thickness used when <see cref="BorderWidth"/> is not set.</summary>
    public const double DefaultBorderWidth = 2d;

    internal bool HasFill => this.Fill is not null;

    internal bool HasBorder => this.BorderStyle != DataGridBorderStyle.None && this.BorderColor is not null;

    internal bool HasText => this.TextColor is not null || this.FontAttributes is not null;

    /// <summary>
    /// The cell background this style asks for: the opaque <see cref="BackgroundColor"/> if it set
    /// one, with <see cref="Fill"/> composited over it at <see cref="FillOpacity"/>. With no
    /// background of its own the wash is simply returned translucent, which is what lets the row's
    /// own stripe and selection tint carry on showing through it.
    /// </summary>
    internal Color EffectiveBackground()
    {
        var wash = this.WashColor();
        if (wash is null)
            return this.BackgroundColor ?? Colors.Transparent;

        if (this.BackgroundColor is null)
            return wash;

        var a = wash.Alpha;
        return new Color(
            wash.Red * a + this.BackgroundColor.Red * (1 - a),
            wash.Green * a + this.BackgroundColor.Green * (1 - a),
            wash.Blue * a + this.BackgroundColor.Blue * (1 - a),
            1f
        );
    }

    /// <summary><see cref="Fill"/> scaled by <see cref="FillOpacity"/>, or null when there is no fill.</summary>
    internal Color? WashColor()
    {
        if (this.Fill is null)
            return null;

        var opacity = Math.Clamp(this.FillOpacity ?? DefaultFillOpacity, 0d, 1d);
        return opacity >= 1
            ? this.Fill
            : this.Fill.WithAlpha((float)(this.Fill.Alpha * opacity));
    }

    /// <summary>
    /// Lays <paramref name="over"/> on top of <paramref name="under"/> and returns the result, leaving
    /// both operands alone. Members are taken in <b>groups</b> rather than one at a time: fill, stroke
    /// and text each come wholesale from the most specific style that speaks to them, so a cell rule
    /// that sets only a stroke keeps the row rule's fill instead of inheriting half of each.
    /// </summary>
    internal static DataGridCellStyle? Merge(DataGridCellStyle? under, DataGridCellStyle? over)
    {
        if (under is null)
            return over;
        if (over is null)
            return under;

        var merged = new DataGridCellStyle
        {
            BackgroundColor = over.BackgroundColor ?? under.BackgroundColor
        };

        var fill = over.HasFill ? over : under;
        merged.Fill = fill.Fill;
        merged.FillOpacity = fill.FillOpacity;

        var border = over.HasBorder ? over : under;
        merged.BorderColor = border.BorderColor;
        merged.BorderStyle = border.BorderStyle;
        merged.BorderWidth = border.BorderWidth;
        merged.BorderEdges = border.BorderEdges;

        var text = over.HasText ? over : under;
        merged.TextColor = text.TextColor;
        merged.FontAttributes = text.FontAttributes;

        return merged;
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
            FontAttributes = this.FontAttributes
        };
}
