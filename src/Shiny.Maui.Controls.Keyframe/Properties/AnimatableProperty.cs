using System.Globalization;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>A keyframe as authored in XAML, before its value has been converted to the target type.</summary>
/// <param name="Offset">Position within the iteration, 0 to 1.</param>
/// <param name="Value">The raw value, typically a string straight from the XAML parser.
/// Null means "whatever the target's value is when playback starts".</param>
/// <param name="Easing">Easing for the segment starting at this keyframe.</param>
public readonly record struct RawKey(double Offset, object? Value, EasingFunction? Easing);

/// <summary>
/// Describes one animatable property of a view: how to read it, how to write it, how to blend it,
/// and how to parse a value the XAML parser handed us as a string.
/// </summary>
/// <remarks>
/// <para><b>Why a registry instead of reflection.</b> A XAML surface that says
/// <c>Property="Opacity"</c> has to get from that string to a real setter somehow. Reflection or a
/// compiled <c>Expression</c> would both work at runtime and both break under Native AOT and
/// aggressive trimming — the property gets rooted out of existence and the animation fails on
/// device but not in the emulator. Registering explicit delegates means the linker sees ordinary
/// static method calls, so nothing is trimmed away and nothing needs a preservation attribute.</para>
/// </remarks>
public abstract class AnimatableProperty
{
    /// <summary>The name used in XAML.</summary>
    public abstract string Name { get; }

    /// <summary>The property's value type.</summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// Whether writing this property forces a new measure and arrange pass. Animating one of these
    /// runs full layout every frame, which is the difference between a smooth animation and a
    /// janky one on a complex page.
    /// </summary>
    public abstract bool InvalidatesLayout { get; }

    /// <summary>Builds a track that drives this property on the given view.</summary>
    public abstract ITrack CreateTrack(VisualElement target, IReadOnlyList<RawKey> keys);
}

/// <summary>A strongly typed animatable property.</summary>
/// <typeparam name="TValue">The property's value type.</typeparam>
public sealed class AnimatableProperty<TValue> : AnimatableProperty
{
    readonly Func<VisualElement, TValue> getter;
    readonly Action<VisualElement, TValue> setter;
    readonly IInterpolator<TValue> interpolator;
    readonly Func<object, TValue> parse;

    /// <summary>Describes a property.</summary>
    /// <param name="name">The name used in XAML.</param>
    /// <param name="getter">Reads the current value.</param>
    /// <param name="setter">Writes a value.</param>
    /// <param name="interpolator">Blends between keyframe values.</param>
    /// <param name="parse">Converts a raw XAML value to <typeparamref name="TValue"/>.</param>
    /// <param name="invalidatesLayout">Whether writing forces a measure and arrange pass.</param>
    public AnimatableProperty(
        string name,
        Func<VisualElement, TValue> getter,
        Action<VisualElement, TValue> setter,
        IInterpolator<TValue> interpolator,
        Func<object, TValue> parse,
        bool invalidatesLayout = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        ArgumentNullException.ThrowIfNull(interpolator);
        ArgumentNullException.ThrowIfNull(parse);

        Name = name;
        this.getter = getter;
        this.setter = setter;
        this.interpolator = interpolator;
        this.parse = parse;
        InvalidatesLayout = invalidatesLayout;
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override Type ValueType => typeof(TValue);

    /// <inheritdoc />
    public override bool InvalidatesLayout { get; }

    /// <inheritdoc />
    public override ITrack CreateTrack(VisualElement target, IReadOnlyList<RawKey> keys)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(keys);

        var converted = new List<Key<TValue>>(keys.Count);

        foreach (var key in keys)
        {
            // A missing value is the implicit keyframe: resolve against the live value at playback
            // time rather than a literal, so a re-triggered animation continues smoothly.
            if (key.Value is null)
            {
                converted.Add(Key<TValue>.Current(key.Offset, key.Easing));
                continue;
            }

            converted.Add(new Key<TValue>(key.Offset, Parse(key.Value), key.Easing));
        }

        return new Track<VisualElement, TValue>(target, setter, converted, interpolator, getter, Name);
    }

    TValue Parse(object value)
    {
        if (value is TValue typed)
            return typed;

        try
        {
            return parse(value);
        }
        catch (Exception error) when (error is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new FormatException(
                $"Could not read '{value}' as a value for the '{Name}' property, which expects {typeof(TValue).Name}.",
                error);
        }
    }
}

/// <summary>
/// The set of view properties that can be animated by name from XAML.
/// </summary>
/// <remarks>
/// Register your own with <see cref="Register"/> at startup to animate properties on a custom
/// control. Built-in entries can be replaced by registering the same name again.
/// </remarks>
public static class AnimatableProperties
{
    static readonly Dictionary<string, AnimatableProperty> Registry =
        new(StringComparer.OrdinalIgnoreCase);

    static AnimatableProperties() => RegisterBuiltIns();

    /// <summary>Adds or replaces a property definition.</summary>
    public static void Register(AnimatableProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        Registry[property.Name] = property;
    }

    /// <summary>Looks up a property by name, returning null if it is not registered.</summary>
    public static AnimatableProperty? Find(string name)
        => string.IsNullOrWhiteSpace(name) ? null : Registry.GetValueOrDefault(name);

    /// <summary>Looks up a property by name, throwing a message that lists the alternatives.</summary>
    public static AnimatableProperty Get(string name)
        => Find(name) ?? throw new KeyNotFoundException(
            $"'{name}' is not a registered animatable property. " +
            $"Known properties: {string.Join(", ", Registry.Keys.Order())}. " +
            "Call AnimatableProperties.Register to add your own.");

