using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>
/// Cuts a path down to a span of its own length — the mechanic behind "draw on" strokes and
/// Lottie-style trim paths.
/// </summary>
/// <remarks>
/// <para>The web does this with <c>stroke-dasharray</c> and an animated <c>stroke-dashoffset</c>,
/// which needs no geometry at all because the browser already knows how long the path is. Nothing
/// in <c>Microsoft.Maui.Graphics</c> exposes a path length, and the dash offset it does expose is
/// specified in multiples of the stroke width — so the same trick here would need the length anyway
/// and would then change shape every time the stroke width did. Measuring and rebuilding is the
/// honest version, and it behaves identically on every backend.</para>
/// <para>The rebuilt path is a polyline: curves are flattened before being measured, because the
/// arc length of a bezier has no closed form and sampling is what every implementation does. At
/// normal sizes the difference is invisible, and it is only paid while a trim is actually in
/// flight — an untrimmed path is returned untouched.</para>
/// </remarks>
public static class PathTrimmer
{
    // Enough to keep a curve smooth when scaled well up, cheap enough to run every frame for every
    // trimming path on screen.
    const int SamplesPerCurve = 16;

    /// <summary>Returns the span of a path between two fractions of its length.</summary>
    /// <param name="path">The path to trim. Never modified.</param>
    /// <param name="start">Where the span begins, 0 to 1.</param>
    /// <param name="end">Where the span ends, 0 to 1.</param>
    /// <returns>
    /// The original instance when nothing is trimmed away, so an untrimmed path costs nothing;
    /// otherwise a newly built polyline.
    /// </returns>
    public static PathF Trim(PathF path, float start, float end)
    {
        ArgumentNullException.ThrowIfNull(path);

        start = Math.Clamp(start, 0f, 1f);
        end = Math.Clamp(end, 0f, 1f);

        if (start <= 0f && end >= 1f)
            return path;

        var result = new PathF();

        if (end <= start)
            return result;

        var subpaths = Flatten(path);
        var total = 0f;

        foreach (var subpath in subpaths)
            total += Length(subpath);

        if (total <= 0f)
            return result;

        var from = total * start;
        var to = total * end;
        var travelled = 0f;

        foreach (var subpath in subpaths)
        {
            var open = false;

            for (var i = 1; i < subpath.Count; i++)
            {
                var a = subpath[i - 1];
                var b = subpath[i];
                var length = Distance(a, b);

                if (length <= 0f)
                    continue;

                var segmentStart = travelled;
                var segmentEnd = travelled + length;
                travelled = segmentEnd;

                if (segmentEnd <= from || segmentStart >= to)
                    continue;

                // The span begins or ends inside this segment: walk into it rather than snapping to
                // the nearest vertex, which is what would make a draw-on advance in visible steps.
                var enter = (Math.Max(from, segmentStart) - segmentStart) / length;
                var exit = (Math.Min(to, segmentEnd) - segmentStart) / length;

                if (!open)
                {
                    result.MoveTo(Lerp(a, b, enter));
                    open = true;
                }

                result.LineTo(Lerp(a, b, exit));
            }
        }

        return result;
    }

    /// <summary>Returns the first fraction of a path, measured along its length.</summary>
    public static PathF Trim(PathF path, float end) => Trim(path, 0f, end);

    /// <summary>Measures a path by flattening its curves and summing the result.</summary>
    public static float Measure(PathF path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var total = 0f;

        foreach (var subpath in Flatten(path))
            total += Length(subpath);

        return total;
    }

    static List<List<PointF>> Flatten(PathF path)
    {
        var subpaths = new List<List<PointF>>();
        var current = new List<PointF>();
        var cursor = PointF.Zero;
        var subpathStart = PointF.Zero;
        var pointIndex = 0;

        void Commit()
        {
            if (current.Count > 1)
                subpaths.Add(current);

            current = [];
        }

        foreach (var operation in path.SegmentTypes)
        {
            switch (operation)
            {
                case PathOperation.Move:
                    Commit();
                    cursor = subpathStart = path[pointIndex++];
                    current.Add(cursor);
                    break;

                case PathOperation.Line:
                    cursor = path[pointIndex++];
                    current.Add(cursor);
                    break;

                case PathOperation.Quad:
                {
                    var control = path[pointIndex++];
                    var end = path[pointIndex++];

                    for (var i = 1; i <= SamplesPerCurve; i++)
                        current.Add(Quad(cursor, control, end, (float)i / SamplesPerCurve));

                    cursor = end;
                    break;
                }

                case PathOperation.Cubic:
                {
                    var control1 = path[pointIndex++];
                    var control2 = path[pointIndex++];
                    var end = path[pointIndex++];

                    for (var i = 1; i <= SamplesPerCurve; i++)
                        current.Add(Cubic(cursor, control1, control2, end, (float)i / SamplesPerCurve));

                    cursor = end;
                    break;
                }

                case PathOperation.Close:
                    current.Add(subpathStart);
                    cursor = subpathStart;
                    break;
            }
        }

        Commit();
        return subpaths;
    }

    static float Length(List<PointF> points)
    {
        var total = 0f;

        for (var i = 1; i < points.Count; i++)
            total += Distance(points[i - 1], points[i]);

        return total;
    }

    static float Distance(PointF from, PointF to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    static PointF Lerp(PointF from, PointF to, float t)
        => new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);

    static PointF Quad(PointF from, PointF control, PointF to, float t)
    {
        var u = 1f - t;
        var a = u * u;
        var b = 2f * u * t;
        var c = t * t;

        return new PointF(
            a * from.X + b * control.X + c * to.X,
            a * from.Y + b * control.Y + c * to.Y);
    }

    static PointF Cubic(PointF from, PointF control1, PointF control2, PointF to, float t)
    {
        var u = 1f - t;
        var a = u * u * u;
        var b = 3f * u * u * t;
        var c = 3f * u * t * t;
        var d = t * t * t;

        return new PointF(
            a * from.X + b * control1.X + c * control2.X + d * to.X,
            a * from.Y + b * control1.Y + c * control2.Y + d * to.Y);
    }
}
