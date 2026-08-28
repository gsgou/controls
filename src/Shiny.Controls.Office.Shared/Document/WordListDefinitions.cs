using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Creates the <c>numbering.xml</c> definitions a paragraph needs before it can become a list item.
/// </summary>
/// <remarks>
/// <para>
/// A Word paragraph does not carry its own bullet. It carries a <c>numId</c> pointing into
/// <c>numbering.xml</c>, so "make this a bulleted list" is really "find or create a nine-level
/// definition, then point at it" — and a document that has never had a list in it has no numbering
/// part at all.
/// </para>
/// <para>
/// The definitions this writes are stamped with a fixed <c>w:nsid</c>, which is how a second call
/// finds the first one's work instead of adding a near-identical abstract definition every time the
/// button is pressed. Word uses that field the same way; it is a list identity, not a checksum.
/// </para>
/// </remarks>
static class WordListDefinitions
{
    /// <summary>How many levels a created definition defines. Word writes nine; so does this.</summary>
    public const int Levels = 9;

    /// <summary>The <c>w:nsid</c> stamped on the definitions this creates, one per style.</summary>
    const string BulletNsid = "5A1B0001";
    const string NumberNsid = "5A1B0002";

    /// <summary>
    /// The bullet glyphs, by level, cycling every three — Word's own defaults.
    /// </summary>
    /// <remarks>
    /// These are Symbol and Wingdings code points, not the glyphs they look like. Written as Word
    /// writes them so a saved document opens in Word with the bullets it had here; the reader maps
    /// them back to drawable characters on the way in.
    /// </remarks>
    static readonly (string Char, string Font)[] BulletGlyphs =
    [
        ("\uF0B7", "Symbol"),
        ("o", "Courier New"),
        ("\uF0A7", "Wingdings")
    ];

    /// <summary>The number formats, by level, cycling every three.</summary>
    static readonly NumberFormatValues[] NumberFormats =
    [
        NumberFormatValues.Decimal,
        NumberFormatValues.LowerLetter,
        NumberFormatValues.LowerRoman
    ];

    /// <summary>
    /// The <c>numId</c> for a list of this style, creating the definitions when they do not exist yet.
    /// </summary>
    /// <returns>Zero when the document has no main part to write into.</returns>
    public static int Ensure(MainDocumentPart? main, ListStyle style)
    {
        if (main is null || style == ListStyle.None)
            return 0;

        var part = main.NumberingDefinitionsPart ?? main.AddNewPart<NumberingDefinitionsPart>();
        var numbering = part.Numbering ??= new Numbering();

        var nsid = style == ListStyle.Bullet ? BulletNsid : NumberNsid;
        var abstractNum = numbering
            .Elements<AbstractNum>()
            .FirstOrDefault(x => x.Nsid?.Val?.Value == nsid);

        if (abstractNum is null)
        {
            abstractNum = Build(style, nsid, NextAbstractId(numbering));

            // w:abstractNum comes before every w:num in the schema, so a new definition goes in front
            // of the first instance rather than on the end. Appending produces a file Word rejects.
            var firstInstance = numbering.Elements<NumberingInstance>().FirstOrDefault();
            if (firstInstance is null)
                numbering.AppendChild(abstractNum);
            else
                numbering.InsertBefore(abstractNum, firstInstance);
        }

        var abstractId = abstractNum.AbstractNumberId!.Value;

        // One instance per definition is enough: every list this editor creates of a given style
        // shares a sequence, which is what makes a second bulleted list continue looking like the
        // first. A numbered list that should restart is a separate feature, and needs a
        // w:lvlOverride rather than another instance.
        var instance = numbering
            .Elements<NumberingInstance>()
            .FirstOrDefault(x => x.AbstractNumId?.Val?.Value == abstractId);

        if (instance is null)
        {
            instance = new NumberingInstance(new AbstractNumId { Val = abstractId })
            {
                NumberID = NextNumberId(numbering)
            };

            numbering.AppendChild(instance);
        }

        return instance.NumberID!.Value;
    }

    static AbstractNum Build(ListStyle style, string nsid, int abstractId)
    {
        var abstractNum = new AbstractNum(
            new Nsid { Val = nsid },
            new MultiLevelType { Val = MultiLevelValues.Multilevel })
        {
            AbstractNumberId = abstractId
        };

        for (var level = 0; level < Levels; level++)
            abstractNum.AppendChild(style == ListStyle.Bullet ? BulletLevel(level) : NumberLevel(level));

        return abstractNum;
    }

    static Level BulletLevel(int index)
    {
        var (glyph, font) = BulletGlyphs[index % BulletGlyphs.Length];

        return new Level(
            new StartNumberingValue { Val = 1 },
            new NumberingFormat { Val = NumberFormatValues.Bullet },
            new LevelText { Val = glyph },
            new LevelJustification { Val = LevelJustificationValues.Left },
            Indent(index),

            // Without the font the glyph is a code point in whatever face the run uses, which for
            // F0B7 is either a wrong character or a blank.
            new NumberingSymbolRunProperties(new RunFonts { Ascii = font, HighAnsi = font, Hint = FontTypeHintValues.Default }))
        {
            LevelIndex = index
        };
    }

    /// <summary>
    /// One level of a compounding numbered list — 1., then 1a., then 1ai.
    /// </summary>
    /// <remarks>
    /// The template names every level above this one as well as this one, which is what makes a
    /// nested item read as <c>1a</c> rather than as a bare <c>a</c> that says nothing about which
    /// item it belongs to. Each placeholder renders in its own level's format, so the outer counters
    /// stay decimal while this one is a letter.
    /// </remarks>
    static Level NumberLevel(int index)
    {
        var template = new System.Text.StringBuilder();
        for (var i = 0; i <= index; i++)
            template.Append('%').Append(i + 1);

        template.Append('.');

        return new Level(
            new StartNumberingValue { Val = 1 },
            new NumberingFormat { Val = NumberFormats[index % NumberFormats.Length] },
            new LevelText { Val = template.ToString() },
            new LevelJustification { Val = LevelJustificationValues.Left },
            Indent(index))
        {
            LevelIndex = index
        };
    }

    /// <summary>
    /// The hanging indent for a level: half an inch per level, with the label in the hanging part.
    /// </summary>
    /// <remarks>
    /// Wrapped in <c>w:pPr</c>, which is where a level's paragraph properties live. Handing the
    /// <c>w:ind</c> to the level directly compiles against the typed API and writes a child the
    /// schema does not have there, so the indent is silently not found: the item is not indented and
    /// its bullet, having no hanging space to sit in, is painted on top of the first letter.
    /// </remarks>
    static PreviousParagraphProperties Indent(int index) => new(new Indentation
    {
        Left = (720 * (index + 1)).ToString(),
        Hanging = "360"
    });

    static int NextAbstractId(Numbering numbering)
    {
        var max = -1;
        foreach (var existing in numbering.Elements<AbstractNum>())
        {
            if (existing.AbstractNumberId?.Value is { } id && id > max)
                max = id;
        }

        return max + 1;
    }

    static int NextNumberId(Numbering numbering)
    {
        // Zero is not an id: a paragraph with w:numId="0" is explicitly *not* in a list, which is how
        // a style-supplied list is turned off.
        var max = 0;
        foreach (var existing in numbering.Elements<NumberingInstance>())
        {
            if (existing.NumberID?.Value is { } id && id > max)
                max = id;
        }

        return max + 1;
    }
}
