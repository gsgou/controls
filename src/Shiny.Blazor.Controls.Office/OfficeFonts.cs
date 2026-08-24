using Microsoft.JSInterop;
using Shiny.Controls.Office.Skia;

namespace Shiny.Blazor.Controls.Office;

/// <summary>
/// Loads the bundled metric-compatible fonts into <see cref="OfficeFontRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// SkiaSharp on WebAssembly has no access to system fonts, so without this every document renders in
/// one embedded fallback face regardless of what it asks for. The bundled Carlito and Caladea are
/// metric-compatible with Calibri and Cambria, meaning their glyph advances match — a document laid
/// out against Calibri breaks its lines in the same places.
/// </para>
/// <para>
/// Loading happens once per browser session, lazily, the first time an Office view is shown, and the
/// files are HTTP-cached after that. The views call this themselves; an application only needs to
/// touch it to add fonts of its own.
/// </para>
/// </remarks>
public static class OfficeFonts
{
    const string BasePath = "_content/Shiny.Blazor.Controls.Office/fonts/";

    static readonly string[] Bundled =
    [
        "Carlito-Regular.ttf",
        "Carlito-Bold.ttf",
        "Carlito-Italic.ttf",
        "Carlito-BoldItalic.ttf",
        "Caladea-Regular.ttf",
        "Caladea-Bold.ttf",
        "Caladea-Italic.ttf",
        "Caladea-BoldItalic.ttf"
    ];

    static Task<bool>? loading;

    /// <summary>True once the bundled fonts have been registered.</summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>
    /// Loads the bundled fonts if they are not already loaded. Safe to call from every view on every
    /// render: concurrent callers share one download, and later calls return immediately.
    /// </summary>
    /// <returns>True when the fonts are available, false when they could not be fetched.</returns>
    public static Task<bool> EnsureLoadedAsync(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);
        return loading ??= LoadAsync(js);
    }

    static async Task<bool> LoadAsync(IJSRuntime js)
    {
        IJSObjectReference? module = null;
        try
        {
            module = await js.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Shiny.Blazor.Controls.Office/officeFonts.js");

            var registered = 0;
            foreach (var file in Bundled)
            {
                try
                {
                    var bytes = await module.InvokeAsync<byte[]>("fetchFont", BasePath + file);
                    if (OfficeFontRegistry.Default.Register(bytes) is not null)
                        registered++;
                }
                catch (Exception)
                {
                    // One missing face is survivable - the registry falls back within the family, and
                    // a missing italic is better than no document.
                }
            }

            IsLoaded = registered > 0;
            return IsLoaded;
        }
        catch (Exception)
        {
            // Prerendering, a host without JS interop, or the assets not being served. The viewers
            // still work; they just fall back to whatever fonts the platform has.
            return false;
        }
        finally
        {
            if (module is not null)
            {
                try
                {
                    await module.DisposeAsync();
                }
                catch (Exception)
                {
                    // Disposing the module is best-effort; the circuit may already be gone.
                }
            }
        }
    }

    /// <summary>Registers an additional font from its bytes, for a face the bundle does not include.</summary>
    public static string? Register(byte[] font) => OfficeFontRegistry.Default.Register(font);
}
