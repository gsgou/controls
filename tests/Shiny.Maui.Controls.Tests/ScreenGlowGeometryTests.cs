using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.QuickEntry;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The maths behind the screen-edge glow. Both renderers — MAUI Graphics on macOS/Linux and GDI+ on
/// Windows — consume these numbers, so a mistake here is a mistake on every platform at once, and
/// it is the only part of the glow that can be tested without a display.
/// </summary>
public class ScreenGlowGeometryTests
{
    static readonly ScreenGlowOptions Options = new() { BlobCount = 4 };

    [Fact]
    public void Compute_produces_one_blob_per_requested_pool()
    {
        var blobs = ScreenGlowGeometry.Compute(1000, 600, 100, 0, 0, new ScreenGlowOptions { BlobCount = 7 });
        blobs.Length.ShouldBe(7);
    }

    [Fact]
    public void Compute_never_produces_zero_blobs()
    {
        var blobs = ScreenGlowGeometry.Compute(1000, 600, 100, 0, 0, new ScreenGlowOptions { BlobCount = 0 });
        blobs.Length.ShouldBe(1);
    }

    [Fact]
    public void Phase_zero_starts_at_the_top_left_corner()
    {
        var blobs = ScreenGlowGeometry.Compute(800, 800, 50, 0, 0, Options);
        blobs[0].X.ShouldBe(0f, 0.001f);
        blobs[0].Y.ShouldBe(0f, 0.001f);
    }

    [Theory]
    // A 400x400 square has a perimeter of 1600, so each quarter lap is exactly one corner.
    [InlineData(0.25, 400f, 0f)]
    [InlineData(0.50, 400f, 400f)]
    [InlineData(0.75, 0f, 400f)]
    public void Blobs_walk_the_perimeter_clockwise(double phase, float x, float y)
    {
        var blobs = ScreenGlowGeometry.Compute(400, 400, 50, phase, 0, new ScreenGlowOptions { BlobCount = 1 });
        blobs[0].X.ShouldBe(x, 0.01f);
        blobs[0].Y.ShouldBe(y, 0.01f);
    }

    [Fact]
    public void A_full_lap_returns_to_the_start()
    {
        var start = ScreenGlowGeometry.Compute(640, 480, 50, 0, 0, new ScreenGlowOptions { BlobCount = 1 })[0];
        var lap = ScreenGlowGeometry.Compute(640, 480, 50, 1, 0, new ScreenGlowOptions { BlobCount = 1 })[0];

        lap.X.ShouldBe(start.X, 0.01f);
        lap.Y.ShouldBe(start.Y, 0.01f);
    }

    [Fact]
    public void Negative_phase_wraps_rather_than_leaving_the_rectangle()
    {
        var blobs = ScreenGlowGeometry.Compute(400, 400, 50, -0.25, 0, new ScreenGlowOptions { BlobCount = 1 });
        blobs[0].X.ShouldBe(0f, 0.01f);
        blobs[0].Y.ShouldBe(400f, 0.01f);
    }

    [Fact]
    public void Blobs_are_spaced_evenly_around_the_loop()
    {
        var blobs = ScreenGlowGeometry.Compute(400, 400, 50, 0, 0, new ScreenGlowOptions { BlobCount = 4 });

        blobs[0].X.ShouldBe(0f, 0.01f);
        blobs[1].X.ShouldBe(400f, 0.01f);
        blobs[2].X.ShouldBe(400f, 0.01f);
        blobs[3].X.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void Colour_advances_independently_of_position()
    {
        // The whole point of the split: the edge changes colour without the pools having to travel,
        // which is what stops the glow reading as a chase light.
        var options = new ScreenGlowOptions { BlobCount = 1 };
        var first = ScreenGlowGeometry.Compute(400, 400, 50, 0, 0, options)[0];
        var later = ScreenGlowGeometry.Compute(400, 400, 50, 0, 0.5, options)[0];

        later.X.ShouldBe(first.X, 0.01f);
        later.Y.ShouldBe(first.Y, 0.01f);
        later.Color.ShouldNotBe(first.Color);
    }

    [Fact]
    public void Palette_sampling_lands_on_the_first_colour_at_zero()
    {
        var palette = new List<Color> { Colors.Red, Colors.Lime, Colors.Blue };
        ScreenGlowGeometry.SamplePalette(palette, 0).ShouldBe(Colors.Red);
    }

    [Fact]
    public void Palette_sampling_loops_so_the_ramp_has_no_seam()
    {
        var palette = new List<Color> { Colors.Red, Colors.Lime };

        // Just short of a full lap the sample must be heading back to the first colour, not
        // hard-stopping on the last one.
        var nearlyLooped = ScreenGlowGeometry.SamplePalette(palette, 0.999);
        nearlyLooped.Red.ShouldBeGreaterThan(0.9f);
        nearlyLooped.Green.ShouldBeLessThan(0.1f);
    }

    [Fact]
    public void An_empty_palette_is_not_a_crash()
        => ScreenGlowGeometry.SamplePalette(new List<Color>(), 0.4).ShouldBe(Colors.White);

    [Fact]
    public void A_single_colour_palette_is_that_colour_everywhere()
    {
        var palette = new List<Color> { Colors.Magenta };
        ScreenGlowGeometry.SamplePalette(palette, 0.13).ShouldBe(Colors.Magenta);
        ScreenGlowGeometry.SamplePalette(palette, 0.87).ShouldBe(Colors.Magenta);
    }
}
