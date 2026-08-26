namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// Builds the primitive shapes SVG has but <see cref="PathF"/> does not express directly.
/// </summary>
/// <remarks>
/// Everything is emitted as cubic beziers rather than leaning on the convenience appenders, so a
/// rectangle with different horizontal and vertical corner radii - which SVG allows and rounded-rect
/// helpers do not - comes out right instead of nearly right.
/// </remarks>
static class SvgGeometry
{
    // The classic circle-to-bezier constant: the control-point distance, as a fraction of the
    // radius, that makes four cubics approximate a quarter arc to within about 0.02%.
    const float Kappa = 0.5522847498307933f;


    /// <summary>Appends an axis-aligned ellipse, drawn clockwise from the rightmost point.</summary>
    public static void AppendEllipse(PathF path, float centerX, float centerY, float radiusX, float radiusY)
    {
        if (radiusX <= 0f || radiusY <= 0f)
            return;

        var offsetX = radiusX * Kappa;
        var offsetY = radiusY * Kappa;

        var left = centerX - radiusX;
        var right = centerX + radiusX;
        var top = centerY - radiusY;
        var bottom = centerY + radiusY;

        path.MoveTo(right, centerY);
        path.CurveTo(right, centerY + offsetY, centerX + offsetX, bottom, centerX, bottom);
        path.CurveTo(centerX - offsetX, bottom, left, centerY + offsetY, left, centerY);
        path.CurveTo(left, centerY - offsetY, centerX - offsetX, top, centerX, top);
        path.CurveTo(centerX + offsetX, top, right, centerY - offsetY, right, centerY);
        path.Close();
    }


    /// <summary>Appends a rectangle, with optionally elliptical corners.</summary>
    public static void AppendRectangle(PathF path, RectF rect, float radiusX, float radiusY)
    {
        if (rect.Width <= 0f || rect.Height <= 0f)
            return;

        // A radius wider than half the side would fold the corner back on itself.
        radiusX = Math.Clamp(radiusX, 0f, rect.Width / 2f);
        radiusY = Math.Clamp(radiusY, 0f, rect.Height / 2f);

        if (radiusX <= 0f || radiusY <= 0f)
        {
            path.MoveTo(rect.Left, rect.Top);
            path.LineTo(rect.Right, rect.Top);
            path.LineTo(rect.Right, rect.Bottom);
            path.LineTo(rect.Left, rect.Bottom);
            path.Close();
            return;
        }

        var offsetX = radiusX * Kappa;
        var offsetY = radiusY * Kappa;

        path.MoveTo(rect.Left + radiusX, rect.Top);
        path.LineTo(rect.Right - radiusX, rect.Top);
        path.CurveTo(rect.Right - radiusX + offsetX, rect.Top, rect.Right, rect.Top + radiusY - offsetY, rect.Right, rect.Top + radiusY);
        path.LineTo(rect.Right, rect.Bottom - radiusY);
        path.CurveTo(rect.Right, rect.Bottom - radiusY + offsetY, rect.Right - radiusX + offsetX, rect.Bottom, rect.Right - radiusX, rect.Bottom);
        path.LineTo(rect.Left + radiusX, rect.Bottom);
        path.CurveTo(rect.Left + radiusX - offsetX, rect.Bottom, rect.Left, rect.Bottom - radiusY + offsetY, rect.Left, rect.Bottom - radiusY);
        path.LineTo(rect.Left, rect.Top + radiusY);
        path.CurveTo(rect.Left, rect.Top + radiusY - offsetY, rect.Left + radiusX - offsetX, rect.Top, rect.Left + radiusX, rect.Top);
        path.Close();
    }


    /// <summary>Appends a run of points as connected line segments.</summary>
    public static void AppendPolygon(PathF path, PointF[] points, bool close)
    {
        if (points.Length < 2)
            return;

        path.MoveTo(points[0].X, points[0].Y);

        for (var i = 1; i < points.Length; i++)
            path.LineTo(points[i].X, points[i].Y);

        if (close)
            path.Close();
    }

    /// <summary>
    /// Appends one path's segments onto another, producing the union of the two regions.
    /// </summary>
    /// <remarks>
    /// <see cref="PathF"/> has no append of its own, and a clip built from several shapes has to be
    /// one path - calling <c>ClipPath</c> twice intersects, which is the opposite of what a
    /// multi-shape <c>clipPath</c> means. Walking the segments is exact for everything this renderer
    /// produces: the shapes here emit cubics, and MAUI's own path-data parser turns SVG arcs into
    /// quads, so no arc segment ever reaches this.
    /// </remarks>
    public static void Append(PathF target, PathF source)
    {
        var pointIndex = 0;

        foreach (var operation in source.SegmentTypes)
        {
            switch (operation)
            {
                case PathOperation.Move:
                    target.MoveTo(source[pointIndex++]);
                    break;

                case PathOperation.Line:
                    target.LineTo(source[pointIndex++]);
                    break;

                case PathOperation.Quad:
                    target.QuadTo(source[pointIndex], source[pointIndex + 1]);
                    pointIndex += 2;
                    break;

                case PathOperation.Cubic:
                    target.CurveTo(source[pointIndex], source[pointIndex + 1], source[pointIndex + 2]);
                    pointIndex += 3;
                    break;

                case PathOperation.Close:
                    target.Close();
                    break;

                default:
                    // Nothing upstream emits an arc segment, but a future parser change should
                    // degrade to a straight edge rather than walking the points out of step.
                    pointIndex = Math.Min(pointIndex + 1, source.Count - 1);
                    break;
            }
        }
    }
}
