using System.Numerics;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// A node in a parsed SVG. Every expensive decision - path construction, colour resolution, matrix
/// composition - is made once at parse time and frozen here, so drawing is nothing but canvas calls.
/// </summary>
/// <remarks>
/// Nodes are immutable after parsing, which is what makes a parsed document safe to share between
/// every control displaying the same artwork. See <see cref="SvgCache"/>. They are records so the
/// parser can attach the transform, clip and opacity that any element may carry without every
/// builder repeating the same three assignments.
/// </remarks>
abstract record SvgNode
{
    /// <summary>The node's own transform, applied before it draws.</summary>
    public Matrix3x2 Transform { get; init; } = Matrix3x2.Identity;

    /// <summary>Group opacity, multiplied with every ancestor's.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Geometry from a referenced <c>clipPath</c>, in this node's own space.</summary>
    public PathF? Clip { get; init; }

    /// <summary>How many drawable primitives this node accounts for.</summary>
    public abstract int Weight { get; }


    /// <summary>Draws the node beneath an ancestor's accumulated opacity.</summary>
    public void Draw(ICanvas canvas, in SvgDrawContext context, float inheritedOpacity)
    {
        if (this.Opacity <= 0f || inheritedOpacity <= 0f)
            return;

        var effective = Math.Clamp(inheritedOpacity * this.Opacity, 0f, 1f);
        var transformed = !this.Transform.IsIdentity;

        // Saving state costs a backend call, so only pay for it when this node actually changes
        // something the siblings after it must not inherit.
        if (!transformed && this.Clip is null)
        {
            this.OnDraw(canvas, context, effective);
            return;
        }

        canvas.SaveState();

        try
        {
            if (transformed)
                canvas.ConcatenateTransform(this.Transform);

            if (this.Clip is not null)
                canvas.ClipPath(this.Clip, WindingMode.NonZero);

            this.OnDraw(canvas, context, effective);
        }
        finally
        {
            canvas.RestoreState();
        }
    }


    /// <summary>Draws the node's content, with its transform and clip already applied.</summary>
    protected abstract void OnDraw(ICanvas canvas, in SvgDrawContext context, float opacity);
}


/// <summary>Values that vary per draw rather than per document.</summary>
/// <param name="CurrentColor">
/// What <c>currentColor</c> resolves to - the tint the control was asked to draw the artwork in.
/// </param>
readonly record struct SvgDrawContext(Color CurrentColor);


/// <summary>A container: <c>g</c>, a nested <c>svg</c>, <c>a</c>, or an expanded <c>use</c>.</summary>
sealed record SvgGroup : SvgNode
{
    /// <summary>The children, drawn in document order.</summary>
    public required SvgNode[] Children { get; init; }

    /// <inheritdoc />
    public override int Weight
    {
        get
        {
            var total = 1;
            foreach (var child in this.Children)
                total += child.Weight;

            return total;
        }
    }

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, in SvgDrawContext context, float opacity)
    {
        foreach (var child in this.Children)
            child.Draw(canvas, context, opacity);
    }
}


/// <summary>
/// Anything with geometry: <c>path</c>, <c>rect</c>, <c>circle</c>, <c>ellipse</c>, <c>line</c>,
/// <c>polyline</c> and <c>polygon</c> all reduce to this by parse time.
/// </summary>
sealed record SvgShape : SvgNode
{
    /// <summary>The geometry, in this node's own coordinate space.</summary>
    public required PathF Path { get; init; }

    /// <summary>
    /// The geometry's extent, measured once at parse time.
    /// </summary>
    /// <remarks>
    /// Flattening a path to measure it is not cheap, and a gradient fill needs the answer on every
    /// single draw to know where to put its control points. Measuring here means a scrolling list
    /// pays for it once per document rather than once per frame per cell.
    /// </remarks>
    public required RectF Bounds { get; init; }

    /// <summary>How overlapping subpaths decide what is inside the shape.</summary>
    public WindingMode Winding { get; init; } = WindingMode.NonZero;

    /// <summary>The fill, or null when the shape is unfilled.</summary>
    public SvgPaintServer? Fill { get; init; }

    /// <summary><c>fill-opacity</c>, combined with the inherited opacity at draw time.</summary>
    public float FillOpacity { get; init; } = 1f;

    /// <summary>The stroke, or null when the shape is unstroked.</summary>
    public SvgPaintServer? Stroke { get; init; }

    /// <summary><c>stroke-opacity</c>, combined with the inherited opacity at draw time.</summary>
    public float StrokeOpacity { get; init; } = 1f;

