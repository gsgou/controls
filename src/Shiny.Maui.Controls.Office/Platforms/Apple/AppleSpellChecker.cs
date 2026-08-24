using Foundation;
using Shiny.Controls.Office.Spelling;
using UIKit;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Spell checking through <see cref="UITextChecker"/> — the same engine as the system keyboard,
/// including words the user has taught it.
/// </summary>
/// <remarks>
/// <para>
/// <c>UITextChecker</c> finds one misspelling at a time and takes a starting offset, so collecting
/// them all means walking the string rather than making a single call.
/// </para>
/// <para>
/// Everything here is synchronous. UITextChecker is a local call and marshalling it to a background
/// thread would cost more than it saves — unlike Android, where the checker is a separate process.
/// </para>
/// </remarks>
public sealed class AppleSpellChecker : SpellCheckerBase
{
    readonly UITextChecker checker = new();

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
            // NSRange counts UTF-16 units, which is what a .NET string index already is, so offsets
            // carry straight across.
            var range = this.checker.RangeOfMisspelledWordInString(
                text,
                new NSRange(offset, text.Length - offset),
                offset,
                false,
                resolved);

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
        var guesses = this.checker.GuessesForWordRange(
            new NSRange(0, word.Length),
            word,
            Resolve(language));

        IReadOnlyList<string> result = guesses?.ToList() ?? [];
        return new ValueTask<IReadOnlyList<string>>(result);
    }

    public override void Learn(string word)
    {
        base.Learn(word);

        // Goes into the user's own dictionary, so it survives this session and is shared with every
        // other app on the device — which is the point of using the platform checker at all.
        UITextChecker.LearnWord(word);
    }

    /// <summary>
    /// Converts a BCP-47 tag to the POSIX form UITextChecker expects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CultureInfo.Name</c> gives <c>en-US</c>; UITextChecker's languages are <c>en_US</c>. Handed
    /// the wrong form it does not fail — it finds no dictionary and reports no misspellings, which
    /// looks exactly like text with nothing wrong in it.
    /// </para>
    /// <para>
    /// The available list is deliberately not consulted: the .NET for iOS binding exposes
    /// <c>+availableLanguages</c> as <c>UITextChecker.AvailableLangauges</c> — misspelled, and typed
    /// <c>string</c> where the native API returns an array — so it cannot be used to check membership.
    /// An app targeting a language the device lacks should set <see cref="SpellCheckerBase.DefaultLanguage"/>
    /// itself.
    /// </para>
    /// </remarks>
    static string Resolve(string language)
        => string.IsNullOrWhiteSpace(language) ? "en_US" : language.Replace('-', '_');
}
