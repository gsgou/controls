namespace Shiny.Controls.Office.Text;

/// <summary>
/// Finds every occurrence of a query in a run of text.
/// </summary>
/// <remarks>
/// The one matcher behind all three Office finders. A paragraph, a shape's line and a cell's contents
/// are all just strings by the time they reach here, so the rules that decide what counts as a match —
/// case, word boundaries, and what an empty query means — are written once instead of three times.
/// </remarks>
public static class TextSearch
{
    /// <summary>
    /// Every match in <paramref name="text"/>, left to right and non-overlapping.
    /// </summary>
    /// <remarks>
    /// Non-overlapping on purpose: searching <c>aa</c> in <c>aaaa</c> gives two matches rather than
    /// three, because a find box's "next" is meant to step through the document rather than shuffle one
    /// character along inside a word.
    /// </remarks>
    public static IEnumerable<TextMatch> Matches(string? text, string? query, FindOptions? options = null)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            yield break;

        options ??= FindOptions.Default;
        var comparison = options.Comparison;

        var from = 0;
        while (from <= text.Length - query.Length)
        {
            var at = text.IndexOf(query, from, comparison);
            if (at < 0)
                yield break;

            if (!options.WholeWord || IsWholeWord(text, at, query.Length))
            {
                yield return new TextMatch(at, query.Length);
                from = at + query.Length;
            }
            else
            {
                // A rejected whole-word hit advances by one rather than by the query's length: the "on"
                // inside "front" must not swallow the rest of the word, or the standalone "on" in
                // "front on" is never seen.
                from = at + 1;
            }
        }
    }

    /// <summary>True when nothing word-like abuts either end of the span.</summary>
    public static bool IsWholeWord(string text, int start, int length)
    {
        if (start > 0 && WordBoundaries.IsWordChar(text[start - 1]))
            return false;

        var end = start + length;
        return end >= text.Length || !WordBoundaries.IsWordChar(text[end]);
    }
}


/// <summary>A hit inside one string: where it starts and how long it is.</summary>
public readonly record struct TextMatch(int Start, int Length)
{
    public int End => this.Start + this.Length;
}
