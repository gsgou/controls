using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>Blends <see cref="PointF"/> values componentwise.</summary>
public sealed class PointFInterpolator : IInterpolator<PointF>
{
    /// <summary>The shared instance.</summary>
    public static readonly PointFInterpolator Instance = new();

    /// <inheritdoc />
    public PointF Interpolate(PointF from, PointF to, double progress) => new(
        (float)(from.X + (to.X - from.X) * progress),
        (float)(from.Y + (to.Y - from.Y) * progress));
}

/// <summary>Blends <see cref="SizeF"/> values componentwise.</summary>
public sealed class SizeFInterpolator : IInterpolator<SizeF>
{
    /// <summary>The shared instance.</summary>
    public static readonly SizeFInterpolator Instance = new();

    /// <inheritdoc />
    public SizeF Interpolate(SizeF from, SizeF to, double progress) => new(
        (float)(from.Width + (to.Width - from.Width) * progress),
        (float)(from.Height + (to.Height - from.Height) * progress));
}

/// <summary>Blends <see cref="RectF"/> values componentwise.</summary>
public sealed class RectFInterpolator : IInterpolator<RectF>
{
    /// <summary>The shared instance.</summary>
    public static readonly RectFInterpolator Instance = new();

    /// <inheritdoc />
    public RectF Interpolate(RectF from, RectF to, double progress) => new(
        (float)(from.X + (to.X - from.X) * progress),
        (float)(from.Y + (to.Y - from.Y) * progress),
        (float)(from.Width + (to.Width - from.Width) * progress),
        (float)(from.Height + (to.Height - from.Height) * progress));
}

/// <summary>
/// Morphs one <see cref="PathF"/> into another by blending corresponding points.
/// </summary>
/// <remarks>
/// <para>Path morphing only works when both paths describe the same sequence of operations —
/// same number of segments, same types, in the same order. That is a real constraint, not an
/// implementation shortcut: there is no single correct answer for how a triangle becomes a
/// five-pointed star, and every tool that appears to do it is really running a separate
/// point-matching pass first.</para>
/// <para>When the structures do not match, this interpolator falls back to a hard cut at the
/// midpoint rather than throwing, so a mismatched pair degrades to something watchable instead of
/// crashing the render loop. Set <see cref="ThrowOnMismatch"/> to surface it loudly during
/// development.</para>
/// </remarks>
public sealed class PathFInterpolator : IInterpolator<PathF>
{
    /// <summary>The shared instance, with silent fallback on mismatch.</summary>
    public static readonly PathFInterpolator Instance = new();

    /// <summary>An instance that throws when two paths cannot be morphed.</summary>
    public static readonly PathFInterpolator Strict = new() { ThrowOnMismatch = true };

    /// <summary>Whether a structural mismatch throws rather than falling back to a hard cut.</summary>
    public bool ThrowOnMismatch { get; init; }

    /// <inheritdoc />
    public PathF Interpolate(PathF from, PathF to, double progress)
    {
        if (from is null)
            return to ?? new PathF();

        if (to is null)
            return from;

        if (!AreCompatible(from, to))
        {
            if (ThrowOnMismatch)
                throw new InvalidOperationException(
                    $"Cannot morph between paths with different structures " +
                    $"({from.OperationCount} operations and {from.Count} points versus " +
                    $"{to.OperationCount} and {to.Count}). Rebuild both paths with matching " +
                    "segment sequences, or animate them as separate layers with a cross-fade.");

            return progress < 0.5d ? from : to;
        }

        var result = new PathF();
        var pointIndex = 0;

        for (var i = 0; i < from.OperationCount; i++)
        {
            switch (from.GetSegmentType(i))
            {
                case PathOperation.Move:
                    result.MoveTo(Blend(from, to, pointIndex++, progress));
                    break;

                case PathOperation.Line:
                    result.LineTo(Blend(from, to, pointIndex++, progress));
                    break;

                case PathOperation.Quad:
                    result.QuadTo(
                        Blend(from, to, pointIndex++, progress),
                        Blend(from, to, pointIndex++, progress));
                    break;

                case PathOperation.Cubic:
                    result.CurveTo(
                        Blend(from, to, pointIndex++, progress),
                        Blend(from, to, pointIndex++, progress),
                        Blend(from, to, pointIndex++, progress));
                    break;

                case PathOperation.Close:
                    result.Close();
                    break;

                default:
                    // Arcs carry radii and flags that do not survive naive point blending, so we
                    // decline rather than emit something subtly wrong.
                    return progress < 0.5d ? from : to;
            }
        }

        return result;
    }

    static bool AreCompatible(PathF from, PathF to)
    {
        if (from.OperationCount != to.OperationCount || from.Count != to.Count)
            return false;

        for (var i = 0; i < from.OperationCount; i++)
        {
            if (from.GetSegmentType(i) != to.GetSegmentType(i))
                return false;
        }

        return true;
    }

    static PointF Blend(PathF from, PathF to, int index, double progress)
    {
        var a = from[index];
        var b = to[index];

        return new PointF(
            (float)(a.X + (b.X - a.X) * progress),
            (float)(a.Y + (b.Y - a.Y) * progress));
    }
}
