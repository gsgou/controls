using System.Globalization;

namespace Shiny.ThemeGen;

// The "personality" axes of a theme. Everything here is optional in the JSON: the defaults below
// reproduce the original colour-only output byte for byte, so adding a block to one theme can never
// move another one.

sealed record ShapeSpec(double Scale, IReadOnlyDictionary<string, double> Corners)
{
    public static readonly ShapeSpec Default = new(1d, new Dictionary<string, double>());
}

sealed record TypeSpec(
    string Family,
    string DisplayFamily,
    string MonoFamily,
    double Scale,
    int WeightOffset,
    double TrackingOffset,
    double LineHeightScale)
{
    public static readonly TypeSpec Default = new("", "", "", 1d, 0, 0d, 1d);
}

/// <summary>How a theme expresses depth: real shadows, nothing at all, a ring, or a coloured glow.</summary>
enum ElevationStyle { Shadow, Flat, Outline, Glow }

/// <summary>
/// Intensity and softness are deliberately separate: "big soft halo at low opacity" and "tight dark
/// shadow" are different looks that a single knob collapses into one dim, shrunken shadow.
/// </summary>
sealed record ElevationSpec(ElevationStyle Style, double Intensity, double Softness, bool TintPrimary)
{
    public static readonly ElevationSpec Default = new(ElevationStyle.Shadow, 1d, 1d, false);
}

sealed record DensitySpec(
    double Scale,
    double? ControlHeight,
    double? ControlHeightSmall,
    double? RowHeight)
{
    public static readonly DensitySpec Default = new(1d, null, null, null);
}

sealed record BorderSpec(double Thin, double Medium, double Thick)
{
    public static readonly BorderSpec Default = new(1d, 2d, 4d);
}

sealed record StateSpec(double Hover, double Focus, double Pressed, double Dragged)
{
    public static readonly StateSpec Default = new(0.08d, 0.10d, 0.10d, 0.16d);
}

sealed record TypeToken(string Role, double Size, double LineHeight, int Weight, double Tracking);

sealed record ShadowToken(string Name, double OffsetX, double OffsetY, double Radius, double Opacity, string ColorHex);

/// <summary>Turns the specs above into the concrete token values both emitters write out.</summary>
static class Resolve
{
    static double R(double v) => Math.Round(v, 4, MidpointRounding.ToEven);

    /// <summary>Layout metrics stay on whole pixels — subpixel padding just blurs edges.</summary>
    static double RPx(double v) => v == 0 ? 0 : Math.Max(1, Math.Round(v, MidpointRounding.AwayFromZero));

    public static IReadOnlyList<(string Name, double Value)> Shape(ShapeSpec s) =>
        Tokens.Shape
            .Select(t => (
                t.Name,
                // CornerFull is a sentinel ("as round as it goes"), never a measurement to scale.
                Value: s.Corners.TryGetValue(t.Name, out var v) ? v
                    : t.Value >= 9999 ? t.Value
                    : Math.Round(t.Value * s.Scale, 1, MidpointRounding.AwayFromZero)))
            .ToList();

    public static IReadOnlyList<TypeToken> Type(TypeSpec t) =>
        Tokens.Type
            .Select(x => new TypeToken(
                x.Role,
                R(x.Size * t.Scale),
                R(x.LineHeight * t.Scale * t.LineHeightScale),
                Math.Clamp(x.Weight + t.WeightOffset, 100, 900),
                R(x.Tracking + t.TrackingOffset)))
            .ToList();

    public static IReadOnlyList<(string Name, double Value)> Spacing(DensitySpec d) =>
        Tokens.Spacing
            .Select(s => (s.Name, RPx(s.Value * d.Scale)))
            .ToList();

    public static IReadOnlyList<(string Name, double Value)> Density(DensitySpec d) =>
    [
        ("Scale", R(d.Scale)),
        ("ControlHeight", RPx(d.ControlHeight ?? Tokens.ControlHeight * d.Scale)),
        ("ControlHeightSmall", RPx(d.ControlHeightSmall ?? Tokens.ControlHeightSmall * d.Scale)),
        ("RowHeight", RPx(d.RowHeight ?? Tokens.RowHeight * d.Scale)),
        // Never scaled: shrinking the hit target below the platform minimum is an accessibility bug,
        // not a design choice, so a compact theme gets tighter paint but the same touchable area.
        ("TouchTarget", Tokens.TouchTarget),
    ];

