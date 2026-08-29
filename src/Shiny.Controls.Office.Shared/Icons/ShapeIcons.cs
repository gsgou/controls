using Shiny.Controls.Office.Shapes;

namespace Shiny.Controls.Office.Icons;

/// <summary>
/// Draws each insertable shape as its own toolbar icon.
/// </summary>
/// <remarks>
/// <para>
/// A shapes gallery is one of the few places where the icon has to be the thing itself — a row of
/// buttons captioned "Trapezoid" and "Parallelogram" is a list, and nobody reads a list to pick a
/// shape. Which is why this exists rather than the gallery reusing a single generic shape mark.
/// </para>
/// <para>
/// The outlines are built with the same polygon, star and arrow maths the painter uses to lay the
/// shape into the document, at a smaller size. That is deliberate: hand-drawn icons drift from what
/// gets inserted the first time either side is adjusted, and a pentagon icon that yields a differently
/// proportioned pentagon is a small lie the user only catches after clicking.
/// </para>
/// </remarks>
public static class ShapeIcons
{
    /// <summary>The icon's drawing bounds, inset from the 24x24 grid so strokes are not clipped.</summary>
    const float Left = 3.5f;
    const float Top = 3.5f;
    const float Right = 20.5f;
    const float Bottom = 20.5f;

    const float Width = Right - Left;
    const float Height = Bottom - Top;
    const float MidX = Left + (Width / 2);
    const float MidY = Top + (Height / 2);

    /// <summary>The toolbar icon for a shape, or null for one with no drawing of its own.</summary>
    public static IReadOnlyList<OfficeIconShape> For(ShapeGeometry geometry) => geometry switch
    {
        ShapeGeometry.Rectangle => [OfficeIconShape.Rectangle(Left, Top + 2, Width, Height - 4)],
        ShapeGeometry.RoundedRectangle => [OfficeIconShape.Rectangle(Left, Top + 2, Width, Height - 4, 3.5f)],
        ShapeGeometry.Ellipse => [OfficeIconShape.Ellipse(Left, Top + 2, Width, Height - 4)],

        ShapeGeometry.Triangle => [Closed(MidX, Top, Right, Bottom, Left, Bottom)],
        ShapeGeometry.RightTriangle => [Closed(Left, Top, Left, Bottom, Right, Bottom)],
        ShapeGeometry.Diamond => [Closed(MidX, Top, Right, MidY, MidX, Bottom, Left, MidY)],

        ShapeGeometry.Pentagon => [Polygon(5, -90)],
        ShapeGeometry.Hexagon => [Polygon(6, 0)],
        ShapeGeometry.Star5 => [Star(5)],

        ShapeGeometry.RightArrow => [Arrow(0)],
        ShapeGeometry.LeftArrow => [Arrow(180)],
        ShapeGeometry.UpArrow => [Arrow(-90)],
        ShapeGeometry.DownArrow => [Arrow(90)],

        // Not a closed polygon: a chevron is drawn as the stroke it looks like.
        ShapeGeometry.Chevron =>
        [
            Closed(Left, Top + 2, Left + (Width * 0.55f), Top + 2, Right, MidY,
                   Left + (Width * 0.55f), Bottom - 2, Left, Bottom - 2, Left + (Width * 0.35f), MidY)
        ],

        ShapeGeometry.Parallelogram =>
        [
            Closed(Left + (Width * 0.25f), Top + 3, Right, Top + 3,
                   Right - (Width * 0.25f), Bottom - 3, Left, Bottom - 3)
        ],

        ShapeGeometry.Trapezoid =>
        [
            Closed(Left + (Width * 0.22f), Top + 3, Right - (Width * 0.22f), Top + 3,
                   Right, Bottom - 3, Left, Bottom - 3)
        ],

        ShapeGeometry.Plus =>
        [
            Closed(
                MidX - 3, Top + 1, MidX + 3, Top + 1, MidX + 3, MidY - 3,
                Right - 1, MidY - 3, Right - 1, MidY + 3, MidX + 3, MidY + 3,
                MidX + 3, Bottom - 1, MidX - 3, Bottom - 1, MidX - 3, MidY + 3,
                Left + 1, MidY + 3, Left + 1, MidY - 3, MidX - 3, MidY - 3)
        ],

        // A cylinder is the one shape whose icon needs two figures: the barrel, and the ellipse that
        // makes it read as a cylinder rather than a rounded rectangle.
        ShapeGeometry.Can =>
        [
            OfficeIconShape.Path(
                Vertex(OfficeIconVerb.Move, Left + 2, Top + 4),
                Vertex(OfficeIconVerb.Line, Left + 2, Bottom - 4),
                Cubic(Left + 2, Bottom, MidX, Bottom + 1.5f, MidX, Bottom - 1),
                Cubic(MidX, Bottom + 1.5f, Right - 2, Bottom, Right - 2, Bottom - 4),
                Vertex(OfficeIconVerb.Line, Right - 2, Top + 4)),

            OfficeIconShape.Ellipse(Left + 2, Top + 1.5f, Width - 4, 5)
        ],

        ShapeGeometry.Cloud =>
        [
            OfficeIconShape.Path(
                Vertex(OfficeIconVerb.Move, Left + 3, Bottom - 4),
                Cubic(Left - 1, Bottom - 4, Left, MidY - 2, Left + 3.5f, MidY - 2.5f),
                Cubic(Left + 3, Top + 1, Left + 9, Top, MidX + 0.5f, Top + 3),
                Cubic(Right - 4, Top + 1, Right, Top + 4, Right - 2.5f, MidY - 1),
                Cubic(Right + 1, MidY + 1, Right, Bottom - 4, Right - 3, Bottom - 4),
                Vertex(OfficeIconVerb.Close, 0, 0))
        ],

        ShapeGeometry.Line => [OfficeIconShape.Path(
            Vertex(OfficeIconVerb.Move, Left, Bottom),
            Vertex(OfficeIconVerb.Line, Right, Top))],

        _ => []
    };

