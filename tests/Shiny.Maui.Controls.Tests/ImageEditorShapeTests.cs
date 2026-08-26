using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.ImageEditor;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The shape tools are a drag between two points, and everything that can go wrong with that is in
/// the one pure function that turns those points into bounds: dragging backwards, and the circle
/// constraint - which has to shrink to the smaller extent, since the drag was already clamped to
/// the image and growing to the larger one would push the shape off it.
/// </summary>
public class ImageEditorShapeTests
{
    [Fact]
    public void Rectangle_ForwardDrag_UsesDragAsIs()
    {
        var rect = ImageEditorDrawable.BuildShapeRect(
            new PointF(10, 20), new PointF(110, 70), ImageEditorShape.Rectangle);

        rect.X.ShouldBe(10);
        rect.Y.ShouldBe(20);
        rect.Width.ShouldBe(100);
        rect.Height.ShouldBe(50);
    }

    [Fact]
    public void Rectangle_BackwardDrag_Normalizes()
    {
        var rect = ImageEditorDrawable.BuildShapeRect(
            new PointF(110, 70), new PointF(10, 20), ImageEditorShape.Rectangle);

        rect.X.ShouldBe(10);
        rect.Y.ShouldBe(20);
        rect.Width.ShouldBe(100);
        rect.Height.ShouldBe(50);
    }

    [Fact]
    public void Ellipse_IsNotConstrained()
    {
        var rect = ImageEditorDrawable.BuildShapeRect(
            new PointF(0, 0), new PointF(100, 40), ImageEditorShape.Ellipse);

        rect.Width.ShouldBe(100);
        rect.Height.ShouldBe(40);
    }

    [Fact]
    public void Circle_TakesTheSmallerExtent()
    {
        var rect = ImageEditorDrawable.BuildShapeRect(
            new PointF(0, 0), new PointF(100, 40), ImageEditorShape.Circle);

        rect.X.ShouldBe(0);
        rect.Y.ShouldBe(0);
        rect.Width.ShouldBe(40);
        rect.Height.ShouldBe(40);
    }

    [Fact]
    public void Circle_BackwardDrag_GrowsFromTheStartCorner()
    {
        // Dragging up and to the left from (100,100): the square has to end at the start corner,
        // not begin at the finger
        var rect = ImageEditorDrawable.BuildShapeRect(
            new PointF(100, 100), new PointF(20, 40), ImageEditorShape.Circle);

        rect.Width.ShouldBe(60);
        rect.Height.ShouldBe(60);
        rect.X.ShouldBe(40);
        rect.Y.ShouldBe(40);
        rect.Right.ShouldBe(100);
        rect.Bottom.ShouldBe(100);
    }

    [Fact]
    public void Circle_StaysInsideTheDragBox()
    {
        var start = new PointF(30, 30);
        var end = new PointF(200, 90);
        var rect = ImageEditorDrawable.BuildShapeRect(start, end, ImageEditorShape.Circle);

        rect.X.ShouldBeGreaterThanOrEqualTo(Math.Min(start.X, end.X));
        rect.Y.ShouldBeGreaterThanOrEqualTo(Math.Min(start.Y, end.Y));
        rect.Right.ShouldBeLessThanOrEqualTo(Math.Max(start.X, end.X));
        rect.Bottom.ShouldBeLessThanOrEqualTo(Math.Max(start.Y, end.Y));
    }

    [Theory]
    [InlineData(ImageEditorToolMode.Rectangle, true)]
    [InlineData(ImageEditorToolMode.Ellipse, true)]
    [InlineData(ImageEditorToolMode.Circle, true)]
    [InlineData(ImageEditorToolMode.Draw, false)]
    [InlineData(ImageEditorToolMode.Line, false)]
    [InlineData(ImageEditorToolMode.Arrow, false)]
    [InlineData(ImageEditorToolMode.Text, false)]
    [InlineData(ImageEditorToolMode.Crop, false)]
    [InlineData(ImageEditorToolMode.Move, false)]
    public void IsShapeMode_CoversOnlyTheShapeTools(ImageEditorToolMode mode, bool expected)
        => ImageEditorDrawable.IsShapeMode(mode).ShouldBe(expected);
}
