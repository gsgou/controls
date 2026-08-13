using System.Globalization;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Splatted attributes clobber attributes the component wrote itself (last one wins), so a
/// consumer passing class= or style= would silently drop the layout styling. These helpers pull
/// those two out of the splat so the component can merge them instead.
/// </summary>
static class LayoutAttributes
{
    public static IDictionary<string, object>? Split(
        IDictionary<string, object>? attributes,
        out string? cssClass,
        out string? style
    )
    {
        cssClass = null;
        style = null;
        if (attributes is null || attributes.Count == 0)
            return attributes;

        var hasClass = attributes.ContainsKey("class");
        var hasStyle = attributes.ContainsKey("style");
        if (!hasClass && !hasStyle)
            return attributes;

        var copy = new Dictionary<string, object>(attributes);
        if (hasClass && copy.Remove("class", out var c))
            cssClass = c?.ToString();
        if (hasStyle && copy.Remove("style", out var s))
            style = s?.ToString();

        return copy;
    }

    /// <summary>Appends a user-supplied style string, tolerating a missing trailing semicolon.</summary>
    public static string Append(string style, string? userStyle)
    {
        if (string.IsNullOrWhiteSpace(userStyle))
            return style;

        return style.Length > 0 && !style.EndsWith(';')
            ? style + ";" + userStyle
            : style + userStyle;
    }

    /// <summary>
    /// Lets a CSS shorthand be written as bare numbers — <c>"16"</c> and <c>"8 16"</c> mean the same
    /// as <c>"16px"</c> and <c>"8px 16px"</c>. Anything already carrying a unit is left alone.
    /// </summary>
    public static string? Spacing(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                parts[i] = number == 0 ? "0" : Px(number);
        }

        return string.Join(' ', parts);
    }

    public static string Px(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture) + "px";

    public static string Num(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string ToCss(this StackAlign align) => align switch
    {
        StackAlign.Start => "flex-start",
        StackAlign.Center => "center",
        StackAlign.End => "flex-end",
        StackAlign.Baseline => "baseline",
        _ => "stretch"
    };

    public static string ToCss(this StackJustify justify) => justify switch
    {
        StackJustify.Center => "center",
        StackJustify.End => "flex-end",
        StackJustify.SpaceBetween => "space-between",
        StackJustify.SpaceAround => "space-around",
        StackJustify.SpaceEvenly => "space-evenly",
        _ => "flex-start"
    };
}
