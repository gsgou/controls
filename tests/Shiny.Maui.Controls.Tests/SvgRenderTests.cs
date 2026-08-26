using System.Numerics;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Images.Svg;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// An <see cref="ICanvas"/> that records rather than draws, so the mapping from a document's viewBox
/// onto the bounds it is given can be asserted without a platform surface.
/// </summary>
/// <remarks>
/// The transform is tracked the way a real backend tracks it - a matrix stack pushed by
/// <c>SaveState</c> - because that composition is exactly what the tests are here to check.
/// </remarks>
sealed class RecordingCanvas : ICanvas
{
    readonly Stack<Matrix3x2> saved = new();

    public Matrix3x2 Transform { get; private set; } = Matrix3x2.Identity;

    public List<RectF> Clips { get; } = [];

    /// <summary>Every filled path, with the transform that was in force, and the colour used.</summary>
    public List<(PathF Path, Matrix3x2 Transform, Color Color)> Fills { get; } = [];

    /// <summary>Every stroked path, with the transform in force and the stroke width in user units.</summary>
    public List<(PathF Path, Matrix3x2 Transform, Color Color, float Width)> Strokes { get; } = [];

    /// <summary>Maps a point from document space into canvas space.</summary>
    public PointF Project(PointF point, Matrix3x2 transform)
    {
        var mapped = Vector2.Transform(new Vector2(point.X, point.Y), transform);
        return new PointF(mapped.X, mapped.Y);
    }

    public void SaveState() => this.saved.Push(this.Transform);

    public bool RestoreState()
    {
        if (this.saved.Count == 0)
            return false;

        this.Transform = this.saved.Pop();
        return true;
    }

    public void ResetState()
    {
        this.saved.Clear();
        this.Transform = Matrix3x2.Identity;
    }

    public void ConcatenateTransform(Matrix3x2 transform) => this.Transform = transform * this.Transform;
    public void Translate(float tx, float ty) => this.ConcatenateTransform(Matrix3x2.CreateTranslation(tx, ty));
    public void Scale(float sx, float sy) => this.ConcatenateTransform(Matrix3x2.CreateScale(sx, sy));
    public void Rotate(float degrees) => this.ConcatenateTransform(Matrix3x2.CreateRotation(degrees * MathF.PI / 180f));
    public void Rotate(float degrees, float x, float y)
        => this.ConcatenateTransform(Matrix3x2.CreateRotation(degrees * MathF.PI / 180f, new Vector2(x, y)));

    public void ClipRectangle(float x, float y, float width, float height) => this.Clips.Add(new RectF(x, y, width, height));

    public void FillPath(PathF path, WindingMode windingMode) => this.Fills.Add((path, this.Transform, this.FillColor));
    public void DrawPath(PathF path) => this.Strokes.Add((path, this.Transform, this.StrokeColor, this.StrokeSize));

    public Color FillColor { get; set; } = Colors.White;
    public Color StrokeColor { get; set; } = Colors.Black;
    public float StrokeSize { get; set; } = 1f;
    public float MiterLimit { get; set; } = 4f;
    public LineCap StrokeLineCap { get; set; }
    public LineJoin StrokeLineJoin { get; set; }
    public float[]? StrokeDashPattern { get; set; }
    public float StrokeDashOffset { get; set; }
    public Color FontColor { get; set; } = Colors.Black;
    public IFont? Font { get; set; }
    public float FontSize { get; set; }
    public float Alpha { get; set; } = 1f;
    public bool Antialias { get; set; } = true;
    public BlendMode BlendMode { get; set; }
    public float DisplayScale { get; set; } = 1f;

    public Paint? LastFillPaint { get; private set; }
    public RectF LastFillPaintBounds { get; private set; }

    public void SetFillPaint(Paint paint, RectF rectangle)
    {
        this.LastFillPaint = paint;
        this.LastFillPaintBounds = rectangle;
    }

    public List<string> Strings { get; } = [];

    public void DrawString(string value, float x, float y, HorizontalAlignment horizontalAlignment) => this.Strings.Add(value);

    public void DrawString(string value, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment, TextFlow textFlow = TextFlow.ClipBounds, float lineSpacingAdjustment = 0)
        => this.Strings.Add(value);

