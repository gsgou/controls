namespace Shiny.Controls.Office.Icons;

/// <summary>The kind of drawing command a vertex in an <see cref="OfficeIconShape"/> carries.</summary>
public enum OfficeIconVerb
{
    /// <summary>Start a new subpath at the vertex point.</summary>
    Move,

    /// <summary>Straight line to the vertex point.</summary>
    Line,

    /// <summary>Cubic bezier to the vertex point, through its two control points.</summary>
    Cubic,

    /// <summary>Close the current subpath back to where it started. The point is ignored.</summary>
    Close
}


/// <summary>The primitive an <see cref="OfficeIconShape"/> draws.</summary>
/// <remarks>
/// Rectangles and ellipses are kept as primitives rather than flattened into paths because both hosts
/// draw them natively and better than a bezier approximation would — <c>DrawRoundedRectangle</c> and
/// <c>DrawEllipse</c> on MAUI, <c>&lt;rect&gt;</c> and <c>&lt;ellipse&gt;</c> in SVG.
/// </remarks>
public enum OfficeIconPrimitive
{
    Path,
    Rectangle,
    Ellipse
}


/// <summary>
/// One stroked (or filled) figure of a toolbar icon, on the icon's 24x24 grid.
/// </summary>
/// <remarks>
/// Deliberately not an SVG path string. MAUI's <c>PathBuilder</c> parses SVG data with real gaps —
/// implicit line-tos become move-tos and run-together decimals truncate silently — so artwork
/// authored as a <c>d</c> attribute can look perfect in the browser and draw a stump on a device,
/// with nothing thrown. Commands as data render the same on both hosts because neither one parses.
/// </remarks>
public sealed class OfficeIconShape
{
    OfficeIconShape(
        OfficeIconPrimitive primitive,
        IReadOnlyList<OfficeIconVertex> vertices,
        float x,
        float y,
        float width,
        float height,
        float cornerRadius,
        bool filled)
    {
        this.Primitive = primitive;
        this.Vertices = vertices;
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
        this.CornerRadius = cornerRadius;
        this.IsFilled = filled;
    }


    public OfficeIconPrimitive Primitive { get; }

    /// <summary>The commands, for <see cref="OfficeIconPrimitive.Path"/>. Empty otherwise.</summary>
    public IReadOnlyList<OfficeIconVertex> Vertices { get; }

    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }

    /// <summary>Corner radius, for <see cref="OfficeIconPrimitive.Rectangle"/>. Zero is a square corner.</summary>
    public float CornerRadius { get; }

    /// <summary>Filled with the icon colour rather than stroked with it.</summary>
    public bool IsFilled { get; }


    /// <summary>A copy of this shape, filled rather than stroked.</summary>
    public OfficeIconShape Filled()
        => new(this.Primitive, this.Vertices, this.X, this.Y, this.Width, this.Height, this.CornerRadius, true);


    public static OfficeIconShape Path(params OfficeIconVertex[] vertices)
        => new(OfficeIconPrimitive.Path, vertices, 0, 0, 0, 0, 0, false);


    /// <summary>A straight line between two points.</summary>
    public static OfficeIconShape Line(float x1, float y1, float x2, float y2)
        => Path(OfficeIconVertex.MoveTo(x1, y1), OfficeIconVertex.LineTo(x2, y2));


    /// <summary>
    /// An open run of straight lines through the given x,y pairs.
    /// </summary>
    /// <remarks>
    /// The single most common figure in the set — the alignment rules, the chevrons, the indent
    /// arrows — and the one an SVG string gets wrong, since <c>M</c> followed by bare number pairs is
    /// exactly the implicit-lineto shape MAUI drops.
    /// </remarks>
    public static OfficeIconShape Polyline(params float[] points)
    {
        if (points.Length < 4 || points.Length % 2 != 0)
            throw new ArgumentException("A polyline needs at least two x,y pairs.", nameof(points));

        var vertices = new OfficeIconVertex[points.Length / 2];
        vertices[0] = OfficeIconVertex.MoveTo(points[0], points[1]);

        for (var i = 1; i < vertices.Length; i++)
            vertices[i] = OfficeIconVertex.LineTo(points[i * 2], points[i * 2 + 1]);

        return Path(vertices);
    }


    public static OfficeIconShape Rectangle(float x, float y, float width, float height, float cornerRadius = 0)
        => new(OfficeIconPrimitive.Rectangle, [], x, y, width, height, cornerRadius, false);


    /// <summary>An ellipse described by its bounding box, matching how both hosts take one.</summary>
    public static OfficeIconShape Ellipse(float x, float y, float width, float height)
        => new(OfficeIconPrimitive.Ellipse, [], x, y, width, height, 0, false);


    public static OfficeIconShape Circle(float centreX, float centreY, float radius)
        => Ellipse(centreX - radius, centreY - radius, radius * 2, radius * 2);
}


/// <summary>One command in an <see cref="OfficeIconShape"/> path, in absolute grid coordinates.</summary>
/// <param name="Verb">What to draw.</param>
/// <param name="X">The end point's x.</param>
/// <param name="Y">The end point's y.</param>
/// <param name="C1X">First control point x, for <see cref="OfficeIconVerb.Cubic"/>.</param>
/// <param name="C1Y">First control point y.</param>
/// <param name="C2X">Second control point x.</param>
/// <param name="C2Y">Second control point y.</param>
public readonly record struct OfficeIconVertex(
    OfficeIconVerb Verb,
    float X,
    float Y,
    float C1X = 0,
    float C1Y = 0,
    float C2X = 0,
    float C2Y = 0)
{
    public static OfficeIconVertex MoveTo(float x, float y) => new(OfficeIconVerb.Move, x, y);

    public static OfficeIconVertex LineTo(float x, float y) => new(OfficeIconVerb.Line, x, y);

    public static OfficeIconVertex CurveTo(float c1x, float c1y, float c2x, float c2y, float x, float y)
        => new(OfficeIconVerb.Cubic, x, y, c1x, c1y, c2x, c2y);

    public static readonly OfficeIconVertex Close = new(OfficeIconVerb.Close, 0, 0);
}
