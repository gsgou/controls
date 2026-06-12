namespace Shiny.ThemeGen;

sealed class Palettes
{
    public required TonalPalette Primary { get; init; }
    public required TonalPalette Secondary { get; init; }
    public required TonalPalette Tertiary { get; init; }
    public required TonalPalette Neutral { get; init; }
    public required TonalPalette NeutralVariant { get; init; }
    public required TonalPalette Error { get; init; }
    public required TonalPalette Success { get; init; }
    public required TonalPalette Info { get; init; }
    public required TonalPalette Warning { get; init; }
    public required TonalPalette Caution { get; init; }
    public required TonalPalette Critical { get; init; }

    public static Palettes FromSeeds(IReadOnlyDictionary<string, string> seeds) => new()
    {
        Primary = TonalPalette.FromSeed(seeds["primary"]),
        Secondary = TonalPalette.FromSeed(seeds["secondary"]),
        Tertiary = TonalPalette.FromSeed(seeds["tertiary"]),
        Neutral = TonalPalette.FromSeed(seeds["neutral"]),
        NeutralVariant = TonalPalette.FromSeed(seeds["neutralVariant"]),
        Error = TonalPalette.FromSeed(seeds["error"]),
        Success = TonalPalette.FromSeed(seeds["success"]),
        Info = TonalPalette.FromSeed(seeds["info"]),
        Warning = TonalPalette.FromSeed(seeds["warning"]),
        Caution = TonalPalette.FromSeed(seeds["caution"]),
        Critical = TonalPalette.FromSeed(seeds["critical"]),
    };
}

static class SchemeBuilder
{
    /// <summary>Builds the full role->hex map for a scheme, in Tokens.ColorRoles order.</summary>
    public static IReadOnlyList<(string Role, string Hex)> Build(Palettes p, bool dark)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        // M3 accent groups: (role, container) tone pairs differ between light & dark.
        void Accent(string prefix, TonalPalette tp)
        {
            if (!dark)
            {
                map[prefix] = tp.Tone(40);
                map["On" + prefix] = tp.Tone(100);
                map[prefix + "Container"] = tp.Tone(90);
                map["On" + prefix + "Container"] = tp.Tone(10);
            }
            else
            {
                map[prefix] = tp.Tone(80);
                map["On" + prefix] = tp.Tone(20);
                map[prefix + "Container"] = tp.Tone(30);
                map["On" + prefix + "Container"] = tp.Tone(90);
            }
        }

        Accent("Primary", p.Primary);
        Accent("Secondary", p.Secondary);
        Accent("Tertiary", p.Tertiary);
        Accent("Error", p.Error);
        Accent("Success", p.Success);
        Accent("Info", p.Info);
        Accent("Warning", p.Warning);
        Accent("Caution", p.Caution);
        Accent("Critical", p.Critical);

        if (!dark)
        {
            map["Background"] = p.Neutral.Tone(98);
            map["OnBackground"] = p.Neutral.Tone(10);
            map["Surface"] = p.Neutral.Tone(98);
            map["OnSurface"] = p.Neutral.Tone(10);
            map["SurfaceVariant"] = p.NeutralVariant.Tone(90);
            map["OnSurfaceVariant"] = p.NeutralVariant.Tone(30);
            map["SurfaceContainerLowest"] = p.Neutral.Tone(100);
            map["SurfaceContainerLow"] = p.Neutral.Tone(96);
            map["SurfaceContainer"] = p.Neutral.Tone(94);
            map["SurfaceContainerHigh"] = p.Neutral.Tone(92);
            map["SurfaceContainerHighest"] = p.Neutral.Tone(90);
            map["SurfaceTint"] = p.Primary.Tone(40);
            map["Outline"] = p.NeutralVariant.Tone(50);
            map["OutlineVariant"] = p.NeutralVariant.Tone(80);
            map["InverseSurface"] = p.Neutral.Tone(20);
            map["InverseOnSurface"] = p.Neutral.Tone(95);
            map["InversePrimary"] = p.Primary.Tone(80);
        }
        else
        {
            map["Background"] = p.Neutral.Tone(6);
            map["OnBackground"] = p.Neutral.Tone(90);
            map["Surface"] = p.Neutral.Tone(6);
            map["OnSurface"] = p.Neutral.Tone(90);
            map["SurfaceVariant"] = p.NeutralVariant.Tone(30);
            map["OnSurfaceVariant"] = p.NeutralVariant.Tone(80);
            map["SurfaceContainerLowest"] = p.Neutral.Tone(4);
            map["SurfaceContainerLow"] = p.Neutral.Tone(10);
            map["SurfaceContainer"] = p.Neutral.Tone(12);
            map["SurfaceContainerHigh"] = p.Neutral.Tone(17);
            map["SurfaceContainerHighest"] = p.Neutral.Tone(22);
            map["SurfaceTint"] = p.Primary.Tone(80);
            map["Outline"] = p.NeutralVariant.Tone(60);
            map["OutlineVariant"] = p.NeutralVariant.Tone(30);
            map["InverseSurface"] = p.Neutral.Tone(90);
            map["InverseOnSurface"] = p.Neutral.Tone(20);
            map["InversePrimary"] = p.Primary.Tone(40);
        }

        // Shadow & scrim are always pure black regardless of scheme.
        map["Shadow"] = p.Neutral.Tone(0);
        map["Scrim"] = p.Neutral.Tone(0);

        return Tokens.ColorRoles.Select(role => (role, map[role])).ToList();
    }
}
