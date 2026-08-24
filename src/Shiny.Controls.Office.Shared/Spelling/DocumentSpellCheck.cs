using Shiny.Controls.Office.Document;

namespace Shiny.Controls.Office.Spelling;

/// <summary>
/// Runs a spell checker over a document's paragraphs and caches the results.
/// </summary>
/// <remarks>
/// <para>
/// Checking is per paragraph and keyed on the paragraph's text, so editing one paragraph re-checks
/// only that paragraph and scrolling re-checks nothing. Without the cache every repaint would issue
/// interop calls for the whole visible page, which on Android means a service round trip per frame.
/// </para>
/// <para>
/// Results are returned as offsets into the paragraph's text, the same space the caret uses.
/// </para>
/// </remarks>
public sealed class DocumentSpellCheck(ISpellChecker checker)
{
    readonly Dictionary<int, CachedResult> cache = new();

    sealed record CachedResult(string Text, IReadOnlyList<SpellingError> Errors);

    public ISpellChecker Checker { get; set; } = checker;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Raised when a check completes and something needs repainting.</summary>
    public event EventHandler? Updated;

    /// <summary>Errors already known for a paragraph. Never blocks; may be empty until a check lands.</summary>
    public IReadOnlyList<SpellingError> ErrorsFor(int block, string text)
    {
        if (!this.IsEnabled || !this.Checker.IsAvailable)
            return [];

        return this.cache.TryGetValue(block, out var cached) && cached.Text == text
            ? cached.Errors
            : [];
    }

    /// <summary>
    /// Checks the paragraphs whose results are missing or stale.
    /// </summary>
    /// <remarks>
    /// Callers pass only the blocks currently on screen. Checking the whole document up front would
    /// stall on a long one for no benefit, since nothing off screen can show a squiggle.
    /// </remarks>
    public async Task RefreshAsync(
        IReadOnlyList<DocumentBlock> blocks,
        int first,
        int last,
        CancellationToken cancellationToken = default)
    {
        if (!this.IsEnabled || !this.Checker.IsAvailable)
            return;

        var changed = false;

        for (var i = Math.Max(0, first); i <= Math.Min(last, blocks.Count - 1); i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (blocks[i] is not DocumentParagraph paragraph)
                continue;

            var text = paragraph.PlainText;
            if (text.Length == 0)
            {
                changed |= this.cache.Remove(i);
                continue;
            }

            if (this.cache.TryGetValue(i, out var cached) && cached.Text == text)
                continue;

            var errors = await this.Checker.CheckAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Filter here rather than in the tokenizer: the platform checker may well flag a URL, and
            // this is the one place that knows the surrounding text well enough to tell.
            var filtered = errors.Where(x => !SpellingTokenizer.IsInsideUri(text, x.Start)).ToList();

            this.cache[i] = new CachedResult(text, filtered);
            changed = true;
        }

        if (changed)
            this.Updated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Drops cached results, e.g. after the language or the checker itself changes.</summary>
    public void Invalidate()
    {
        this.cache.Clear();
        this.Updated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Drops one paragraph's results. Later blocks shift when paragraphs are added or removed.</summary>
    public void InvalidateFrom(int block)
    {
        foreach (var key in this.cache.Keys.Where(x => x >= block).ToList())
            this.cache.Remove(key);
    }
}
