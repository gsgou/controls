using System.Runtime.InteropServices;
using Shiny.Controls.Office.Spelling;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Spell checking through the Windows <c>ISpellChecker</c> COM API (Windows 8 and later).
/// </summary>
/// <remarks>
/// <para>
/// There is no .NET projection of this API, so the interfaces are declared below. It is the same
/// engine Word and Edge use, including the words the user has added themselves.
/// </para>
/// <para>
/// The COM object is created lazily and reused, so every call stays on the thread that created it.
/// Anything that goes wrong — no dictionary, a refused activation — leaves
/// <see cref="IsAvailable"/> false rather than throwing at the editor.
/// </para>
/// </remarks>
public sealed class WindowsSpellChecker : SpellCheckerBase, IDisposable
{
    INativeSpellChecker? native;
    bool attempted;

    public override bool IsAvailable => this.Ensure() is not null;

    INativeSpellChecker? Ensure()
    {
        if (this.attempted)
            return this.native;

        this.attempted = true;

        try
        {
            var factory = (INativeSpellCheckerFactory)new SpellCheckerFactory();
            var language = this.DefaultLanguage;

            // A language with no installed dictionary throws from CreateSpellChecker rather than
            // returning null, so it is asked about first.
            if (factory.IsSupported(language) == 0)
                language = "en-US";

            this.native = factory.CreateSpellChecker(language);
        }
        catch (Exception ex)
        {
            // Deliberately broad: an unregistered class, a refused activation and a Windows build
            // without the spelling engine all surface differently, and every one of them means the
            // same thing here — no squiggles, editor unaffected.
            System.Diagnostics.Debug.WriteLine($"[Shiny.Office] Windows spell checker unavailable: {ex}");
            this.native = null;
        }

        return this.native;
    }

    protected override ValueTask<IReadOnlyList<SpellingError>> CheckCoreAsync(
        string text,
        string language,
        CancellationToken cancellationToken)
    {
        var instance = this.Ensure();
        if (instance is null)
            return new ValueTask<IReadOnlyList<SpellingError>>([]);

        var errors = new List<SpellingError>();

        try
        {
            var enumerator = instance.Check(text);

            // Next returns S_FALSE with a null error at the end of the enumeration, which surfaces
            // here as null rather than as an exception.
            while (enumerator.Next() is { } error)
            {
                var start = (int)error.StartIndex;
                var length = (int)error.Length;
                if (length <= 0 || start < 0 || start + length > text.Length)
                    continue;

                errors.Add(new SpellingError(start, length, text.Substring(start, length)));
            }
        }
        catch (COMException)
        {
            // A failed check reports nothing rather than taking the document down with it.
        }

        return new ValueTask<IReadOnlyList<SpellingError>>(errors);
    }

    protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
        string word,
        string language,
        CancellationToken cancellationToken)
    {
        var instance = this.Ensure();
        if (instance is null)
            return new ValueTask<IReadOnlyList<string>>([]);

        var result = new List<string>();

        try
        {
            var suggestions = instance.Suggest(word);

            // IEnumString hands back COM-allocated strings and the caller owns them, so each one is
            // copied and then freed. Marshalling straight to `out string` would read the text and
            // silently leak the allocation on every suggestion shown.
            while (suggestions.Next(1, out var candidate, out var fetched) == 0 && fetched == 1)
            {
                if (candidate == IntPtr.Zero)
                    continue;

                var text = Marshal.PtrToStringUni(candidate);
                Marshal.FreeCoTaskMem(candidate);

                if (!string.IsNullOrEmpty(text))
                    result.Add(text);
            }
        }
        catch (COMException)
        {
        }

        return new ValueTask<IReadOnlyList<string>>(result);
    }

    public override void Ignore(string word)
    {
        base.Ignore(word);

        try
        {
            this.Ensure()?.Ignore(word);
        }
        catch (COMException)
        {
        }
    }

    public override void Learn(string word)
    {
        base.Learn(word);

        try
        {
            // Adds to the user's dictionary, so it survives the session and is shared with Word.
            this.Ensure()?.Add(word);
        }
        catch (COMException)
        {
        }
    }

    public void Dispose()
    {
        // Deliberately not Marshal.ReleaseComObject: it throws wherever built-in COM interop is
        // disabled, which includes NativeAOT - and this package is IsAotCompatible. Dropping the
        // reference lets the RCW release it on collection instead.
        this.native = null;
        this.attempted = false;
    }

    // ---- COM declarations, transcribed from spellcheck.h ----
    //
    // Method order is the vtable, so nothing here may be reordered or omitted from the middle. Methods
    // this class never calls are still declared, for exactly that reason.

    [ComImport, Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
    class SpellCheckerFactory
    {
    }

    [ComImport, Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INativeSpellCheckerFactory
    {
        IEnumString GetSupportedLanguages();

        int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag);

        INativeSpellChecker CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
    }

    [ComImport, Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INativeSpellChecker
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguageTag();

        IEnumSpellingError Check([MarshalAs(UnmanagedType.LPWStr)] string text);

        IEnumString Suggest([MarshalAs(UnmanagedType.LPWStr)] string word);

        void Add([MarshalAs(UnmanagedType.LPWStr)] string word);

        void Ignore([MarshalAs(UnmanagedType.LPWStr)] string word);

        void AutoCorrect([MarshalAs(UnmanagedType.LPWStr)] string from, [MarshalAs(UnmanagedType.LPWStr)] string to);
    }

    [ComImport, Guid("803E3BD4-2828-4410-8290-418D1D73C762"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumSpellingError
    {
        ISpellingError? Next();
    }

    [ComImport, Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISpellingError
    {
        uint StartIndex { get; }

        uint Length { get; }

        uint CorrectiveAction { get; }

        // LPWStr, not the BSTR that string marshals to by default: the callee allocates with
        // CoTaskMemAlloc, and freeing it as a BSTR corrupts the heap.
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetReplacement();
    }

    [ComImport, Guid("00000101-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumString
    {
        [PreserveSig]
        int Next(int count, out IntPtr element, out int fetched);
    }
}
