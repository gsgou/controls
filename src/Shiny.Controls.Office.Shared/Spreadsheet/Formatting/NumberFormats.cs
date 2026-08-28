using System.Globalization;
using System.Text;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>The number formats a toolbar offers by name.</summary>
public enum NumberFormatPreset
{
    /// <summary>No format at all: numbers show as typed, text as text.</summary>
    General,

    /// <summary>Thousands separator and two decimals.</summary>
    Number,

    /// <summary>The current culture's currency symbol, thousands separator and two decimals.</summary>
    Currency,
    Percent,
    Scientific,
    ShortDate,
    Time,

    /// <summary>Forces the cell to show its content verbatim, digits included.</summary>
    Text
}

/// <summary>
/// Excel number format codes, and the arithmetic on them that a toolbar needs.
/// </summary>
/// <remarks>
/// A number format is a string like <c>#,##0.00;[Red]-#,##0.00</c> — up to four semicolon-separated
/// sections, for positive, negative, zero and text. Everything here works on the first section only,
/// which is the one a "more decimals" button is understood to be about.
/// </remarks>
public static class NumberFormats
{
    /// <summary>The code for a preset, in the current culture where the preset depends on one.</summary>
    public static string CodeOf(NumberFormatPreset preset) => preset switch
    {
        NumberFormatPreset.Number => "#,##0.00",
        NumberFormatPreset.Currency => CurrencyCode(),
        NumberFormatPreset.Percent => "0.00%",
        NumberFormatPreset.Scientific => "0.00E+00",
        NumberFormatPreset.ShortDate => "m/d/yyyy",
        NumberFormatPreset.Time => "h:mm:ss",
        NumberFormatPreset.Text => "@",
        _ => string.Empty
    };

    /// <summary>A label for a menu.</summary>
    public static string DisplayName(NumberFormatPreset preset) => preset switch
    {
        NumberFormatPreset.Number => "Number",
        NumberFormatPreset.Currency => "Currency",
        NumberFormatPreset.Percent => "Percent",
        NumberFormatPreset.Scientific => "Scientific",
        NumberFormatPreset.ShortDate => "Date",
        NumberFormatPreset.Time => "Time",
        NumberFormatPreset.Text => "Text",
        _ => "General"
    };

    /// <summary>The preset a code came from, or null when it is a code nothing here produces.</summary>
    /// <remarks>
    /// For a toolbar's dropdown, which has to show what the active cell already is. A cell whose format
    /// came from Excel usually will not match any preset, and showing nothing selected is the honest
    /// answer — better than showing "Number" over a format that is not the one that button applies.
    /// </remarks>
    public static NumberFormatPreset? PresetOf(string code)
    {
        if (string.IsNullOrEmpty(code) || code == "General")
            return NumberFormatPreset.General;

        foreach (var preset in Enum.GetValues<NumberFormatPreset>())
        {
            if (preset != NumberFormatPreset.General && string.Equals(CodeOf(preset), code, StringComparison.Ordinal))
                return preset;
        }

        return null;
    }

    /// <summary>
    /// Adds or removes decimal places, returning the adjusted code.
    /// </summary>
    /// <remarks>
    /// General has no decimal places to adjust, so asking for more turns it into <c>0.0</c> — which is
    /// what Excel does, and the only way the button can have an effect on an unformatted cell.
    /// </remarks>
    public static string AdjustDecimals(string code, int delta)
    {
        if (delta == 0)
            return code;

        if (string.IsNullOrEmpty(code) || code == "General")
        {
            if (delta < 0)
                return code;

            return "0." + new string('0', Math.Min(delta, MaxDecimals));
        }

        var sections = code.Split(';');
        sections[0] = AdjustSection(sections[0], delta);
        return string.Join(';', sections);
    }

    /// <summary>How many decimal places a code shows, counted in its first section.</summary>
    public static int DecimalsOf(string code)
    {
        if (string.IsNullOrEmpty(code))
            return 0;

        var section = code.Split(';')[0];
        var dot = IndexOfDecimalPoint(section);
        if (dot < 0)
            return 0;

        var count = 0;
        for (var i = dot + 1; i < section.Length && section[i] is '0' or '#' or '?'; i++)
            count++;

        return count;
    }

    /// <summary>Excel's own ceiling on decimal places.</summary>
    const int MaxDecimals = 30;

    static string AdjustSection(string section, int delta)
    {
        var current = DecimalsOf(section);
        var wanted = Math.Clamp(current + delta, 0, MaxDecimals);
        if (wanted == current)
            return section;

        var dot = IndexOfDecimalPoint(section);

        if (dot < 0)
        {
            // No decimal point yet. It goes after the integer part, which ends at the last digit
            // placeholder - not at the end of the string, or a code like 0"kg" would gain ".00kg".
            var end = LastDigitPlaceholder(section);
            if (end < 0)
                return section;

            return string.Concat(section.AsSpan(0, end + 1), ".", new string('0', wanted), section.AsSpan(end + 1));
        }

        var placeholders = 0;
        while (dot + 1 + placeholders < section.Length && section[dot + 1 + placeholders] is '0' or '#' or '?')
            placeholders++;

        var builder = new StringBuilder(section.Length + delta);
        builder.Append(section, 0, dot);

        if (wanted > 0)
        {
            builder.Append('.');
            builder.Append('0', wanted);
        }

        builder.Append(section, dot + 1 + placeholders, section.Length - dot - 1 - placeholders);
        return builder.ToString();
    }

    /// <summary>
    /// The decimal point in a format code, ignoring any inside a quoted literal or escaped.
    /// </summary>
    /// <remarks>
    /// A code may contain literal text — <c>0" pt."</c> — and the full stop in it is not a decimal
    /// point. Treating it as one puts the extra zeros inside the quotes, where they print rather than
    /// format.
    /// </remarks>
    static int IndexOfDecimalPoint(string section)
    {
        var quoted = false;

        for (var i = 0; i < section.Length; i++)
        {
            var c = section[i];

            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (quoted)
                continue;

            if (c == '\\')
            {
                i++;
                continue;
            }

            if (c == '[')
            {
                // A colour or condition block, e.g. [Red] or [>1000]. Nothing inside it is a decimal.
                var close = section.IndexOf(']', i);
                i = close < 0 ? section.Length : close;
                continue;
            }

            if (c == '.')
                return i;
        }

        return -1;
    }

    static int LastDigitPlaceholder(string section)
    {
        var quoted = false;
        var last = -1;

        for (var i = 0; i < section.Length; i++)
        {
            var c = section[i];

            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (quoted)
                continue;

            if (c is '0' or '#' or '?')
                last = i;
        }

        return last;
    }

    static string CurrencyCode()
    {
        var format = CultureInfo.CurrentCulture.NumberFormat;
        var symbol = format.CurrencySymbol;

        // Quoted so a multi-character symbol, or one made of characters the format language reserves,
        // reaches the cell as itself rather than as formatting instructions.
        var quoted = '"' + symbol.Replace("\"", "\\\"") + '"';

        // Symbol placement follows the culture: a code that always led with the symbol would show
        // "$1 234,56" in every culture that writes 1 234,56 kr.
        return format.CurrencyPositivePattern is 1 or 3
            ? "#,##0.00" + quoted
            : quoted + "#,##0.00";
    }
}