    /// <summary>Every registered property name.</summary>
    public static IEnumerable<string> Names => Registry.Keys;

    static void RegisterBuiltIns()
    {
        // --- Transform and opacity. Cheap: these never invalidate layout, and on every platform
        // they map onto a native compositor property. Prefer them wherever a choice exists. ---
        Add("Opacity", static v => v.Opacity, static (v, x) => v.Opacity = x);
        Add("Scale", static v => v.Scale, static (v, x) => v.Scale = x);
        Add("ScaleX", static v => v.ScaleX, static (v, x) => v.ScaleX = x);
        Add("ScaleY", static v => v.ScaleY, static (v, x) => v.ScaleY = x);
        Add("TranslationX", static v => v.TranslationX, static (v, x) => v.TranslationX = x);
        Add("TranslationY", static v => v.TranslationY, static (v, x) => v.TranslationY = x);
        Add("RotationX", static v => v.RotationX, static (v, x) => v.RotationX = x);
        Add("RotationY", static v => v.RotationY, static (v, x) => v.RotationY = x);
        Add("AnchorX", static v => v.AnchorX, static (v, x) => v.AnchorX = x);
        Add("AnchorY", static v => v.AnchorY, static (v, x) => v.AnchorY = x);

        // Rotation takes the shortest arc, so 350 to 10 turns forward through zero. Use "Spin"
        // when you actually want multiple turns.
        Register(new AnimatableProperty<double>(
            "Rotation",
            static v => v.Rotation,
            static (v, x) => v.Rotation = x,
            AngleInterpolator.Degrees,
            ParseDouble));

        Register(new AnimatableProperty<double>(
            "Spin",
            static v => v.Rotation,
            static (v, x) => v.Rotation = x,
            DoubleInterpolator.Instance,
            ParseDouble));

        // --- Colour. Blended in Oklab so midpoints stay saturated. ---
        Register(new AnimatableProperty<Microsoft.Maui.Graphics.Color>(
            "BackgroundColor",
            static v => v.BackgroundColor ?? Colors.Transparent,
            static (v, x) => v.BackgroundColor = x,
            Shiny.Controls.Keyframe.Graphics.ColorInterpolator.Oklab,
            ParseColor));

        // --- Layout-affecting. Each of these runs measure and arrange on every frame. ---
        Add("WidthRequest", static v => v.WidthRequest, static (v, x) => v.WidthRequest = x, invalidatesLayout: true);
        Add("HeightRequest", static v => v.HeightRequest, static (v, x) => v.HeightRequest = x, invalidatesLayout: true);

        Register(new AnimatableProperty<Thickness>(
            "Margin",
            static v => v is View view ? view.Margin : default,
            static (v, x) => { if (v is View view) view.Margin = x; },
            new DelegateInterpolator<Thickness>(LerpThickness),
            ParseThickness,
            invalidatesLayout: true));

        Register(new AnimatableProperty<Thickness>(
            "Padding",
            static v => v is IPaddingElement element ? element.Padding : default,
            SetPadding,
            new DelegateInterpolator<Thickness>(LerpThickness),
            ParseThickness,
            invalidatesLayout: true));
    }

    // IPaddingElement exposes Padding read-only, so the setter has to go through the concrete
    // types that declare it. Switching over them keeps this AOT-safe; reflecting for a
    // "Padding" BindableProperty would not survive trimming.
    static void SetPadding(VisualElement element, Thickness value)
    {
        switch (element)
        {
            case Layout layout:
                layout.Padding = value;
                break;

            case Border border:
                border.Padding = value;
                break;

            case Page page:
                page.Padding = value;
                break;
        }
    }

    static void Add(
        string name,
        Func<VisualElement, double> getter,
        Action<VisualElement, double> setter,
        bool invalidatesLayout = false)
        => Register(new AnimatableProperty<double>(
            name, getter, setter, DoubleInterpolator.Instance, ParseDouble, invalidatesLayout));

    static double ParseDouble(object value) => value switch
    {
        double d => d,
        string text => double.Parse(text, CultureInfo.InvariantCulture),
        IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
        _ => throw new FormatException($"Cannot read '{value}' as a number.")
    };

    static Microsoft.Maui.Graphics.Color ParseColor(object value) => value switch
    {
        Microsoft.Maui.Graphics.Color color => color,
        string text => Microsoft.Maui.Graphics.Color.Parse(text),
        _ => throw new FormatException($"Cannot read '{value}' as a colour.")
    };

    static Thickness ParseThickness(object value)
    {
        if (value is Thickness thickness)
            return thickness;

        if (value is double uniform)
            return new Thickness(uniform);

        if (value is not string text)
            throw new FormatException($"Cannot read '{value}' as a thickness.");

        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var numbers = Array.ConvertAll(parts, p => double.Parse(p, CultureInfo.InvariantCulture));

        return numbers.Length switch
        {
            1 => new Thickness(numbers[0]),
            2 => new Thickness(numbers[0], numbers[1]),
            4 => new Thickness(numbers[0], numbers[1], numbers[2], numbers[3]),
            _ => throw new FormatException(
                $"'{text}' is not a valid thickness. Use one, two, or four comma-separated numbers.")
        };
    }

    static Thickness LerpThickness(Thickness from, Thickness to, double t) => new(
        from.Left + (to.Left - from.Left) * t,
        from.Top + (to.Top - from.Top) * t,
        from.Right + (to.Right - from.Right) * t,
        from.Bottom + (to.Bottom - from.Bottom) * t);
}
