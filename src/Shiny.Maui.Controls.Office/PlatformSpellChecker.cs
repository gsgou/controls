using System.Runtime.CompilerServices;
using Shiny.Controls.Office.Spelling;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// The platform's own spell checker, registered as the default for every Office editor in the app.
/// </summary>
/// <remarks>
/// <para>
/// This is a lazy proxy rather than the checker itself. Registration happens in a module initializer
/// — before <c>MauiProgram</c> has run, let alone before there is an
/// <c>Android.App.Application.Context</c> or an initialized UIKit — so the real checker cannot be
/// constructed at that point. Deferring to first use puts construction inside an editor's own
/// lifetime, which is always late enough.
/// </para>
/// <para>
/// An app that sets <see cref="SpellCheckers.Default"/> itself wins: registration goes through
/// <see cref="SpellCheckers.SetDefaultIfUnset"/>, and the platform checker is never even constructed
/// in that case.
/// </para>
/// </remarks>
public sealed class PlatformSpellChecker : ISpellChecker
{
    /// <summary>True on platforms with a built-in checker — everywhere but plain .NET and the web.</summary>
    public static bool IsSupported =>
#if IOS || MACCATALYST || MACOS || ANDROID || WINDOWS
        true;
#else
        false;
#endif

    /// <summary>
    /// Registers the platform checker as the default, unless the app has already chosen one.
    /// </summary>
    /// <remarks>
    /// A module initializer, so an app gets spell checking simply by referencing the package. It runs
    /// once, on first touch of any type in this assembly, which for this package means the first
    /// Office control being constructed.
    /// </remarks>
    // CA2255 warns off module initializers in libraries because they run before the consumer can
    // intervene. That is exactly the point here: registration is a single TryAdd-style call that an
    // app overrides simply by assigning SpellCheckers.Default, and the alternative - a UseShinyOffice
    // call - would make spell checking opt-in on every platform that already has a checker.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255:The ModuleInitializer attribute should not be used in libraries", Justification = "Registration is override-safe: SetDefaultIfUnset never replaces an application's own choice.")]
    [ModuleInitializer]
    internal static void Register()
    {
        if (IsSupported)
            SpellCheckers.SetDefaultIfUnset(new PlatformSpellChecker());
    }

    ISpellChecker? inner;

    ISpellChecker Inner => this.inner ??= Create();

    static ISpellChecker Create()
    {
        try
        {
#if IOS || MACCATALYST
            return new AppleSpellChecker();
#elif MACOS
            return new AppKitSpellChecker();
#elif ANDROID
            return new AndroidSpellChecker();
#elif WINDOWS
            return new WindowsSpellChecker();
#else
            return NullSpellChecker.Instance;
#endif
        }
        catch (Exception)
        {
            // A checker that will not construct must not take the editor down with it: the document
            // still has to open, just without squiggles.
            return NullSpellChecker.Instance;
        }
    }

    public bool IsAvailable => this.Inner.IsAvailable;

    public string DefaultLanguage => this.Inner.DefaultLanguage;

    public ValueTask<IReadOnlyList<SpellingError>> CheckAsync(string text, string? language = null, CancellationToken cancellationToken = default)
        => this.Inner.CheckAsync(text, language, cancellationToken);

    public ValueTask<IReadOnlyList<string>> SuggestAsync(string word, string? language = null, CancellationToken cancellationToken = default)
        => this.Inner.SuggestAsync(word, language, cancellationToken);

    public void Ignore(string word) => this.Inner.Ignore(word);

    public void Learn(string word) => this.Inner.Learn(word);
}
