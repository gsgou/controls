using System.Globalization;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Picks readable text for a surface whose background the app chose per item.
/// </summary>
/// <remarks>
/// Calendar event chips and chat bubbles both take a colour supplied per event or per user, so no
/// single ink works for all of them: a fixed white disappears on a pale amber and a fixed near-black
/// disappears on a deep indigo. Nor does a theme token help - the background does not follow the
/// theme, so <c>on-surface</c> is the right ink only by coincidence, and in dark mode it is reliably
/// wrong. Deriving the ink from the colour itself is the only answer that holds for every value in
/// both schemes.
/// </remarks>
static class ContrastInk
{
    const string Light = "#FFFFFF";
    const string Dark = "#1A1A1A";

    /// <summary>
    /// Returns white or near-black for <paramref name="background"/>, whichever reads on it.
    /// Returns <paramref name="fallback"/> for anything that is not a plain hex colour - a
    /// <c>var()</c> token, a gradient, a named colour - because those either already carry a
    /// matching ink or cannot be measured here.
    /// </summary>
    public static string For(string? background, string? fallback = null)
    {
        if (!TryParseHex(background, out var r, out var g, out var b))
            return fallback ?? Light;

        // Relative luminance, sRGB. The 0.5 split lands where WCAG contrast against white and
        // against black cross over closely enough for a chip label.
        var luminance = (0.2126 * Channel(r)) + (0.7152 * Channel(g)) + (0.0722 * Channel(b));
        return luminance > 0.45 ? Dark : Light;
    }

    static double Channel(int value)
    {
        var c = value / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    static bool TryParseHex(string? value, out int r, out int g, out int b)
    {
        r = g = b = 0;

        var hex = value?.Trim();
        if (string.IsNullOrEmpty(hex) || hex[0] != '#')
            return false;

        hex = hex[1..];

        // #RGB shorthand doubles each nibble; #RRGGBBAA drops the alpha, which does not change the
        // hue the label sits on in any way that matters here.
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        else if (hex.Length == 8)
            hex = hex[..6];

        if (hex.Length != 6)
            return false;

        return int.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && int.TryParse(hex[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
