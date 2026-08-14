using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Tooltip
{
    static void Restyle(BindableObject b, object o, object n)
        => StyleGuard.WhenReady(b, typeof(Tooltip), () => ((Tooltip)b).ApplyBubbleStyle());

    static void Rewire(BindableObject b, object o, object n)
        => StyleGuard.WhenReady(b, typeof(Tooltip), () => ((Tooltip)b).RewireTrigger());


    // ---------------------------------------------------------------------------------------------
    // What it says
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(Tooltip), null, propertyChanged: Restyle);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(Tooltip), null, propertyChanged: Restyle);

    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(Tooltip), null, propertyChanged: Restyle);

    /// <summary>The tooltip body.</summary>
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Optional bold heading above <see cref="Text"/>.</summary>
    public string? Title
    {
        get => (string?)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Replaces the title/text pair with your own view. The template's binding context is the
    /// tooltip's, so it reaches the same view-model everything else on the page does.
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)this.GetValue(ContentTemplateProperty);
        set => this.SetValue(ContentTemplateProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // What it points at, and when
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty TargetProperty = BindableProperty.Create(
        nameof(Target), typeof(View), typeof(Tooltip), null, propertyChanged: Rewire);

    public static readonly BindableProperty TargetNameProperty = BindableProperty.Create(
        nameof(TargetName), typeof(string), typeof(Tooltip), null, propertyChanged: Rewire);

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(Tooltip), false, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Tooltip), () =>
            ((Tooltip)b).OnIsOpenChanged((bool)n)));

    public static readonly BindableProperty TriggerProperty = BindableProperty.Create(
        nameof(Trigger), typeof(TooltipTrigger), typeof(Tooltip), TooltipTrigger.Manual,
        propertyChanged: Rewire);

    public static readonly BindableProperty ShowDelayProperty = BindableProperty.Create(
        nameof(ShowDelay), typeof(int), typeof(Tooltip), 0);

    public static readonly BindableProperty AutoDismissDelayProperty = BindableProperty.Create(
        nameof(AutoDismissDelay), typeof(int), typeof(Tooltip), 0);

    public static readonly BindableProperty LongPressDelayProperty = BindableProperty.Create(
        nameof(LongPressDelay), typeof(int), typeof(Tooltip), 450);

    public static readonly BindableProperty DismissOnTapProperty = BindableProperty.Create(
        nameof(DismissOnTap), typeof(bool), typeof(Tooltip), true);

    public static readonly BindableProperty DismissOnTapOutsideProperty = BindableProperty.Create(
        nameof(DismissOnTapOutside), typeof(bool), typeof(Tooltip), true);

    /// <summary>
    /// The view the bubble points at. Use <c>{x:Reference someName}</c>. Defaults to this tooltip's
    /// own <c>Content</c> when it is wrapping something, which is the common case.
    /// </summary>
    public View? Target
    {
        get => (View?)this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }

    /// <summary>
    /// The <c>x:Name</c> of the view to point at, resolved through the page's name scope. Only needed
    /// where <c>{x:Reference}</c> cannot reach — inside a <c>DataTemplate</c>, most often. Ignored when
    /// <see cref="Target"/> is set.
    /// </summary>
    public string? TargetName
    {
        get => (string?)this.GetValue(TargetNameProperty);
        set => this.SetValue(TargetNameProperty, value);
    }

    /// <summary>
    /// Whether the bubble is showing. Two-way, so a trigger writes it back and a view-model can drive
    /// it directly.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>IsVisible</c>: that is <see cref="VisualElement.IsVisible"/>, and setting it
    /// would hide the anchor this tooltip is wrapping, not the bubble — as well as colliding with every
    /// style and trigger that already targets it.
    /// </remarks>
    public bool IsOpen
    {
        get => (bool)this.GetValue(IsOpenProperty);
        set => this.SetValue(IsOpenProperty, value);
    }

    /// <summary>What opens the tooltip. Defaults to <see cref="TooltipTrigger.Manual"/>.</summary>
    public TooltipTrigger Trigger
    {
        get => (TooltipTrigger)this.GetValue(TriggerProperty);
        set => this.SetValue(TriggerProperty, value);
    }

    /// <summary>Milliseconds a trigger has to persist before the bubble appears. Hover's grace period.</summary>
    public int ShowDelay
    {
        get => (int)this.GetValue(ShowDelayProperty);
        set => this.SetValue(ShowDelayProperty, value);
    }

    /// <summary>Milliseconds before the bubble closes itself. Zero leaves it up until something closes it.</summary>
    public int AutoDismissDelay
    {
        get => (int)this.GetValue(AutoDismissDelayProperty);
        set => this.SetValue(AutoDismissDelayProperty, value);
    }

    /// <summary>How long a press has to be held to count, for <see cref="TooltipTrigger.LongPress"/>.</summary>
    public int LongPressDelay
    {
        get => (int)this.GetValue(LongPressDelayProperty);
        set => this.SetValue(LongPressDelayProperty, value);
    }

    /// <summary>Tapping the bubble closes it. Turn off when the bubble carries its own controls.</summary>
    public bool DismissOnTap
    {
        get => (bool)this.GetValue(DismissOnTapProperty);
        set => this.SetValue(DismissOnTapProperty, value);
    }

    /// <summary>
    /// Tapping anywhere else closes it. This puts a transparent catcher over the page while the bubble
    /// is up, so that tap does not also reach whatever is underneath — which is what you want from a
    /// popover and not from a hover hint, so it is ignored for the Hover and Focus triggers.
    /// </summary>
    public bool DismissOnTapOutside
    {
        get => (bool)this.GetValue(DismissOnTapOutsideProperty);
        set => this.SetValue(DismissOnTapOutsideProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // How it looks
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty PlacementProperty = BindableProperty.Create(
        nameof(Placement), typeof(TooltipPlacement), typeof(Tooltip), TooltipPlacement.Auto,
        propertyChanged: Restyle);

    public static readonly BindableProperty ShowTailProperty = BindableProperty.Create(
        nameof(ShowTail), typeof(bool), typeof(Tooltip), true, propertyChanged: Restyle);

    public static readonly BindableProperty TailSizeProperty = BindableProperty.Create(
        nameof(TailSize), typeof(double), typeof(Tooltip), 7d, propertyChanged: Restyle);

    public static readonly BindableProperty BubbleColorProperty = BindableProperty.Create(
        nameof(BubbleColor), typeof(Color), typeof(Tooltip), null, propertyChanged: Restyle);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(Tooltip), null, propertyChanged: Restyle);

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(Tooltip), null, propertyChanged: Restyle);

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(Tooltip), 0d, propertyChanged: Restyle);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(Tooltip), Themes.ThemeTokens.Unset, propertyChanged: Restyle);

    public static readonly BindableProperty BubblePaddingProperty = BindableProperty.Create(
        nameof(BubblePadding), typeof(Thickness), typeof(Tooltip), new Thickness(12, 8),
        propertyChanged: Restyle);

    public static readonly BindableProperty MaxBubbleWidthProperty = BindableProperty.Create(
        nameof(MaxBubbleWidth), typeof(double), typeof(Tooltip), 280d, propertyChanged: Restyle);

    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow), typeof(bool), typeof(Tooltip), true, propertyChanged: Restyle);

    public static readonly BindableProperty OffsetProperty = BindableProperty.Create(
        nameof(Offset), typeof(double), typeof(Tooltip), 8d, propertyChanged: Restyle);

    public static readonly BindableProperty ScreenMarginProperty = BindableProperty.Create(
        nameof(ScreenMargin), typeof(double), typeof(Tooltip), 12d, propertyChanged: Restyle);

    public static readonly BindableProperty AnimationProperty = BindableProperty.Create(
        nameof(Animation), typeof(TooltipAnimation), typeof(Tooltip), TooltipAnimation.Scale);

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration), typeof(uint), typeof(Tooltip), 160u);

    /// <summary>Which side to prefer. <see cref="TooltipPlacement.Auto"/> picks the roomiest.</summary>
    public TooltipPlacement Placement
    {
        get => (TooltipPlacement)this.GetValue(PlacementProperty);
        set => this.SetValue(PlacementProperty, value);
    }

    /// <summary>Draw the pointer back at the target.</summary>
    public bool ShowTail
    {
        get => (bool)this.GetValue(ShowTailProperty);
        set => this.SetValue(ShowTailProperty, value);
    }

    public double TailSize
    {
        get => (double)this.GetValue(TailSizeProperty);
        set => this.SetValue(TailSizeProperty, value);
    }

    /// <summary>Leave unset to follow the theme's inverse surface.</summary>
    public Color? BubbleColor
    {
        get => (Color?)this.GetValue(BubbleColorProperty);
        set => this.SetValue(BubbleColorProperty, value);
    }

    public Color? TextColor
    {
        get => (Color?)this.GetValue(TextColorProperty);
        set => this.SetValue(TextColorProperty, value);
    }

    public Color? BorderColor
    {
        get => (Color?)this.GetValue(BorderColorProperty);
        set => this.SetValue(BorderColorProperty, value);
    }

    public double BorderThickness
    {
        get => (double)this.GetValue(BorderThicknessProperty);
        set => this.SetValue(BorderThicknessProperty, value);
    }

    /// <summary>Leave unset (negative) to follow the theme's corner token.</summary>
    public double CornerRadius
    {
        get => (double)this.GetValue(CornerRadiusProperty);
        set => this.SetValue(CornerRadiusProperty, value);
    }

    public Thickness BubblePadding
    {
        get => (Thickness)this.GetValue(BubblePaddingProperty);
        set => this.SetValue(BubblePaddingProperty, value);
    }

    /// <summary>Ceiling on the bubble's width, so long text wraps rather than spanning the screen.</summary>
    public double MaxBubbleWidth
    {
        get => (double)this.GetValue(MaxBubbleWidthProperty);
        set => this.SetValue(MaxBubbleWidthProperty, value);
    }

    public bool HasShadow
    {
        get => (bool)this.GetValue(HasShadowProperty);
        set => this.SetValue(HasShadowProperty, value);
    }

    /// <summary>Gap between the target and the bubble's tail.</summary>
    public double Offset
    {
        get => (double)this.GetValue(OffsetProperty);
        set => this.SetValue(OffsetProperty, value);
    }

    /// <summary>How close to the page edges the bubble is allowed to get.</summary>
    public double ScreenMargin
    {
        get => (double)this.GetValue(ScreenMarginProperty);
        set => this.SetValue(ScreenMarginProperty, value);
    }

    public TooltipAnimation Animation
    {
        get => (TooltipAnimation)this.GetValue(AnimationProperty);
        set => this.SetValue(AnimationProperty, value);
    }

    public uint AnimationDuration
    {
        get => (uint)this.GetValue(AnimationDurationProperty);
        set => this.SetValue(AnimationDurationProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Reacting to it
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(Tooltip), null);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(Tooltip), null);

    public static readonly BindableProperty OpenedCommandProperty = BindableProperty.Create(
        nameof(OpenedCommand), typeof(ICommand), typeof(Tooltip), null);

    public static readonly BindableProperty ClosedCommandProperty = BindableProperty.Create(
        nameof(ClosedCommand), typeof(ICommand), typeof(Tooltip), null);

    /// <summary>Runs when the bubble is tapped, before <see cref="DismissOnTap"/> closes it.</summary>
    public ICommand? Command
    {
        get => (ICommand?)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    public ICommand? OpenedCommand
    {
        get => (ICommand?)this.GetValue(OpenedCommandProperty);
        set => this.SetValue(OpenedCommandProperty, value);
    }

    public ICommand? ClosedCommand
    {
        get => (ICommand?)this.GetValue(ClosedCommandProperty);
        set => this.SetValue(ClosedCommandProperty, value);
    }
}
