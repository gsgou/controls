using System.Windows.Input;

namespace Shiny.Maui.Controls;

/// <summary>
/// One stop on a <see cref="Walkthrough"/>: what to highlight, what to say about it, and how that
/// arrives and leaves.
/// </summary>
/// <remarks>
/// A step is declared with the walkthrough rather than attached to the control it describes. That is
/// the point of the control: the order of a tour is a property of the tour, and on a busy screen —
/// nested layouts, templated cells, a collection view — attached properties scatter it across the
/// markup where nothing can see the sequence as a whole.
/// </remarks>
[ContentProperty(nameof(Content))]
public class WalkthroughStep : BindableObject
{
    // ---------------------------------------------------------------------------------------------
    // Target
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty TargetProperty = BindableProperty.Create(
        nameof(Target), typeof(View), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty TargetNameProperty = BindableProperty.Create(
        nameof(TargetName), typeof(string), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(WalkthroughStep), true,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    /// <summary>Identifies the step for <c>GoTo</c> and for the walkthrough's <c>CurrentStep</c>.</summary>
    public string? Name
    {
        get => (string?)this.GetValue(NameProperty);
        set => this.SetValue(NameProperty, value);
    }

    /// <summary>
    /// The view to highlight, as <c>{x:Reference SaveButton}</c>. Prefer this over
    /// <see cref="TargetName"/>: it is checked when the XAML is compiled, so a renamed control breaks
    /// the build instead of quietly producing a tour that highlights nothing.
    /// </summary>
    public View? Target
    {
        get => (View?)this.GetValue(TargetProperty);
        set => this.SetValue(TargetProperty, value);
    }

    /// <summary>
    /// The <c>x:Name</c> of the view to highlight, resolved through the page's name scope at the
    /// moment the step is shown. Use it where <c>{x:Reference}</c> cannot reach — a control created in
    /// code, or one on a page the walkthrough does not share markup with.
    /// </summary>
    public string? TargetName
    {
        get => (string?)this.GetValue(TargetNameProperty);
        set => this.SetValue(TargetNameProperty, value);
    }

    /// <summary>
    /// Whether the step is part of the run. Bind it to drop steps that do not apply — a tour that
    /// skips the "invite a teammate" step for a solo account, say — without rebuilding the collection.
    /// </summary>
    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // What it says
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(WalkthroughStep), null,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).OnContentTemplateChanged());

    public static readonly BindableProperty DisplayProperty = BindableProperty.Create(
        nameof(Display), typeof(WalkthroughDisplay), typeof(WalkthroughStep), WalkthroughDisplay.Popover,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public static readonly BindableProperty PlacementProperty = BindableProperty.Create(
        nameof(Placement), typeof(TooltipPlacement), typeof(WalkthroughStep), TooltipPlacement.Auto,
        propertyChanged: (b, o, n) => ((WalkthroughStep)b).RaiseChanged());

    public string? Title
    {
        get => (string?)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>
    /// Your own view in place of the title/text pair, built with the markup. Everything else about the
    /// step — the highlight, the placement, the animation — still applies.
    /// </summary>
    public View? Content
    {
        get => (View?)this.GetValue(ContentProperty);
        set => this.SetValue(ContentProperty, value);
    }

    /// <summary>
    /// The same, built on first use and then reused. Its binding context is inherited from the page,
    /// so it reaches the same view-model as the rest of the screen.
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)this.GetValue(ContentTemplateProperty);
        set => this.SetValue(ContentTemplateProperty, value);
    }

    /// <summary>Which of the four presentations to use. Defaults to <see cref="WalkthroughDisplay.Popover"/>.</summary>
    public WalkthroughDisplay Display
    {
        get => (WalkthroughDisplay)this.GetValue(DisplayProperty);
        set => this.SetValue(DisplayProperty, value);
    }

