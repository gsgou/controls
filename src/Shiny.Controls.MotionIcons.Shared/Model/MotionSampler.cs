namespace Shiny.Controls.MotionIcons;

/// <summary>
/// Reads the value of a track at an arbitrary point in the cycle.
/// </summary>
/// <remarks>
/// Shared rather than duplicated per host on purpose. MAUI calls this once per track per frame;
/// Blazor calls it while flattening tracks into <c>@keyframes</c>. If the two ever disagreed about
/// which segment an offset falls in, or about which key owns the easing, the same icon would move
/// differently in a MAUI app and a browser and it would be nearly impossible to see why.
/// </remarks>
public static class MotionSampler
{
    /// <summary>Evaluates a numeric track.</summary>
    /// <param name="keys">Keyframes in ascending offset order.</param>
    /// <param name="offset">Position within the cycle, 0 to 1.</param>
    public static double ValueAt(IReadOnlyList<MotionKey> keys, double offset)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0)
            return 0d;

        if (offset <= keys[0].Offset)
            return keys[0].Value;

        for (var i = 1; i < keys.Count; i++)
        {
            var to = keys[i];

            if (offset > to.Offset)
                continue;

            var from = keys[i - 1];
            var span = to.Offset - from.Offset;
            var local = span <= 0d ? 1d : (offset - from.Offset) / span;

            // The easing shapes the segment that *starts* at the earlier key, which is CSS's rule
            // and the reason the last key's easing is never used.
            var eased = MotionEasings.Evaluate(from.Ease, local);

            return from.Value + (to.Value - from.Value) * eased;
        }

        return keys[^1].Value;
    }

    /// <summary>
    /// Locates a colour track's position at an offset, without blending — the two hosts have
    /// different colour types, so they each do the final mix themselves.
    /// </summary>
    /// <param name="keys">Keyframes in ascending offset order.</param>
    /// <param name="offset">Position within the cycle, 0 to 1.</param>
    /// <returns>The colours either side and how far between them, 0 to 1. Null means "the host's icon colour".</returns>
    public static (string? From, string? To, double Progress) ColorAt(
        IReadOnlyList<MotionColorKey> keys, double offset)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0)
            return (null, null, 0d);

        if (offset <= keys[0].Offset)
            return (keys[0].Color, keys[0].Color, 0d);

        for (var i = 1; i < keys.Count; i++)
        {
            var to = keys[i];

            if (offset > to.Offset)
                continue;

            var from = keys[i - 1];
            var span = to.Offset - from.Offset;
            var local = span <= 0d ? 1d : (offset - from.Offset) / span;

            return (from.Color, to.Color, MotionEasings.Evaluate(from.Ease, local));
        }

        return (keys[^1].Color, keys[^1].Color, 1d);
    }

    /// <summary>Whether the track has a keyframe exactly at this offset.</summary>
    public static bool HasKeyAt(IReadOnlyList<MotionKey> keys, double offset)
    {
        foreach (var key in keys)
        {
            if (Math.Abs(key.Offset - offset) < Epsilon)
                return true;
        }

        return false;
    }

    /// <summary>
    /// The easing of the segment starting at this offset, or null when the track has no key there
    /// and is therefore mid-segment.
    /// </summary>
    public static MotionEase? EaseAt(IReadOnlyList<MotionKey> keys, double offset)
    {
        foreach (var key in keys)
        {
            if (Math.Abs(key.Offset - offset) < Epsilon)
                return key.Ease;
        }

        return null;
    }

    /// <summary>Offsets closer than this are the same keyframe.</summary>
    public const double Epsilon = 1e-6d;
}
