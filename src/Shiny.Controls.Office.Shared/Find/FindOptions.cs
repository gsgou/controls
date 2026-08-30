namespace Shiny.Controls.Office.Text;

/// <summary>
/// How a search compares its query against the text it walks.
/// </summary>
/// <remarks>
/// One record shared by the document, slide and spreadsheet finders. The three walk very different
/// content, but "does this stretch of text match?" is the same question in all three, and answering it
/// three times is how one of them ends up case-sensitive by accident.
/// </remarks>
public sealed record FindOptions
{
    /// <summary>Case-insensitive, whole or partial words — what a find box does before anyone opens its options.</summary>
    public static readonly FindOptions Default = new();

    /// <summary>True to require the same case, so <c>IT</c> stops matching <c>it</c>.</summary>
    public bool MatchCase { get; init; }

    /// <summary>
    /// True to reject a match that is part of a longer word.
    /// </summary>
    /// <remarks>
    /// "Whole" is decided with <see cref="WordBoundaries.IsWordChar"/>, the same rule double-click
    /// selection uses — so a search for <c>don</c> does not match <c>don't</c>, which is one word to
    /// the user however many characters it is made of.
    /// </remarks>
    public bool WholeWord { get; init; }

    /// <summary>The comparison this implies, for callers that search with the framework's own methods.</summary>
    public StringComparison Comparison
        => this.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
