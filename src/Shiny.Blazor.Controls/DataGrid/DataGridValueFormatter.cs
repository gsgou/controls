using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Everything a column needs in order to turn a raw value into display text. Kept as one object so
/// the formatter can be called from the cell, the header search index, group headers and export
/// without any of them drifting apart.
/// </summary>
sealed class DataGridFormatSpec
{
    public DataGridColumnFormat DisplayAs { get; set; }
    public string? StringFormat { get; set; }
    public int? Decimals { get; set; }
    public string? NullText { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? TrueText { get; set; }
    public string? FalseText { get; set; }
    public CultureInfo? Culture { get; set; }
}

/// <summary>
/// Turns a column value into display text: preset (<see cref="DataGridColumnFormat"/>), explicit
/// format string, null placeholder, and prefix/suffix - in that order.
/// </summary>
static class DataGridValueFormatter
{
    const string DefaultTrue = "✓";  // check
    const string DefaultFalse = "✗"; // ballot X

    static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string? Format(object? value, DataGridFormatSpec spec)
    {
        var culture = spec.Culture ?? CultureInfo.CurrentCulture;

        // A null (or blank) value shows the placeholder alone - prefix/suffix decorate a real value,
        // not the absence of one ("$-" reads like a number).
        if (value is null || (value is string es && es.Length == 0))
            return spec.NullText;

        var text = Core(value, spec, culture);
        if (text is null)
            return spec.NullText;

        if (!string.IsNullOrEmpty(spec.Prefix))
            text = spec.Prefix + text;
        if (!string.IsNullOrEmpty(spec.Suffix))
            text += spec.Suffix;

        return text;
    }

    static string? Core(object value, DataGridFormatSpec spec, CultureInfo culture)
        => spec.DisplayAs switch
        {
            DataGridColumnFormat.FileSize => FileSizeText(value, spec, culture),
            DataGridColumnFormat.Boolean => BooleanText(value, spec),
            DataGridColumnFormat.Enum => Humanize(EnumText(value)),
            _ => Formatted(value, spec.StringFormat ?? PresetFormat(spec.DisplayAs, spec.Decimals), culture)
        };

    /// <summary>
    /// Applies a .NET format string, accepting <b>both</b> dialects: the bare
    /// <c>IFormattable</c> form (<c>"C0"</c>) and MAUI's binding form (<c>"{0:C0}"</c>). Older XAML
    /// documented the second, so both have to keep working - and both have to produce the same text
    /// as the search/aggregate path, which is why every caller comes through here.
    /// </summary>
    static string? Formatted(object value, string? format, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(format))
            return value.ToString();

        if (format.Contains("{0"))
            return string.Format(culture, format, value);

        return value is IFormattable f ? f.ToString(format, culture) : value.ToString();
    }

    /// <summary>The .NET format string a preset maps to. Explicit <c>StringFormat</c> always wins over this.</summary>
    static string? PresetFormat(DataGridColumnFormat displayAs, int? decimals)
        => displayAs switch
        {
            DataGridColumnFormat.Number => "N" + (decimals?.ToString(CultureInfo.InvariantCulture) ?? ""),
            DataGridColumnFormat.Currency => "C" + (decimals?.ToString(CultureInfo.InvariantCulture) ?? ""),
            DataGridColumnFormat.Percent => "P" + (decimals?.ToString(CultureInfo.InvariantCulture) ?? ""),
            DataGridColumnFormat.Date => "d",
            DataGridColumnFormat.Time => "t",
            DataGridColumnFormat.DateTime => "g",
            _ => null
        };

    static string BooleanText(object value, DataGridFormatSpec spec)
    {
        var truthy = value switch
        {
            bool b => b,
            string s => string.Equals(s, "true", StringComparison.OrdinalIgnoreCase),
            IConvertible c => SafeToBool(c),
            _ => false
        };
        return truthy
            ? spec.TrueText ?? DefaultTrue
            : spec.FalseText ?? DefaultFalse;
    }

    static bool SafeToBool(IConvertible c)
    {
        try
        {
            return c.ToBoolean(CultureInfo.InvariantCulture);
        }
        catch
        {
            return false;
        }
    }

    static string? FileSizeText(object value, DataGridFormatSpec spec, CultureInfo culture)
    {
        double bytes;
        try
        {
            bytes = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return value.ToString();
        }

        var negative = bytes < 0;
        bytes = Math.Abs(bytes);

        var unit = 0;
        while (bytes >= 1024 && unit < SizeUnits.Length - 1)
        {
            bytes /= 1024;
            unit++;
        }

        // Whole bytes have no fractional part worth showing - "512 B", not "512.0 B".
        var places = spec.Decimals ?? (unit == 0 ? 0 : 1);
        var text = bytes.ToString("N" + places.ToString(CultureInfo.InvariantCulture), culture);
        return (negative ? "-" : "") + text + " " + SizeUnits[unit];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "The enum type is statically referenced by the bound model property; enum fields are preserved with it.")]
    static string EnumText(object value)
    {
        if (value is not Enum e)
            return value.ToString() ?? string.Empty;

        var name = e.ToString();
        var field = e.GetType().GetField(name);
        var description = field?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();

        return description?.Description ?? name;
    }

    /// <summary>Splits a PascalCase identifier into words ("InProgress" -> "In Progress").</summary>
    static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Whether a column of this shape reads better right-aligned. Drives
    /// <see cref="DataGridCellAlignment.Auto"/>: quantities line up on their decimal point, text does not.
    /// </summary>
    public static bool IsNumericAlignment(DataGridColumnFormat displayAs, Type? dataType)
    {
        switch (displayAs)
        {
            case DataGridColumnFormat.Number:
            case DataGridColumnFormat.Currency:
            case DataGridColumnFormat.Percent:
            case DataGridColumnFormat.FileSize:
                return true;
            case DataGridColumnFormat.Boolean:
            case DataGridColumnFormat.Enum:
            case DataGridColumnFormat.Text:
            case DataGridColumnFormat.Date:
            case DataGridColumnFormat.Time:
            case DataGridColumnFormat.DateTime:
                return false;
        }

        if (dataType is null)
            return false;

        var t = Nullable.GetUnderlyingType(dataType) ?? dataType;
        return t == typeof(byte) || t == typeof(sbyte)
            || t == typeof(short) || t == typeof(ushort)
            || t == typeof(int) || t == typeof(uint)
            || t == typeof(long) || t == typeof(ulong)
            || t == typeof(float) || t == typeof(double) || t == typeof(decimal);
    }
}