    static OfficeIconVertex Vertex(OfficeIconVerb verb, float x, float y) => new(verb, x, y, 0, 0, 0, 0);

    static OfficeIconVertex Cubic(float c1x, float c1y, float c2x, float c2y, float x, float y)
        => new(OfficeIconVerb.Cubic, x, y, c1x, c1y, c2x, c2y);

    /// <summary>A closed outline through the given x,y pairs.</summary>
    static OfficeIconShape Closed(params float[] points)
    {
        var vertices = new List<OfficeIconVertex>();

        for (var i = 0; i + 1 < points.Length; i += 2)
            vertices.Add(Vertex(i == 0 ? OfficeIconVerb.Move : OfficeIconVerb.Line, points[i], points[i + 1]));

        vertices.Add(Vertex(OfficeIconVerb.Close, 0, 0));
        return OfficeIconShape.Path([.. vertices]);
    }

    /// <summary>The same construction as <c>ShapePainting.AddPolygon</c>.</summary>
    static OfficeIconShape Polygon(int sides, double startAngleDegrees)
    {
        var points = new float[sides * 2];

        for (var i = 0; i < sides; i++)
        {
            var angle = (startAngleDegrees + (i * 360d / sides)) * Math.PI / 180;
            points[i * 2] = (float)(MidX + (Width / 2 * Math.Cos(angle)));
            points[(i * 2) + 1] = (float)(MidY + (Height / 2 * Math.Sin(angle)));
        }

        return Closed(points);
    }

    /// <summary>The same construction as <c>ShapePainting.AddStar</c>.</summary>
    static OfficeIconShape Star(int points)
    {
        const double innerRatio = 0.4;
        var values = new float[points * 4];

        for (var i = 0; i < points * 2; i++)
        {
            var angle = (-90 + (i * 180d / points)) * Math.PI / 180;
            var ratio = i % 2 == 0 ? 1 : innerRatio;

            values[i * 2] = (float)(MidX + (Width / 2 * ratio * Math.Cos(angle)));
            values[(i * 2) + 1] = (float)(MidY + (Height / 2 * ratio * Math.Sin(angle)));
        }

        return Closed(values);
    }

    /// <summary>The same construction as <c>ShapePainting.AddArrow</c>, rotated about the centre.</summary>
    static OfficeIconShape Arrow(double rotationDegrees)
    {
        var headStart = Left + (Width * 0.6f);
        var shaftTop = Top + (Height * 0.3f);
        var shaftBottom = Bottom - (Height * 0.3f);

        float[] points =
        [
            Left, shaftTop,
            headStart, shaftTop,
            headStart, Top,
            Right, MidY,
            headStart, Bottom,
            headStart, shaftBottom,
            Left, shaftBottom
        ];

        if (rotationDegrees != 0)
        {
            var radians = rotationDegrees * Math.PI / 180;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            for (var i = 0; i + 1 < points.Length; i += 2)
            {
                var dx = points[i] - MidX;
                var dy = points[i + 1] - MidY;

                points[i] = (float)(MidX + (dx * cos) - (dy * sin));
                points[i + 1] = (float)(MidY + (dx * sin) + (dy * cos));
            }
        }

        return Closed(points);
    }
}
