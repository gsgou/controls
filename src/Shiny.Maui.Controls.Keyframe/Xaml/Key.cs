using System.ComponentModel;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// One keyframe in a XAML-authored track.
/// </summary>
/// <remarks>
/// As in CSS, <see cref="Easing"/> shapes the segment that <i>starts</i> at this keyframe, so the
/// curve on the last key is never used. Leaving <see cref="Value"/> unset makes the keyframe
/// resolve to whatever the target's value is when playback begins.
/// </remarks>
public sealed class Key : BindableObject
{
    /// <summary>Backing store for <see cref="Offset"/>.</summary>
    public static readonly BindableProperty OffsetProperty =
        BindableProperty.Create(nameof(Offset), typeof(double), typeof(Key), 0d);

    /// <summary>Backing store for <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(object), typeof(Key));

    /// <summary>Backing store for <see cref="Easing"/>.</summary>
    public static readonly BindableProperty EasingProperty =
        BindableProperty.Create(nameof(Easing), typeof(EasingFunction), typeof(Key));

    /// <summary>Position within the iteration, 0 to 1.</summary>
    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    /// <summary>
    /// The value at this position. Leave unset to start from the target's current value.
    /// </summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Easing for the segment beginning at this keyframe.</summary>
    [TypeConverter(typeof(EasingFunctionTypeConverter))]
    public EasingFunction? Easing
    {
        get => (EasingFunction?)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    internal RawKey ToRawKey() => new(Offset, Value, Easing);
}
