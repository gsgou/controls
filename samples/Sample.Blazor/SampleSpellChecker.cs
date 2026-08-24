using Shiny.Controls.Office.Spelling;

namespace Sample.Blazor;

/// <summary>
/// A small spell checker for the web sample.
/// </summary>
/// <remarks>
/// <para>
/// The MAUI package registers the platform's own checker — UITextChecker, NSSpellChecker, Android's
/// text services, the Windows COM spell checker — so an app there gets this for free. The browser has
/// no equivalent: it spell-checks its own editable elements and exposes neither the results nor the
/// suggestions to script, and a canvas is not an editable element in the first place.
/// </para>
/// <para>
/// So on Blazor a checker has to be supplied, which is precisely what <c>ISpellChecker</c> is for.
/// This one carries a list of common misspellings rather than a real dictionary: a dictionary big
/// enough not to flag ordinary prose would dwarf the sample, and inverting the problem gives an
/// honest demonstration of the same plumbing. A real app would ship a Hunspell dictionary or call a
/// service.
/// </para>
/// </remarks>
public sealed class SampleSpellChecker : SpellCheckerBase
{
    static readonly Dictionary<string, string[]> Corrections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["teh"] = ["the", "ten"],
        ["recieve"] = ["receive"],
        ["seperate"] = ["separate"],
        ["occured"] = ["occurred"],
        ["definately"] = ["definitely"],
        ["neccessary"] = ["necessary"],
        ["accomodate"] = ["accommodate"],
        ["publically"] = ["publicly"],
        ["existance"] = ["existence"],
        ["reccomend"] = ["recommend", "recommended"],
        ["untill"] = ["until"],
        ["wich"] = ["which", "witch"]
    };

    public override bool IsAvailable => true;

    protected override ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SpellingError> found = SpellingTokenizer
            .Words(text)
            .Select(x => new SpellingError(x.Start, x.Length, text.Substring(x.Start, x.Length)))
            .Where(x => Corrections.ContainsKey(x.Word))
            .ToList();

        return new ValueTask<IReadOnlyList<SpellingError>>(found);
    }

    protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> suggestions = Corrections.TryGetValue(word, out var found)
            // Match the case of what was typed, so correcting a word that starts a sentence does not
            // quietly lower-case it.
            ? found.Select(x => char.IsUpper(word[0]) ? char.ToUpper(x[0]) + x[1..] : x).ToList()
            : [];

        return new ValueTask<IReadOnlyList<string>>(suggestions);
    }
}
