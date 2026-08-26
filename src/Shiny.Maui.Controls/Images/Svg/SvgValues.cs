using System.Globalization;
using System.Numerics;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// The small parsers SVG attributes need: numbers, lengths, colours, transform lists and point runs.
/// </summary>
/// <remarks>
/// Everything here is deliberately forgiving. Artwork arrives from designers, export tools and CDNs,
/// and a single unrecognised unit should cost one attribute rather than the whole drawing - so every
/// entry point takes a fallback and none of them throw.
/// </remarks>
static class SvgValues
{
    static readonly char[] ListSeparators = [' ', ',', '\t', '\r', '\n'];

    // The CSS absolute units, expressed in the 96dpi user units SVG measures in.
    const float PointsToPixels = 96f / 72f;
    const float PicasToPixels = 16f;
    const float MillimetresToPixels = 96f / 25.4f;
    const float CentimetresToPixels = 96f / 2.54f;
    const float InchesToPixels = 96f;


    /// <summary>Parses a bare number, ignoring any trailing unit.</summary>
    public static float? ParseNumber(string? text)
    {
        if (String.IsNullOrWhiteSpace(text))
            return null;

        var span = text.AsSpan().Trim();
        var digits = TakeNumeric(span);

        return digits.IsEmpty || !Single.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? null
            : value;
    }