    // Nothing below is reachable from the SVG renderer, which reduces everything it draws to a path.
    public void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero) { }
    public void SubtractFromClip(float x, float y, float width, float height) { }
    public void DrawLine(float x1, float y1, float x2, float y2) { }
    public void DrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed) { }
    public void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise) { }
    public void DrawRectangle(float x, float y, float width, float height) { }
    public void FillRectangle(float x, float y, float width, float height) { }
    public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }
    public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }
    public void DrawEllipse(float x, float y, float width, float height) { }
    public void FillEllipse(float x, float y, float width, float height) { }
    public void DrawImage(Microsoft.Maui.Graphics.IImage image, float x, float y, float width, float height) { }
    public void DrawText(Microsoft.Maui.Graphics.Text.IAttributedText value, float x, float y, float width, float height) { }
    public void SetShadow(SizeF offset, float blur, Color color) { }
    public SizeF GetStringSize(string value, IFont font, float fontSize) => SizeF.Zero;
    public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment h, VerticalAlignment v) => SizeF.Zero;
}


/// <summary>
/// That a document actually lands where it was told to. The parse tests cover what was read; these
/// cover the arithmetic between a viewBox and the rectangle it is drawn into.
/// </summary>
public class SvgRenderTests
{
    const string Square =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
        "<rect width='10' height='10' fill='red' /></svg>";

    static (RecordingCanvas Canvas, PathF Path, Matrix3x2 Transform) Draw(
        string markup, RectF bounds, Aspect aspect = Aspect.AspectFit, Color? tint = null)
    {
        var canvas = new RecordingCanvas();
        SvgDocument.Parse(markup).Draw(canvas, bounds, aspect, tint ?? Colors.Black);

        canvas.Fills.Count.ShouldBeGreaterThan(0);
        var fill = canvas.Fills[0];

        return (canvas, fill.Path, fill.Transform);
    }


    [Fact]
    public void ViewBox_IsMappedOntoTheBounds()
    {
        var (canvas, _, transform) = Draw(Square, new RectF(0f, 0f, 100f, 100f));

        // The viewBox corners have to land on the bounds corners, or nothing else about the drawing
        // is in the right place.
        canvas.Project(new PointF(0f, 0f), transform).ShouldBe(new PointF(0f, 0f));
        canvas.Project(new PointF(10f, 10f), transform).ShouldBe(new PointF(100f, 100f));
    }


    [Fact]
    public void ViewBox_Origin_IsSubtracted()
    {
        // A viewBox that does not start at zero is how exporters crop; the offset must come out.
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='5 5 10 10'>" +
                     "<rect x='5' y='5' width='10' height='10' fill='red' /></svg>";

        var (canvas, _, transform) = Draw(markup, new RectF(0f, 0f, 100f, 100f));

        canvas.Project(new PointF(5f, 5f), transform).ShouldBe(new PointF(0f, 0f));
        canvas.Project(new PointF(15f, 15f), transform).ShouldBe(new PointF(100f, 100f));
    }


    [Fact]
    public void AspectFit_Letterboxes_AndCentres()
    {
        var (canvas, _, transform) = Draw(Square, new RectF(0f, 0f, 200f, 100f));

        // Uniform scale of 10, centred horizontally: 50 units of slack on each side.
        var topLeft = canvas.Project(new PointF(0f, 0f), transform);
        var bottomRight = canvas.Project(new PointF(10f, 10f), transform);

        topLeft.X.ShouldBe(50f, 0.001f);
        topLeft.Y.ShouldBe(0f, 0.001f);
        bottomRight.X.ShouldBe(150f, 0.001f);
        bottomRight.Y.ShouldBe(100f, 0.001f);
    }


    [Fact]
    public void AspectFill_Overflows_AndIsClipped()
    {
        var (canvas, _, transform) = Draw(Square, new RectF(0f, 0f, 200f, 100f), Aspect.AspectFill);

        // Scale of 20 to cover the width, so the drawing is twice as tall as the box and centred.
        canvas.Project(new PointF(0f, 0f), transform).Y.ShouldBe(-50f, 0.001f);
        canvas.Project(new PointF(10f, 10f), transform).Y.ShouldBe(150f, 0.001f);

        // A vector has no frame of its own to stop at, so the clip is what keeps it inside the cell.
        canvas.Clips.ShouldContain(new RectF(0f, 0f, 200f, 100f));
    }


    [Fact]
    public void Fill_StretchesEachAxisIndependently()
    {
        var (canvas, _, transform) = Draw(Square, new RectF(0f, 0f, 200f, 100f), Aspect.Fill);

        canvas.Project(new PointF(10f, 10f), transform).ShouldBe(new PointF(200f, 100f));
    }


    [Fact]
    public void PreserveAspectRatio_DecidesWhereTheSlackGoes()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10' preserveAspectRatio='xMinYMid'>" +
                     "<rect width='10' height='10' fill='red' /></svg>";

        var (canvas, _, transform) = Draw(markup, new RectF(0f, 0f, 200f, 100f));

