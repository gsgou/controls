using Shiny.Controls.Office.Spreadsheet;

namespace Shiny.Controls.Office.Text;

/// <summary>
/// The text-highlight colours the editors offer, and the Word names they round-trip through.
/// </summary>
/// <remarks>
/// <para>
/// The two formats disagree about what a highlight is. WordprocessingML's <c>w:highlight</c> takes a
/// value from a closed list of sixteen names — there is no way to say "this exact orange" — while
/// DrawingML's <c>a:highlight</c> wraps an arbitrary <c>a:srgbClr</c>. Offering two different palettes
/// would be the honest reading of the formats and the wrong thing for a user, who sees one highlight
/// button and expects the same swatches behind it in a document and in a deck.
/// </para>
/// <para>
/// So: this list is the offer, Word writes the name and PowerPoint writes the RGB. Because every
/// swatch here <em>is</em> one of Word's named values, nothing is approximated on the way in either
/// direction — <see cref="NameOf"/> only has to fall back to a nearest match for a colour that came
/// from somewhere else, such as a deck authored in PowerPoint with a colour Word cannot name.
/// </para>
/// </remarks>
public static class HighlightPalette
{
    /// <summary>A highlight swatch: what to show, what to write, and what to call it.</summary>
    public sealed record Swatch(string Name, string DisplayName, ArgbColor Color);

    /// <summary>
    /// The swatches, in the order a picker should show them.
    /// </summary>
    /// <remarks>
    /// Yellow first because it is what almost every highlight is, then the rest of the brights, then
    /// the darks. Word's own gallery is ordered the same way for the same reason.
    /// </remarks>
    public static IReadOnlyList<Swatch> Swatches { get; } =
    [
        new("yellow", "Yellow", new ArgbColor(255, 255, 255, 0)),
        new("green", "Bright Green", new ArgbColor(255, 0, 255, 0)),
        new("cyan", "Turquoise", new ArgbColor(255, 0, 255, 255)),
        new("magenta", "Pink", new ArgbColor(255, 255, 0, 255)),
        new("blue", "Blue", new ArgbColor(255, 0, 0, 255)),
        new("red", "Red", new ArgbColor(255, 255, 0, 0)),
        new("darkBlue", "Dark Blue", new ArgbColor(255, 0, 0, 139)),
        new("darkCyan", "Teal", new ArgbColor(255, 0, 139, 139)),
        new("darkGreen", "Green", new ArgbColor(255, 0, 100, 0)),
        new("darkMagenta", "Violet", new ArgbColor(255, 139, 0, 139)),
        new("darkRed", "Dark Red", new ArgbColor(255, 139, 0, 0)),
        new("darkYellow", "Dark Yellow", new ArgbColor(255, 128, 128, 0)),
        new("darkGray", "Grey 50%", new ArgbColor(255, 128, 128, 128)),
        new("lightGray", "Grey 25%", new ArgbColor(255, 192, 192, 192)),
        new("black", "Black", new ArgbColor(255, 0, 0, 0)),
        new("white", "White", new ArgbColor(255, 255, 255, 255))
    ];

    /// <summary>The colour a Word highlight name means, or null for <c>none</c> and anything unknown.</summary>
    public static ArgbColor? ColorOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var swatch in Swatches)
        {
            if (swatch.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return swatch.Color;
        }

        return null;
    }

    /// <summary>
    /// The Word highlight name closest to a colour, or <c>none</c> for null.
    /// </summary>
    /// <remarks>
    /// Nearest by squared distance in RGB. That is not a perceptually correct metric, but the palette
    /// is sixteen widely-separated colours rather than a gradient, so anything more careful would pick
    /// the same swatch and cost an colour-space conversion to do it.
    /// </remarks>
    public static string NameOf(ArgbColor? color)
    {
        if (color is not { } value)
            return "none";

        var best = "yellow";
        var bestDistance = double.MaxValue;

        foreach (var swatch in Swatches)
        {
            var dr = (double)swatch.Color.R - value.R;
            var dg = (double)swatch.Color.G - value.G;
            var db = (double)swatch.Color.B - value.B;
            var distance = (dr * dr) + (dg * dg) + (db * db);

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = swatch.Name;
        }

        return best;
    }
}
