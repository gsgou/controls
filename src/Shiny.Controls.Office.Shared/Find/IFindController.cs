namespace Shiny.Controls.Office.Text;

/// <summary>
/// The find state a toolbar drives: a query, a count, and a way to step through the hits.
/// </summary>
/// <remarks>
/// <para>
/// Implemented by the document, slide and spreadsheet finders, which is what lets one find bar per
/// host serve all three Office controls. The bar shows a box, a previous, a next and a readout; it has
/// no idea whether "the next one" means a paragraph, a shape on slide nine or a cell three sheets over.
/// </para>
/// <para>
/// Everything here is synchronous and cheap to read: <see cref="Count"/> and <see cref="Status"/> are
/// read on every render of the bar, so the matches behind them are collected once per query and cached
/// until the content changes.
/// </para>
/// </remarks>
public interface IFindController
{
    /// <summary>
    /// What is being searched for. Setting it re-runs the search and steps to the first hit.
    /// </summary>
    /// <remarks>
    /// Stepping immediately rather than waiting for a press on "next" is what makes the readout mean
    /// something as you type: a count with nothing selected leaves the user unable to tell a search
    /// that found twelve things from one that found twelve things somewhere they cannot see.
    /// </remarks>
    string Query { get; set; }

    /// <summary>Case and whole-word rules. Setting it re-runs the search.</summary>
    FindOptions Options { get; set; }

    /// <summary>True when there is a query to search for.</summary>
    bool IsSearching { get; }

    /// <summary>How many matches the current query has.</summary>
    int Count { get; }

    /// <summary>The zero-based index of the match the view is currently sitting on, or -1.</summary>
    int ActiveIndex { get; }

    /// <summary>
    /// The toolbar readout — <c>3/12</c> — or an empty string when nothing is being searched for.
    /// </summary>
    /// <remarks>
    /// One-based, because it is counting things for a person rather than indexing an array. A query
    /// with no hits reads <c>0/0</c> rather than going blank, so "found nothing" and "not searching"
    /// stay tellable apart.
    /// </remarks>
    string Status { get; }

    /// <summary>Steps to the next match, wrapping past the end. False when there is nothing to step to.</summary>
    bool FindNext();

    /// <summary>Steps to the previous match, wrapping past the start.</summary>
    bool FindPrevious();

    /// <summary>Drops the query and the matches, leaving the selection where the last step put it.</summary>
    void Clear();

    /// <summary>Raised when the query, the options or the match list change.</summary>
    event EventHandler? Changed;
}
