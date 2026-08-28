namespace Shiny.Controls.Office.Text;

/// <summary>What kind of list a paragraph is in, if any.</summary>
/// <remarks>
/// Shared by the Word and PowerPoint editors deliberately. The two file formats express a list in
/// completely different ways — Word points a paragraph at <c>numbering.xml</c>, PowerPoint puts the
/// bullet in the paragraph's own properties — but a toolbar has the same three states either way, and
/// so does the autoformat below.
/// </remarks>
public enum ListStyle
{
    /// <summary>Not a list item.</summary>
    None,

    /// <summary>A bulleted item. Which glyph depends on the level.</summary>
    Bullet,

    /// <summary>A numbered item. The number is a function of its place in the sequence.</summary>
    Numbered
}


/// <summary>
/// Recognises a list a user has started typing by hand.
/// </summary>
/// <remarks>
/// <para>
/// Word and PowerPoint both do this: type <c>-</c> or <c>1.</c> at the start of a paragraph, press
/// space, and the marker disappears into a real list. Both editors call this from their text-insertion
/// path with the text standing before the caret, so the two behave identically without either one
/// owning the rules.
/// </para>
/// <para>
/// Deliberately conservative. Only a marker that is the <em>entire</em> paragraph so far counts, so a
/// hyphen mid-sentence is never touched, and only digits number a list — a lone letter followed by a
/// full stop is far too easy to type on purpose.
/// </para>
/// </remarks>
public static class ListAutoFormat
{
    /// <summary>The longest prefix worth examining, so a long paragraph is rejected on its length.</summary>
    const int MaxMarkerLength = 5;

    /// <summary>
    /// The list a paragraph beginning with <paramref name="prefix"/> is asking to become.
    /// </summary>
    /// <param name="prefix">Everything in the paragraph before the caret, with nothing else after it.</param>
    /// <returns><see cref="ListStyle.None"/> when the prefix is not a list marker.</returns>
    public static ListStyle Detect(string? prefix)
    {
        if (String.IsNullOrEmpty(prefix) || prefix.Length > MaxMarkerLength)
            return ListStyle.None;

        if (prefix is "-" or "*" or "+" or "•")
            return ListStyle.Bullet;

        // A run of digits closed by '.' or ')'. The digits themselves are ignored: a list that
        // started at "7." would have to renumber the whole sequence around it to keep that promise,
        // and Word does not honour it either.
        if (prefix.Length < 2 || (prefix[^1] != '.' && prefix[^1] != ')'))
            return ListStyle.None;

        foreach (var c in prefix[..^1])
        {
            if (!char.IsAsciiDigit(c))
                return ListStyle.None;
        }

        return ListStyle.Numbered;
    }
}
