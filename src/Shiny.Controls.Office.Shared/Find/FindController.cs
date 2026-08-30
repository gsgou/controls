namespace Shiny.Controls.Office.Text;

/// <summary>
/// The half of a find that is the same whatever is being searched: the query, the cached match list,
/// which one is active, and the wrap.
/// </summary>
/// <typeparam name="TMatch">
/// How one hit is addressed in the content being searched — a block and an offset, a shape on a slide,
/// a cell on a sheet. A value type with structural equality, because the active match is remembered by
/// value and re-found by value after the content underneath it changes.
/// </typeparam>
/// <remarks>
/// <para>
/// Subclasses supply three things: how to collect the matches, how to move the view onto one, and
/// where the caret currently is in the match order. Everything else — stepping, wrapping, the
/// one-based readout, invalidating on an edit — is here, so the three Office finders cannot disagree
/// about what "next" does at the end of the content.
/// </para>
/// <para>
/// Matches are collected lazily and cached. A toolbar reads <see cref="Count"/> and
/// <see cref="Status"/> on every render, and re-walking the document each time would put a full text
/// scan in front of every repaint.
/// </para>
/// </remarks>
public abstract class FindController<TMatch> : IFindController
    where TMatch : struct
{
    string query = string.Empty;
    FindOptions options = FindOptions.Default;
    IReadOnlyList<TMatch>? matches;
    TMatch? active;

    /// <inheritdoc/>
    public string Query
    {
        get => this.query;
        set
        {
            var next = value ?? string.Empty;
            if (this.query == next)
                return;

            this.query = next;
            this.Restart();
        }
    }

    /// <inheritdoc/>
    public FindOptions Options
    {
        get => this.options;
        set
        {
            var next = value ?? FindOptions.Default;
            if (this.options == next)
                return;

            this.options = next;
            this.Restart();
        }
    }

    /// <inheritdoc/>
    public bool IsSearching => this.query.Length > 0;

    /// <summary>Every match for the current query, in reading order. Empty while nothing is searched for.</summary>
    public IReadOnlyList<TMatch> Matches => this.matches ??= this.IsSearching ? this.Collect(this.query, this.options) : [];

    /// <inheritdoc/>
    public int Count => this.Matches.Count;

    /// <inheritdoc/>
    public int ActiveIndex
    {
        get
        {
            if (this.active is not { } current)
                return -1;

            // Looked up by value rather than remembered as an index. An edit anywhere above the active
            // match shifts every index below it, and an index kept across that lands on a different
            // word - which reads as the arrows having jumped somewhere at random.
            var found = this.Matches;
            for (var i = 0; i < found.Count; i++)
            {
                if (EqualityComparer<TMatch>.Default.Equals(found[i], current))
                    return i;
            }

            return -1;
        }
    }

    /// <summary>The match the view is sitting on, or null when nothing has been stepped to.</summary>
    public TMatch? Active => this.ActiveIndex >= 0 ? this.active : null;

    /// <inheritdoc/>
    public string Status
    {
        get
        {
            if (!this.IsSearching)
                return string.Empty;

            var count = this.Count;
            if (count == 0)
                return "0/0";

            var index = this.ActiveIndex;
            return $"{(index < 0 ? 0 : index + 1)}/{count}";
        }
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public bool FindNext() => this.Step(forward: true);

    /// <inheritdoc/>
    public bool FindPrevious() => this.Step(forward: false);

    /// <inheritdoc/>
    public void Clear()
    {
        if (!this.IsSearching && this.active is null)
            return;

        this.query = string.Empty;
        this.matches = null;
        this.active = null;
        this.RaiseChanged();
    }

    /// <summary>
    /// Drops the cached matches after the content has changed, keeping the query.
    /// </summary>
    /// <remarks>
    /// Deliberately does not move the view. An edit is the user typing, and jumping them to a match
    /// because the paragraph they are in gained one is the last thing a find should do.
    /// </remarks>
    public void Invalidate()
    {
        if (this.matches is null)
            return;

        this.matches = null;
        this.RaiseChanged();
    }

    /// <summary>Every match for a query, in the order the content reads.</summary>
    protected abstract IReadOnlyList<TMatch> Collect(string query, FindOptions options);

    /// <summary>Selects a match and brings it on screen.</summary>
    protected abstract void MoveTo(TMatch match);

    /// <summary>
    /// Where the caret sits in the match order: the index of the first match at or after it, or
    /// <c>matches.Count</c> when the caret is past the last one.
    /// </summary>
    /// <remarks>
    /// This is what makes the first press of "next" land on the hit below the caret rather than at the
    /// top of the document — a find that always restarted from the beginning would take the user away
    /// from what they were reading.
    /// </remarks>
    protected abstract int IndexAtOrAfterCaret(IReadOnlyList<TMatch> matches);

    protected void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Re-runs the search and steps onto the first hit at or after the caret.</summary>
    void Restart()
    {
        this.matches = null;
        this.active = null;

        var found = this.Matches;
        if (found.Count > 0)
        {
            var index = this.IndexAtOrAfterCaret(found);
            this.Land(found, index >= found.Count ? 0 : index);
        }

        this.RaiseChanged();
    }

    bool Step(bool forward)
    {
        var found = this.Matches;
        if (found.Count == 0)
            return false;

        var current = this.ActiveIndex;

        int next;
        if (current < 0)
        {
            // Nothing active - either the query has only just been typed or the match that was active
            // has been edited away. Either way the caret is the only thing that says where "next"
            // starts from.
            var anchor = this.IndexAtOrAfterCaret(found);
            next = forward
                ? (anchor >= found.Count ? 0 : anchor)
                : (anchor <= 0 ? found.Count - 1 : anchor - 1);
        }
        else
        {
            next = forward
                ? (current + 1) % found.Count
                : (current - 1 + found.Count) % found.Count;
        }

        this.Land(found, next);
        this.RaiseChanged();
        return true;
    }

    void Land(IReadOnlyList<TMatch> found, int index)
    {
        var match = found[Math.Clamp(index, 0, found.Count - 1)];
        this.active = match;
        this.MoveTo(match);
    }
}
