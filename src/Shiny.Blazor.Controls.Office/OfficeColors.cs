using System.Globalization;
using Shiny.Controls.Office.Spreadsheet;

namespace Shiny.Blazor.Controls.Office;

/// <summary>
/// Converts between the hex strings <c>ColorPicker</c> speaks and the <see cref="ArgbColor"/> the
/// document kernel stores.
/// </summary>
/// <remarks>
/// The picker emits <c>#RRGGBB</c> when opacity is off and <c>#AARRGGBB</c> when it is on — alpha
/// first, which is the same order <see cref="ArgbColor.FromUInt32"/> reads. Both forms have to be
/// accepted here, because whether a given toolbar slot shows the opacity strip is a decision made at
/// the call site, not in this file.
/// </remarks>
static class OfficeColors
{
    public static bool TryParse(string? hex, out ArgbColor color)
    {
        color = default;

        if (hex is null)
            return false;

        var text = hex.AsSpan().Trim();
        if (text.Length == 0 || text[0] != '#')
            return false;

        var digits = text[1..];
        if (!uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return false;

        color = digits.Length switch
        {
            // No alpha in the string means fully opaque, not fully transparent.
            6 => ArgbColor.FromUInt32(0xFF000000u | value),
            8 => ArgbColor.FromUInt32(value),
            _ => default
        };

        return digits.Length is 6 or 8;
    }

    /// <summary>The <c>#RRGGBB</c> a picker with no opacity strip expects back.</summary>
    public static string ToHex(ArgbColor color)
        => $"#{color.R:x2}{color.G:x2}{color.B:x2}";
}
