using Android.Content;
using Android.Views.TextService;
using Shiny.Controls.Office.Spelling;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Spell checking through Android's text-services framework.
/// </summary>
/// <remarks>
/// <para>
/// Android's checker is an out-of-process service reached through a session that answers on a
/// callback, so every call here is genuinely asynchronous — unlike Apple's, which is a local call.
/// A <see cref="TaskCompletionSource"/> bridges the listener back to the awaiting caller.
/// </para>
/// <para>
/// The session is created once and reused. Opening one per check would pay the service bind cost on
/// every paragraph, which costs far more than the check itself.
/// </para>
/// </remarks>
public sealed class AndroidSpellChecker : SpellCheckerBase, IDisposable
{
    /// <summary>
    /// The only attribute that means "this word is wrong".
    /// </summary>
    /// <remarks>
    /// The others report that the word is in the dictionary, that it is a grammar problem, or that the
    /// checker had no opinion — treating any of those as an error underlines most of the document. The
    /// enum is used rather than <c>SuggestionsInfo.ResultAttrLooksLikeTypo</c>, which is obsolete.
    /// </remarks>
    const int LooksLikeTypo = (int)SuggestionsAttributes.LooksLikeTypo;

    readonly object gate = new();
    readonly TimeSpan timeout = TimeSpan.FromSeconds(3);

    SpellCheckerSession? session;
    Listener? listener;
    bool unavailable;

    public override bool IsAvailable => !this.unavailable && IsEnabled(GetManager());

    static TextServicesManager? GetManager()
        => Android.App.Application.Context.GetSystemService(Context.TextServicesManagerService)
            as TextServicesManager;

    /// <summary>
    /// Whether the user has a spell checker turned on.
    /// </summary>
    /// <remarks>
    /// <c>IsSpellCheckerEnabled</c> only exists from API 31. Below that the setting cannot be read at
    /// all, so having a text-services manager is taken as enough — a session that then fails to open
    /// sets <c>unavailable</c> and the editor stops asking.
    /// </remarks>
    static bool IsEnabled(TextServicesManager? manager)
        => manager is not null
            && (!OperatingSystem.IsAndroidVersionAtLeast(31) || manager.IsSpellCheckerEnabled);

    SpellCheckerSession? EnsureSession()
    {
        lock (this.gate)
        {
            if (this.session is not null)
                return this.session;

            var manager = GetManager();
            if (!IsEnabled(manager))
            {
                this.unavailable = true;
                return null;
            }

            this.listener = new Listener();

            // referToSpellCheckerLanguageSettings: honour the user's own choice of dictionary rather
            // than forcing one, which is what the system keyboard does.
            this.session = manager!.NewSpellCheckerSession(null, null, this.listener, referToSpellCheckerLanguageSettings: true);
            if (this.session is null)
                this.unavailable = true;

            return this.session;
        }
    }

    /// <summary>
    /// Checks a whole paragraph in one request.
    /// </summary>
    /// <remarks>
    /// The sentence API is used rather than the per-word one for two reasons: it is the only one not
    /// deprecated, and it reports offsets back into the text passed in, so nothing has to be mapped
    /// from a word list. It also lets the checker see each word in its sentence, which is how it tells
    /// "there" from "their".
    /// </remarks>
    protected override async ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken)
    {
        var results = await this.RequestAsync(text, suggestionsLimit: 5, cancellationToken).ConfigureAwait(false);
        if (results is null)
            return [];

        var errors = new List<SpellingError>();

        foreach (var sentence in results)
        {
            if (sentence is null)
                continue;

            for (var i = 0; i < sentence.SuggestionsCount; i++)
            {
                var info = sentence.GetSuggestionsInfoAt(i);
                if (info is null || (info.SuggestionsAttributes & LooksLikeTypo) == 0)
                    continue;

                var start = sentence.GetOffsetAt(i);
                var length = sentence.GetLengthAt(i);

                // The service is another process and its offsets are not trusted blindly: a stale
                // reply against newer text would otherwise throw out of Substring.
                if (start < 0 || length <= 0 || start + length > text.Length)
                    continue;

                errors.Add(new SpellingError(start, length, text.Substring(start, length)));
            }
        }

        errors.Sort((a, b) => a.Start.CompareTo(b.Start));
        return errors;
    }

    protected override async ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken)
    {
        var results = await this.RequestAsync(word, suggestionsLimit: 8, cancellationToken).ConfigureAwait(false);

        var info = results?.FirstOrDefault() is { SuggestionsCount: > 0 } sentence
            ? sentence.GetSuggestionsInfoAt(0)
            : null;

        if (info is null)
            return [];

        var result = new List<string>();
        for (var i = 0; i < info.SuggestionsCount; i++)
        {
            var candidate = info.GetSuggestionAt(i);
            if (!string.IsNullOrEmpty(candidate))
                result.Add(candidate);
        }

        return result;
    }

    async Task<SentenceSuggestionsInfo[]?> RequestAsync(string text, int suggestionsLimit, CancellationToken cancellationToken)
    {
        var session = this.EnsureSession();
        if (session is null || this.listener is null)
            return null;

        var completion = this.listener.Expect();
        session.GetSentenceSuggestions([new TextInfo(text)], suggestionsLimit);

        try
        {
            // The session is an IPC binding to another process, and a spell checker that has died or
            // is still starting up simply never calls back. Giving up returns no errors, which is what
            // a background check with nobody awaiting its exception needs.
            return await completion.WaitAsync(this.timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>Bridges the service's callback back to the awaiting caller.</summary>
    sealed class Listener : Java.Lang.Object, SpellCheckerSession.ISpellCheckerSessionListener
    {
        TaskCompletionSource<SentenceSuggestionsInfo[]?>? pending;

        public Task<SentenceSuggestionsInfo[]?> Expect()
        {
            // One request in flight at a time: the callback carries no correlation id, so two
            // overlapping requests could not be told apart.
            this.pending?.TrySetResult(null);
            this.pending = new TaskCompletionSource<SentenceSuggestionsInfo[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
            return this.pending.Task;
        }

        public void OnGetSentenceSuggestions(SentenceSuggestionsInfo[]? results) => this.pending?.TrySetResult(results);

        /// <summary>Never called — this checker only ever uses the sentence API.</summary>
        public void OnGetSuggestions(SuggestionsInfo[]? results) => this.pending?.TrySetResult(null);
    }

    public void Dispose()
    {
        lock (this.gate)
        {
            this.session?.Close();
            this.session = null;
            this.listener?.Dispose();
            this.listener = null;
        }
    }
}
