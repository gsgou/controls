namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>One travelling pool of colour in the screen-edge glow.</summary>
readonly record struct ScreenGlowBlob(float X, float Y, float Radius, Color Color);

/// <summary>
/// Works out where the glow's colour pools sit for a given animation phase, and what colour each
/// one is.
/// </summary>
/// <remarks>
/// Kept separate from any drawing API because the two renderers that consume it have nothing else
/// in common: macOS and Linux draw through <c>Microsoft.Maui.Graphics</c> inside a transparent
/// MAUI window, while Windows has no per-pixel-transparent XAML window and has to render the same
/// frame with GDI+ into a layered Win32 window.
/// </remarks>
static class ScreenGlowGeometry
{
    /// <summary>
    /// Positions <paramref name="count"/> pools evenly around the perimeter of a
    /// <paramref name="width"/> × <paramref name="height"/> rectangle, offset by
    /// <paramref name="phase"/> (0–1 is one full lap), and colours them from the palette at
    /// <paramref name="colorPhase"/>.
    /// </summary>
    /// <remarks>
    /// Position and colour advance separately on purpose. Sampling the palette at the pool's own
    /// position would nail each colour to a place on the edge, so the only way to see it change
    /// would be for the pools to travel — which reads as a chase light. Cycling the colour on its
    /// own clock lets the whole edge shift hue while barely moving.
    /// </remarks>
    public static ScreenGlowBlob[] Compute(double width, double height, double thickness, double phase, double colorPhase, ScreenGlowOptions options)
    {
        var count = Math.Max(1, options.BlobCount);
        var blobs = new ScreenGlowBlob[count];
        var radius = (float)(thickness * 2.4d);

        for (var i = 0; i < count; i++)
        {
            var offset = (double)i / count;
            var t = Wrap(phase + offset);
            var (x, y) = PointOnPerimeter(width, height, t);

            // A fraction of the offset, not the whole of it: the pools stay close enough in hue to
            // read as one wash rather than as separate coloured lights.
            var colour = SamplePalette(options.Palette, colorPhase + offset * 0.4d);
            blobs[i] = new ScreenGlowBlob((float)x, (float)y, radius, colour);
        }
        return blobs;
    }

    /// <summary>
    /// Maps 0–1 onto a walk around the rectangle's edge, starting at the top-left and going
    /// clockwise.
    /// </summary>
    static (double X, double Y) PointOnPerimeter(double width, double height, double t)
    {
        var perimeter = 2 * (width + height);
        var d = Wrap(t) * perimeter;

        if (d < width)
            return (d, 0);
        d -= width;

        if (d < height)
            return (width, d);
        d -= height;

        if (d < width)
            return (width - d, height);
        d -= width;

        return (0, height - d);
    }

    /// <summary>Samples the palette as a loop, so the ramp has no seam where it wraps.</summary>
    public static Color SamplePalette(IList<Color> palette, double t)
    {
        if (palette.Count == 0)
            return Colors.White;
        if (palette.Count == 1)
            return palette[0];

        var scaled = Wrap(t) * palette.Count;
        var index = (int)scaled;
        var fraction = (float)(scaled - index);

        var from = palette[index % palette.Count];
        var to = palette[(index + 1) % palette.Count];

        return Color.FromRgba(
            from.Red + (to.Red - from.Red) * fraction,
            from.Green + (to.Green - from.Green) * fraction,
            from.Blue + (to.Blue - from.Blue) * fraction,
            from.Alpha + (to.Alpha - from.Alpha) * fraction
        );
    }

    static double Wrap(double t)
    {
        t %= 1d;
        return t < 0 ? t + 1d : t;
    }
}
