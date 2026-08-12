using Microsoft.Maui.Graphics;
using Shiny.Controls.Keyframe.Graphics;

namespace Shiny.Controls.Keyframe.Tests;

public class PathTrimTests
{
    static PathF Line(params float[] coordinates)
    {
        var path = new PathF();
        path.MoveTo(coordinates[0], coordinates[1]);

        for (var i = 2; i < coordinates.Length; i += 2)
            path.LineTo(coordinates[i], coordinates[i + 1]);

        return path;
    }

    [Fact]
    public void AnUntrimmedPathIsHandedStraightBack()
    {
        // Rebuilding a path that is not being trimmed would flatten its curves for nothing, on
        // every frame, for every finished draw-on on screen.
        var path = Line(0, 0, 10, 0);

        Assert.Same(path, PathTrimmer.Trim(path, 0f, 1f));
    }

    [Fact]
    public void AnEmptySpanProducesNothing()
    {
        Assert.Equal(0, PathTrimmer.Trim(Line(0, 0, 10, 0), 0f, 0f).OperationCount);
        Assert.Equal(0, PathTrimmer.Trim(Line(0, 0, 10, 0), 0.8f, 0.2f).OperationCount);
    }

    [Fact]
    public void TrimmingHalfALineStopsAtTheMidpoint()
    {
        var trimmed = PathTrimmer.Trim(Line(0f, 0f, 10f, 0f), 0.5f);

        Assert.Equal(5f, trimmed.LastPoint.X, 3);
        Assert.Equal(0f, trimmed.LastPoint.Y, 3);
    }

    [Fact]
    public void TrimmingMeasuresAlongThePathNotAcrossIt()
    {
        // An L of two equal legs: halfway along it is the corner, not the point halfway between the
        // two ends. Getting this wrong is what makes a draw-on cut corners.
        var trimmed = PathTrimmer.Trim(Line(0f, 0f, 10f, 0f, 10f, 10f), 0.5f);

        Assert.Equal(10f, trimmed.LastPoint.X, 3);
        Assert.Equal(0f, trimmed.LastPoint.Y, 3);
    }

