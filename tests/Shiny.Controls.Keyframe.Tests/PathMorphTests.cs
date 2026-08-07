using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Tests;

public class PathMorphTests
{
    static PathF Triangle(float scale)
    {
        var path = new PathF();
        path.MoveTo(0f, 0f);
        path.LineTo(scale, 0f);
        path.LineTo(scale / 2f, scale);
        path.Close();
        return path;
    }

    [Fact]
    public void MatchingStructuresBlendPointwise()
    {
        var result = PathFInterpolator.Instance.Interpolate(Triangle(10f), Triangle(20f), 0.5d);

        Assert.Equal(4, result.OperationCount);
        Assert.Equal(new PointF(0f, 0f), result[0]);
        Assert.Equal(new PointF(15f, 0f), result[1]);
        Assert.Equal(new PointF(7.5f, 15f), result[2]);
    }

    [Fact]
    public void CurvesBlendTheirControlPointsToo()
    {
        var from = new PathF();
        from.MoveTo(0f, 0f);
        from.CurveTo(0f, 0f, 10f, 0f, 10f, 10f);

        var to = new PathF();
        to.MoveTo(0f, 0f);
        to.CurveTo(0f, 20f, 30f, 20f, 30f, 30f);

        var result = PathFInterpolator.Instance.Interpolate(from, to, 0.5d);

        Assert.Equal(2, result.OperationCount);
        Assert.Equal(new PointF(0f, 10f), result[1]);
        Assert.Equal(new PointF(20f, 10f), result[2]);
        Assert.Equal(new PointF(20f, 20f), result[3]);
    }

    [Fact]
    public void EndpointsAreReproducedExactly()
    {
        var from = Triangle(10f);
        var to = Triangle(20f);

        Assert.Equal(from[1], PathFInterpolator.Instance.Interpolate(from, to, 0d)[1]);
        Assert.Equal(to[1], PathFInterpolator.Instance.Interpolate(from, to, 1d)[1]);
    }

    [Fact]
    public void MismatchedStructuresFallBackToAHardCut()
    {
        var triangle = Triangle(10f);

        var square = new PathF();
        square.MoveTo(0f, 0f);
        square.LineTo(10f, 0f);
        square.LineTo(10f, 10f);
        square.LineTo(0f, 10f);
        square.Close();

        Assert.Same(triangle, PathFInterpolator.Instance.Interpolate(triangle, square, 0.4d));
        Assert.Same(square, PathFInterpolator.Instance.Interpolate(triangle, square, 0.6d));
    }

    [Fact]
    public void StrictModeSurfacesAMismatchLoudly()
    {
        var triangle = Triangle(10f);

        var line = new PathF();
        line.MoveTo(0f, 0f);
        line.LineTo(10f, 10f);

        var error = Assert.Throws<InvalidOperationException>(
            () => PathFInterpolator.Strict.Interpolate(triangle, line, 0.5d));

        Assert.Contains("different structures", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SameOperationCountButDifferentOperationTypesIsAMismatch()
    {
        var withLine = new PathF();
        withLine.MoveTo(0f, 0f);
        withLine.LineTo(10f, 10f);

        var withQuad = new PathF();
        withQuad.MoveTo(0f, 0f);
        withQuad.QuadTo(5f, 5f, 10f, 10f);

        Assert.Same(withLine, PathFInterpolator.Instance.Interpolate(withLine, withQuad, 0.4d));
    }

    [Fact]
    public void NullPathsDegradeGracefully()
    {
        var triangle = Triangle(10f);

        Assert.Same(triangle, PathFInterpolator.Instance.Interpolate(null!, triangle, 0.5d));
        Assert.Same(triangle, PathFInterpolator.Instance.Interpolate(triangle, null!, 0.5d));
    }
}
