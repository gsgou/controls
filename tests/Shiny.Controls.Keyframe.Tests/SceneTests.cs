using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Tests;

public class SceneTests
{
    /// <summary>Records the calls a scene makes, so drawing can be asserted without a real surface.</summary>
    sealed class RecordingCanvas : ICanvas
    {
        public List<string> Calls { get; } = [];
        public List<float> AlphaValues { get; } = [];

        public float Alpha { set { AlphaValues.Add(value); Calls.Add($"Alpha={value:F3}"); } }

        public void Translate(float tx, float ty) => Calls.Add($"Translate({tx:F2},{ty:F2})");
        public void Scale(float sx, float sy) => Calls.Add($"Scale({sx:F2},{sy:F2})");
        public void Rotate(float degrees) => Calls.Add($"Rotate({degrees:F2})");
        public void SaveState() => Calls.Add("Save");
        public bool RestoreState() { Calls.Add("Restore"); return true; }
        public void FillRectangle(float x, float y, float w, float h) => Calls.Add($"FillRect({x:F2},{y:F2},{w:F2},{h:F2})");
        public void FillEllipse(float x, float y, float w, float h) => Calls.Add($"FillEllipse({x:F2},{y:F2},{w:F2},{h:F2})");

        // --- Everything below is inert plumbing the interface requires. ---
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

