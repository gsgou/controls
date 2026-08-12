namespace Shiny.Controls.MotionIcons;

/// <summary>Collects the keyframes of one numeric track.</summary>
public sealed class MotionKeyBuilder
{
    readonly List<MotionKey> keys = [];

    internal IReadOnlyList<MotionKey> Keys => keys;

    /// <summary>Adds a keyframe.</summary>
    /// <param name="offset">Position within the cycle, 0 to 1.</param>
    /// <param name="value">The value at that position.</param>
    /// <param name="ease">Curve for the segment starting here.</param>
    public MotionKeyBuilder At(double offset, double value, MotionEase ease = MotionEase.Ease)
    {
        keys.Add(new MotionKey(offset, value, ease));
        return this;
    }

    /// <summary>Spreads values evenly across the cycle.</summary>
    public MotionKeyBuilder Evenly(MotionEase ease, params double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 1)
            return At(0d, values[0], ease);

        for (var i = 0; i < values.Length; i++)
            At((double)i / (values.Length - 1), values[i], ease);

        return this;
    }
}

/// <summary>Collects the keyframes of one colour track.</summary>
public sealed class MotionColorKeyBuilder
{
    readonly List<MotionColorKey> keys = [];

    internal IReadOnlyList<MotionColorKey> Keys => keys;

    /// <summary>Adds a keyframe.</summary>
    /// <param name="offset">Position within the cycle, 0 to 1.</param>
    /// <param name="color">The colour at that position. Null means the host's icon colour.</param>
    /// <param name="ease">Curve for the segment starting here.</param>
    public MotionColorKeyBuilder At(double offset, string? color, MotionEase ease = MotionEase.Ease)
    {
        keys.Add(new MotionColorKey(offset, color, ease));
        return this;
    }
}

/// <summary>
/// Fluent construction of a <see cref="MotionSpec"/>.
/// </summary>
/// <remarks>
/// Tracks are normalised as they are added — sorted, and padded with an implicit key at 0 and 1
/// where the author left one off. Both compilers can then assume a track spans the whole cycle,
/// which removes a pile of edge cases from code that runs per frame or generates CSS.
/// </remarks>
public sealed class MotionSpecBuilder
{
    readonly List<MotionTrack> tracks = [];
    readonly List<MotionColorTrack> colorTracks = [];
    readonly TimeSpan duration;

    /// <summary>Starts a builder.</summary>
    /// <param name="duration">Length of one cycle.</param>
    public MotionSpecBuilder(TimeSpan duration) => this.duration = duration;

    /// <summary>Starts a builder with the duration given in milliseconds.</summary>
    public MotionSpecBuilder(double milliseconds) : this(TimeSpan.FromMilliseconds(milliseconds)) { }

    /// <summary>Builds a spec in one expression.</summary>
    /// <param name="milliseconds">Length of one cycle.</param>
    /// <param name="build">Adds the tracks.</param>
    public static MotionSpec Build(double milliseconds, Action<MotionSpecBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new MotionSpecBuilder(milliseconds);
        build(builder);

        return builder.Build();
    }

    /// <summary>Adds a track for any channel.</summary>
    /// <param name="partId">The part to drive. Null drives the icon as a whole.</param>
    /// <param name="channel">The property being driven.</param>
    /// <param name="keys">Adds the keyframes.</param>
    public MotionSpecBuilder Track(string? partId, MotionChannel channel, Action<MotionKeyBuilder> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var builder = new MotionKeyBuilder();
        keys(builder);

        var normalised = Normalise(builder.Keys);

        if (normalised.Count > 0)
            tracks.Add(new MotionTrack(partId, channel, normalised));

        return this;
    }

    /// <summary>Rotates a part, in degrees about its origin.</summary>
    public MotionSpecBuilder Rotate(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.Rotate, keys);

    /// <summary>Rotates the whole icon.</summary>
    public MotionSpecBuilder Rotate(Action<MotionKeyBuilder> keys) => Rotate(null, keys);

