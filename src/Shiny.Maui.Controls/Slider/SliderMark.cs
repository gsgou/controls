namespace Shiny.Maui.Controls;

/// <summary>
/// A stop point on a <see cref="Slider"/>: a dot, tick or labelled bubble drawn at a fixed
/// <see cref="Value"/> on the track.
/// </summary>
/// <remarks>
/// Marks are declared with the slider rather than derived from <see cref="Slider.Step"/> so each one
/// can carry its own text and colour — "Low", "Target", "Max" are rarely evenly spaced, and when they
/// are, they still rarely share a colour. Set <see cref="Slider.SnapToMarks"/> to make the thumb come
/// to rest on them.
/// </remarks>
public class SliderMark : BindableObject
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(SliderMark), 0.0,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(SliderMark), null,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(SliderMark), null,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(SliderMark), null,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty ShapeProperty = BindableProperty.Create(
        nameof(Shape), typeof(SliderMarkShape?), typeof(SliderMark), null,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(SliderMark), -1.0,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());

    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(SliderMark), true,
        propertyChanged: (b, _, _) => ((SliderMark)b).RaiseChanged());


    /// <summary>Where on the track the mark sits, in the slider's own units.</summary>
    public double Value
    {
        get => (double)this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    /// <summary>
    /// The label. It is the caption under a <see cref="SliderMarkShape.Dot"/> or
    /// <see cref="SliderMarkShape.Line"/>, and the content of a <see cref="SliderMarkShape.Bubble"/>.
    /// Leave it null for an unlabelled tick.
    /// </summary>
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Fill colour of the dot, tick or bubble. Null falls back to <see cref="Slider.MarkColor"/>.</summary>
    public Color? Color
    {
        get => (Color?)this.GetValue(ColorProperty);
        set => this.SetValue(ColorProperty, value);
    }

    /// <summary>Colour of <see cref="Text"/>. Null falls back to <see cref="Slider.MarkTextColor"/>.</summary>
    public Color? TextColor
    {
        get => (Color?)this.GetValue(TextColorProperty);
        set => this.SetValue(TextColorProperty, value);
    }

    /// <summary>Overrides the slider's <see cref="Slider.MarkShape"/> for this one mark. Null inherits.</summary>
    public SliderMarkShape? Shape
    {
        get => (SliderMarkShape?)this.GetValue(ShapeProperty);
        set => this.SetValue(ShapeProperty, value);
    }

    /// <summary>
    /// Dot diameter, or tick thickness, for this mark. The default, <c>-1</c>, inherits
    /// <see cref="Slider.MarkSize"/>. Ignored by <see cref="SliderMarkShape.Bubble"/>, which sizes to its text.
    /// </summary>
    public double Size
    {
        get => (double)this.GetValue(SizeProperty);
        set => this.SetValue(SizeProperty, value);
    }

    /// <summary>Whether the mark is drawn. A hidden mark is not a snap target either.</summary>
    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }


    /// <summary>Raised whenever anything the slider draws the mark from changes.</summary>
    public event EventHandler? Changed;

    void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
}