        public void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero) { }
        public void ClipRectangle(float x, float y, float width, float height) => Calls.Add("Clip");
        public void ConcatenateTransform(System.Numerics.Matrix3x2 transform) => Calls.Add("Concat");
        public void DrawArc(float x, float y, float w, float h, float a1, float a2, bool clockwise, bool closed) { }
        public void DrawEllipse(float x, float y, float width, float height) { }
        public void DrawImage(IImage image, float x, float y, float width, float height) { }
        public void DrawLine(float x1, float y1, float x2, float y2) { }
        public void DrawPath(PathF path) { }
        public void DrawRectangle(float x, float y, float width, float height) { }
        public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius) { }
        public void DrawString(string value, float x, float y, HorizontalAlignment alignment) { }
        public void DrawString(string value, float x, float y, float width, float height,
            HorizontalAlignment h, VerticalAlignment v, TextFlow f = TextFlow.ClipBounds, float lineSpacing = 0) { }
        public void DrawText(Microsoft.Maui.Graphics.Text.IAttributedText value, float x, float y, float width, float height) { }
        public void Rotate(float degrees, float x, float y) => Calls.Add($"Rotate({degrees:F2},{x:F2},{y:F2})");
        public SizeF GetStringSize(string value, IFont font, float fontSize) => SizeF.Zero;
        public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment h, VerticalAlignment v) => SizeF.Zero;
        public void FillArc(float x, float y, float w, float h, float a1, float a2, bool clockwise) { }
        public void FillPath(PathF path, WindingMode windingMode) { }
        public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
            => Calls.Add($"FillRounded({x:F2},{y:F2},{width:F2},{height:F2},{cornerRadius:F2})");
        public void ResetState() { }
        public void SetFillPaint(Paint paint, RectF rectangle) { }
        public void SetShadow(SizeF offset, float blur, Color color) { }
        public void SubtractFromClip(float x, float y, float width, float height) { }
    }

    [Fact]
    public void OpacityCompoundsThroughNestedGroups()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        var group = scene.Add(new GroupLayer { Opacity = 0.5f, Size = new SizeF(100, 100) });
        group.Add(new RectangleLayer { Opacity = 0.5f, Size = new SizeF(10, 10), Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        // 0.5 on the group times 0.5 on the rectangle.
        Assert.Contains(0.25f, canvas.AlphaValues);
    }

    [Fact]
    public void FullyTransparentSubtreesAreSkippedEntirely()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        var group = scene.Add(new GroupLayer { Opacity = 0f, Size = new SizeF(100, 100) });
        group.Add(new RectangleLayer { Size = new SizeF(10, 10), Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        Assert.DoesNotContain(canvas.Calls, c => c.StartsWith("FillRect", StringComparison.Ordinal));
    }

    [Fact]
    public void InvisibleLayersAreNotDrawn()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        scene.Add(new RectangleLayer { IsVisible = false, Size = new SizeF(10, 10), Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        Assert.DoesNotContain(canvas.Calls, c => c.StartsWith("FillRect", StringComparison.Ordinal));
    }

    [Fact]
    public void RotationPivotsAboutTheAnchorNotTheOrigin()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        scene.Add(new RectangleLayer
        {
            Size = new SizeF(20, 20),
            Rotation = 90f,
            Fill = Colors.Red
        });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        // Default anchor is the centre, so the pivot is offset by half the size and back again.
        var index = canvas.Calls.IndexOf("Rotate(90.00)");
        Assert.True(index > 0, "Expected a rotation to be applied.");
        Assert.Equal("Translate(10.00,10.00)", canvas.Calls[index - 1]);
        Assert.Equal("Translate(-10.00,-10.00)", canvas.Calls[index + 1]);
    }

    [Fact]
    public void NoTransformIsEmittedWhenALayerIsUntransformed()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        scene.Add(new RectangleLayer { Size = new SizeF(20, 20), Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        Assert.DoesNotContain(canvas.Calls, c => c.StartsWith("Rotate", StringComparison.Ordinal));
        Assert.DoesNotContain(canvas.Calls, c => c.StartsWith("Scale", StringComparison.Ordinal));
    }

    [Fact]
    public void UniformStretchLettersboxesAndCentres()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.Uniform, ClipToBounds = false };
        scene.Add(new RectangleLayer { Size = new SizeF(10, 10), Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 200, 100));

        // Scale 1.0 (limited by height), horizontally centred in the 200-wide canvas.
        Assert.Contains("Scale(1.00,1.00)", canvas.Calls);
        Assert.Contains("Translate(50.00,0.00)", canvas.Calls);
    }

    [Fact]
    public void UniformToFillCoversAndCropsSymmetrically()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.UniformToFill, ClipToBounds = false };

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 200, 100));

        Assert.Contains("Scale(2.00,2.00)", canvas.Calls);
        Assert.Contains("Translate(0.00,-50.00)", canvas.Calls);
    }

    [Fact]
    public void FillStretchDistortsEachAxisIndependently()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.Fill, ClipToBounds = false };

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 200, 50));

        Assert.Contains("Scale(2.00,0.50)", canvas.Calls);
    }

    [Fact]
    public void SaveAndRestoreAreBalanced()
    {
        var scene = new KeyframeScene(100, 100);
        var group = scene.Add(new GroupLayer { Size = new SizeF(100, 100) });
        group.Add(new RectangleLayer { Size = new SizeF(10, 10), Fill = Colors.Red });
        group.Add(new EllipseLayer { Size = new SizeF(10, 10), Fill = Colors.Blue });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        Assert.Equal(
            canvas.Calls.Count(c => c == "Save"),
            canvas.Calls.Count(c => c == "Restore"));
    }

    [Fact]
    public void FindByIdSearchesTheWholeSubtree()
    {
        var scene = new KeyframeScene(100, 100);
        var group = scene.Add(new GroupLayer { Id = "group", Size = new SizeF(100, 100) });
        var nested = group.Add(new GroupLayer { Id = "nested" });
        var target = nested.Add(new RectangleLayer { Id = "target" });

        Assert.Same(target, scene.FindById("target"));
        Assert.Null(scene.FindById("missing"));
    }

    [Fact]
    public void DescendantsEnumeratesDepthFirst()
    {
        var scene = new KeyframeScene(100, 100);
        var group = scene.Add(new GroupLayer { Id = "a" });
        group.Add(new RectangleLayer { Id = "b" });
        group.Add(new EllipseLayer { Id = "c" });

        Assert.Equal(["a", "b", "c"], scene.Root.Descendants().Select(l => l.Id));
    }

    [Fact]
    public void GroupRejectsContainingItself()
    {
        var group = new GroupLayer();
        Assert.Throws<ArgumentException>(() => group.Add(group));
    }

    [Fact]
    public void SceneSeekDrivesLayerProperties()
    {
        var scene = new KeyframeScene(100, 100);
        var rect = scene.Add(new RectangleLayer { Size = new SizeF(10, 10), Fill = Colors.Red });

        scene.Animation = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .Fill(FillMode.Both)
            .AnimateOpacity(rect, k => k.From(0f).To(1f))
            .Build();

        scene.Animation.CaptureBaselines();
        scene.SeekProgress(0.5d);

        Assert.Equal(0.5f, rect.Opacity, 3);
    }

    [Fact]
    public void SeekProgressIsRejectedForInfiniteAnimations()
    {
        var scene = new KeyframeScene(100, 100);
        var rect = scene.Add(new RectangleLayer { Size = new SizeF(10, 10) });

        scene.Animation = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .RepeatForever()
            .AnimateOpacity(rect, k => k.From(0f).To(1f))
            .Build();

        Assert.Throws<InvalidOperationException>(() => scene.SeekProgress(0.5d));
    }

    [Fact]
    public void NonPositiveDesignSizeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyframeScene(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyframeScene(100, -1));
    }

    [Fact]
    public void OverLargeCornerRadiusIsClampedToAStadium()
    {
        var scene = new KeyframeScene(100, 100) { Stretch = SceneStretch.None, ClipToBounds = false };
        scene.Add(new RectangleLayer { Size = new SizeF(40, 20), CornerRadius = 999f, Fill = Colors.Red });

        var canvas = new RecordingCanvas();
        scene.Draw(canvas, new RectF(0, 0, 100, 100));

        Assert.Contains("FillRounded(0.00,0.00,40.00,20.00,10.00)", canvas.Calls);
    }
}
