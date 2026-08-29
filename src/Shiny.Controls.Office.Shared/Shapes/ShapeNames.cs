namespace Shiny.Controls.Office.Shapes;

/// <summary>
/// The display name for each insertable shape.
/// </summary>
/// <remarks>
/// One list, because there used to be two: a MAUI array and a Blazor gallery, kept in step by a
/// comment saying they had to match. They are what a user reads to tell a trapezoid from a
/// parallelogram, so a drift between the hosts is a drift in what the control is called.
/// </remarks>
public static class ShapeNames
{
    public static IReadOnlyList<(ShapeGeometry Geometry, string Name)> All { get; } =
    [
        (ShapeGeometry.Rectangle, "Rectangle"),
        (ShapeGeometry.RoundedRectangle, "Rounded rectangle"),
        (ShapeGeometry.Ellipse, "Ellipse"),
        (ShapeGeometry.Triangle, "Triangle"),
        (ShapeGeometry.RightTriangle, "Right triangle"),
        (ShapeGeometry.Diamond, "Diamond"),
        (ShapeGeometry.Pentagon, "Pentagon"),
        (ShapeGeometry.Hexagon, "Hexagon"),
        (ShapeGeometry.Star5, "Star"),
        (ShapeGeometry.RightArrow, "Right arrow"),
        (ShapeGeometry.LeftArrow, "Left arrow"),
        (ShapeGeometry.UpArrow, "Up arrow"),
        (ShapeGeometry.DownArrow, "Down arrow"),
        (ShapeGeometry.Chevron, "Chevron"),
        (ShapeGeometry.Parallelogram, "Parallelogram"),
        (ShapeGeometry.Trapezoid, "Trapezoid"),
        (ShapeGeometry.Plus, "Plus"),
        (ShapeGeometry.Can, "Cylinder"),
        (ShapeGeometry.Cloud, "Cloud"),
        (ShapeGeometry.Line, "Line")
    ];

    /// <summary>The name for a geometry, falling back to the enum member for one not listed.</summary>
    public static string Of(ShapeGeometry geometry)
    {
        foreach (var (candidate, name) in All)
        {
            if (candidate == geometry)
                return name;
        }

        return geometry.ToString();
    }
}
