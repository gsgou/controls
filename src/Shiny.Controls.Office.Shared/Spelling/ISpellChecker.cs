namespace Shiny.Controls.Office.Spelling;

/// <summary>A misspelled span within a piece of text.</summary>
public readonly record struct SpellingError(int Start, int Length, string Word)
{
    public int End => this.Start + this.Length;

    public bool Contains(int offset) => offset >= this.Start && offset < this.End;
}

/// <summary>
/// Spell checking and suggestions, supplied by the platform where one exists.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately text-in, ranges-out rather than word-by-word: iOS scans a whole string and Android
/// checks batches of sentences, and forcing either through a per-word call would mean one interop
/// round trip per word and would throw away the context both use to judge a word.
/// </para>
/// <para>
/// Implementations must be safe to call from a UI thread and must not block. Everything here is async
/// because Android's checker is a service session that answers on a callback.
/// </para>
/// </remarks>
public interface ISpellChecker
{
    /// <summary>
    /// False when this checker cannot actually check anything.
    /// </summary>
    /// <remarks>
    /// Separate from simply having no implementation: a platform checker can be present but have no
    /// dictionary installed for the requested language, and a caller wants to know that rather than
    /// silently getting zero errors and assuming the text is clean.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>Language tag to use when a caller does not specify one, e.g. <c>en-GB</c>.</summary>
    string DefaultLanguage { get; }

    /// <summary>Misspelled spans in <paramref name="text"/>, in ascending order.</summary>
    ValueTask<IReadOnlyList<SpellingError>> CheckAsync(
        string text,
        string? language = null,
        CancellationToken cancellationToken = default);

    /// <summary>Replacement candidates for one word, best first. Empty when there are none.</summary>
    ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        string? language = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stops reporting a word for the rest of the session.</summary>
    void Ignore(string word);

    /// <summary>Adds a word to the user's dictionary, where the platform supports it.</summary>
    void Learn(string word);
}

/// <summary>
/// Base class handling the parts every checker shares: the ignore list and language defaulting.
/// </summary>
public abstract class SpellCheckerBase : ISpellChecker
{
    readonly HashSet<string> ignored = new(StringComparer.OrdinalIgnoreCase);

    public abstract bool IsAvailable { get; }

    public virtual string DefaultLanguage { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.Name;

    public virtual void Ignore(string word)
    {
        if (!string.IsNullOrWhiteSpace(word))
            this.ignored.Add(word);
    }

    public virtual void Learn(string word) => this.Ignore(word);

    protected bool IsIgnored(string word) => this.ignored.Contains(word);

    protected string Language(string? requested)
        => string.IsNullOrWhiteSpace(requested) ? this.DefaultLanguage : requested;

    public async ValueTask<IReadOnlyList<SpellingError>> CheckAsync(
        string text,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || !this.IsAvailable)
            return [];

        var found = await this.CheckCoreAsync(text, this.Language(language), cancellationToken).ConfigureAwait(false);

        // Ignored words are filtered here rather than in every implementation, so "ignore all" behaves
        // identically on every platform.
        return found.Count == 0
            ? found
            : found.Where(x => !this.IsIgnored(x.Word)).ToList();
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        string? language = null,
        CancellationToken cancellationToken = default)
        => string.IsNullOrWhiteSpace(word) || !this.IsAvailable
            ? new ValueTask<IReadOnlyList<string>>([])
            : this.SuggestCoreAsync(word, this.Language(language), cancellationToken);

    protected abstract ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken);

    protected abstract ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken);
}

/// <summary>
/// The checker used where the platform provides none — notably WebAssembly.
/// </summary>
/// <remarks>
/// Reports nothing and says so through <see cref="IsAvailable"/>. Browsers only spell-check native
/// editable elements and expose no API to query results or suggestions, so there is nothing to call:
/// the alternative on the web is an application-supplied dictionary, which is what makes the checker
/// replaceable rather than fixed.
/// </remarks>
public sealed class NullSpellChecker : ISpellChecker
{
    public static readonly NullSpellChecker Instance = new();

    NullSpellChecker() { }

    public bool IsAvailable => false;

    public string DefaultLanguage => System.Globalization.CultureInfo.CurrentUICulture.Name;

    public ValueTask<IReadOnlyList<SpellingError>> CheckAsync(string text, string? language = null, CancellationToken cancellationToken = default)
        => new([]);

    public ValueTask<IReadOnlyList<string>> SuggestAsync(string word, string? language = null, CancellationToken cancellationToken = default)
        => new([]);

    public void Ignore(string word) { }

    public void Learn(string word) { }
}

/// <summary>
/// The checker the controls use when none is set explicitly.
/// </summary>
/// <remarks>
/// Each host package registers its platform checker here at startup, so an application gets spell
/// checking without wiring anything — and can still replace it, per control or globally, with its own
/// implementation (a bundled dictionary on the web, a server-side service, a domain word list).
/// </remarks>
public static class SpellCheckers
{
    static ISpellChecker current = NullSpellChecker.Instance;

    public static ISpellChecker Default
    {
        get => current;
        set => current = value ?? NullSpellChecker.Instance;
    }

    /// <summary>Registers a checker unless the application has already chosen one.</summary>
    public static void SetDefaultIfUnset(ISpellChecker checker)
    {
        if (ReferenceEquals(current, NullSpellChecker.Instance))
            current = checker;
    }
}
