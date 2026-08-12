namespace Shiny.Controls.MotionIcons;

/// <summary>
/// Turns the properties a caller sets on a control into the artwork and motion to play.
/// </summary>
/// <remarks>
/// Both hosts route through here rather than each working it out for themselves, so that
/// precedence — an explicit definition beating a name beating raw path data, a preset beating an
/// icon's own motion — is answered once and cannot drift between MAUI and Blazor.
/// </remarks>
public static class MotionResolver
{
    /// <summary>
    /// Works out which artwork to draw.
    /// </summary>
    /// <param name="source">An explicit definition. Wins over everything else.</param>
    /// <param name="name">A name to look up in <see cref="MotionIconLibrary"/>.</param>
    /// <param name="pathData">Raw SVG path data, wrapped into a one-part icon.</param>
    /// <returns>The artwork, or null if nothing was specified and nothing matched.</returns>
    public static MotionIconDefinition? ResolveIcon(
        MotionIconDefinition? source,
        string? name,
        string? pathData)
    {
        if (source is not null)
            return source;

        if (!string.IsNullOrWhiteSpace(name) && MotionIconLibrary.Find(name) is { } found)
            return found;

        if (!string.IsNullOrWhiteSpace(pathData))
            return MotionIconLibrary.FromPath(pathData);

        return null;
    }

    /// <summary>
    /// Works out what motion to play, applying the caller's retiming and resting interval.
    /// </summary>
    /// <param name="icon">The artwork.</param>
    /// <param name="preset">Which motion to use.</param>
    /// <param name="duration">Overrides the length of one cycle.</param>
    /// <param name="interval">Rests this long between cycles.</param>
    /// <returns>The motion, or null when there is nothing to play.</returns>
    public static MotionSpec? ResolveMotion(
        MotionIconDefinition? icon,
        MotionPreset preset,
        TimeSpan? duration = null,
        TimeSpan? interval = null)
    {
        if (icon is null)
            return null;

        var spec = MotionPresets.Build(preset, icon);

        if (spec is null || spec.IsEmpty)
            return null;

        if (duration is { } value && value > TimeSpan.Zero)
            spec = spec.WithDuration(value);

        if (interval is { } gap && gap > TimeSpan.Zero)
            spec = spec.WithInterval(gap);

        return spec;
    }
}
