using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Walkthrough
{
    static void Restyle(BindableObject b, object o, object n)
        => StyleGuard.WhenReady(b, typeof(Walkthrough), () => ((Walkthrough)b).ApplyChrome());


    // ---------------------------------------------------------------------------------------------
    // Running
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
        nameof(IsRunning), typeof(bool), typeof(Walkthrough), false, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Walkthrough), () =>
            ((Walkthrough)b).OnIsRunningChanged((bool)n)));

    public static readonly BindableProperty AutoStartProperty = BindableProperty.Create(
        nameof(AutoStart), typeof(bool), typeof(Walkthrough), false);

    public static readonly BindableProperty AutoStartDelayProperty = BindableProperty.Create(
        nameof(AutoStartDelay), typeof(int), typeof(Walkthrough), 400);

    public static readonly BindableProperty RememberRunKeyProperty = BindableProperty.Create(
        nameof(RememberRunKey), typeof(string), typeof(Walkthrough), null);

    public static readonly BindableProperty RememberOnSkipProperty = BindableProperty.Create(
        nameof(RememberOnSkip), typeof(bool), typeof(Walkthrough), true);

    /// <summary>
    /// Whether the tour is on screen. Two-way — the walkthrough writes it back when it ends, so a
    /// view-model can both start it and be told it finished from one property.
    /// </summary>
    public bool IsRunning
    {
        get => (bool)this.GetValue(IsRunningProperty);
        set => this.SetValue(IsRunningProperty, value);
    }

    /// <summary>
    /// Start as soon as the page is up, subject to <see cref="RememberRunKey"/>. This is the onboarding
    /// case: the first launch shows the tour, later ones do not.
    /// </summary>
    public bool AutoStart
    {
        get => (bool)this.GetValue(AutoStartProperty);
        set => this.SetValue(AutoStartProperty, value);
    }

    /// <summary>
    /// Milliseconds to wait before auto-starting. The default leaves room for the page's own entrance
    /// animation to settle — starting into a moving layout measures targets that are still travelling.
    /// </summary>
    public int AutoStartDelay
    {
        get => (int)this.GetValue(AutoStartDelayProperty);
        set => this.SetValue(AutoStartDelayProperty, value);
    }

    /// <summary>
    /// Remember, under this key, that the user has been through the tour, and do not auto-start it
    /// again. Leave it unset and the tour runs every time. Clear it with <see cref="Reset"/> or
    /// <see cref="ClearRun"/>.
    /// </summary>
    public string? RememberRunKey
    {
        get => (string?)this.GetValue(RememberRunKeyProperty);
        set => this.SetValue(RememberRunKeyProperty, value);
    }

    /// <summary>
    /// Count a skip as having run. On by default: a user who dismissed the tour does not want it back
    /// at the next launch. Turn it off to only remember a full run.
    /// </summary>
    public bool RememberOnSkip
    {
        get => (bool)this.GetValue(RememberOnSkipProperty);
        set => this.SetValue(RememberOnSkipProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Position
    // ---------------------------------------------------------------------------------------------

    static readonly BindablePropertyKey StepCountPropertyKey = BindableProperty.CreateReadOnly(
        nameof(StepCount), typeof(int), typeof(Walkthrough), 0);

    static readonly BindablePropertyKey StepNumberPropertyKey = BindableProperty.CreateReadOnly(
        nameof(StepNumber), typeof(int), typeof(Walkthrough), 0);

    static readonly BindablePropertyKey CurrentStepPropertyKey = BindableProperty.CreateReadOnly(
        nameof(CurrentStep), typeof(string), typeof(Walkthrough), null);

    static readonly BindablePropertyKey CurrentStepIndexPropertyKey = BindableProperty.CreateReadOnly(
        nameof(CurrentStepIndex), typeof(int), typeof(Walkthrough), -1);

    public static readonly BindableProperty StepCountProperty = StepCountPropertyKey.BindableProperty;
    public static readonly BindableProperty StepNumberProperty = StepNumberPropertyKey.BindableProperty;
    public static readonly BindableProperty CurrentStepProperty = CurrentStepPropertyKey.BindableProperty;
    public static readonly BindableProperty CurrentStepIndexProperty = CurrentStepIndexPropertyKey.BindableProperty;

    /// <summary>How many steps are in the run — visible ones only.</summary>
    public int StepCount
    {
        get => (int)this.GetValue(StepCountProperty);
        private set => this.SetValue(StepCountPropertyKey, value);
    }

    /// <summary>One-based position of the step showing, for your own captions.</summary>
    public int StepNumber
    {
        get => (int)this.GetValue(StepNumberProperty);
        private set => this.SetValue(StepNumberPropertyKey, value);
    }

    /// <summary>The <see cref="WalkthroughStep.Name"/> of the step showing.</summary>
    public string? CurrentStep
    {
        get => (string?)this.GetValue(CurrentStepProperty);
        private set => this.SetValue(CurrentStepPropertyKey, value);
    }

    /// <summary>Zero-based index among the visible steps, or -1 when nothing is running.</summary>
    public int CurrentStepIndex
    {
        get => (int)this.GetValue(CurrentStepIndexProperty);
        private set => this.SetValue(CurrentStepIndexPropertyKey, value);
    }


    // ---------------------------------------------------------------------------------------------
    // The backdrop
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty UseOverlayProperty = BindableProperty.Create(
        nameof(UseOverlay), typeof(bool), typeof(Walkthrough), true, propertyChanged: Restyle);

    public static readonly BindableProperty OverlayColorProperty = BindableProperty.Create(
        nameof(OverlayColor), typeof(Color), typeof(Walkthrough), null, propertyChanged: Restyle);

    public static readonly BindableProperty OverlayOpacityProperty = BindableProperty.Create(
        nameof(OverlayOpacity), typeof(double), typeof(Walkthrough), 0.8d, propertyChanged: Restyle);

    public static readonly BindableProperty HighlightProperty = BindableProperty.Create(
        nameof(Highlight), typeof(WalkthroughHighlight), typeof(Walkthrough),
        WalkthroughHighlight.RoundedRectangle, propertyChanged: Restyle);

    public static readonly BindableProperty HighlightPaddingProperty = BindableProperty.Create(
        nameof(HighlightPadding), typeof(double), typeof(Walkthrough), 6d, propertyChanged: Restyle);

    public static readonly BindableProperty HighlightCornerRadiusProperty = BindableProperty.Create(
        nameof(HighlightCornerRadius), typeof(double), typeof(Walkthrough), 10d, propertyChanged: Restyle);

    public static readonly BindableProperty RingColorProperty = BindableProperty.Create(
        nameof(RingColor), typeof(Color), typeof(Walkthrough), null, propertyChanged: Restyle);

    public static readonly BindableProperty RingThicknessProperty = BindableProperty.Create(
        nameof(RingThickness), typeof(double), typeof(Walkthrough), 0d, propertyChanged: Restyle);

    public static readonly BindableProperty SpotlightMoveDurationProperty = BindableProperty.Create(
        nameof(SpotlightMoveDuration), typeof(uint), typeof(Walkthrough), 320u);

    /// <summary>
    /// Dim the page behind the tour. Turn it off to leave the app fully visible and let the callouts
    /// float over live content — which also disables the cut-out, since there is nothing to cut.
    /// </summary>
    public bool UseOverlay
    {
        get => (bool)this.GetValue(UseOverlayProperty);
        set => this.SetValue(UseOverlayProperty, value);
    }

    /// <summary>Leave unset to follow the theme's scrim token.</summary>
    public Color? OverlayColor
    {
        get => (Color?)this.GetValue(OverlayColorProperty);
        set => this.SetValue(OverlayColorProperty, value);
    }

    public double OverlayOpacity
    {
        get => (double)this.GetValue(OverlayOpacityProperty);
        set => this.SetValue(OverlayOpacityProperty, value);
    }

    /// <summary>The default cut-out shape. A step can override it.</summary>
    public WalkthroughHighlight Highlight
    {
        get => (WalkthroughHighlight)this.GetValue(HighlightProperty);
        set => this.SetValue(HighlightProperty, value);
    }

    /// <summary>Breathing room left around the target inside the cut-out.</summary>
    public double HighlightPadding
    {
        get => (double)this.GetValue(HighlightPaddingProperty);
        set => this.SetValue(HighlightPaddingProperty, value);
    }

    public double HighlightCornerRadius
    {
        get => (double)this.GetValue(HighlightCornerRadiusProperty);
        set => this.SetValue(HighlightCornerRadiusProperty, value);
    }

    /// <summary>An outline traced round the cut-out. Set a thickness to turn it on.</summary>
    public Color? RingColor
    {
        get => (Color?)this.GetValue(RingColorProperty);
        set => this.SetValue(RingColorProperty, value);
    }

    public double RingThickness
    {
        get => (double)this.GetValue(RingThicknessProperty);
        set => this.SetValue(RingThicknessProperty, value);
    }

    /// <summary>How long the spotlight takes to travel from one target to the next.</summary>
    public uint SpotlightMoveDuration
    {
        get => (uint)this.GetValue(SpotlightMoveDurationProperty);
        set => this.SetValue(SpotlightMoveDurationProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Callout chrome
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty ShowNavigationProperty = BindableProperty.Create(
        nameof(ShowNavigation), typeof(bool), typeof(Walkthrough), true, propertyChanged: Restyle);

    public static readonly BindableProperty ShowStepCounterProperty = BindableProperty.Create(
        nameof(ShowStepCounter), typeof(bool), typeof(Walkthrough), true, propertyChanged: Restyle);

    public static readonly BindableProperty ShowSkipProperty = BindableProperty.Create(
        nameof(ShowSkip), typeof(bool), typeof(Walkthrough), true, propertyChanged: Restyle);

    public static readonly BindableProperty ShowBackProperty = BindableProperty.Create(
        nameof(ShowBack), typeof(bool), typeof(Walkthrough), true, propertyChanged: Restyle);

    public static readonly BindableProperty NextTextProperty = BindableProperty.Create(
        nameof(NextText), typeof(string), typeof(Walkthrough), "Next", propertyChanged: Restyle);

    public static readonly BindableProperty BackTextProperty = BindableProperty.Create(
        nameof(BackText), typeof(string), typeof(Walkthrough), "Back", propertyChanged: Restyle);

    public static readonly BindableProperty SkipTextProperty = BindableProperty.Create(
        nameof(SkipText), typeof(string), typeof(Walkthrough), "Skip", propertyChanged: Restyle);

    public static readonly BindableProperty FinishTextProperty = BindableProperty.Create(
        nameof(FinishText), typeof(string), typeof(Walkthrough), "Done", propertyChanged: Restyle);

    public static readonly BindableProperty AdvanceOnBackdropTapProperty = BindableProperty.Create(
        nameof(AdvanceOnBackdropTap), typeof(bool), typeof(Walkthrough), false);

    public static readonly BindableProperty ScrollToTargetProperty = BindableProperty.Create(
        nameof(ScrollToTarget), typeof(bool), typeof(Walkthrough), true);

    public static readonly BindableProperty CalloutColorProperty = BindableProperty.Create(
        nameof(CalloutColor), typeof(Color), typeof(Walkthrough), null, propertyChanged: Restyle);

    public static readonly BindableProperty CalloutTextColorProperty = BindableProperty.Create(
        nameof(CalloutTextColor), typeof(Color), typeof(Walkthrough), null, propertyChanged: Restyle);

    public static readonly BindableProperty CalloutCornerRadiusProperty = BindableProperty.Create(
        nameof(CalloutCornerRadius), typeof(double), typeof(Walkthrough), Themes.ThemeTokens.Unset, propertyChanged: Restyle);

    public static readonly BindableProperty MaxCalloutWidthProperty = BindableProperty.Create(
        nameof(MaxCalloutWidth), typeof(double), typeof(Walkthrough), 320d, propertyChanged: Restyle);

    public static readonly BindableProperty CalloutOffsetProperty = BindableProperty.Create(
        nameof(CalloutOffset), typeof(double), typeof(Walkthrough), 14d, propertyChanged: Restyle);

    public static readonly BindableProperty ScreenMarginProperty = BindableProperty.Create(
        nameof(ScreenMargin), typeof(double), typeof(Walkthrough), 16d, propertyChanged: Restyle);

    /// <summary>Draw the built-in Back/Next row. Off leaves advancing to taps, timers or your own template.</summary>
    public bool ShowNavigation
    {
        get => (bool)this.GetValue(ShowNavigationProperty);
        set => this.SetValue(ShowNavigationProperty, value);
    }

    /// <summary>Show "2 of 5" on the callout.</summary>
    public bool ShowStepCounter
    {
        get => (bool)this.GetValue(ShowStepCounterProperty);
        set => this.SetValue(ShowStepCounterProperty, value);
    }

    public bool ShowSkip
    {
        get => (bool)this.GetValue(ShowSkipProperty);
        set => this.SetValue(ShowSkipProperty, value);
    }

    public bool ShowBack
    {
        get => (bool)this.GetValue(ShowBackProperty);
        set => this.SetValue(ShowBackProperty, value);
    }

    public string NextText
    {
        get => (string)this.GetValue(NextTextProperty);
        set => this.SetValue(NextTextProperty, value);
    }

    public string BackText
    {
        get => (string)this.GetValue(BackTextProperty);
        set => this.SetValue(BackTextProperty, value);
    }

    public string SkipText
    {
        get => (string)this.GetValue(SkipTextProperty);
        set => this.SetValue(SkipTextProperty, value);
    }

    /// <summary>Replaces <see cref="NextText"/> on the last step.</summary>
    public string FinishText
    {
        get => (string)this.GetValue(FinishTextProperty);
        set => this.SetValue(FinishTextProperty, value);
    }

    /// <summary>
    /// Tapping the dimmed area moves to the next step. Off by default because it makes a stray tap end
    /// the tour early; on, it is the fastest way through a short one.
    /// </summary>
    public bool AdvanceOnBackdropTap
    {
        get => (bool)this.GetValue(AdvanceOnBackdropTapProperty);
        set => this.SetValue(AdvanceOnBackdropTapProperty, value);
    }

    /// <summary>Bring each target into view before highlighting it, when it is inside a scroll view.</summary>
    public bool ScrollToTarget
    {
        get => (bool)this.GetValue(ScrollToTargetProperty);
        set => this.SetValue(ScrollToTargetProperty, value);
    }

    /// <summary>Leave unset to follow the theme's high surface container.</summary>
    public Color? CalloutColor
    {
        get => (Color?)this.GetValue(CalloutColorProperty);
        set => this.SetValue(CalloutColorProperty, value);
    }

    public Color? CalloutTextColor
    {
        get => (Color?)this.GetValue(CalloutTextColorProperty);
        set => this.SetValue(CalloutTextColorProperty, value);
    }

    /// <summary>Leave unset (negative) to follow the theme's corner token.</summary>
    public double CalloutCornerRadius
    {
        get => (double)this.GetValue(CalloutCornerRadiusProperty);
        set => this.SetValue(CalloutCornerRadiusProperty, value);
    }

    public double MaxCalloutWidth
    {
        get => (double)this.GetValue(MaxCalloutWidthProperty);
        set => this.SetValue(MaxCalloutWidthProperty, value);
    }

    /// <summary>Gap between the highlight and the callout.</summary>
    public double CalloutOffset
    {
        get => (double)this.GetValue(CalloutOffsetProperty);
        set => this.SetValue(CalloutOffsetProperty, value);
    }

    /// <summary>How close to the page edges a callout is allowed to get.</summary>
    public double ScreenMargin
    {
        get => (double)this.GetValue(ScreenMarginProperty);
        set => this.SetValue(ScreenMarginProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Commands raised outward
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty StartedCommandProperty = BindableProperty.Create(
        nameof(StartedCommand), typeof(ICommand), typeof(Walkthrough), null);

    public static readonly BindableProperty StepChangedCommandProperty = BindableProperty.Create(
        nameof(StepChangedCommand), typeof(ICommand), typeof(Walkthrough), null);

    public static readonly BindableProperty CompletedCommandProperty = BindableProperty.Create(
        nameof(CompletedCommand), typeof(ICommand), typeof(Walkthrough), null);

    public static readonly BindableProperty SkippedCommandProperty = BindableProperty.Create(
        nameof(SkippedCommand), typeof(ICommand), typeof(Walkthrough), null);

    public static readonly BindableProperty EndedCommandProperty = BindableProperty.Create(
        nameof(EndedCommand), typeof(ICommand), typeof(Walkthrough), null);

    /// <summary>Runs when the tour starts.</summary>
    public ICommand? StartedCommand
    {
        get => (ICommand?)this.GetValue(StartedCommandProperty);
        set => this.SetValue(StartedCommandProperty, value);
    }

    /// <summary>Runs on every move, with the step's <c>Name</c> as the parameter.</summary>
    public ICommand? StepChangedCommand
    {
        get => (ICommand?)this.GetValue(StepChangedCommandProperty);
        set => this.SetValue(StepChangedCommandProperty, value);
    }

    /// <summary>Runs when the user reaches the end.</summary>
    public ICommand? CompletedCommand
    {
        get => (ICommand?)this.GetValue(CompletedCommandProperty);
        set => this.SetValue(CompletedCommandProperty, value);
    }

    /// <summary>Runs when the user takes Skip.</summary>
    public ICommand? SkippedCommand
    {
        get => (ICommand?)this.GetValue(SkippedCommandProperty);
        set => this.SetValue(SkippedCommandProperty, value);
    }

    /// <summary>Runs however the tour ended, with the <see cref="WalkthroughEndReason"/> as the parameter.</summary>
    public ICommand? EndedCommand
    {
        get => (ICommand?)this.GetValue(EndedCommandProperty);
        set => this.SetValue(EndedCommandProperty, value);
    }
}
