using SkiaSharp;

namespace Shiny.Controls.Office.Skia;

/// <summary>
/// Typefaces supplied by the application, consulted before the platform's font manager.
/// </summary>
/// <remarks>
/// <para>
/// This exists because SkiaSharp on WebAssembly has **no system fonts at all** — no fontconfig, no
/// CoreText — so every <c>SKTypeface.FromFamilyName</c> returns the same embedded fallback. It never
/// returns null, which is what makes the failure silent: substitution appears to work while every
/// document renders in one wrong face.
/// </para>
/// <para>
/// Registering a face here also lets a desktop host supply a font the machine does not have, which is
/// the same problem in a milder form.
/// </para>
/// </remarks>
public sealed class OfficeFontRegistry
{
    readonly Dictionary<FaceKey, SKTypeface> faces = new();
    readonly HashSet<string> families = new(StringComparer.OrdinalIgnoreCase);

    readonly record struct FaceKey(string Family, bool Bold, bool Italic);

    /// <summary>The registry the painters and measurers consult by default.</summary>
    public static OfficeFontRegistry Default { get; } = new();

    public int Count => this.faces.Count;

    /// <summary>Families registered here, whatever their weight or slant.</summary>
    public IReadOnlyCollection<string> Families => this.families;

    /// <summary>
    /// Registers a font from its bytes. The family name and style are taken from the font itself, so
    /// the caller does not have to describe what it is handing over.
    /// </summary>
    /// <returns>The family name that was registered, or null when the data is not a usable font.</returns>
    public string? Register(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data, writable: false);
        return this.Register(stream);
    }

    public string? Register(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);

        SKTypeface? typeface;
        try
        {
            typeface = SKTypeface.FromStream(data);
        }
        catch (Exception)
        {
            return null;
        }

        if (typeface is null)
            return null;

        var family = typeface.FamilyName;
        if (string.IsNullOrEmpty(family))
        {
            typeface.Dispose();
            return null;
        }

        var key = new FaceKey(family, typeface.IsBold, typeface.IsItalic);

        // Re-registering the same face replaces it rather than leaking the old one.
        if (this.faces.Remove(key, out var previous))
            previous.Dispose();

        this.faces[key] = typeface;
        this.families.Add(family);
        return family;
    }

    /// <summary>
    /// The closest registered face for a family and style, or null when the family is not registered.
    /// </summary>
    /// <remarks>
    /// Falls back within the family before giving up: a bold-italic request will take bold, then
    /// italic, then regular. Skia can synthesise the missing axis, and a real face in the right family
    /// beats a perfect style match in the wrong one.
    /// </remarks>
    public SKTypeface? Find(string family, bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(family) || !this.families.Contains(family))
            return null;

        foreach (var candidate in Candidates(bold, italic))
        {
            if (this.faces.TryGetValue(new FaceKey(family, candidate.Bold, candidate.Italic), out var typeface))
                return typeface;
        }

        return null;
    }

    static IEnumerable<(bool Bold, bool Italic)> Candidates(bool bold, bool italic)
    {
        yield return (bold, italic);

        if (bold && italic)
        {
            yield return (true, false);
            yield return (false, true);
        }

        yield return (false, false);
    }

    public bool Contains(string family) => this.families.Contains(family);

    public void Clear()
    {
        foreach (var typeface in this.faces.Values)
            typeface.Dispose();

        this.faces.Clear();
        this.families.Clear();
    }
}
