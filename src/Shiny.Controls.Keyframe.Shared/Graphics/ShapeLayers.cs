using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>Shared fill and stroke appearance for the drawn shape layers.</summary>
public abstract class ShapeLayer : SceneLayer
{
    /// <summary>Fill colour. Null leaves the shape unfilled.</summary>
    public Color? Fill { get; set; }

    /// <summary>Stroke colour. Null leaves the shape unstroked.</summary>
    public Color? Stroke { get; set; }

    /// <summary>Stroke width in scene units.</summary>
    public float StrokeWidth { get; set; } = 1f;

    /// <summary>Dash pattern, in multiples of the stroke width. Null draws a solid line.</summary>
    public float[]? StrokeDashPattern { get; set; }

    /// <summary>Offset into the dash pattern — animate this for a marching-ants effect.</summary>
    public float StrokeDashOffset { get; set; }

    /// <summary>How the ends of an open stroke are drawn.</summary>
    public LineCap StrokeLineCap { get; set; } = LineCap.Butt;

    /// <summary>How corners between stroke segments are drawn.</summary>
    public LineJoin StrokeLineJoin { get; set; } = LineJoin.Miter;

    /// <summary>Applies the stroke settings to the canvas. Returns false if there is no stroke.</summary>
    protected bool PrepareStroke(ICanvas canvas)
    {
        if (Stroke is null || StrokeWidth <= 0f)
            return false;

        canvas.StrokeColor = Stroke;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = StrokeLineCap;
        canvas.StrokeLineJoin = StrokeLineJoin;
        canvas.StrokeDashPattern = StrokeDashPattern;
        canvas.StrokeDashOffset = StrokeDashOffset;

        return true;
    }

    /// <summary>Applies the fill settings to the canvas. Returns false if there is no fill.</summary>
    protected bool PrepareFill(ICanvas canvas)
    {
        if (Fill is null)
            return false;

        canvas.FillColor = Fill;
        return true;
    }
}

/// <summary>A rectangle, optionally with rounded corners.</summary>
public sealed class RectangleLayer : ShapeLayer
{
    /// <summary>Corner radius in scene units. Zero draws square corners.</summary>
    public float CornerRadius { get; set; }

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        var bounds = new RectF(0f, 0f, Size.Width, Size.Height);

        if (CornerRadius > 0f)
        {
            // Clamp so an over-large radius degrades to a stadium shape instead of inverting.
            var radius = Math.Min(CornerRadius, Math.Min(Size.Width, Size.Height) / 2f);

            if (PrepareFill(canvas))
                canvas.FillRoundedRectangle(bounds, radius);

            if (PrepareStroke(canvas))
                canvas.DrawRoundedRectangle(bounds, radius);

            return;
        }

        if (PrepareFill(canvas))
            canvas.FillRectangle(bounds);

        if (PrepareStroke(canvas))
            canvas.DrawRectangle(bounds);
    }
}

/// <summary>An ellipse inscribed in the layer's size.</summary>
public sealed class EllipseLayer : ShapeLayer
{
    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        var bounds = new RectF(0f, 0f, Size.Width, Size.Height);

        if (PrepareFill(canvas))
            canvas.FillEllipse(bounds);

        if (PrepareStroke(canvas))
            canvas.DrawEllipse(bounds);
    }
}

/// <summary>An arbitrary vector path.</summary>
public sealed class PathLayer : ShapeLayer
{
    /// <summary>The geometry. Animate this with <see cref="PathFInterpolator"/> to morph shapes.</summary>
    public PathF? Data { get; set; }

    /// <summary>How overlapping subpaths decide what is inside the shape.</summary>
    public WindingMode WindingMode { get; set; } = WindingMode.NonZero;

    /// <summary>
    /// Where the drawn stroke begins, as a fraction of the path's own length.
    /// </summary>
    public float TrimStart { get; set; }

    /// <summary>
    /// Where the drawn stroke ends, as a fraction of the path's own length. Animate this from 0 to
    /// 1 for a "draw on" reveal.
    /// </summary>
    /// <remarks>
    /// Trimming applies to the stroke only — the fill always uses the whole path, because a
    /// partially drawn outline filled in would read as a mistake rather than an effect. This
    /// matches how trim paths behave in Lottie and every design tool that has them.
    /// </remarks>
    public float TrimEnd { get; set; } = 1f;

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        if (Data is null || Data.OperationCount == 0)
            return;

        if (PrepareFill(canvas))
            canvas.FillPath(Data, WindingMode);

        if (!PrepareStroke(canvas))
            return;

        // Measuring and rebuilding is only paid for while a trim is actually in flight; an
        // untrimmed path is handed straight back.
        var geometry = PathTrimmer.Trim(Data, TrimStart, TrimEnd);

        if (geometry.OperationCount > 0)
            canvas.DrawPath(geometry);
    }
}

/// <summary>A run of text.</summary>
public sealed class TextLayer : SceneLayer
{
    /// <summary>The string to draw.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Text colour.</summary>
    public Color Color { get; set; } = Colors.Black;

    /// <summary>Font size in scene units.</summary>
    public float FontSize { get; set; } = 16f;

    /// <summary>Font to draw with. Null uses the canvas default.</summary>
    public IFont? Font { get; set; }

    /// <summary>Horizontal alignment within <see cref="SceneLayer.Size"/>.</summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>Vertical alignment within <see cref="SceneLayer.Size"/>.</summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        if (string.IsNullOrEmpty(Text))
            return;

        canvas.FontColor = Color;
        canvas.FontSize = FontSize;

        if (Font is not null)
            canvas.Font = Font;

        canvas.DrawString(
            Text,
            0f,
            0f,
            Size.Width,
            Size.Height,
            HorizontalAlignment,
            VerticalAlignment);
    }
}

/// <summary>A bitmap.</summary>
public sealed class ImageLayer : SceneLayer
{
    /// <summary>The image to draw. Not owned — disposal stays with whoever loaded it.</summary>
    public IImage? Image { get; set; }

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        if (Image is null)
            return;

        var width = Size.Width > 0f ? Size.Width : Image.Width;
        var height = Size.Height > 0f ? Size.Height : Image.Height;

        canvas.DrawImage(Image, 0f, 0f, width, height);
    }
}
