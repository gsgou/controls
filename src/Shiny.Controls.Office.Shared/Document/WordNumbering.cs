using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>One level of a numbering definition, resolved to the parts a label is built from.</summary>
/// <remarks>
/// For a bullet level <see cref="Template"/> is the glyph itself rather than a placeholder template —
/// a bullet has nothing to substitute into, and carrying the mapped glyph here keeps the
/// <c>lvlText</c> handling in one place.
/// </remarks>
sealed record ListLevel(
    NumberFormatValues Format,
    string Template,
    int Start,
    double Indent,
    double Hanging)
{
    public bool IsBullet => this.Format == NumberFormatValues.Bullet;

    /// <summary>A level that deliberately draws nothing.</summary>
    public bool IsNone => this.Format == NumberFormatValues.None;
}


/// <summary>
/// Resolves <c>numbering.xml</c> into the level definitions a list label is built from.
/// </summary>
/// <remarks>
/// <para>
/// <c>numbering.xml</c> has a reputation and it is deserved. A paragraph references a <c>numId</c>,
/// which points at a <c>num</c>, which points at an <c>abstractNum</c> — and may override individual
/// levels along the way. The level definition then carries a format and a <c>lvlText</c> template like
/// <c>%1.%2.</c> whose placeholders refer to the running counters of *outer* levels, not to itself.
/// </para>
/// <para>
/// Deliberately stateless. Running the counters is <see cref="NumberingSequencer"/>'s job, because
/// they are a property of a walk over the document rather than of the definitions — and definitions
/// that held counters could only be walked once, which is exactly the trap that made a re-read
/// paragraph come back with the wrong number.
/// </para>
/// </remarks>
sealed class WordNumbering
{
    readonly Dictionary<int, AbstractNum> abstractNumbering = new();
    readonly Dictionary<int, int> numToAbstract = new();
    readonly Dictionary<int, Dictionary<int, Level>> overrides = new();

    public WordNumbering(MainDocumentPart main) => this.Reload(main);

    /// <summary>
    /// Re-reads the definitions from the package.
    /// </summary>
    /// <remarks>
    /// Needed because the editor writes to <c>numbering.xml</c>: turning a paragraph into a list item
    /// creates the definition it points at, and a resolver still holding the state it was constructed
    /// with would report that brand new list as one the document does not have — so the paragraph
    /// would carry a <c>numId</c> and render with no bullet at all.
    /// </remarks>
    public void Reload(MainDocumentPart main)
    {
        this.abstractNumbering.Clear();
        this.numToAbstract.Clear();
        this.overrides.Clear();

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

    /// <summary>The definition for one level of one list, or null when the document has no such level.</summary>
    public ListLevel? Level(int numId, int levelIndex)
    {
        var level = this.FindLevel(numId, levelIndex);
        if (level is null)
            return null;

        var format = level.NumberingFormat?.Val?.Value ?? NumberFormatValues.Decimal;

        // Bullet glyphs come from symbol fonts whose code points mean nothing in a text font; mapping
        // the common ones keeps a list looking like a list.
        var template = format == NumberFormatValues.Bullet
            ? MapBullet(level.LevelText?.Val?.Value)
            : level.LevelText?.Val?.Value ?? "%1.";

        return new ListLevel(
            format,
            template,
            level.StartNumberingValue?.Val?.Value ?? 1,
            IndentOf(level),
            HangingOf(level));
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

    /// <summary>Renders one counter value in a level's number format.</summary>
    public static string Render(int value, NumberFormatValues format)
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
