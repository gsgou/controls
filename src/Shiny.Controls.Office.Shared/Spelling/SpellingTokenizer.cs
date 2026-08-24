namespace Shiny.Controls.Office.Spelling;

/// <summary>
/// Splits text into the words worth spell checking.
/// </summary>
/// <remarks>
/// The exclusions matter more than the splitting. Flagging URLs, email addresses, numbers and
/// ALL-CAPS acronyms produces a document covered in red underlines that the reader learns to ignore,
/// which is worse than not checking at all.
/// </remarks>
public static class SpellingTokenizer
{
    /// <summary>Word spans in <paramref name="text"/>, skipping anything not worth checking.</summary>
    public static IEnumerable<(int Start, int Length)> Words(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        var i = 0;
        while (i < text.Length)
        {
            if (!IsWordStart(text[i]))
            {
                i++;
                continue;
            }

            var start = i;
            while (i < text.Length && IsWordPart(text, i))
                i++;

            var length = i - start;
            if (length > 1 && IsCheckable(text.AsSpan(start, length)))
                yield return (start, length);
        }
    }

    /// <summary>The word containing or immediately before an offset — what a right-click should act on.</summary>
    public static (int Start, int Length)? WordAt(string text, int offset)
    {
        foreach (var (start, length) in Words(text))
        {
            if (offset >= start && offset <= start + length)
                return (start, length);

            if (start > offset)
                break;
        }

        return null;
    }

    static bool IsWordStart(char c) => char.IsLetter(c);

    static bool IsWordPart(string text, int index)
    {
        var c = text[index];
        if (char.IsLetter(c))
            return true;

        // An apostrophe or hyphen continues a word only between letters, so "don't" and "well-known"
        // stay whole while a closing quote does not glue onto the next word.
        if (c is '\'' or '’' or '-')
            return index > 0 && char.IsLetter(text[index - 1]) &&
                   index + 1 < text.Length && char.IsLetter(text[index + 1]);

        return false;
    }

    static bool IsCheckable(ReadOnlySpan<char> word)
    {
        var upper = 0;
        var letters = 0;

        foreach (var c in word)
        {
            if (!char.IsLetter(c))
                continue;

            letters++;
            if (char.IsUpper(c))
                upper++;
        }

        // An acronym: no dictionary has it and every one of them would be flagged.
        if (letters > 1 && upper == letters)
            return false;

        // Mixed-case interior (camelCase, ProperNounInCode) is almost always an identifier.
        for (var i = 1; i < word.Length - 1; i++)
        {
            if (char.IsUpper(word[i]) && char.IsLower(word[i - 1]))
                return false;
        }

        return true;
    }

    /// <summary>True when an offset sits inside a URL, email address or file path.</summary>
    public static bool IsInsideUri(string text, int offset)
    {
        // Walk back to the start of the whitespace-delimited token and judge the whole thing: a URL is
        // only recognisable as one when its punctuation is still attached.
        var start = offset;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
            start--;

        var end = offset;
        while (end < text.Length && !char.IsWhiteSpace(text[end]))
            end++;

        var token = text.AsSpan(start, end - start);
        return token.Contains("://", StringComparison.Ordinal)
            || token.Contains('@')
            || token.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            || token.Contains('/')
            || token.Contains('\\');
    }
}
