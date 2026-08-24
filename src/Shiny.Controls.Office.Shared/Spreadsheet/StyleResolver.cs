using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Shiny.Controls.Office.Packaging;
using Fill = DocumentFormat.OpenXml.Spreadsheet.Fill;
using Font = DocumentFormat.OpenXml.Spreadsheet.Font;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// Flattens a cell's style index into a <see cref="ResolvedFormat"/>.
/// </summary>
/// <remarks>
/// Results are cached per style index. A sheet of 100k cells typically uses a few dozen distinct
/// formats, so resolving on every paint would repeat the same chain walk thousands of times per frame.
/// </remarks>
public sealed class StyleResolver
{
    readonly Stylesheet? stylesheet;
    readonly IUnsupportedFeatureSink unsupported;
    readonly Dictionary<uint, ResolvedFormat> cache = new();
    readonly Dictionary<uint, string> customNumberFormats = new();
    readonly List<ArgbColor> themeColors = new();
    readonly Dictionary<string, ExcelNumberFormat.NumberFormat> formatCache = new(StringComparer.Ordinal);

    internal StyleResolver(WorkbookPart workbookPart, IUnsupportedFeatureSink unsupported)
    {
        this.unsupported = unsupported;
        this.stylesheet = workbookPart.WorkbookStylesPart?.Stylesheet;

        foreach (var format in this.stylesheet?.NumberingFormats?.Elements<NumberingFormat>() ?? Enumerable.Empty<NumberingFormat>())
        {
            if (format.NumberFormatId?.Value is { } id && format.FormatCode?.Value is { } code)
                this.customNumberFormats[id] = code;
        }

        this.LoadThemeColors(workbookPart);
    }

    public ResolvedFormat Resolve(uint? styleIndex)
    {
        if (styleIndex is not { } index)
            return ResolvedFormat.Default;

        if (this.cache.TryGetValue(index, out var cached))
            return cached;

        var resolved = this.Build(index);
        this.cache[index] = resolved;
        return resolved;
    }

