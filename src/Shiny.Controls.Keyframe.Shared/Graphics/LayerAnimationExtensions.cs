using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>
/// Shorthand for animating the common scene-layer properties, so authoring a scene does not mean
/// writing a setter lambda for every track.
/// </summary>
public static class LayerAnimationExtensions
{
    /// <summary>Animates a layer's opacity.</summary>
    public static TimelineBuilder AnimateOpacity(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Opacity = v, SingleInterpolator.Instance, keys,
            static l => l.Opacity, $"{layer.Id ?? "layer"}.Opacity");

    /// <summary>Animates a layer's rotation in degrees, taking the shortest arc between values.</summary>
    public static TimelineBuilder AnimateRotation(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<double>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Rotation = (float)v, AngleInterpolator.Degrees, keys,
            static l => l.Rotation, $"{layer.Id ?? "layer"}.Rotation");

    /// <summary>
    /// Animates rotation without wrapping, so a value going 0 → 720 spins twice rather than
    /// resolving to no movement at all.
    /// </summary>
    public static TimelineBuilder AnimateSpin(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<double>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Rotation = (float)v, keys,
            static l => l.Rotation, $"{layer.Id ?? "layer"}.Spin");

    /// <summary>Animates a layer's position.</summary>
    public static TimelineBuilder AnimatePosition(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<PointF>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Position = v, PointFInterpolator.Instance, keys,
            static l => l.Position, $"{layer.Id ?? "layer"}.Position");

    /// <summary>Animates a layer's scale.</summary>
    public static TimelineBuilder AnimateScale(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<SizeF>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Scale = v, SizeFInterpolator.Instance, keys,
            static l => l.Scale, $"{layer.Id ?? "layer"}.Scale");

    /// <summary>
    /// Animates only the horizontal component of a layer's position, leaving the vertical one to
    /// whatever else is driving it.
    /// </summary>
    /// <remarks>
    /// Two tracks on the same property would normally fight, with the last one evaluated winning.
    /// These read the component they are not driving straight off the layer, so an X track and a Y
    /// track compose instead — which is what lets two independently authored curves move one layer
    /// on two axes.
    /// </remarks>
    public static TimelineBuilder AnimatePositionX(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Position = new PointF(v, l.Position.Y),
            SingleInterpolator.Instance, keys, static l => l.Position.X, $"{layer.Id ?? "layer"}.PositionX");

    /// <summary>Animates only the vertical component of a layer's position.</summary>
    public static TimelineBuilder AnimatePositionY(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Position = new PointF(l.Position.X, v),
            SingleInterpolator.Instance, keys, static l => l.Position.Y, $"{layer.Id ?? "layer"}.PositionY");

    /// <summary>Animates only the horizontal component of a layer's scale.</summary>
    public static TimelineBuilder AnimateScaleX(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Scale = new SizeF(v, l.Scale.Height),
            SingleInterpolator.Instance, keys, static l => l.Scale.Width, $"{layer.Id ?? "layer"}.ScaleX");

    /// <summary>Animates only the vertical component of a layer's scale.</summary>
    public static TimelineBuilder AnimateScaleY(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Scale = new SizeF(l.Scale.Width, v),
            SingleInterpolator.Instance, keys, static l => l.Scale.Height, $"{layer.Id ?? "layer"}.ScaleY");

    /// <summary>
    /// Animates where a path's stroke stops, as a fraction of its length. Run it from 0 to 1 for a
    /// "draw on" reveal.
    /// </summary>
    public static TimelineBuilder AnimateTrimEnd(
        this TimelineBuilder builder, PathLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.TrimEnd = v, SingleInterpolator.Instance, keys,
            static l => l.TrimEnd, $"{layer.Id ?? "layer"}.TrimEnd");

    /// <summary>Animates where a path's stroke starts, as a fraction of its length.</summary>
    public static TimelineBuilder AnimateTrimStart(
        this TimelineBuilder builder, PathLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.TrimStart = v, SingleInterpolator.Instance, keys,
            static l => l.TrimStart, $"{layer.Id ?? "layer"}.TrimStart");

