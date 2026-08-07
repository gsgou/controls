namespace Shiny.Controls.Keyframe;

/// <summary>Collects keyframes for a single track.</summary>
/// <typeparam name="TValue">The property's value type.</typeparam>
public sealed class TrackBuilder<TValue>
{
    readonly List<Key<TValue>> keys = [];

    /// <summary>The keyframes gathered so far.</summary>
    public IReadOnlyList<Key<TValue>> Keys => keys;

    /// <summary>Adds a keyframe at a normalised offset.</summary>
    /// <param name="offset">Position within the iteration, 0 to 1.</param>
    /// <param name="value">The value at that position.</param>
    /// <param name="easing">Easing for the segment starting here.</param>
    public TrackBuilder<TValue> Key(double offset, TValue value, EasingFunction? easing = null)
    {
        keys.Add(new Key<TValue>(offset, value, easing));
        return this;
    }

    /// <summary>Adds a keyframe at offset 0.</summary>
    public TrackBuilder<TValue> From(TValue value, EasingFunction? easing = null) => Key(0d, value, easing);

    /// <summary>
    /// Starts from whatever the target's value is when playback begins, rather than a fixed value.
    /// This is what lets a re-triggered animation continue smoothly instead of snapping.
    /// </summary>
    public TrackBuilder<TValue> FromCurrent(EasingFunction? easing = null)
    {
        keys.Add(Key<TValue>.Current(0d, easing));
        return this;
    }

    /// <summary>Adds a keyframe at offset 1.</summary>
    public TrackBuilder<TValue> To(TValue value) => Key(1d, value);

    /// <summary>
    /// Spreads values evenly from offset 0 to 1. Handy for hand-drawn-looking motion where the
    /// exact offsets do not matter, only the shape.
    /// </summary>
    public TrackBuilder<TValue> Evenly(params TValue[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 1)
            return Key(0d, values[0]);

        for (var i = 0; i < values.Length; i++)
            Key((double)i / (values.Length - 1), values[i]);

        return this;
    }

    /// <summary>Replaces the easing on the most recently added keyframe.</summary>
    public TrackBuilder<TValue> Ease(EasingFunction easing)
    {
        if (keys.Count == 0)
            throw new InvalidOperationException("Add a keyframe before setting its easing.");

        keys[^1] = keys[^1].WithEasing(easing);
        return this;
    }
}
