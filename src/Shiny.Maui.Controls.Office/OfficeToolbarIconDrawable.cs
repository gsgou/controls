using Shiny.Controls.Office.Icons;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Paints an <see cref="OfficeIcon"/> from the shared icon set.
/// </summary>
/// <remarks>
/// The geometry lives in <c>Shiny.Controls.Office.Shared</c> and is drawn command for command here
/// and element for element in the Blazor package, so the two toolbars cannot drift: there is one
/// definition of what "align centre" looks like, and neither host parses anything to get at it.
/// </remarks>
internal sealed class OfficeToolbarIconDrawable : IDrawable
{
    public OfficeIcon Icon { get; set; }

    /// <summary>
    /// Artwork to draw instead of <see cref="Icon"/>'s.
    /// </summary>
    /// <remarks>
    /// The shapes gallery needs an icon per geometry, and those are built rather than enumerated -
    /// adding twenty members to <see cref="OfficeIcon"/> for them would put the drawing of a shape in
    /// the same list as the mark for Bold.
    /// </remarks>
    public IReadOnlyList<OfficeIconShape>? Shapes { get; set; }

    public Color Color { get; set; } = Colors.Black;

    public float StrokeWidth { get; set; } = OfficeIcons.StrokeWidth;


    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - size) / 2f,
            dirtyRect.Y + (dirtyRect.Height - size) / 2f);
        canvas.Scale(size / OfficeIcons.Grid, size / OfficeIcons.Grid);

        canvas.StrokeColor = this.Color;
        canvas.FillColor = this.Color;
        canvas.StrokeSize = this.StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        foreach (var shape in this.Shapes ?? OfficeIcons.Shapes(this.Icon))
            Draw(canvas, shape);

        canvas.RestoreState();
    }


    static void Draw(ICanvas canvas, OfficeIconShape shape)
    {
        switch (shape.Primitive)
        {
            case OfficeIconPrimitive.Rectangle when shape.IsFilled:
                canvas.FillRoundedRectangle(shape.X, shape.Y, shape.Width, shape.Height, shape.CornerRadius);
                break;

            case OfficeIconPrimitive.Rectangle:
                canvas.DrawRoundedRectangle(shape.X, shape.Y, shape.Width, shape.Height, shape.CornerRadius);
                break;

            case OfficeIconPrimitive.Ellipse when shape.IsFilled:
                canvas.FillEllipse(shape.X, shape.Y, shape.Width, shape.Height);
                break;

            case OfficeIconPrimitive.Ellipse:
                canvas.DrawEllipse(shape.X, shape.Y, shape.Width, shape.Height);
                break;

            default:
                var path = ToPath(shape);

                if (shape.IsFilled)
                    canvas.FillPath(path);
                else
                    canvas.DrawPath(path);
                break;
        }
    }


    static PathF ToPath(OfficeIconShape shape)
    {
        var path = new PathF();

        foreach (var vertex in shape.Vertices)
        {
            switch (vertex.Verb)
            {
                case OfficeIconVerb.Move:
                    path.MoveTo(vertex.X, vertex.Y);
                    break;

                case OfficeIconVerb.Line:
                    path.LineTo(vertex.X, vertex.Y);
                    break;

                case OfficeIconVerb.Cubic:
                    path.CurveTo(vertex.C1X, vertex.C1Y, vertex.C2X, vertex.C2Y, vertex.X, vertex.Y);
                    break;

                case OfficeIconVerb.Close:
                    path.Close();
                    break;
            }
        }

        return path;
    }
}
