using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class CoordinateTransformTests
{
    // InvertOrientation is the exact inverse of ApplyOrientation — this is what lets the barcode scanner push a
    // ScanWindow (upright space) down to Vision's regionOfInterest / the MLKit crop (sensor space) and map back.
    [Theory]
    [InlineData(0, false)]
    [InlineData(90, false)]
    [InlineData(180, false)]
    [InlineData(270, false)]
    [InlineData(0, true)]
    [InlineData(90, true)]
    [InlineData(180, true)]
    [InlineData(270, true)]
    public void InvertOrientation_round_trips_ApplyOrientation(int rotation, bool mirrored)
    {
        var upright = new RectF(0.1f, 0.4f, 0.5f, 0.2f);

        var raw = CoordinateTransform.InvertOrientation(upright, rotation, mirrored);
        var back = CoordinateTransform.ApplyOrientation(raw, rotation, mirrored);

        back.X.ShouldBe(upright.X, 0.0001f);
        back.Y.ShouldBe(upright.Y, 0.0001f);
        back.Width.ShouldBe(upright.Width, 0.0001f);
        back.Height.ShouldBe(upright.Height, 0.0001f);
    }

    [Fact]
    public void InvertOrientation_unmirrors_with_no_rotation()
    {
        // a left-aligned band, mirror-corrected, sits on the right of the raw (un-mirrored) sensor frame
        var raw = CoordinateTransform.InvertOrientation(new RectF(0.1f, 0.4f, 0.2f, 0.2f), 0, mirrored: true);
        raw.X.ShouldBe(0.7f, 0.0001f);   // 1 - 0.1 - 0.2
        raw.Y.ShouldBe(0.4f, 0.0001f);
        raw.Width.ShouldBe(0.2f, 0.0001f);
        raw.Height.ShouldBe(0.2f, 0.0001f);
    }
}
