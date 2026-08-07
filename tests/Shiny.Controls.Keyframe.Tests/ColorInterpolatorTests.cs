using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Tests;

public class ColorInterpolatorTests
{
    [Fact]
    public void EndpointsAreReproducedExactly()
    {
        foreach (var interpolator in new[] { ColorInterpolator.Oklab, ColorInterpolator.Srgb, ColorInterpolator.LinearRgb })
        {
            var start = interpolator.Interpolate(Colors.Red, Colors.Blue, 0d);
            var end = interpolator.Interpolate(Colors.Red, Colors.Blue, 1d);

            AssertColorsClose(Colors.Red, start);
            AssertColorsClose(Colors.Blue, end);
        }
    }

    [Fact]
    public void AlphaBlendsLinearlyRegardlessOfColorSpace()
    {
        var from = Color.FromRgba(1f, 0f, 0f, 0f);
        var to = Color.FromRgba(0f, 0f, 1f, 1f);

        foreach (var interpolator in new[] { ColorInterpolator.Oklab, ColorInterpolator.Srgb, ColorInterpolator.LinearRgb })
            Assert.Equal(0.5f, interpolator.Interpolate(from, to, 0.5d).Alpha, 3);
    }

    [Fact]
    public void OklabKeepsTheMidpointMoreSaturatedThanSrgb()
    {
        // The headline reason to bother with Oklab: blending blue to yellow through sRGB dips
        // toward grey, while Oklab keeps chroma up. Measure "greyness" as how close the channels are.
        var oklab = ColorInterpolator.Oklab.Interpolate(Colors.Blue, Colors.Yellow, 0.5d);
        var srgb = ColorInterpolator.Srgb.Interpolate(Colors.Blue, Colors.Yellow, 0.5d);

        Assert.True(ChannelSpread(oklab) > ChannelSpread(srgb),
            $"Expected Oklab midpoint to stay more saturated. Oklab spread {ChannelSpread(oklab):F3}, sRGB {ChannelSpread(srgb):F3}.");
    }

    [Fact]
    public void OklabRoundTripsAColorThroughAZeroLengthBlend()
    {
        // Blending a colour with itself exercises the full RGB→Oklab→RGB path; any error in the
        // matrices shows up here immediately.
        foreach (var color in new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.White, Colors.Black, Colors.Orange })
        {
            var result = ColorInterpolator.Oklab.Interpolate(color, color, 0.5d);
            AssertColorsClose(color, result, tolerance: 0.002f);
        }
    }

    [Fact]
    public void OklabLightnessProgressesMonotonically()
    {
        var previous = -1d;

        for (var i = 0; i <= 20; i++)
        {
            var color = ColorInterpolator.Oklab.Interpolate(Colors.Black, Colors.White, i / 20d);
            var luminance = 0.2126d * color.Red + 0.7152d * color.Green + 0.0722d * color.Blue;

            Assert.True(luminance >= previous - 1e-4d, $"Luminance went backwards at t={i / 20d}.");
            previous = luminance;
        }
    }

    [Fact]
    public void ChannelsStayInRangeEvenWhenEasingOvershoots()
    {
        // An overshooting curve hands progress outside [0,1] to the interpolator. Colour channels
        // genuinely cannot represent out-of-gamut values, so this is the one place clamping is right.
        var over = ColorInterpolator.Oklab.Interpolate(Colors.Red, Colors.Blue, 1.4d);
        var under = ColorInterpolator.Oklab.Interpolate(Colors.Red, Colors.Blue, -0.4d);

        foreach (var color in new[] { over, under })
        {
            Assert.InRange(color.Red, 0f, 1f);
            Assert.InRange(color.Green, 0f, 1f);
            Assert.InRange(color.Blue, 0f, 1f);
            Assert.InRange(color.Alpha, 0f, 1f);
        }
    }

    [Fact]
    public void SrgbBlendsRawChannels()
    {
        var result = ColorInterpolator.Srgb.Interpolate(Colors.Black, Colors.White, 0.5d);

        Assert.Equal(0.5f, result.Red, 3);
        Assert.Equal(0.5f, result.Green, 3);
        Assert.Equal(0.5f, result.Blue, 3);
    }

    [Fact]
    public void LinearRgbMidpointIsBrighterThanSrgbMidpoint()
    {
        // Blending in linear light and re-encoding lands around 0.73, not 0.5 — the classic
        // "why is my gradient darker than expected" difference.
        var linear = ColorInterpolator.LinearRgb.Interpolate(Colors.Black, Colors.White, 0.5d);
        var srgb = ColorInterpolator.Srgb.Interpolate(Colors.Black, Colors.White, 0.5d);

        Assert.True(linear.Red > srgb.Red);
    }

    [Fact]
    public void NullColorsAreTreatedAsTransparent()
    {
        var result = ColorInterpolator.Oklab.Interpolate(null!, Colors.Red, 0d);
        Assert.Equal(0f, result.Alpha, 3);
    }

    static float ChannelSpread(Color color)
        => Math.Max(color.Red, Math.Max(color.Green, color.Blue))
         - Math.Min(color.Red, Math.Min(color.Green, color.Blue));

    static void AssertColorsClose(Color expected, Color actual, float tolerance = 0.001f)
    {
        Assert.Equal(expected.Red, actual.Red, tolerance);
        Assert.Equal(expected.Green, actual.Green, tolerance);
        Assert.Equal(expected.Blue, actual.Blue, tolerance);
        Assert.Equal(expected.Alpha, actual.Alpha, tolerance);
    }
}
