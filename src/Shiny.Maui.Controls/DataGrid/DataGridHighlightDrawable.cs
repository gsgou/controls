namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Paints a highlight's stroke over one cell. Only the stroke - the fill is the cell's own background
/// colour, so it sits under the text where a highlight belongs rather than over it, and this layer
/// stays transparent everywhere it is not drawing a line.
/// </summary>
/// <remarks>
/// MAUI's <c>Border</c> strokes all four sides or none, and a highlight has to be able to stroke the
/// two sides of a cell that happen to fall on the edge of its region. Hence a drawable.
/// </remarks>
sealed class DataGridHighlightDrawable : IDrawable
{
    public Color? Stroke { get; set; }

    public float Thickness { get; set; } = (float)DataGridCellStyle.DefaultBorderWidth;

    public DataGridBorderStyle Style { get; set; }

    public DataGridBorderEdges Edges { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (this.Stroke is null || this.Style == DataGridBorderStyle.None || this.Edges == DataGridBorderEdges.None)
            return;

        var thickness = this.Thickness <= 0 ? (float)DataGridCellStyle.DefaultBorderWidth : this.Thickness;
        canvas.StrokeColor = this.Stroke;
        canvas.StrokeLineCap = LineCap.Butt;

        if (this.Style == DataGridBorderStyle.Double)
        {
            // Two lines sharing the declared weight: a third of it each, a third between them. Drawn
            // as two solid passes rather than a dash pattern, which runs along a line rather than across it.
            var band = thickness / 3f;
            this.StrokeEdges(canvas, dirtyRect, band, band / 2f);
            this.StrokeEdges(canvas, dirtyRect, band, thickness - (band / 2f));
            return;
        }

        // A dash pattern is expressed in multiples of the stroke width, so it scales with thickness.
        canvas.StrokeDashPattern = this.Style switch
        {
            DataGridBorderStyle.Dashed => [3f, 2f],
            DataGridBorderStyle.Dotted => [1f, 1.5f],
            _ => null
        };
        this.StrokeEdges(canvas, dirtyRect, thickness, thickness / 2f);
    }

    /// <summary>
    /// Strokes the selected edges inset by <paramref name="inset"/> from the cell's bounds, so the
    /// line lands wholly inside the cell instead of being clipped in half by it.
    /// </summary>
    void StrokeEdges(ICanvas canvas, RectF r, float thickness, float inset)
    {
        canvas.StrokeSize = thickness;

        var left = r.Left + inset;
        var right = r.Right - inset;
        var top = r.Top + inset;
        var bottom = r.Bottom - inset;

        if (this.Edges.HasFlag(DataGridBorderEdges.Top))
            canvas.DrawLine(r.Left, top, r.Right, top);
        if (this.Edges.HasFlag(DataGridBorderEdges.Bottom))
            canvas.DrawLine(r.Left, bottom, r.Right, bottom);
        if (this.Edges.HasFlag(DataGridBorderEdges.Left))
            canvas.DrawLine(left, r.Top, left, r.Bottom);
        if (this.Edges.HasFlag(DataGridBorderEdges.Right))
            canvas.DrawLine(right, r.Top, right, r.Bottom);
    }

    /// <summary>Copies a resolved style onto this drawable. Returns true when there is anything to paint.</summary>
    public bool Apply(DataGridCellStyle? style, DataGridBorderEdges edges)
    {
        if (style is null || !style.HasBorder || edges == DataGridBorderEdges.None)
        {
            this.Stroke = null;
            this.Style = DataGridBorderStyle.None;
            this.Edges = DataGridBorderEdges.None;
            return false;
        }

        this.Stroke = style.BorderColor;
        this.Style = style.BorderStyle;
        this.Edges = edges;
        this.Thickness = (float)(style.BorderWidth > 0 ? style.BorderWidth : DataGridCellStyle.DefaultBorderWidth);
        return true;
    }
}
