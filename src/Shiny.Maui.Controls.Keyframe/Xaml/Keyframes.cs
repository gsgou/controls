using System.ComponentModel;
using System.Globalization;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// A XAML-authored keyframe animation — the direct analogue of a CSS <c>@keyframes</c> rule
/// together with the <c>animation-*</c> properties that drive it.
/// </summary>
[ContentProperty(nameof(Tracks))]
public sealed class Keyframes : BindableObject
{
    /// <summary>Backing store for <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty =
        BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(Keyframes),
            TimeSpan.FromMilliseconds(300));

    /// <summary>Backing store for <see cref="Delay"/>.</summary>
    public static readonly BindableProperty DelayProperty =
        BindableProperty.Create(nameof(Delay), typeof(TimeSpan), typeof(Keyframes), TimeSpan.Zero);

    /// <summary>Backing store for <see cref="Iterations"/>.</summary>
    public static readonly BindableProperty IterationsProperty =
        BindableProperty.Create(nameof(Iterations), typeof(double), typeof(Keyframes), 1d);

    /// <summary>Backing store for <see cref="Direction"/>.</summary>
    public static readonly BindableProperty DirectionProperty =
        BindableProperty.Create(nameof(Direction), typeof(PlaybackDirection), typeof(Keyframes),
            PlaybackDirection.Normal);

    /// <summary>Backing store for <see cref="Fill"/>.</summary>
    public static readonly BindableProperty FillProperty =
        BindableProperty.Create(nameof(Fill), typeof(FillMode), typeof(Keyframes), FillMode.None);

    /// <summary>Backing store for <see cref="Easing"/>.</summary>
    public static readonly BindableProperty EasingProperty =
        BindableProperty.Create(nameof(Easing), typeof(EasingFunction), typeof(Keyframes));

    /// <summary>Backing store for <see cref="AutoPlay"/>.</summary>
    public static readonly BindableProperty AutoPlayProperty =
        BindableProperty.Create(nameof(AutoPlay), typeof(bool), typeof(Keyframes), true);

    /// <summary>Backing store for <see cref="Speed"/>.</summary>
    public static readonly BindableProperty SpeedProperty =
        BindableProperty.Create(nameof(Speed), typeof(double), typeof(Keyframes), 1d);

    /// <summary>Length of a single iteration.</summary>
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Delay before the first iteration. Negative values start partway through.</summary>
    public TimeSpan Delay
    {
        get => (TimeSpan)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    /// <summary>
    /// How many times to repeat. Accepts <c>Infinite</c> in XAML as well as a number.
    /// </summary>
    [TypeConverter(typeof(IterationsTypeConverter))]
    public double Iterations
    {
        get => (double)GetValue(IterationsProperty);
        set => SetValue(IterationsProperty, value);
    }

    /// <summary>Which way each iteration runs.</summary>
    public PlaybackDirection Direction
    {
        get => (PlaybackDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>What happens to the target outside the active window.</summary>
    public FillMode Fill
    {
        get => (FillMode)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Easing applied across each whole iteration, on top of the per-segment curves.</summary>
    [TypeConverter(typeof(EasingFunctionTypeConverter))]
    public EasingFunction? Easing
    {
        get => (EasingFunction?)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>Whether the animation starts as soon as the element is loaded.</summary>
    public bool AutoPlay
    {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    /// <summary>Playback rate. 1 is real time; negative values run backwards.</summary>
    public double Speed
    {
        get => (double)GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>The animated properties.</summary>
    public IList<Track> Tracks { get; } = [];

    /// <summary>
    /// Builds a runnable timeline bound to a view. Called by the attached property when the
    /// element loads; also usable directly if you want to own playback yourself.
    /// </summary>
    /// <param name="target">The element the tracks drive by default.</param>
    /// <param name="resolveTarget">Resolves a <see cref="Track.TargetName"/> to an element.</param>
    public Timeline Build(VisualElement target, Func<string, VisualElement?>? resolveTarget = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (Tracks.Count == 0)
            throw new InvalidOperationException(
                "A Keyframes animation needs at least one Track.");

        var timing = new Timing
        {
            Duration = Duration,
            Delay = Delay,
            Iterations = Iterations,
            Direction = Direction,
            Fill = Fill,
            Easing = Easing ?? Easings.Linear
        };

        var timeline = new Timeline(timing);

        foreach (var track in Tracks)
        {
            var element = target;

            if (!string.IsNullOrWhiteSpace(track.TargetName))
            {
                element = resolveTarget?.Invoke(track.TargetName)
                    ?? throw new InvalidOperationException(
                        $"Could not find an element named '{track.TargetName}'. Check the x:Name, " +
                        "and note that the name must be in scope from the element the animation is attached to.");
            }

            timeline.Add(track.Build(element));
        }

        return timeline;
    }
}

/// <summary>Lets XAML write <c>Iterations="Infinite"</c> as well as a plain number.</summary>
public sealed class IterationsTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
            return base.ConvertFrom(context, culture, value);

        text = text.Trim();

        if (text.Equals("Infinite", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("Forever", StringComparison.OrdinalIgnoreCase))
            return double.PositiveInfinity;

        return double.Parse(text, CultureInfo.InvariantCulture);
    }
}