    /// <summary>Scales a part uniformly about its origin.</summary>
    public MotionSpecBuilder Scale(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.Scale, keys);

    /// <summary>Scales the whole icon.</summary>
    public MotionSpecBuilder Scale(Action<MotionKeyBuilder> keys) => Scale(null, keys);

    /// <summary>Scales a part horizontally.</summary>
    public MotionSpecBuilder ScaleX(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.ScaleX, keys);

    /// <summary>Scales a part vertically.</summary>
    public MotionSpecBuilder ScaleY(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.ScaleY, keys);

    /// <summary>Fades a part.</summary>
    public MotionSpecBuilder Opacity(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.Opacity, keys);

    /// <summary>Fades the whole icon.</summary>
    public MotionSpecBuilder Opacity(Action<MotionKeyBuilder> keys) => Opacity(null, keys);

    /// <summary>Moves a part horizontally, in viewBox units.</summary>
    public MotionSpecBuilder MoveX(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.TranslateX, keys);

    /// <summary>Moves a part vertically, in viewBox units.</summary>
    public MotionSpecBuilder MoveY(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.TranslateY, keys);

    /// <summary>Scales a part's stroke width relative to the host's.</summary>
    public MotionSpecBuilder StrokeWidth(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.StrokeWidth, keys);

    /// <summary>Draws a part's path on or off, 0 to 1.</summary>
    public MotionSpecBuilder Trim(string? partId, Action<MotionKeyBuilder> keys)
        => Track(partId, MotionChannel.Trim, keys);

    /// <summary>Adds a colour track.</summary>
    /// <param name="partId">The part to drive. Null drives every part.</param>
    /// <param name="channel">Fill or stroke.</param>
    /// <param name="keys">Adds the keyframes.</param>
    public MotionSpecBuilder Paint(string? partId, MotionPaintChannel channel, Action<MotionColorKeyBuilder> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var builder = new MotionColorKeyBuilder();
        keys(builder);

        var normalised = NormaliseColors(builder.Keys);

        if (normalised.Count > 0)
            colorTracks.Add(new MotionColorTrack(partId, channel, normalised));

        return this;
    }

    /// <summary>Animates a part's fill colour.</summary>
    public MotionSpecBuilder Fill(string? partId, Action<MotionColorKeyBuilder> keys)
        => Paint(partId, MotionPaintChannel.Fill, keys);

    /// <summary>Animates a part's stroke colour.</summary>
    public MotionSpecBuilder Stroke(string? partId, Action<MotionColorKeyBuilder> keys)
        => Paint(partId, MotionPaintChannel.Stroke, keys);

    /// <summary>Produces the spec.</summary>
    public MotionSpec Build() => new(duration, tracks, colorTracks);

    static List<MotionKey> Normalise(IReadOnlyList<MotionKey> source)
    {
        if (source.Count == 0)
            return [];

        var keys = new List<MotionKey>(source.Count + 2);
        keys.AddRange(source);
        keys.Sort(static (a, b) => a.Offset.CompareTo(b.Offset));

        if (keys[0].Offset > 0d)
            keys.Insert(0, keys[0] with { Offset = 0d });

        if (keys[^1].Offset < 1d)
            keys.Add(keys[^1] with { Offset = 1d });

        return keys;
    }

    static List<MotionColorKey> NormaliseColors(IReadOnlyList<MotionColorKey> source)
    {
        if (source.Count == 0)
            return [];

        var keys = new List<MotionColorKey>(source.Count + 2);
        keys.AddRange(source);
        keys.Sort(static (a, b) => a.Offset.CompareTo(b.Offset));

        if (keys[0].Offset > 0d)
            keys.Insert(0, keys[0] with { Offset = 0d });

        if (keys[^1].Offset < 1d)
            keys.Add(keys[^1] with { Offset = 1d });

        return keys;
    }
}
