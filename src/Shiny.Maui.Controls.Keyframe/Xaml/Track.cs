namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// One animated property in a XAML-authored timeline.
/// </summary>
[ContentProperty(nameof(Keys))]
public sealed class Track : BindableObject
{
    /// <summary>Backing store for <see cref="Property"/>.</summary>
    public static readonly BindableProperty PropertyProperty =
        BindableProperty.Create(nameof(Property), typeof(string), typeof(Track));

    /// <summary>Backing store for <see cref="TargetName"/>.</summary>
    public static readonly BindableProperty TargetNameProperty =
        BindableProperty.Create(nameof(TargetName), typeof(string), typeof(Track));

    /// <summary>
    /// The property to animate, by registered name — <c>Opacity</c>, <c>Scale</c>,
    /// <c>TranslationY</c>, <c>BackgroundColor</c> and so on. See
    /// <see cref="AnimatableProperties"/> for the full list and how to extend it.
    /// </summary>
    public string? Property
    {
        get => (string?)GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }

    /// <summary>
    /// Optional <c>x:Name</c> of a different element to animate. When unset the track drives the
    /// element the timeline is attached to, which is the common case.
    /// </summary>
    public string? TargetName
    {
        get => (string?)GetValue(TargetNameProperty);
        set => SetValue(TargetNameProperty, value);
    }

    /// <summary>The keyframes.</summary>
    public IList<Key> Keys { get; } = [];

    internal ITrack Build(VisualElement target)
    {
        if (string.IsNullOrWhiteSpace(Property))
            throw new InvalidOperationException(
                "A Track needs a Property. Set it to a registered animatable property name, " +
                $"such as one of: {string.Join(", ", AnimatableProperties.Names.Order())}.");

        if (Keys.Count == 0)
            throw new InvalidOperationException(
                $"The '{Property}' track has no keyframes. Add at least one Key element.");

        var descriptor = AnimatableProperties.Get(Property);
        var raw = new RawKey[Keys.Count];

        for (var i = 0; i < Keys.Count; i++)
            raw[i] = Keys[i].ToRawKey();

        return descriptor.CreateTrack(target, raw);
    }
}