    /// <summary>
    /// Parses a length, resolving units and percentages.
    /// </summary>
    /// <param name="text">The attribute value.</param>
    /// <param name="percentBasis">What 100% means here - the viewport width, height or diagonal.</param>
    /// <param name="fallback">Returned when the value is missing or unparseable.</param>
    public static float ParseLength(string? text, float percentBasis, float fallback)
    {
        if (String.IsNullOrWhiteSpace(text))
            return fallback;

        var span = text.AsSpan().Trim();
        var digits = TakeNumeric(span);

        if (digits.IsEmpty || !Single.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return fallback;

        var unit = span[digits.Length..].Trim();
        if (unit.IsEmpty)
            return value;

        if (unit.SequenceEqual("%"))
            return value / 100f * percentBasis;

        return unit switch
        {
            "px" => value,
            "pt" => value * PointsToPixels,
            "pc" => value * PicasToPixels,
            "mm" => value * MillimetresToPixels,
            "cm" => value * CentimetresToPixels,
            "in" => value * InchesToPixels,
            _ => value
        };
    }


    /// <summary>True when the value is written as a percentage rather than an absolute length.</summary>
    public static bool IsPercentage(string? text)
        => text is not null && text.AsSpan().Trim().EndsWith("%", StringComparison.Ordinal);


    /// <summary>
    /// Parses an opacity, which SVG allows to be written either as a fraction or as a percentage.
    /// </summary>
    public static float ParseOpacity(string? text, float fallback)
    {
        if (String.IsNullOrWhiteSpace(text))
            return fallback;

        if (ParseNumber(text) is not { } value)
            return fallback;

        if (IsPercentage(text))
            value /= 100f;

        return Math.Clamp(value, 0f, 1f);
    }


    /// <summary>Parses a <c>viewBox</c>: four numbers, min-x min-y width height.</summary>
    public static RectF? ParseViewBox(string? text)
    {
        if (String.IsNullOrWhiteSpace(text))
            return null;

        var parts = text.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return null;

        var numbers = new float[4];
        for (var i = 0; i < 4; i++)
        {
            if (ParseNumber(parts[i]) is not { } value)
                return null;

            numbers[i] = value;
        }

        // A zero or negative extent means "render nothing" per spec, and would otherwise divide by
        // zero when the viewBox is mapped onto the viewport.
        return numbers[2] <= 0f || numbers[3] <= 0f
            ? null
            : new RectF(numbers[0], numbers[1], numbers[2], numbers[3]);
    }


    /// <summary>Parses a whitespace/comma separated run of numbers.</summary>
    public static float[] ParseNumberList(string? text)
    {
        if (String.IsNullOrWhiteSpace(text))
            return [];

        var parts = text.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
        var values = new List<float>(parts.Length);

        foreach (var part in parts)
        {
            if (ParseNumber(part) is { } value)
                values.Add(value);
        }

        return [.. values];
    }


    /// <summary>Parses a <c>points</c> attribute into coordinate pairs, dropping a dangling odd value.</summary>
    public static PointF[] ParsePoints(string? text)
    {
        var numbers = ParseNumberList(text);
        var count = numbers.Length / 2;
        var points = new PointF[count];

        for (var i = 0; i < count; i++)
            points[i] = new PointF(numbers[i * 2], numbers[(i * 2) + 1]);

        return points;
    }


    /// <summary>
    /// Parses a <c>stroke-dasharray</c>. Returns null for <c>none</c>, an empty list, or a pattern
    /// that sums to zero.
    /// </summary>
    /// <remarks>
    /// The values come back divided by the stroke width, because <c>ICanvas.StrokeDashPattern</c> is
    /// expressed in multiples of the stroke while SVG expresses it in user units.
    /// </remarks>
    public static float[]? ParseDashArray(string? text, float strokeWidth)
    {
        if (String.IsNullOrWhiteSpace(text) || text.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;

        var numbers = ParseNumberList(text);
        if (numbers.Length == 0 || strokeWidth <= 0f)
            return null;

        var total = 0f;
        for (var i = 0; i < numbers.Length; i++)
        {
            numbers[i] = Math.Max(0f, numbers[i]) / strokeWidth;
            total += numbers[i];
        }

        if (total <= 0f)
            return null;

        // An odd count repeats to make the on/off pairs whole, exactly as the spec requires.
        return numbers.Length % 2 == 1 ? [.. numbers, .. numbers] : numbers;
    }


    /// <summary>
    /// Parses a transform list into a single matrix. Entries compose left to right, so the leftmost
    /// function is the outermost one.
    /// </summary>
    public static Matrix3x2 ParseTransform(string? text)
    {
        var matrix = Matrix3x2.Identity;

        if (String.IsNullOrWhiteSpace(text))
            return matrix;

        var index = 0;
        while (index < text.Length)
        {
            var open = text.IndexOf('(', index);
            if (open < 0)
                break;

            var close = text.IndexOf(')', open + 1);
            if (close < 0)
                break;

            var name = text[index..open].Trim(ListSeparators).Trim();
            var args = ParseNumberList(text[(open + 1)..close]);
            index = close + 1;

            if (BuildTransform(name, args) is { } step)
                matrix = step * matrix;
        }

        return matrix;
    }


    static Matrix3x2? BuildTransform(string name, float[] args) => name switch
    {
        "matrix" when args.Length >= 6 => new Matrix3x2(args[0], args[1], args[2], args[3], args[4], args[5]),
        "translate" when args.Length >= 2 => Matrix3x2.CreateTranslation(args[0], args[1]),
        "translate" when args.Length == 1 => Matrix3x2.CreateTranslation(args[0], 0f),
        "scale" when args.Length >= 2 => Matrix3x2.CreateScale(args[0], args[1]),
        "scale" when args.Length == 1 => Matrix3x2.CreateScale(args[0], args[0]),
        // The three-argument form rotates about a point rather than the origin.
        "rotate" when args.Length >= 3 => Matrix3x2.CreateRotation(Radians(args[0]), new Vector2(args[1], args[2])),
        "rotate" when args.Length == 1 => Matrix3x2.CreateRotation(Radians(args[0])),
        "skewX" when args.Length >= 1 => Matrix3x2.CreateSkew(Radians(args[0]), 0f),
        "skewY" when args.Length >= 1 => Matrix3x2.CreateSkew(0f, Radians(args[0])),
        _ => null
    };


    static float Radians(float degrees) => degrees * MathF.PI / 180f;


    /// <summary>
    /// Parses a colour. Understands hex in all four lengths, <c>rgb()</c>/<c>rgba()</c>,
    /// <c>hsl()</c>/<c>hsla()</c>, <c>currentColor</c>, and the CSS colour names.
    /// </summary>
    /// <param name="text">The attribute value.</param>
    /// <param name="currentColor">What <c>currentColor</c> resolves to.</param>
    /// <returns>Null for <c>none</c>, an empty value, or anything unrecognised.</returns>
    public static Color? ParseColor(string? text, Color currentColor)
    {
        if (String.IsNullOrWhiteSpace(text))
            return null;

        var value = text.Trim();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            return null;

        if (value.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            return currentColor;

        if (value[0] == '#')
            return ParseHex(value.AsSpan(1));

        var open = value.IndexOf('(');
        if (value.EndsWith(')') && open > 0)
        {
            var function = value[..open].Trim().ToLowerInvariant();
            var args = value[(open + 1)..^1];

            return function switch
            {
                "rgb" or "rgba" => ParseRgb(args),
                "hsl" or "hsla" => ParseHsl(args),
                _ => null
            };
        }

        // Covers the CSS colour names. TryParse rather than Parse: an unknown name is artwork the
        // author got wrong, not something worth throwing over.
        return Color.TryParse(value, out var named) ? named : null;
    }


    static Color? ParseHex(ReadOnlySpan<char> hex)
    {
        static int? Nibble(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => null
        };

        if (hex.Length is not (3 or 4 or 6 or 8))
            return null;

        Span<int> parts = stackalloc int[8];
        for (var i = 0; i < hex.Length; i++)
        {
            if (Nibble(hex[i]) is not { } nibble)
                return null;

            parts[i] = nibble;
        }

        // The short forms double each digit: #abc is #aabbcc, not #0a0b0c.
        if (hex.Length is 3 or 4)
        {
            var shortAlpha = hex.Length == 4 ? (parts[3] * 17) / 255f : 1f;
            return new Color((parts[0] * 17) / 255f, (parts[1] * 17) / 255f, (parts[2] * 17) / 255f, shortAlpha);
        }

        var r = (parts[0] * 16) + parts[1];
        var g = (parts[2] * 16) + parts[3];
        var b = (parts[4] * 16) + parts[5];
        var a = hex.Length == 8 ? (parts[6] * 16) + parts[7] : 255;

        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }


    static Color? ParseRgb(string args)
    {
        var (channels, alphaText) = SplitAlpha(args);
        if (channels.Length < 3)
            return null;

        var values = new float[3];
        for (var i = 0; i < 3; i++)
        {
            if (ParseNumber(channels[i]) is not { } value)
                return null;

            values[i] = IsPercentage(channels[i]) ? value / 100f : value / 255f;
        }

        return new Color(
            Math.Clamp(values[0], 0f, 1f),
            Math.Clamp(values[1], 0f, 1f),
            Math.Clamp(values[2], 0f, 1f),
            alphaText is null ? 1f : ParseOpacity(alphaText, 1f)
        );
    }


    static Color? ParseHsl(string args)
    {
        var (channels, alphaText) = SplitAlpha(args);
        if (channels.Length < 3)
            return null;

        if (ParseNumber(channels[0]) is not { } hue ||
            ParseNumber(channels[1]) is not { } saturation ||
            ParseNumber(channels[2]) is not { } lightness)
            return null;

        return Color.FromHsla(
            ((hue % 360f) + 360f) % 360f / 360f,
            Math.Clamp(saturation / 100f, 0f, 1f),
            Math.Clamp(lightness / 100f, 0f, 1f),
            alphaText is null ? 1f : ParseOpacity(alphaText, 1f)
        );
    }


    // The modern space-separated syntax - rgb(255 0 0 / 50%) - puts the alpha after a slash; the
    // legacy comma syntax makes it a fourth argument. Both end up in the same place here.
    static (string[] Channels, string? Alpha) SplitAlpha(string args)
    {
        string? alpha = null;

        var slash = args.IndexOf('/');
        if (slash >= 0)
        {
            alpha = args[(slash + 1)..];
            args = args[..slash];
        }

        var parts = args.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
        alpha ??= parts.Length > 3 ? parts[3] : null;

        return (parts, alpha);
    }


    static ReadOnlySpan<char> TakeNumeric(ReadOnlySpan<char> span)
    {
        var length = 0;
        var seenDigit = false;

        while (length < span.Length)
        {
            var c = span[length];

            if (Char.IsAsciiDigit(c))
            {
                seenDigit = true;
                length++;
                continue;
            }

            var isSign = (c is '+' or '-') && (length == 0 || span[length - 1] is 'e' or 'E');
            var isExponent = (c is 'e' or 'E') && seenDigit;

            if (c == '.' || isSign || isExponent)
            {
                length++;
                continue;
            }

            break;
        }

        return seenDigit ? span[..length] : [];
    }
}