        // xMin pins the drawing to the left rather than centring it.
        canvas.Project(new PointF(0f, 0f), transform).X.ShouldBe(0f, 0.001f);
    }


    [Fact]
    public void Bounds_Offset_IsHonoured()
    {
        var (canvas, _, transform) = Draw(Square, new RectF(20f, 30f, 100f, 100f));

        canvas.Project(new PointF(0f, 0f), transform).ShouldBe(new PointF(20f, 30f));
    }


    [Fact]
    public void NestedTransforms_ComposeWithTheViewBoxMapping()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
                     "<g transform='translate(5 0)'><rect width='5' height='5' fill='red' /></g></svg>";

        var (canvas, _, transform) = Draw(markup, new RectF(0f, 0f, 100f, 100f));

        // The group's own translate is in document units, so it scales with everything else.
        canvas.Project(new PointF(0f, 0f), transform).X.ShouldBe(50f, 0.001f);
    }


    [Fact]
    public void CurrentColor_TakesTheTintHandedToDraw()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
                     "<rect width='10' height='10' fill='currentColor' /></svg>";

        var (teal, _, _) = Draw(markup, new RectF(0f, 0f, 10f, 10f), Aspect.AspectFit, Colors.Teal);
        var (red, _, _) = Draw(markup, new RectF(0f, 0f, 10f, 10f), Aspect.AspectFit, Colors.Red);

        teal.Fills[0].Color.ShouldBe(Colors.Teal);
        red.Fills[0].Color.ShouldBe(Colors.Red);
    }


    [Fact]
    public void Opacity_CompoundsThroughTheTree()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
                     "<g opacity='0.5'><rect width='10' height='10' fill='#ff0000' fill-opacity='0.5' /></g></svg>";

        var (canvas, _, _) = Draw(markup, new RectF(0f, 0f, 10f, 10f));

        canvas.Fills[0].Color.Alpha.ShouldBe(0.25f, 0.001f);
    }


    [Fact]
    public void Gradient_GoesThroughSetFillPaint_AgainstTheShapeBounds()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
                     "<defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs>" +
                     "<rect x='2' y='2' width='6' height='6' fill='url(#g)' /></svg>";

        var canvas = new RecordingCanvas();
        SvgDocument.Parse(markup).Draw(canvas, new RectF(0f, 0f, 100f, 100f), Aspect.AspectFit, Colors.Black);

        var paint = canvas.LastFillPaint.ShouldBeOfType<LinearGradientPaint>();
        paint.GradientStops.Length.ShouldBe(2);

        // The rectangle handed to SetFillPaint is what MAUI maps the 0..1 control points onto, so it
        // has to be the shape's own bounds in document units - not the whole viewBox.
        canvas.LastFillPaintBounds.X.ShouldBe(2f, 0.05f);
        canvas.LastFillPaintBounds.Width.ShouldBe(6f, 0.05f);
    }


    [Fact]
    public void GradientStroke_FallsBackToItsFirstStop()
    {
        // ICanvas fills with a Paint but strokes with a Color. Losing the ramp is a smaller lie than
        // losing the outline entirely.
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
                     "<defs><linearGradient id='g'><stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
                     "<path d='M0 0 L10 10' fill='none' stroke='url(#g)' stroke-width='2' /></svg>";

        var canvas = new RecordingCanvas();
        SvgDocument.Parse(markup).Draw(canvas, new RectF(0f, 0f, 10f, 10f), Aspect.AspectFit, Colors.Black);

        canvas.Strokes.Count.ShouldBe(1);
        canvas.Strokes[0].Color.ShouldBe(Colors.Red);
        canvas.Strokes[0].Width.ShouldBe(2f);
    }


    [Fact]
    public void ZeroSizedBounds_DrawNothing()
    {
        var canvas = new RecordingCanvas();
        SvgDocument.Parse(Square).Draw(canvas, new RectF(0f, 0f, 0f, 0f), Aspect.AspectFit, Colors.Black);

        canvas.Fills.ShouldBeEmpty();
    }


    [Fact]
    public void DrawingTwice_LeavesNoStateBehind()
    {
        // The same document is shared by every control showing that artwork, so a draw that leaked
        // canvas state would corrupt whatever was drawn next.
        var canvas = new RecordingCanvas();
        var document = SvgDocument.Parse(Square);

        document.Draw(canvas, new RectF(0f, 0f, 100f, 100f), Aspect.AspectFit, Colors.Black);
        document.Draw(canvas, new RectF(0f, 0f, 100f, 100f), Aspect.AspectFit, Colors.Black);

        canvas.Transform.ShouldBe(Matrix3x2.Identity);
        canvas.Fills[0].Transform.ShouldBe(canvas.Fills[1].Transform);
    }
}
