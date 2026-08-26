using System.Numerics;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>How a shape is painted: a flat colour, the inherited colour, or a gradient.</summary>
abstract class SvgPaintServer
{
    /// <summary>
    /// A single colour standing in for this paint, used wherever a gradient cannot be expressed.
    /// </summary>
    /// <remarks>
    /// <see cref="ICanvas"/> can fill with a <see cref="Paint"/> but can only stroke with a
    /// <see cref="Color"/>, so a gradient stroke degrades to this rather than disappearing.
    /// </remarks>
    public abstract Color ColorFor(in SvgDrawContext context);

    /// <summary>
    /// Builds the canvas paint for one shape.
    /// </summary>
    /// <param name="bounds">The shape's bounds in the coordinate space it will be drawn in.</param>
    /// <param name="opacity">Multiplied into every stop's alpha - <c>fill-opacity</c> and friends.</param>
    /// <param name="context">Per-draw values, chiefly what <c>currentColor</c> means.</param>
    public abstract Paint Resolve(RectF bounds, float opacity, in SvgDrawContext context);
}


/// <summary>A flat colour.</summary>
sealed class SvgSolidPaint(Color color) : SvgPaintServer
{
    /// <summary>The paint every SVG starts with: <c>fill</c> defaults to black.</summary>
    public static SvgSolidPaint Black { get; } = new(Colors.Black);

    /// <summary>The colour.</summary>
    public Color Color { get; } = color;

    /// <inheritdoc />
    public override Color ColorFor(in SvgDrawContext context) => this.Color;

    /// <inheritdoc />
    public override Paint Resolve(RectF bounds, float opacity, in SvgDrawContext context)
        => new SolidPaint(this.Color.WithAlpha(this.Color.Alpha * opacity));
}


/// <summary>
/// <c>currentColor</c> - the paint an icon library uses so one file can be tinted per placement.
/// </summary>
/// <remarks>
/// Resolved at draw time rather than parse time, deliberately: baking the tint in would make the
/// parsed document tint-specific, and the whole point of <see cref="SvgCache"/> is that one parse
/// serves every control showing that artwork, whatever colour each of them wants it in.
/// </remarks>
sealed class SvgCurrentColorPaint : SvgPaintServer
{
    /// <summary>The single instance - it carries no state.</summary>
    public static SvgCurrentColorPaint Instance { get; } = new();

    SvgCurrentColorPaint() { }

    /// <inheritdoc />
    public override Color ColorFor(in SvgDrawContext context) => context.CurrentColor;

    /// <inheritdoc />
    public override Paint Resolve(RectF bounds, float opacity, in SvgDrawContext context)
        => new SolidPaint(context.CurrentColor.WithAlpha(context.CurrentColor.Alpha * opacity));
}


/// <summary>One colour stop.</summary>
/// <param name="Offset">Position along the gradient, 0 to 1.</param>
/// <param name="Color">The colour, with <c>stop-opacity</c> already folded into its alpha.</param>
readonly record struct SvgGradientStop(float Offset, Color Color);


/// <summary>Whether a gradient's geometry is written in shape-relative or user-space units.</summary>
enum SvgGradientUnits
{
    /// <summary>Coordinates are fractions of the shape's bounding box. The SVG default.</summary>
    ObjectBoundingBox,

    /// <summary>Coordinates are in the user space in force where the gradient is referenced.</summary>
    UserSpaceOnUse
}


/// <summary>
/// A linear or radial gradient.
/// </summary>
/// <remarks>
/// <para>MAUI's gradient paints describe themselves as fractions of the rectangle handed to
/// <see cref="ICanvas.SetFillPaint"/>, so both unit systems collapse to the same thing here: work out
/// where the gradient's control points land relative to the shape's bounds and hand over fractions.
/// That also means <c>gradientTransform</c> is applied to the control points rather than to the ramp
/// itself - exact for translation, rotation and uniform scale, and a close approximation for the
/// shears that almost never appear in real artwork.</para>
///
/// <para><c>spreadMethod</c> is not represented: MAUI pads, always. A <c>reflect</c> or <c>repeat</c>
/// gradient renders as its padded equivalent rather than failing. Nor is a focal point - the
/// <c>fx</c>/<c>fy</c> of a radial gradient is ignored and the ramp stays concentric.</para>
/// </remarks>
sealed class SvgGradientPaint : SvgPaintServer
{
    /// <summary>True for a radial gradient, false for a linear one.</summary>
    public required bool IsRadial { get; init; }

    /// <summary>Which space the control points are in.</summary>
    public required SvgGradientUnits Units { get; init; }

    /// <summary>The colour ramp, ordered by offset. Never empty.</summary>
    public required SvgGradientStop[] Stops { get; init; }

    /// <summary>Linear: the ramp's start point.</summary>
    public PointF Start { get; init; } = new(0f, 0f);

    /// <summary>Linear: the ramp's end point.</summary>
    public PointF End { get; init; } = new(1f, 0f);

    /// <summary>Radial: the outer circle's centre.</summary>
    public PointF Center { get; init; } = new(0.5f, 0.5f);

    /// <summary>Radial: the outer circle's radius.</summary>
    public float Radius { get; init; } = 0.5f;

    /// <summary>The gradient's own transform, folded into the control points at resolve time.</summary>
    public Matrix3x2 Transform { get; init; } = Matrix3x2.Identity;


    /// <inheritdoc />
    public override Color ColorFor(in SvgDrawContext context) => this.Stops[0].Color;


    /// <inheritdoc />
    public override Paint Resolve(RectF bounds, float opacity, in SvgDrawContext context)
    {
        // A zero-area shape has no bounds to map fractions onto - dividing by it would produce NaN
        // control points, which render as nothing on some backends and as garbage on others.
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            var flat = this.Stops[0].Color;
            return new SolidPaint(flat.WithAlpha(flat.Alpha * opacity));
        }

        var stops = new PaintGradientStop[this.Stops.Length];
        for (var i = 0; i < this.Stops.Length; i++)
        {
            var stop = this.Stops[i];
            stops[i] = new PaintGradientStop(stop.Offset, stop.Color.WithAlpha(stop.Color.Alpha * opacity));
        }

        if (this.IsRadial)
        {
            var center = this.ToFraction(this.Center, bounds);
            var edge = this.ToFraction(new PointF(this.Center.X + this.Radius, this.Center.Y), bounds);

            return new RadialGradientPaint(stops)
            {
                Center = center,
                Radius = Math.Max(1e-4f, (float)Math.Abs(edge.X - center.X))
            };
        }

        return new LinearGradientPaint(stops)
        {
            StartPoint = this.ToFraction(this.Start, bounds),
            EndPoint = this.ToFraction(this.End, bounds)
        };
    }


    Point ToFraction(PointF point, RectF bounds)
    {
        var transformed = Vector2.Transform(new Vector2(point.X, point.Y), this.Transform);

        // Object-bounding-box coordinates are already fractions of the shape, so they pass straight
        // through; user-space ones have to be measured against where the shape actually sits.
        if (this.Units == SvgGradientUnits.ObjectBoundingBox)
            return new Point(transformed.X, transformed.Y);

        return new Point(
            (transformed.X - bounds.X) / bounds.Width,
            (transformed.Y - bounds.Y) / bounds.Height
        );
    }
}
