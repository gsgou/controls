using AppKit;
using Foundation;
using Shiny.Controls.Office.Spelling;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Spell checking through <see cref="NSSpellChecker"/> — the engine behind every macOS text view,
/// including the user's own learned words.
/// </summary>
/// <remarks>
/// <para>
/// The AppKit head has no UIKit, so this is the macOS counterpart to <c>AppleSpellChecker</c>. The
/// shape is the same — find one misspelling, resume past it — but the two APIs differ enough that
/// sharing a file would be more <c>#if</c> than code.
/// </para>
/// <para>
/// <c>NSSpellChecker</c> is main-thread-only, and its callers here are the editor's own layout and
/// paint paths, which already are.
/// </para>
/// </remarks>
public sealed class AppKitSpellChecker : SpellCheckerBase
{
    // Shared and app-wide: the same object the rest of macOS checks against, which is exactly why
    // learned words carry across.
    static NSSpellChecker Checker => NSSpellChecker.SharedSpellChecker;

    // A document tag of this checker's own, so its ignored words do not leak into any other document
    // being checked in the same process.
    readonly nint tag = NSSpellChecker.UniqueSpellDocumentTag;

    public override bool IsAvailable => true;

    protected override ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken)
    {
        var errors = new List<SpellingError>();
        var resolved = Resolve(language);
        var offset = 0;

        while (offset < text.Length && !cancellationToken.IsCancellationRequested)
        {
            var range = Checker.CheckSpelling(text, offset, resolved, false, this.tag, out _);

            if (range.Location == NSRange.NotFound || range.Length <= 0)
                break;

            var start = (int)range.Location;
            var length = (int)range.Length;
            if (start < 0 || start + length > text.Length)
                break;

            errors.Add(new SpellingError(start, length, text.Substring(start, length)));
            offset = start + length;
        }

        return new ValueTask<IReadOnlyList<SpellingError>>(errors);
    }

    protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken)
    {
        var guesses = Checker.GuessesForWordRange(
            new NSRange(0, word.Length),
            word,
            Resolve(language),
            this.tag);

        IReadOnlyList<string> result = guesses?.ToList() ?? [];
        return new ValueTask<IReadOnlyList<string>>(result);
    }

    public override void Learn(string word)
    {
        base.Learn(word);
        Checker.LearnWord(word);
    }

    /// <summary>
    /// Falls back to a language macOS actually has a dictionary for.
    /// </summary>
    /// <remarks>
    /// Unlike iOS, the AppKit binding exposes the available list correctly, so membership can be
    /// checked rather than guessed. It matters: an unknown tag makes NSSpellChecker report no
    /// mistakes rather than fail, which is indistinguishable from correctly-spelled text.
    /// </remarks>
    static string Resolve(string language)
    {
        var available = Checker.AvailableLanguages ?? [];
        if (available.Contains(language))
            return language;

        // en-GB -> en, or whichever regional variant the machine has.
        var separator = language.IndexOfAny(['-', '_']);
        if (separator > 0)
        {
            var prefix = language[..separator];
            var match = available.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return Checker.Language ?? "en";
    }
}
