namespace Shiny.Controls.Office.Text;

/// <summary>
/// Where a word starts and ends, for double-click selection.
/// </summary>
/// <remarks>
/// Shared by the document and slide editors so that double-clicking a word means the same thing in
/// both. It is a small rule, but two copies of it drift: one of them ends up swallowing the full stop
/// after a sentence and the other does not, and nobody notices until a user formats the wrong span.
/// </remarks>
public static class WordBoundaries
{
    /// <summary>
    /// The span of the word covering <paramref name="offset"/>, as start and end offsets.
    /// </summary>
    /// <remarks>
    /// Runs of word characters and runs of everything else are both treated as spans, so clicking the
    /// gap between two words selects the gap rather than selecting nothing — which would be
    /// indistinguishable from the gesture having failed.
    /// </remarks>
    public static (int Start, int End) RangeAt(string text, int offset)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        // A click past the end of the line lands on the offset after the last character; the word it
        // means is the one that ends there.
        var index = Math.Clamp(offset, 0, text.Length - 1);
        var word = IsWordChar(text[index]);

        var start = index;
        while (start > 0 && IsWordChar(text[start - 1]) == word)
            start--;

        var end = index + 1;
        while (end < text.Length && IsWordChar(text[end]) == word)
            end++;

        return (start, end);
    }

    /// <summary>
    /// Whether a character counts as part of a word.
    /// </summary>
    /// <remarks>
    /// Apostrophes and underscores are in, so "don't" and "some_name" select whole. The curly
    /// apostrophe is in for the same reason: it is what a word processor autocorrects the straight one
    /// into, so leaving it out would break double-click on exactly the documents Word produced.
    /// Punctuation is out, which is what stops a double-click on "end." from taking the full stop.
    /// </remarks>
    public static bool IsWordChar(char c)
        => char.IsLetterOrDigit(c) || c is '_' or '\'' or '’';
}