    /// <summary>Formats a value for display using the cell's number format.</summary>
    public string Format(CellValue value, ResolvedFormat format)
    {
        switch (value.Kind)
        {
            case CellValueKind.Blank:
                return string.Empty;

            case CellValueKind.Error:
                return CellValue.ErrorText(value.AsError());

            case CellValueKind.Boolean:
                return value.AsBoolean() ? "TRUE" : "FALSE";
        }

        var code = format.NumberFormatCode;
        if (string.IsNullOrEmpty(code) || code == "General")
        {
            return value.Kind == CellValueKind.Text
                ? value.AsText()
                : FormatGeneral(value.AsNumber());
        }

        if (!this.formatCache.TryGetValue(code, out var numberFormat))
        {
            try
            {
                numberFormat = new ExcelNumberFormat.NumberFormat(code);
            }
            catch (Exception ex)
            {
                this.unsupported.Report(new UnsupportedFeature("styles", "Number format", UnsupportedSeverity.NotRendered, $"{code}: {ex.Message}"));
                numberFormat = null!;
            }

            this.formatCache[code] = numberFormat;
        }

        if (numberFormat is null || !numberFormat.IsValid)
            return value.Kind == CellValueKind.Text ? value.AsText() : FormatGeneral(value.AsNumber());

        var boxed = value.Kind == CellValueKind.Text ? value.AsText() : (object)value.AsNumber();
        return numberFormat.Format(boxed, System.Globalization.CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Excel's General format: up to 11 significant digits, switching to scientific when the value will
    /// not fit. This is an approximation of a notoriously fiddly rule, not a reproduction of it.
    /// </summary>
    static string FormatGeneral(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return CellValue.ErrorText(CellError.Num);

        if (value == 0)
            return "0";

        var magnitude = Math.Abs(value);
        if (magnitude >= 1e11 || magnitude < 1e-10)
            return value.ToString("0.#####E+00", System.Globalization.CultureInfo.CurrentCulture);

        return value.ToString("0.###########", System.Globalization.CultureInfo.CurrentCulture);
    }

    ResolvedFormat Build(uint styleIndex)
    {
        var cellFormats = this.stylesheet?.CellFormats;
        if (cellFormats is null || styleIndex >= cellFormats.Count())
            return ResolvedFormat.Default;

        if (cellFormats.ElementAt((int)styleIndex) is not CellFormat cellFormat)
            return ResolvedFormat.Default;

        var result = ResolvedFormat.Default;

        if (cellFormat.NumberFormatId?.Value is { } numberFormatId)
            result = result with { NumberFormatCode = this.NumberFormatCode(numberFormatId) };

        // ApplyFont/ApplyFill are hints, not gates: Excel itself writes cells with the flag absent but
        // the index meaningful, so the index is honoured whenever it is present.
        if (cellFormat.FontId?.Value is { } fontId)
            result = this.ApplyFont(result, fontId);

        if (cellFormat.FillId?.Value is { } fillId)
            result = this.ApplyFill(result, fillId);

        if (cellFormat.Alignment is { } alignment)
            result = ApplyAlignment(result, alignment);

        return result;
    }

    string NumberFormatCode(uint id)
    {
        if (this.customNumberFormats.TryGetValue(id, out var custom))
            return custom;

        return BuiltInNumberFormats.TryGetValue(id, out var builtIn) ? builtIn : string.Empty;
    }

    ResolvedFormat ApplyFont(ResolvedFormat format, uint fontId)
    {
        var fonts = this.stylesheet?.Fonts;
        if (fonts is null || fontId >= fonts.Count() || fonts.ElementAt((int)fontId) is not Font font)
            return format;

        return format with
        {
            FontName = font.FontName?.Val?.Value ?? format.FontName,
            FontSize = font.FontSize?.Val?.Value ?? format.FontSize,
            Bold = font.Bold is not null && (font.Bold.Val is null || font.Bold.Val.Value),
            Italic = font.Italic is not null && (font.Italic.Val is null || font.Italic.Val.Value),
            Underline = font.Underline is not null && font.Underline.Val?.Value != UnderlineValues.None,
            Strike = font.Strike is not null && (font.Strike.Val is null || font.Strike.Val.Value),
            Foreground = this.ResolveColor(font.Color) ?? format.Foreground
        };
    }

    ResolvedFormat ApplyFill(ResolvedFormat format, uint fillId)
    {
        var fills = this.stylesheet?.Fills;
        if (fills is null || fillId >= fills.Count() || fills.ElementAt((int)fillId) is not Fill fill)
            return format;

        var pattern = fill.PatternFill;
        if (pattern is null)
            return format;

        var type = pattern.PatternType?.Value ?? PatternValues.None;
        if (type == PatternValues.None)
            return format;

        if (type != PatternValues.Solid)
        {
            // Hatch patterns render as their background colour rather than as the pattern itself.
            this.unsupported.Report(new UnsupportedFeature("styles", "Pattern fill", UnsupportedSeverity.NotRendered, type.ToString()));
        }

        // In a solid fill it is fgColor that carries the visible colour, not bgColor.
        var color = this.ResolveColor(pattern.ForegroundColor) ?? this.ResolveColor(pattern.BackgroundColor);
        return color is null ? format : format with { Background = color.Value };
    }

    static ResolvedFormat ApplyAlignment(ResolvedFormat format, Alignment alignment)
    {
        var horizontal = alignment.Horizontal?.Value switch
        {
            var v when v == HorizontalAlignmentValues.Left => CellHorizontalAlignment.Left,
            var v when v == HorizontalAlignmentValues.Center => CellHorizontalAlignment.Center,
            var v when v == HorizontalAlignmentValues.Right => CellHorizontalAlignment.Right,
            var v when v == HorizontalAlignmentValues.Fill => CellHorizontalAlignment.Fill,
            var v when v == HorizontalAlignmentValues.Justify => CellHorizontalAlignment.Justify,
            var v when v == HorizontalAlignmentValues.CenterContinuous => CellHorizontalAlignment.CenterContinuous,
            var v when v == HorizontalAlignmentValues.Distributed => CellHorizontalAlignment.Distributed,
            _ => CellHorizontalAlignment.General
        };

        var vertical = alignment.Vertical?.Value switch
        {
            var v when v == VerticalAlignmentValues.Top => CellVerticalAlignment.Top,
            var v when v == VerticalAlignmentValues.Center => CellVerticalAlignment.Center,
            var v when v == VerticalAlignmentValues.Justify => CellVerticalAlignment.Justify,
            var v when v == VerticalAlignmentValues.Distributed => CellVerticalAlignment.Distributed,
            _ => CellVerticalAlignment.Bottom
        };

        return format with
        {
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            WrapText = alignment.WrapText?.Value ?? false,
            Indent = (int)(alignment.Indent?.Value ?? 0)
        };
    }

    ArgbColor? ResolveColor(ColorType? color)
    {
        if (color is null)
            return null;

        if (color.Rgb?.Value is { } hex && TryParseHex(hex, out var parsed))
            return ApplyTint(parsed, color.Tint?.Value ?? 0);

        if (color.Theme?.Value is { } themeIndex && themeIndex < this.themeColors.Count)
            return ApplyTint(this.themeColors[(int)themeIndex], color.Tint?.Value ?? 0);

        if (color.Indexed?.Value is { } indexed)
        {
            if (indexed < (uint)IndexedPalette.Length)
                return ApplyTint(ArgbColor.FromUInt32(IndexedPalette[indexed]), color.Tint?.Value ?? 0);

            // 64/65 are the "system foreground/background" sentinels — deliberately left to the theme.
            return null;
        }

        return null;
    }

    void LoadThemeColors(WorkbookPart workbookPart)
    {
        var scheme = workbookPart.ThemePart?.Theme?.ThemeElements?.ColorScheme;
        if (scheme is null)
            return;

        // Excel's theme indices are not the document order of clrScheme: the first two pairs are
        // swapped, because lt1/dk1 map to background1/text1.
        var ordered = new DocumentFormat.OpenXml.Drawing.Color2Type?[]
        {
            scheme.Light1Color, scheme.Dark1Color, scheme.Light2Color, scheme.Dark2Color,
            scheme.Accent1Color, scheme.Accent2Color, scheme.Accent3Color,
            scheme.Accent4Color, scheme.Accent5Color, scheme.Accent6Color,
            scheme.Hyperlink, scheme.FollowedHyperlinkColor
        };

        foreach (var entry in ordered)
        {
            if (entry?.RgbColorModelHex?.Val?.Value is { } hex && TryParseHex(hex, out var color))
                this.themeColors.Add(color);
            else if (entry?.SystemColor?.LastColor?.Value is { } system && TryParseHex(system, out var systemColor))
                this.themeColors.Add(systemColor);
            else
                this.themeColors.Add(new ArgbColor(255, 0, 0, 0));
        }
    }

    static bool TryParseHex(string hex, out ArgbColor color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex))
            return false;

        var span = hex.AsSpan().TrimStart('#');
        if (span.Length == 6)
        {
            if (!uint.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
                return false;

            color = ArgbColor.FromUInt32(0xFF000000u | rgb);
            return true;
        }

        if (span.Length == 8)
        {
            if (!uint.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out var argb))
                return false;

            color = ArgbColor.FromUInt32(argb);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Applies Excel's tint, which lightens toward white for positive values and darkens toward black
    /// for negative ones. This is what "Accent1, Lighter 40%" actually is in the file.
    /// </summary>
    static ArgbColor ApplyTint(ArgbColor color, double tint)
    {
        if (tint == 0)
            return color;

        static byte Scale(byte channel, double tint)
        {
            var value = channel / 255d;
            value = tint > 0
                ? value * (1 - tint) + tint
                : value * (1 + tint);

            return (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
        }

        return color with { R = Scale(color.R, tint), G = Scale(color.G, tint), B = Scale(color.B, tint) };
    }

    /// <summary>The number formats Excel defines implicitly, which never appear in the file.</summary>
    static readonly Dictionary<uint, string> BuiltInNumberFormats = new()
    {
        [0] = "General",
        [1] = "0",
        [2] = "0.00",
        [3] = "#,##0",
        [4] = "#,##0.00",
        [9] = "0%",
        [10] = "0.00%",
        [11] = "0.00E+00",
        [12] = "# ?/?",
        [13] = "# ??/??",
        [14] = "mm-dd-yy",
        [15] = "d-mmm-yy",
        [16] = "d-mmm",
        [17] = "mmm-yy",
        [18] = "h:mm AM/PM",
        [19] = "h:mm:ss AM/PM",
        [20] = "h:mm",
        [21] = "h:mm:ss",
        [22] = "m/d/yy h:mm",
        [37] = "#,##0 ;(#,##0)",
        [38] = "#,##0 ;[Red](#,##0)",
        [39] = "#,##0.00;(#,##0.00)",
        [40] = "#,##0.00;[Red](#,##0.00)",
        [45] = "mm:ss",
        [46] = "[h]:mm:ss",
        [47] = "mmss.0",
        [48] = "##0.0E+0",
        [49] = "@"
    };

    /// <summary>The legacy 56-colour indexed palette, still referenced by files saved from older Excel.</summary>
    static readonly uint[] IndexedPalette =
    [
        0xFF000000, 0xFFFFFFFF, 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFF00, 0xFFFF00FF, 0xFF00FFFF,
        0xFF000000, 0xFFFFFFFF, 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFF00, 0xFFFF00FF, 0xFF00FFFF,
        0xFF800000, 0xFF008000, 0xFF000080, 0xFF808000, 0xFF800080, 0xFF008080, 0xFFC0C0C0, 0xFF808080,
        0xFF9999FF, 0xFF993366, 0xFFFFFFCC, 0xFFCCFFFF, 0xFF660066, 0xFFFF8080, 0xFF0066CC, 0xFFCCCCFF,
        0xFF000080, 0xFFFF00FF, 0xFFFFFF00, 0xFF00FFFF, 0xFF800080, 0xFF800000, 0xFF008080, 0xFF0000FF,
        0xFF00CCFF, 0xFFCCFFFF, 0xFFCCFFCC, 0xFFFFFF99, 0xFF99CCFF, 0xFFFF99CC, 0xFFCC99FF, 0xFFFFCC99,
        0xFF3366FF, 0xFF33CCCC, 0xFF99CC00, 0xFFFFCC00, 0xFFFF9900, 0xFFFF6600, 0xFF666699, 0xFF969696,
        0xFF003366, 0xFF339966, 0xFF003300, 0xFF333300, 0xFF993300, 0xFF993366, 0xFF333399, 0xFF333333
    ];
}