    /// <summary>Which side of the target to prefer. Auto picks the roomiest.</summary>
    public TooltipPlacement Placement
    {
        get => (TooltipPlacement)this.GetValue(PlacementProperty);
        set => this.SetValue(PlacementProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Timing and motion
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty DurationProperty = BindableProperty.Create(
        nameof(Duration), typeof(int), typeof(WalkthroughStep), 0);

    public static readonly BindableProperty DurationInProperty = BindableProperty.Create(
        nameof(DurationIn), typeof(uint), typeof(WalkthroughStep), 260u);

    public static readonly BindableProperty DurationOutProperty = BindableProperty.Create(
        nameof(DurationOut), typeof(uint), typeof(WalkthroughStep), 180u);

    public static readonly BindableProperty AnimationInProperty = BindableProperty.Create(
        nameof(AnimationIn), typeof(WalkthroughAnimation), typeof(WalkthroughStep), WalkthroughAnimation.Zoom);

    public static readonly BindableProperty AnimationOutProperty = BindableProperty.Create(
        nameof(AnimationOut), typeof(WalkthroughAnimation), typeof(WalkthroughStep), WalkthroughAnimation.Fade);

    /// <summary>
    /// Milliseconds to hold this step before advancing on its own. Zero — the default — waits for the
    /// user. This is dwell time, not animation time: see <see cref="DurationIn"/> for that.
    /// </summary>
    public int Duration
    {
        get => (int)this.GetValue(DurationProperty);
        set => this.SetValue(DurationProperty, value);
    }

    /// <summary>How long the callout takes to arrive.</summary>
    public uint DurationIn
    {
        get => (uint)this.GetValue(DurationInProperty);
        set => this.SetValue(DurationInProperty, value);
    }

    /// <summary>How long it takes to leave.</summary>
    public uint DurationOut
    {
        get => (uint)this.GetValue(DurationOutProperty);
        set => this.SetValue(DurationOutProperty, value);
    }

    public WalkthroughAnimation AnimationIn
    {
        get => (WalkthroughAnimation)this.GetValue(AnimationInProperty);
        set => this.SetValue(AnimationInProperty, value);
    }

    public WalkthroughAnimation AnimationOut
    {
        get => (WalkthroughAnimation)this.GetValue(AnimationOutProperty);
        set => this.SetValue(AnimationOutProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // The highlight
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty HighlightProperty = BindableProperty.Create(
        nameof(Highlight), typeof(WalkthroughHighlight?), typeof(WalkthroughStep), null);

    public static readonly BindableProperty HighlightPaddingProperty = BindableProperty.Create(
        nameof(HighlightPadding), typeof(double?), typeof(WalkthroughStep), null);

    public static readonly BindableProperty HighlightCornerRadiusProperty = BindableProperty.Create(
        nameof(HighlightCornerRadius), typeof(double?), typeof(WalkthroughStep), null);

    /// <summary>Overrides the walkthrough's highlight shape for this step. Null inherits.</summary>
    public WalkthroughHighlight? Highlight
    {
        get => (WalkthroughHighlight?)this.GetValue(HighlightProperty);
        set => this.SetValue(HighlightProperty, value);
    }

    /// <summary>How much breathing room the cut-out leaves around the target. Null inherits.</summary>
    public double? HighlightPadding
    {
        get => (double?)this.GetValue(HighlightPaddingProperty);
        set => this.SetValue(HighlightPaddingProperty, value);
    }

    public double? HighlightCornerRadius
    {
        get => (double?)this.GetValue(HighlightCornerRadiusProperty);
        set => this.SetValue(HighlightCornerRadiusProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Interaction
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty AllowTargetInteractionProperty = BindableProperty.Create(
        nameof(AllowTargetInteraction), typeof(bool), typeof(WalkthroughStep), false);

    public static readonly BindableProperty AdvanceOnTargetTapProperty = BindableProperty.Create(
        nameof(AdvanceOnTargetTap), typeof(bool), typeof(WalkthroughStep), false);

    public static readonly BindableProperty ScrollToTargetProperty = BindableProperty.Create(
        nameof(ScrollToTarget), typeof(bool?), typeof(WalkthroughStep), null);

    public static readonly BindableProperty EnteredCommandProperty = BindableProperty.Create(
        nameof(EnteredCommand), typeof(ICommand), typeof(WalkthroughStep), null);

    public static readonly BindableProperty LeftCommandProperty = BindableProperty.Create(
        nameof(LeftCommand), typeof(ICommand), typeof(WalkthroughStep), null);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(WalkthroughStep), null);

    /// <summary>
    /// Let taps inside the cut-out reach the real control, so the user can try the thing being
    /// explained. The backdrop outside the hole still catches everything else.
    /// </summary>
    public bool AllowTargetInteraction
    {
        get => (bool)this.GetValue(AllowTargetInteractionProperty);
        set => this.SetValue(AllowTargetInteractionProperty, value);
    }

    /// <summary>
    /// Advance when the user actually uses the highlighted control — "tap Save to continue". Implies
    /// <see cref="AllowTargetInteraction"/>, since the tap has to reach the control to count.
    /// </summary>
    public bool AdvanceOnTargetTap
    {
        get => (bool)this.GetValue(AdvanceOnTargetTapProperty);
        set => this.SetValue(AdvanceOnTargetTapProperty, value);
    }

    /// <summary>Bring the target into view first, when it is inside a scroll view. Null inherits.</summary>
    public bool? ScrollToTarget
    {
        get => (bool?)this.GetValue(ScrollToTargetProperty);
        set => this.SetValue(ScrollToTargetProperty, value);
    }

    /// <summary>Runs as the step arrives — for setting the screen up so the step makes sense.</summary>
    public ICommand? EnteredCommand
    {
        get => (ICommand?)this.GetValue(EnteredCommandProperty);
        set => this.SetValue(EnteredCommandProperty, value);
    }

    /// <summary>Runs as the step leaves, in either direction.</summary>
    public ICommand? LeftCommand
    {
        get => (ICommand?)this.GetValue(LeftCommandProperty);
        set => this.SetValue(LeftCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }


    /// <summary>The view built from <see cref="ContentTemplate"/>, once it has been.</summary>
    internal View? TemplatedContent { get; private set; }

    /// <summary>Inline content, or the template realized on first use.</summary>
    internal View? ResolveContent()
    {
        if (this.ContentTemplate is null)
            return this.Content;

        if (this.TemplatedContent is null)
        {
            var template = this.ContentTemplate;
            if (template is DataTemplateSelector selector)
                template = selector.SelectTemplate(this.BindingContext, null);

            this.TemplatedContent = template.CreateContent() as View;
        }
        return this.TemplatedContent;
    }


    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Inline content is parented into the callout and inherits from there. Templated content may
        // not be hosted yet, so it is seeded explicitly — same as StateViewState.
        if (this.TemplatedContent is not null && this.TemplatedContent.Parent is null)
            SetInheritedBindingContext(this.TemplatedContent, this.BindingContext);
    }


    void OnContentTemplateChanged()
    {
        this.TemplatedContent = null;
        this.RaiseChanged();
    }


    /// <summary>Raised when something the owning <see cref="Walkthrough"/> draws from has changed.</summary>
    internal event EventHandler? Changed;

    void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
}