    /// <summary>Animates a layer's size.</summary>
    public static TimelineBuilder AnimateSize(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<SizeF>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.Size = v, SizeFInterpolator.Instance, keys,
            static l => l.Size, $"{layer.Id ?? "layer"}.Size");

    /// <summary>Animates a shape's fill colour, blending in Oklab by default.</summary>
    public static TimelineBuilder AnimateFill(
        this TimelineBuilder builder, ShapeLayer layer, Action<TrackBuilder<Color>> keys,
        ColorInterpolator? interpolator = null)
        => Require(builder).Animate(layer, static (l, v) => l.Fill = v,
            interpolator ?? ColorInterpolator.Oklab, keys,
            static l => l.Fill ?? Colors.Transparent, $"{layer.Id ?? "layer"}.Fill");

    /// <summary>Animates a shape's stroke colour, blending in Oklab by default.</summary>
    public static TimelineBuilder AnimateStroke(
        this TimelineBuilder builder, ShapeLayer layer, Action<TrackBuilder<Color>> keys,
        ColorInterpolator? interpolator = null)
        => Require(builder).Animate(layer, static (l, v) => l.Stroke = v,
            interpolator ?? ColorInterpolator.Oklab, keys,
            static l => l.Stroke ?? Colors.Transparent, $"{layer.Id ?? "layer"}.Stroke");

    /// <summary>Animates a shape's stroke width.</summary>
    public static TimelineBuilder AnimateStrokeWidth(
        this TimelineBuilder builder, ShapeLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.StrokeWidth = v, SingleInterpolator.Instance, keys,
            static l => l.StrokeWidth, $"{layer.Id ?? "layer"}.StrokeWidth");

    /// <summary>
    /// Animates the dash offset — the marching-ants effect, and the basis of a "draw on" stroke
    /// reveal when paired with a dash pattern as long as the path itself.
    /// </summary>
    public static TimelineBuilder AnimateStrokeDashOffset(
        this TimelineBuilder builder, ShapeLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.StrokeDashOffset = v, SingleInterpolator.Instance, keys,
            static l => l.StrokeDashOffset, $"{layer.Id ?? "layer"}.StrokeDashOffset");

    /// <summary>Morphs a path layer's geometry.</summary>
    public static TimelineBuilder AnimatePath(
        this TimelineBuilder builder, PathLayer layer, Action<TrackBuilder<PathF>> keys,
        PathFInterpolator? interpolator = null)
        => Require(builder).Animate(layer, static (l, v) => l.Data = v,
            interpolator ?? PathFInterpolator.Instance, keys,
            static l => l.Data ?? new PathF(), $"{layer.Id ?? "layer"}.Path");

    /// <summary>Animates a rectangle's corner radius.</summary>
    public static TimelineBuilder AnimateCornerRadius(
        this TimelineBuilder builder, RectangleLayer layer, Action<TrackBuilder<float>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.CornerRadius = v, SingleInterpolator.Instance, keys,
            static l => l.CornerRadius, $"{layer.Id ?? "layer"}.CornerRadius");

    /// <summary>Animates a text layer's colour.</summary>
    public static TimelineBuilder AnimateTextColor(
        this TimelineBuilder builder, TextLayer layer, Action<TrackBuilder<Color>> keys,
        ColorInterpolator? interpolator = null)
        => Require(builder).Animate(layer, static (l, v) => l.Color = v,
            interpolator ?? ColorInterpolator.Oklab, keys,
            static l => l.Color, $"{layer.Id ?? "layer"}.TextColor");

    /// <summary>Steps a layer's visibility, which has no meaningful midpoint to blend through.</summary>
    public static TimelineBuilder AnimateVisibility(
        this TimelineBuilder builder, SceneLayer layer, Action<TrackBuilder<bool>> keys)
        => Require(builder).Animate(layer, static (l, v) => l.IsVisible = v, StepInterpolator<bool>.Instance, keys,
            static l => l.IsVisible, $"{layer.Id ?? "layer"}.IsVisible");

    static TimelineBuilder Require(TimelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }
}