    [Fact]
    public void ASpanStartsWhereItIsAskedTo()
    {
        var trimmed = PathTrimmer.Trim(Line(0f, 0f, 10f, 0f), 0.25f, 0.75f);

        Assert.Equal(2.5f, trimmed.FirstPoint.X, 3);
        Assert.Equal(7.5f, trimmed.LastPoint.X, 3);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    public void TrimmedLengthMatchesTheRequestedFraction(float fraction)
    {
        var path = Line(0f, 0f, 10f, 0f, 10f, 10f, 0f, 10f);

        Assert.Equal(30f * fraction, PathTrimmer.Measure(PathTrimmer.Trim(path, fraction)), 1);
    }

    [Fact]
    public void CurvesAreMeasuredNotApproximatedByTheirEndpoints()
    {
        // A semicircular arc from (2,12) to (22,12) is about 31.4 units long, not 20. Measuring it
        // chord to chord would make every rounded icon draw on at the wrong rate.
        var path = new PathBuilder().BuildPath("M2 12a10 10 0 0 1 20 0");

        Assert.Equal(31.4f, PathTrimmer.Measure(path), 0);
    }

    [Fact]
    public void MultipleSubpathsAreTrimmedInOrder()
    {
        var path = new PathF();
        path.MoveTo(0f, 0f);
        path.LineTo(10f, 0f);
        path.MoveTo(0f, 5f);
        path.LineTo(10f, 5f);

        var trimmed = PathTrimmer.Trim(path, 0.25f);

        Assert.Equal(5f, PathTrimmer.Measure(trimmed), 3);
        Assert.Equal(0f, trimmed.LastPoint.Y, 3);
    }

    [Fact]
    public void ASpanCanStraddleTwoSubpaths()
    {
        var path = new PathF();
        path.MoveTo(0f, 0f);
        path.LineTo(10f, 0f);
        path.MoveTo(0f, 5f);
        path.LineTo(10f, 5f);

        // 40% to 60% of 20 total units — the last 2 of the first stroke and the first 2 of the second.
        var trimmed = PathTrimmer.Trim(path, 0.4f, 0.6f);

        Assert.Equal(4f, PathTrimmer.Measure(trimmed), 3);
        Assert.Equal(2, trimmed.SegmentTypes.Count(x => x is PathOperation.Move));
    }

    [Fact]
    public void ALayerDrawsNothingUntilItsTrimOpens()
    {
        var layer = new PathLayer
        {
            Data = Line(0f, 0f, 10f, 0f),
            Stroke = Colors.Black,
            StrokeWidth = 1f,
            TrimEnd = 0f
        };

        var canvas = new CountingCanvas();
        layer.Draw(canvas);

        // A zero-length path drawn with a round cap would otherwise leave a visible dot sitting
        // where the stroke begins.
        Assert.Equal(0, canvas.StrokedPaths);

        layer.TrimEnd = 1f;
        layer.Draw(canvas);

        Assert.Equal(1, canvas.StrokedPaths);
    }

    [Fact]
    public void TrimmingLeavesTheFillAlone()
    {
        // Trim paths apply to the stroke; a partially drawn outline filled in reads as a bug.
        var layer = new PathLayer
        {
            Data = Line(0f, 0f, 10f, 0f, 10f, 10f, 0f, 0f),
            Fill = Colors.Red,
            Stroke = Colors.Black,
            StrokeWidth = 1f,
            TrimEnd = 0.5f
        };

        var canvas = new CountingCanvas();
        layer.Draw(canvas);

        Assert.Equal(1, canvas.FilledPaths);
        Assert.Equal(1, canvas.StrokedPaths);
    }

    [Fact]
    public void PerAxisTracksComposeRatherThanFight()
    {
        // Two tracks driving one Position would normally have the last one evaluated win, resetting
        // the other axis to whatever the track's own keys say. These read the axis they do not own.
        var layer = new RectangleLayer { Size = new SizeF(10, 10) };

        var timeline = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .AnimatePositionX(layer, k => k.From(0f).To(10f))
            .AnimatePositionY(layer, k => k.From(0f).To(20f))
            .Build();

        timeline.Evaluate(TimeSpan.FromMilliseconds(500));

        Assert.Equal(5f, layer.Position.X, 3);
        Assert.Equal(10f, layer.Position.Y, 3);
    }

    [Fact]
    public void PerAxisScaleTracksAlsoCompose()
    {
        var layer = new RectangleLayer { Size = new SizeF(10, 10) };

        // Held at the end rather than evaluated at it: with the default FillMode.None the tracks
        // revert to their baselines the moment the timeline is past, which would read as the two
        // axes failing to compose when in fact both had already finished.
        var timeline = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .HoldEnd()
            .AnimateScaleX(layer, k => k.From(1f).To(2f))
            .AnimateScaleY(layer, k => k.From(1f).To(3f))
            .Build();

        timeline.Evaluate(TimeSpan.FromSeconds(1));

        Assert.Equal(2f, layer.Scale.Width, 3);
        Assert.Equal(3f, layer.Scale.Height, 3);
    }

    [Fact]
    public void TrimEndCanBeAnimated()
    {
        var layer = new PathLayer { Data = Line(0f, 0f, 10f, 0f) };

        var timeline = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .AnimateTrimEnd(layer, k => k.From(0f).To(1f))
            .Build();

        timeline.Evaluate(TimeSpan.FromMilliseconds(250));

        Assert.Equal(0.25f, layer.TrimEnd, 3);
    }

    sealed class CountingCanvas : ICanvas
    {
        public int StrokedPaths { get; private set; }
        public int FilledPaths { get; private set; }

        public void DrawPath(PathF path) => StrokedPaths++;
        public void FillPath(PathF path, WindingMode windingMode) => FilledPaths++;

        public float Alpha { set { } }
        public Color FillColor { set { } }
        public Color StrokeColor { set { } }
        public Color FontColor { set { } }
        public IFont Font { set { } }
        public float FontSize { set { } }
        public float StrokeSize { set { } }
        public float MiterLimit { set { } }
        public LineCap StrokeLineCap { set { } }
        public LineJoin StrokeLineJoin { set { } }
        public float[]? StrokeDashPattern { set { } }
        public float StrokeDashOffset { set { } }
        public BlendMode BlendMode { set { } }
        public bool Antialias { set { } }
        public float DisplayScale { get; set; } = 1f;

        public void Translate(float tx, float ty) { }
        public void Scale(float sx, float sy) { }
        public void Rotate(float degrees) { }
        public void Rotate(float degrees, float x, float y) { }
        public void SaveState() { }
        public bool RestoreState() => true;
        public void ResetState() { }
        public void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero) { }
        public void ClipRectangle(float x, float y, float width, float height) { }
        public void ConcatenateTransform(System.Numerics.Matrix3x2 transform) { }
        public void DrawArc(float x, float y, float w, float h, float a1, float a2, bool clockwise, bool closed) { }
        public void DrawEllipse(float x, float y, float width, float height) { }
        public void DrawImage(IImage image, float x, float y, float width, float height) { }
        public void DrawLine(float x1, float y1, float x2, float y2) { }
        public void DrawRectangle(float x, float y, float width, float height) { }
        public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }
        public void DrawString(string value, float x, float y, HorizontalAlignment alignment) { }
        public void DrawString(string value, float x, float y, float width, float height,
            HorizontalAlignment h, VerticalAlignment v, TextFlow f = TextFlow.ClipBounds, float lineSpacing = 0) { }
        public void DrawText(Microsoft.Maui.Graphics.Text.IAttributedText value, float x, float y, float width, float height) { }
        public SizeF GetStringSize(string value, IFont font, float fontSize) => SizeF.Zero;
        public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment h, VerticalAlignment v) => SizeF.Zero;
        public void FillArc(float x, float y, float w, float h, float a1, float a2, bool clockwise) { }
        public void FillEllipse(float x, float y, float width, float height) { }
        public void FillRectangle(float x, float y, float width, float height) { }
        public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }
        public void SetFillPaint(Paint paint, RectF rectangle) { }
        public void SetShadow(SizeF offset, float blur, Color color) { }
        public void SubtractFromClip(float x, float y, float width, float height) { }
    }
}
