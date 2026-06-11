using System.Globalization;

namespace Shiny.Blazor.Controls;

static class ToolbarFormatExtensions
{
    /// <summary>Formats a number with the invariant culture so CSS values never pick up a comma decimal separator.</summary>
    public static string ToInv(this double value) => value.ToString(CultureInfo.InvariantCulture);

    public static string ToInv(this int value) => value.ToString(CultureInfo.InvariantCulture);
}