    /// <summary>Stroke width in this node's coordinate space.</summary>
    public float StrokeWidth { get; init; } = 1f;

    /// <summary>Dash pattern, already expressed in multiples of the stroke width.</summary>
    public float[]? DashPattern { get; init; }

    /// <summary>Offset into the dash pattern, in multiples of the stroke width.</summary>
    public float DashOffset { get; init; }

    /// <summary>How the ends of an open stroke are drawn.</summary>
    public LineCap LineCap { get; init; } = LineCap.Butt;

    /// <summary>How corners between stroke segments are drawn.</summary>
    public LineJoin LineJoin { get; init; } = LineJoin.Miter;

    /// <summary>How far a miter may extend before it is cut off.</summary>
    public float MiterLimit { get; init; } = 4f;

    /// <inheritdoc />
    public override int Weight => 1;


    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, in SvgDrawContext context, float opacity)
    {
        if (this.Path.OperationCount == 0)
            return;

        this.FillShape(canvas, context, opacity);
        this.StrokeShape(canvas, context, opacity);
    }


    void FillShape(ICanvas canvas, in SvgDrawContext context, float opacity)
    {
        if (this.Fill is null)
            return;

        var alpha = opacity * this.FillOpacity;
        if (alpha <= 0f)
            return;

        // A flat colour goes through FillColor rather than SetFillPaint: it is the overwhelmingly
        // common case and skips building a Paint object per shape per frame.
        if (this.Fill is not SvgGradientPaint)
        {
            var color = this.Fill.ColorFor(context);
            canvas.FillColor = color.WithAlpha(color.Alpha * alpha);
            canvas.FillPath(this.Path, this.Winding);
            return;
        }

        canvas.SetFillPaint(this.Fill.Resolve(this.Bounds, alpha, context), this.Bounds);
        canvas.FillPath(this.Path, this.Winding);
    }


    void StrokeShape(ICanvas canvas, in SvgDrawContext context, float opacity)
    {
        if (this.Stroke is null || this.StrokeWidth <= 0f)
            return;

        var alpha = opacity * this.StrokeOpacity;
        if (alpha <= 0f)
            return;

        // ICanvas fills with a Paint but strokes with a Color, so a gradient stroke resolves to its
        // first stop. Losing the ramp is a far smaller lie than losing the outline.
        var color = this.Stroke.ColorFor(context);

        canvas.StrokeColor = color.WithAlpha(color.Alpha * alpha);
        canvas.StrokeSize = this.StrokeWidth;
        canvas.StrokeLineCap = this.LineCap;
        canvas.StrokeLineJoin = this.LineJoin;
        canvas.MiterLimit = this.MiterLimit;
        canvas.StrokeDashPattern = this.DashPattern;
        canvas.StrokeDashOffset = this.DashOffset;

        canvas.DrawPath(this.Path);
    }
}


/// <summary>A run of text.</summary>
/// <remarks>
/// Text is drawn with the platform's own font stack rather than converted to outlines, so a family
/// the device does not have falls back the way every other string in the app does. Layout is
/// single-line: <c>textPath</c>, and a <c>tspan</c> carrying its own position, are not represented.
/// </remarks>
sealed record SvgText : SvgNode
{
    /// <summary>The string to draw.</summary>
    public required string Text { get; init; }

    /// <summary>The baseline origin.</summary>
    public required PointF Origin { get; init; }

    /// <summary>Font size in this node's coordinate space.</summary>
    public float FontSize { get; init; } = 16f;

    /// <summary>The resolved font, or null to use the canvas default.</summary>
    public Microsoft.Maui.Graphics.Font? Font { get; init; }

    /// <summary>Which end of the string sits at <see cref="Origin"/>.</summary>
    public HorizontalAlignment Alignment { get; init; } = HorizontalAlignment.Left;

    /// <summary>The fill, or null when the text is invisible.</summary>
    public SvgPaintServer? Fill { get; init; }

    /// <summary><c>fill-opacity</c>.</summary>
    public float FillOpacity { get; init; } = 1f;

    /// <inheritdoc />
    public override int Weight => 1;


    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, in SvgDrawContext context, float opacity)
    {
        if (this.Fill is null || String.IsNullOrEmpty(this.Text))
            return;

        var alpha = opacity * this.FillOpacity;
        if (alpha <= 0f)
            return;

        var color = this.Fill.ColorFor(context);

        canvas.FontColor = color.WithAlpha(color.Alpha * alpha);
        canvas.FontSize = this.FontSize;

        if (this.Font is { } font)
            canvas.Font = font;

        canvas.DrawString(this.Text, this.Origin.X, this.Origin.Y, this.Alignment);
    }
}
