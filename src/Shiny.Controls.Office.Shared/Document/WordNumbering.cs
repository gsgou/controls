using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Resolves list numbering into the label text that should appear in front of a paragraph.
/// </summary>
/// <remarks>
/// <para>
/// <c>numbering.xml</c> has a reputation and it is deserved. A paragraph references a <c>numId</c>,
/// which points at a <c>num</c>, which points at an <c>abstractNum</c> — and may override individual
/// levels along the way. The level definition then carries a format and a <c>lvlText</c> template like
/// <c>%1.%2.</c> whose placeholders refer to the running counters of *outer* levels, not to itself.
/// </para>
/// <para>
/// Counters are stateful: they run as the document is read, and starting a level resets every level
/// below it. That is why this is a class you walk the document with rather than a pure function.
/// </para>
/// </remarks>
sealed class WordNumbering
{
    readonly Dictionary<int, AbstractNum> abstractNumbering = new();
    readonly Dictionary<int, int> numToAbstract = new();
    readonly Dictionary<int, Dictionary<int, Level>> overrides = new();
    readonly Dictionary<(int NumId, int Level), int> counters = new();

    public WordNumbering(MainDocumentPart main)
    {
        var numbering = main.NumberingDefinitionsPart?.Numbering;
        if (numbering is null)
            return;

        foreach (var abstractNum in numbering.Elements<AbstractNum>())
        {
            if (abstractNum.AbstractNumberId?.Value is { } id)
                this.abstractNumbering[id] = abstractNum;
        }

        foreach (var num in numbering.Elements<NumberingInstance>())
        {
            if (num.NumberID?.Value is not { } numId)
                continue;

            if (num.AbstractNumId?.Val?.Value is { } abstractId)
                this.numToAbstract[numId] = abstractId;

            foreach (var levelOverride in num.Elements<LevelOverride>())
            {
                if (levelOverride.LevelIndex?.Value is not { } index || levelOverride.Level is null)
                    continue;

                if (!this.overrides.TryGetValue(numId, out var map))
                    this.overrides[numId] = map = new Dictionary<int, Level>();

                map[index] = levelOverride.Level;
            }
        }
    }

    public bool IsEmpty => this.abstractNumbering.Count == 0;

    /// <summary>Advances the counters and returns the label for a numbered paragraph.</summary>
    public ListLabel? Next(int numId, int levelIndex, TextStyle style)
    {
        var level = this.FindLevel(numId, levelIndex);
        if (level is null)
            return null;

        var format = level.NumberingFormat?.Val?.Value ?? NumberFormatValues.Decimal;

        if (format == NumberFormatValues.None)
            return null;

        if (format == NumberFormatValues.Bullet)
        {
            // Bullet glyphs come from symbol fonts whose code points mean nothing in a text font;
            // mapping the common ones keeps a list looking like a list.
            var glyph = MapBullet(level.LevelText?.Val?.Value);
            return new ListLabel(glyph, style, IndentOf(level), HangingOf(level));
        }

        this.Advance(numId, levelIndex, level);

        var template = level.LevelText?.Val?.Value ?? "%1.";
        var text = this.Substitute(template, numId, levelIndex, format);
        return new ListLabel(text, style, IndentOf(level), HangingOf(level));
    }

    void Advance(int numId, int levelIndex, Level level)
    {
        var key = (numId, levelIndex);
        if (this.counters.TryGetValue(key, out var current))
        {
            this.counters[key] = current + 1;
        }
        else
        {
            this.counters[key] = level.StartNumberingValue?.Val?.Value ?? 1;
        }

        // Starting a level restarts everything nested inside it, which is what makes 1.1, 1.2, 2.1
        // rather than 1.1, 1.2, 2.3.
        foreach (var deeper in this.counters.Keys.Where(k => k.NumId == numId && k.Level > levelIndex).ToList())
            this.counters.Remove(deeper);
    }

    string Substitute(string template, int numId, int levelIndex, NumberFormatValues format)
    {
        var builder = new System.Text.StringBuilder();

        for (var i = 0; i < template.Length; i++)
        {
            if (template[i] != '%' || i + 1 >= template.Length || !char.IsAsciiDigit(template[i + 1]))
            {
                builder.Append(template[i]);
                continue;
            }

            // %1 is level 0, %2 is level 1, and so on.
            var referenced = template[i + 1] - '1';
            i++;

            var value = this.counters.GetValueOrDefault((numId, referenced), 0);
            if (value == 0)
                continue;

            // Only the paragraph's own level uses its own format; outer levels contribute their
            // running number, which Word always renders as decimal inside a compound label.
            var levelFormat = referenced == levelIndex ? format : NumberFormatValues.Decimal;
            builder.Append(Render(value, levelFormat));
        }

        return builder.ToString();
    }

    Level? FindLevel(int numId, int levelIndex)
    {
        if (this.overrides.TryGetValue(numId, out var map) && map.TryGetValue(levelIndex, out var overridden))
            return overridden;

        if (!this.numToAbstract.TryGetValue(numId, out var abstractId) ||
            !this.abstractNumbering.TryGetValue(abstractId, out var abstractNum))
            return null;

        return abstractNum.Elements<Level>().FirstOrDefault(x => x.LevelIndex?.Value == levelIndex);
    }

    static double IndentOf(Level level)
    {
        var indent = level.PreviousParagraphProperties?.Indentation;
        if (indent?.Left?.Value is { } left && double.TryParse(left, out var twips))
            return OoxmlUnits.TwipsToPixels(twips);

        return 0;
    }

    static double HangingOf(Level level)
    {
        var indent = level.PreviousParagraphProperties?.Indentation;
        if (indent?.Hanging?.Value is { } hanging && double.TryParse(hanging, out var twips))
            return OoxmlUnits.TwipsToPixels(twips);

        return 0;
    }

    static string Render(int value, NumberFormatValues format)
    {
        if (format == NumberFormatValues.UpperLetter)
            return Alphabetic(value).ToUpperInvariant();

        if (format == NumberFormatValues.LowerLetter)
            return Alphabetic(value);

        if (format == NumberFormatValues.UpperRoman)
            return Roman(value);

        if (format == NumberFormatValues.LowerRoman)
            return Roman(value).ToLowerInvariant();

        if (format == NumberFormatValues.DecimalZero)
            return value.ToString("00");

        return value.ToString();
    }

    /// <summary>a, b, ... z, aa, ab — the same bijective base-26 as spreadsheet columns.</summary>
    static string Alphabetic(int value)
    {
        var builder = new System.Text.StringBuilder();
        while (value > 0)
        {
            value--;
            builder.Insert(0, (char)('a' + value % 26));
            value /= 26;
        }

        return builder.Length == 0 ? "a" : builder.ToString();
    }

    static string Roman(int value)
    {
        if (value is <= 0 or > 3999)
            return value.ToString();

        var numerals = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var symbols = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < numerals.Length; i++)
        {
            while (value >= numerals[i])
            {
                builder.Append(symbols[i]);
                value -= numerals[i];
            }
        }

        return builder.ToString();
    }

    /// <summary>Maps the usual Symbol/Wingdings bullet code points onto glyphs a text font can draw.</summary>
    static string MapBullet(string? levelText) => levelText switch
    {
        null or "" => "•",
        "" => "•",
        "" => "▪",
        "" => "◆",
        "" => "▪",
        "o" => "◦",
        "" => "✓",
        _ => levelText
    };
}
