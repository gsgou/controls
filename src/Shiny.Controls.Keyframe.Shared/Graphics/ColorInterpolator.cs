using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>Which colour space a colour blend is performed in.</summary>
public enum ColorSpace
{
    /// <summary>
    /// Blend the encoded sRGB channels directly. Cheapest, and what most frameworks do by default,
    /// but it darkens and desaturates through the middle — blue to yellow passes through grey.
    /// </summary>
    Srgb,

    /// <summary>
    /// Blend in linear-light sRGB. Physically correct for light mixing, though it can look
    /// perceptually uneven because equal steps do not read as equal changes.
    /// </summary>
    LinearRgb,

    /// <summary>
    /// Blend in Oklab. Perceptually uniform, so the midpoint of a gradient looks like the midpoint.
    /// This is the default because it is the one that consistently looks right.
    /// </summary>
    Oklab
}

/// <summary>
/// Blends <see cref="Color"/> values, defaulting to the perceptually uniform Oklab space.
/// </summary>
/// <remarks>
/// The difference is not subtle. Interpolating blue to yellow in sRGB dips through a muddy grey
/// around the midpoint; in Oklab it passes through the greens and stays saturated. Alpha is always
/// blended linearly, independent of the chosen space.
/// </remarks>
public sealed class ColorInterpolator : IInterpolator<Color>
{
    /// <summary>Perceptually uniform blending. The sensible default.</summary>
    public static readonly ColorInterpolator Oklab = new(ColorSpace.Oklab);

    /// <summary>Naive channel blending, matching what most UI frameworks do.</summary>
    public static readonly ColorInterpolator Srgb = new(ColorSpace.Srgb);

    /// <summary>Physically-correct light mixing.</summary>
    public static readonly ColorInterpolator LinearRgb = new(ColorSpace.LinearRgb);

    readonly ColorSpace space;

    /// <summary>Creates an interpolator for the given colour space.</summary>
    public ColorInterpolator(ColorSpace space) => this.space = space;

    /// <inheritdoc />
    public Color Interpolate(Color from, Color to, double progress)
    {
        from ??= Colors.Transparent;
        to ??= Colors.Transparent;

        var t = (float)progress;
        var alpha = Lerp(from.Alpha, to.Alpha, t);

        return space switch
        {
            ColorSpace.Srgb => new Color(
                Lerp(from.Red, to.Red, t),
                Lerp(from.Green, to.Green, t),
                Lerp(from.Blue, to.Blue, t),
                alpha),

            ColorSpace.LinearRgb => FromLinear(
                Lerp(ToLinear(from.Red), ToLinear(to.Red), t),
                Lerp(ToLinear(from.Green), ToLinear(to.Green), t),
                Lerp(ToLinear(from.Blue), ToLinear(to.Blue), t),
                alpha),

            _ => InterpolateOklab(from, to, t, alpha)
        };
    }

    static Color InterpolateOklab(Color from, Color to, float t, float alpha)
    {
        var a = RgbToOklab(from);
        var b = RgbToOklab(to);

        return OklabToRgb(
            Lerp(a.L, b.L, t),
            Lerp(a.A, b.A, t),
            Lerp(a.B, b.B, t),
            alpha);
    }

    static float Lerp(float from, float to, float t) => from + (to - from) * t;

    // --- sRGB transfer function ---------------------------------------------------------

    static float ToLinear(float channel) => channel <= 0.04045f
        ? channel / 12.92f
        : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);

    static float FromLinear(float channel) => channel <= 0.0031308f
        ? channel * 12.92f
        : 1.055f * MathF.Pow(channel, 1f / 2.4f) - 0.055f;

    static Color FromLinear(float r, float g, float b, float alpha) => new(
        Math.Clamp(FromLinear(r), 0f, 1f),
        Math.Clamp(FromLinear(g), 0f, 1f),
        Math.Clamp(FromLinear(b), 0f, 1f),
        Math.Clamp(alpha, 0f, 1f));

    // --- Oklab (Björn Ottosson's matrices) ----------------------------------------------

    static (float L, float A, float B) RgbToOklab(Color color)
    {
        var r = ToLinear(color.Red);
        var g = ToLinear(color.Green);
        var b = ToLinear(color.Blue);

        var l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
        var m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
        var s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

        var lRoot = MathF.Cbrt(l);
        var mRoot = MathF.Cbrt(m);
        var sRoot = MathF.Cbrt(s);

        return (
            0.2104542553f * lRoot + 0.7936177850f * mRoot - 0.0040720468f * sRoot,
            1.9779984951f * lRoot - 2.4285922050f * mRoot + 0.4505937099f * sRoot,
            0.0259040371f * lRoot + 0.7827717662f * mRoot - 0.8086757660f * sRoot);
    }

    static Color OklabToRgb(float lightness, float a, float b, float alpha)
    {
        var lRoot = lightness + 0.3963377774f * a + 0.2158037573f * b;
        var mRoot = lightness - 0.1055613458f * a - 0.0638541728f * b;
        var sRoot = lightness - 0.0894841775f * a - 1.2914855480f * b;

        var l = lRoot * lRoot * lRoot;
        var m = mRoot * mRoot * mRoot;
        var s = sRoot * sRoot * sRoot;

        return FromLinear(
            +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s,
            alpha);
    }
}
