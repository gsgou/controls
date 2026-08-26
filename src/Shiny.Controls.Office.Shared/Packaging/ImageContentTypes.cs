namespace Shiny.Controls.Office.Packaging;

/// <summary>
/// Works out whether a dropped file is an image the editors can embed, and what to call it.
/// </summary>
/// <remarks>
/// <para>
/// The content type matters more than it looks: it is what decides the part's extension inside the
/// package, and a PNG stored as <c>/media/image1.jpeg</c> is a file Word and PowerPoint both refuse to
/// render even though the bytes are perfectly good.
/// </para>
/// <para>
/// The browser supplies a MIME type on a drop and the desktop usually does not, so both routes are
/// covered: the stated type is trusted when it is one of the formats OOXML allows, and otherwise the
/// extension decides. Sniffing the bytes would be more robust still, but the failure it guards
/// against — a file whose name and MIME type both lie — is one where the user has already been misled
/// by their own file manager.
/// </para>
/// </remarks>
public static class ImageContentTypes
{
    /// <summary>
    /// The formats an editor will embed.
    /// </summary>
    /// <remarks>
    /// Deliberately not SVG. Both formats can hold one, but only as a companion to a rasterised
    /// fallback that Office generates when it inserts one — embedding the vector alone gives a
    /// picture that is blank everywhere except recent Office, which is worse than declining the drop.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> ByExtension { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".webp"] = "image/webp"
        };

    /// <summary>
    /// The content type to store a file under, or null when it is not an image worth embedding.
    /// </summary>
    /// <param name="fileName">The file's name, used for its extension. May be null.</param>
    /// <param name="statedType">The MIME type the source claimed, if any.</param>
    public static string? Resolve(string? fileName, string? statedType = null)
    {
        if (!string.IsNullOrWhiteSpace(statedType))
        {
            foreach (var known in ByExtension.Values)
            {
                if (known.Equals(statedType, StringComparison.OrdinalIgnoreCase))
                    return known;
            }
        }

        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var extension = Path.GetExtension(fileName);

        return extension.Length > 0 && ByExtension.TryGetValue(extension, out var resolved)
            ? resolved
            : null;
    }

    /// <summary>True when a file looks like an image the editors can embed.</summary>
    public static bool IsSupported(string? fileName, string? statedType = null)
        => Resolve(fileName, statedType) is not null;
}
