using System.ComponentModel;
using System.Globalization;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// Resolves easing curves by name, so XAML can say <c>Easing="CubicOut"</c>.
/// </summary>
/// <remarks>
/// Also accepts CSS-style function syntax — <c>cubic-bezier(0.25, 0.1, 0.25, 1)</c>,
/// <c>steps(4)</c>, <c>spring(0.5, 10)</c> — because copying a curve straight out of a design tool
/// or a browser devtools panel is by far the most common way anyone arrives at one.
/// </remarks>
public static class EasingCatalog
{
    static readonly Dictionary<string, EasingFunction> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Linear"] = Easings.Linear,
        ["StepStart"] = Easings.StepStart,
        ["StepEnd"] = Easings.StepEnd,
        ["QuadIn"] = Easings.QuadIn,
        ["QuadOut"] = Easings.QuadOut,
        ["QuadInOut"] = Easings.QuadInOut,
        ["CubicIn"] = Easings.CubicIn,
        ["CubicOut"] = Easings.CubicOut,
        ["CubicInOut"] = Easings.CubicInOut,
        ["QuartIn"] = Easings.QuartIn,
        ["QuartOut"] = Easings.QuartOut,
        ["QuartInOut"] = Easings.QuartInOut,
        ["QuintIn"] = Easings.QuintIn,
        ["QuintOut"] = Easings.QuintOut,
        ["QuintInOut"] = Easings.QuintInOut,
        ["SinIn"] = Easings.SinIn,
        ["SinOut"] = Easings.SinOut,
        ["SinInOut"] = Easings.SinInOut,
        ["ExpoIn"] = Easings.ExpoIn,
        ["ExpoOut"] = Easings.ExpoOut,
        ["ExpoInOut"] = Easings.ExpoInOut,
        ["CircIn"] = Easings.CircIn,
        ["CircOut"] = Easings.CircOut,
        ["CircInOut"] = Easings.CircInOut,
        ["BackIn"] = Easings.BackIn,
        ["BackOut"] = Easings.BackOut,
        ["BackInOut"] = Easings.BackInOut,
        ["ElasticIn"] = Easings.ElasticIn,
        ["ElasticOut"] = Easings.ElasticOut,
        ["ElasticInOut"] = Easings.ElasticInOut,
        ["BounceIn"] = Easings.BounceIn,
        ["BounceOut"] = Easings.BounceOut,
        ["BounceInOut"] = Easings.BounceInOut,
        ["Ease"] = Easings.Ease,
        ["EaseIn"] = Easings.EaseIn,
        ["EaseOut"] = Easings.EaseOut,
        ["EaseInOut"] = Easings.EaseInOut,
        ["Emphasized"] = Easings.Emphasized
    };

    /// <summary>Adds or replaces a named curve.</summary>
    public static void Register(string name, EasingFunction easing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(easing);
        Named[name] = easing;
    }

    /// <summary>Every registered curve name.</summary>
    public static IEnumerable<string> Names => Named.Keys;

    /// <summary>Resolves a curve, returning null when the text is not recognised.</summary>
    public static EasingFunction? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();

        if (Named.TryGetValue(text, out var named))
            return named;

        var open = text.IndexOf('(');
        if (open < 0 || !text.EndsWith(')'))
            return null;

        var function = text[..open].Trim();
        var arguments = ParseArguments(text[(open + 1)..^1]);

        return function.ToLowerInvariant() switch
        {
            "cubic-bezier" or "cubicbezier" when arguments.Length == 4
                => Easings.CubicBezier(arguments[0], arguments[1], arguments[2], arguments[3]),

            "steps" when arguments.Length is 1 or 2
                => Easings.Steps((int)arguments[0], arguments.Length == 2 && arguments[1] != 0d),

            "spring" when arguments.Length is 0 or 1 or 2
                => Easings.Spring(
                    arguments.Length > 0 ? arguments[0] : 0.5d,
                    arguments.Length > 1 ? arguments[1] : 10d),

            _ => null
        };
    }

    /// <summary>Resolves a curve, throwing a message that lists the alternatives.</summary>
    public static EasingFunction Get(string? text)
        => Find(text) ?? throw new FormatException(
            $"'{text}' is not a recognised easing curve. Use one of: {string.Join(", ", Named.Keys.Order())}, " +
            "or a function such as cubic-bezier(0.25, 0.1, 0.25, 1), steps(4), or spring(0.4, 12).");

    static double[] ParseArguments(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var values = new double[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                return [];
        }

        return values;
    }
}

/// <summary>Lets XAML assign an easing curve from a plain string.</summary>
public sealed class EasingFunctionTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string text ? EasingCatalog.Get(text) : base.ConvertFrom(context, culture, value);
}
