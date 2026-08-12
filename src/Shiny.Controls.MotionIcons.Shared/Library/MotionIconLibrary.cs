using System.Collections.Concurrent;

namespace Shiny.Controls.MotionIcons;

/// <summary>
/// The named icons available to both hosts, and the place to add your own.
/// </summary>
/// <remarks>
/// Lookup is case-insensitive and misses return null rather than throwing, because the name
/// usually arrives from XAML or a Razor parameter — a typo should leave a hole in the layout you
/// can see, not take the page down.
/// </remarks>
public static class MotionIconLibrary
{
    static readonly ConcurrentDictionary<string, MotionIconDefinition> Icons =
        new(StringComparer.OrdinalIgnoreCase);

    static MotionIconLibrary()
    {
        foreach (var icon in BuiltInIcons.All())
            Icons[icon.Name] = icon;
    }

    /// <summary>Every registered icon name, in no particular order.</summary>
    public static IReadOnlyCollection<string> Names => (IReadOnlyCollection<string>)Icons.Keys;

    /// <summary>Every registered icon.</summary>
    public static IEnumerable<MotionIconDefinition> All => Icons.Values;

    /// <summary>Looks an icon up, returning null if there is no such name.</summary>
    public static MotionIconDefinition? Find(string? name)
        => string.IsNullOrWhiteSpace(name) ? null : Icons.GetValueOrDefault(name);

    /// <summary>Looks an icon up, throwing if there is no such name.</summary>
    public static MotionIconDefinition Get(string name)
        => Find(name) ?? throw new KeyNotFoundException($"No motion icon is registered under '{name}'.");

    /// <summary>Looks an icon up.</summary>
    public static bool TryGet(string? name, out MotionIconDefinition icon)
    {
        icon = Find(name)!;
        return icon is not null;
    }

    /// <summary>
    /// Adds an icon, or replaces one of the built-ins. Replacing is supported deliberately: an app
    /// with its own visual language can swap the artwork for <c>check</c> once at startup rather
    /// than passing a definition in at every call site.
    /// </summary>
    public static void Register(MotionIconDefinition icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        Icons[icon.Name] = icon;
    }

    /// <summary>Removes an icon.</summary>
    public static bool Unregister(string name) => Icons.TryRemove(name, out _);

    /// <summary>
    /// Wraps raw SVG path data into a one-part definition, for artwork that does not need naming
    /// or splitting up.
    /// </summary>
    /// <param name="pathData">SVG path data.</param>
    /// <param name="name">Optional name. Not registered — pass the result around directly.</param>
    /// <param name="viewBox">The coordinate space the path is drawn in.</param>
    public static MotionIconDefinition FromPath(string pathData, string? name = null, float viewBox = 24f)
        => new(name ?? "custom", [new MotionIconPart("path", pathData)], viewBox: viewBox);
}
