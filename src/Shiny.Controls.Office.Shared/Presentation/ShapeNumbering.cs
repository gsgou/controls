using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Presentation;

/// <summary>
/// Runs the counters behind a shape's auto-numbered paragraphs.
/// </summary>
/// <remarks>
/// <para>
/// PowerPoint stores <c>a:buAutoNum</c> — a scheme and, sometimes, a starting value — and works the
/// actual number out from the paragraph's position in the body. So, exactly like the Word side, a
/// paragraph read on its own cannot know its own number; only a walk over the body in order can.
/// </para>
/// <para>
/// One of these per text body, created by the reader. Sharing one across a slide would have the
/// second bulleted placeholder continue the first one's numbering.
/// </para>
/// </remarks>
sealed class ShapeNumbering
{
    readonly Dictionary<int, int> counters = new();

    /// <summary>The next value at an outline level, restarting anything nested inside it.</summary>
    public int Next(int level, int startAt)
    {
        var value = this.counters.TryGetValue(level, out var current) ? current + 1 : Math.Max(1, startAt);
        this.counters[level] = value;

        // Returning to an outer level restarts the inner ones, which is what makes a second top-level
        // item's sub-list start at "a" again rather than carrying on from the first one's.
        foreach (var deeper in this.counters.Keys.Where(x => x > level).ToList())
            this.counters.Remove(deeper);

        return value;
    }

    /// <summary>
    /// Renders one counter value in a DrawingML auto-number scheme.
    /// </summary>
    /// <remarks>
    /// Matched with <c>==</c> against the scheme values rather than by stringifying them: the SDK's
    /// enums are record structs whose <c>ToString</c> returns <c>"TextAutoNumberSchemeValues { }"</c>,
    /// so a switch on the name compiles, never throws, and matches nothing.
    /// </remarks>
    public static string Render(int value, D.TextAutoNumberSchemeValues scheme)
    {
        var (text, form) = Body(value, scheme);
        return form switch
        {
            Punctuation.Paren => $"({text})",
            Punctuation.ParenRight => $"{text})",
            _ => $"{text}."
        };
    }

    enum Punctuation
    {
        Period,
        Paren,
        ParenRight
    }

    static (string Text, Punctuation Form) Body(int value, D.TextAutoNumberSchemeValues scheme)
    {
        if (scheme == D.TextAutoNumberSchemeValues.AlphaLowerCharacterPeriod)
            return (Alphabetic(value), Punctuation.Period);

        if (scheme == D.TextAutoNumberSchemeValues.AlphaLowerCharacterParenR)
            return (Alphabetic(value), Punctuation.ParenRight);

        if (scheme == D.TextAutoNumberSchemeValues.AlphaLowerCharacterParenBoth)
            return (Alphabetic(value), Punctuation.Paren);

        if (scheme == D.TextAutoNumberSchemeValues.AlphaUpperCharacterPeriod)
            return (Alphabetic(value).ToUpperInvariant(), Punctuation.Period);

        if (scheme == D.TextAutoNumberSchemeValues.AlphaUpperCharacterParenR)
            return (Alphabetic(value).ToUpperInvariant(), Punctuation.ParenRight);

        if (scheme == D.TextAutoNumberSchemeValues.AlphaUpperCharacterParenBoth)
            return (Alphabetic(value).ToUpperInvariant(), Punctuation.Paren);

        if (scheme == D.TextAutoNumberSchemeValues.RomanLowerCharacterPeriod)
            return (Roman(value).ToLowerInvariant(), Punctuation.Period);

        if (scheme == D.TextAutoNumberSchemeValues.RomanLowerCharacterParenR)
            return (Roman(value).ToLowerInvariant(), Punctuation.ParenRight);

        if (scheme == D.TextAutoNumberSchemeValues.RomanLowerCharacterParenBoth)
            return (Roman(value).ToLowerInvariant(), Punctuation.Paren);

        if (scheme == D.TextAutoNumberSchemeValues.RomanUpperCharacterPeriod)
            return (Roman(value), Punctuation.Period);

        if (scheme == D.TextAutoNumberSchemeValues.RomanUpperCharacterParenR)
            return (Roman(value), Punctuation.ParenRight);

        if (scheme == D.TextAutoNumberSchemeValues.RomanUpperCharacterParenBoth)
            return (Roman(value), Punctuation.Paren);

        if (scheme == D.TextAutoNumberSchemeValues.ArabicParenR)
            return (value.ToString(), Punctuation.ParenRight);

        if (scheme == D.TextAutoNumberSchemeValues.ArabicParenBoth)
            return (value.ToString(), Punctuation.Paren);

        // Arabic with a period covers the default and every scheme this does not model — a Japanese
        // or Thai numbering renders as a plain number rather than as nothing at all.
        return (value.ToString(), Punctuation.Period);
    }

    /// <summary>a, b, ... z, aa, ab — the same bijective base-26 the Word side uses.</summary>
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
}