    public static IReadOnlyList<(string Name, double Value)> State(StateSpec s) =>
    [
        ("HoverOpacity", R(s.Hover)),
        ("FocusOpacity", R(s.Focus)),
        ("PressedOpacity", R(s.Pressed)),
        ("DraggedOpacity", R(s.Dragged)),
    ];

    public static IReadOnlyList<(string Name, double Value)> Border(BorderSpec b) =>
    [
        ("Thin", R(b.Thin)),
        ("Medium", R(b.Medium)),
        ("Thick", R(b.Thick)),
    ];

    // ---------------------------------------------------------------- elevation

    static string A(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    static string Px(double v) => v == 0 ? "0" : A(v) + "px";

    /// <summary>Shadow geometry — one decimal is well past what any display can resolve.</summary>
    static double G(double v) => Math.Round(v, 1, MidpointRounding.AwayFromZero);

    static double G3(double v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);

    /// <summary>CSS box-shadow per level. Tinted styles reference colour vars so they follow dark mode.</summary>
    public static IReadOnlyList<(string Name, string BoxShadow)> CssElevation(ElevationSpec e)
    {
        var list = new List<(string, string)>();
        for (var level = 0; level < Tokens.ElevationLayers.Length; level++)
        {
            var layers = Tokens.ElevationLayers[level];
            string css;

            if (level == 0 || e.Style == ElevationStyle.Flat)
            {
                css = "none";
            }
            else if (e.Style == ElevationStyle.Outline)
            {
                // A ring, not a shadow — the whole point of the style is that nothing floats.
                css = $"0 0 0 {Px(Math.Min(level, 2))} var(--shiny-color-outline-variant)";
            }
            else if (e.Style == ElevationStyle.Glow)
            {
                var blur = G(level * 6 * e.Softness);
                var pct = A(Math.Clamp(G(level * 8 * e.Intensity), 0, 100));
                css = $"0 0 {Px(blur)} color-mix(in srgb, var(--shiny-color-primary) {pct}%, transparent)";
            }
            else
            {
                var parts = layers.Select(l =>
                {
                    var alpha = Math.Clamp(Math.Round(l.Alpha * e.Intensity, 3, MidpointRounding.AwayFromZero), 0, 1);
                    var color = e.TintPrimary
                        ? $"color-mix(in srgb, var(--shiny-color-shadow) {A(Math.Round(alpha * 100, 2, MidpointRounding.AwayFromZero))}%, transparent)"
                        : $"rgba(0,0,0,{A(alpha)})";
                    var spread = l.Spread == 0 ? "" : $" {Px(G(l.Spread * e.Softness))}";
                    return $"0 {Px(G(l.OffsetY * e.Softness))} {Px(G(l.Blur * e.Softness))}{spread} {color}";
                });
                css = string.Join(", ", parts);
            }

            list.Add(($"Level{level}", css));
        }
        return list;
    }

    /// <summary>
    /// MAUI Shadow objects per level. Emitted per scheme so a tinted glow can bake the scheme's own
    /// primary — MAUI has no equivalent of resolving a colour var at paint time.
    /// </summary>
    public static IReadOnlyList<ShadowToken> MauiElevation(ElevationSpec e, string primaryHex, string shadowHex)
    {
        var list = new List<ShadowToken>();
        for (var level = 0; level < Tokens.MauiShadowLevels.Length; level++)
        {
            var (ox, oy, radius, opacity) = Tokens.MauiShadowLevels[level];

            // Flat and outline both mean "no drop shadow" on MAUI; outline themes get their character
            // from the border tokens instead, which controls apply to StrokeThickness.
            if (level == 0 || e.Style is ElevationStyle.Flat or ElevationStyle.Outline)
            {
                list.Add(new ShadowToken($"Level{level}", 0, 0, 0, 0, shadowHex));
                continue;
            }

            if (e.Style == ElevationStyle.Glow)
            {
                list.Add(new ShadowToken($"Level{level}", 0, 0, G(level * 5 * e.Softness), G3(Math.Clamp((0.18 + level * 0.06) * e.Intensity, 0, 1)), primaryHex));
                continue;
            }

            list.Add(new ShadowToken(
                $"Level{level}",
                ox,
                G(oy * e.Softness),
                G(radius * e.Softness),
                G3(Math.Clamp(opacity * e.Intensity, 0, 1)),
                e.TintPrimary ? primaryHex : shadowHex));
        }
        return list;
    }
}
