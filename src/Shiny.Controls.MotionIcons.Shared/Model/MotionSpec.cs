namespace Shiny.Controls.MotionIcons;

/// <summary>
/// A complete piece of icon motion: how long one cycle lasts, and what moves during it.
/// </summary>
/// <remarks>
/// Offsets are normalised 0..1 rather than absolute times, so retiming a whole animation — slowing
/// a hover on desktop, shortening a tap on mobile — is a single field change and never touches the
/// keys.
/// </remarks>
public sealed record MotionSpec
{
    /// <summary>Creates a spec.</summary>
    /// <param name="duration">Length of one cycle.</param>
    /// <param name="tracks">The numeric tracks.</param>
    /// <param name="colorTracks">The colour tracks, if any.</param>
    public MotionSpec(
        TimeSpan duration,
        IReadOnlyList<MotionTrack> tracks,
        IReadOnlyList<MotionColorTrack>? colorTracks = null)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A motion spec needs a positive duration.");

        Duration = duration;
        Tracks = tracks;
        ColorTracks = colorTracks ?? [];
    }

    /// <summary>Length of one cycle.</summary>
    public TimeSpan Duration { get; }

    /// <summary>The numeric tracks.</summary>
    public IReadOnlyList<MotionTrack> Tracks { get; }

    /// <summary>The colour tracks.</summary>
    public IReadOnlyList<MotionColorTrack> ColorTracks { get; }

    /// <summary>
    /// Pivot for tracks that drive the icon as a whole, in viewBox units. Null pivots about the
    /// centre — right for a spin, wrong for anything that hangs, which is why a swing can move it
    /// to the top edge.
    /// </summary>
    public MotionPoint? RootOrigin { get; init; }

    /// <summary>Whether this spec drives anything at all.</summary>
    public bool IsEmpty => Tracks.Count == 0 && ColorTracks.Count == 0;

    /// <summary>Retimes the whole animation, leaving the keys alone.</summary>
    public MotionSpec WithDuration(TimeSpan duration)
        => new(duration, Tracks, ColorTracks) { RootOrigin = RootOrigin };

    /// <summary>
    /// Pads a resting gap onto the end of the cycle, so a looping icon plays, waits, and plays
    /// again rather than running continuously.
    /// </summary>
    /// <param name="interval">How long to rest between cycles.</param>
    /// <remarks>
    /// The gap is folded into the animation itself rather than being handled by a timer. That is
    /// what keeps the two hosts honest: a CSS animation has no way to pause between iterations, so
    /// a spec that expressed the gap externally would need a JavaScript timer on the web and a
    /// dispatcher timer on MAUI, and the two would drift. Squeezing the keys into the front of a
    /// longer cycle and holding the final pose through the remainder produces the same result with
    /// nothing but <c>animation-iteration-count: infinite</c>.
    /// </remarks>
    public MotionSpec WithInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            return this;

        var total = Duration + interval;
        var scale = Duration.TotalMilliseconds / total.TotalMilliseconds;

        var tracks = new List<MotionTrack>(Tracks.Count);

        foreach (var track in Tracks)
        {
            var keys = new List<MotionKey>(track.Keys.Count + 1);

            foreach (var key in track.Keys)
                keys.Add(key with { Offset = key.Offset * scale });

            // Hold the resting pose across the gap. Without this the browser would interpolate
            // straight from the last key back to the first over the whole gap, which is the exact
            // opposite of resting.
            if (keys.Count > 0)
                keys.Add(new MotionKey(1d, keys[^1].Value, MotionEase.Linear));

            tracks.Add(track with { Keys = keys });
        }

        var colorTracks = new List<MotionColorTrack>(ColorTracks.Count);

        foreach (var track in ColorTracks)
        {
            var keys = new List<MotionColorKey>(track.Keys.Count + 1);

            foreach (var key in track.Keys)
                keys.Add(key with { Offset = key.Offset * scale });

            if (keys.Count > 0)
                keys.Add(new MotionColorKey(1d, keys[^1].Color, MotionEase.Linear));

            colorTracks.Add(track with { Keys = keys });
        }

        return new MotionSpec(total, tracks, colorTracks);
    }
}
