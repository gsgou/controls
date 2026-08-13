using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Wizard
{
    static void Rebuild(BindableObject bindable, object oldValue, object newValue)
        => StyleGuard.WhenReady(bindable, typeof(Wizard), () => ((Wizard)bindable).Refresh());

    static void Relayout(BindableObject bindable, object oldValue, object newValue)
        => StyleGuard.WhenReady(bindable, typeof(Wizard), () => ((Wizard)bindable).BuildLayout());


    // -------------------------------------------------------------------------------------------
    // Position
    // -------------------------------------------------------------------------------------------

    public static readonly BindableProperty CurrentStepProperty = BindableProperty.Create(
        nameof(CurrentStep), typeof(string), typeof(Wizard), null, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Wizard), () =>
            ((Wizard)b).OnCurrentStepChanged((string?)o, (string?)n)));

    public static readonly BindableProperty CurrentStepIndexProperty = BindableProperty.Create(
        nameof(CurrentStepIndex), typeof(int), typeof(Wizard), -1, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Wizard), () =>
            ((Wizard)b).OnCurrentStepIndexChanged((int)n)));

    static readonly BindablePropertyKey StepCountPropertyKey = BindableProperty.CreateReadOnly(
        nameof(StepCount), typeof(int), typeof(Wizard), 0);

    static readonly BindablePropertyKey StepNumberPropertyKey = BindableProperty.CreateReadOnly(
        nameof(StepNumber), typeof(int), typeof(Wizard), 0);

    static readonly BindablePropertyKey IsFirstStepPropertyKey = BindableProperty.CreateReadOnly(
        nameof(IsFirstStep), typeof(bool), typeof(Wizard), true);

    static readonly BindablePropertyKey IsLastStepPropertyKey = BindableProperty.CreateReadOnly(
        nameof(IsLastStep), typeof(bool), typeof(Wizard), true);

    static readonly BindablePropertyKey ProgressFractionPropertyKey = BindableProperty.CreateReadOnly(
        nameof(ProgressFraction), typeof(double), typeof(Wizard), 0d);

    public static readonly BindableProperty StepCountProperty = StepCountPropertyKey.BindableProperty;
    public static readonly BindableProperty StepNumberProperty = StepNumberPropertyKey.BindableProperty;
    public static readonly BindableProperty IsFirstStepProperty = IsFirstStepPropertyKey.BindableProperty;
    public static readonly BindableProperty IsLastStepProperty = IsLastStepPropertyKey.BindableProperty;
    public static readonly BindableProperty ProgressFractionProperty = ProgressFractionPropertyKey.BindableProperty;


    // -------------------------------------------------------------------------------------------
    // Gates
    // -------------------------------------------------------------------------------------------

    public static readonly BindableProperty CanGoBackProperty = BindableProperty.Create(
        nameof(CanGoBack), typeof(bool), typeof(Wizard), true, propertyChanged: Rebuild);

    public static readonly BindableProperty CanGoNextProperty = BindableProperty.Create(
        nameof(CanGoNext), typeof(bool), typeof(Wizard), true, propertyChanged: Rebuild);

    public static readonly BindableProperty CanCancelProperty = BindableProperty.Create(
        nameof(CanCancel), typeof(bool), typeof(Wizard), true, propertyChanged: Rebuild);

    public static readonly BindableProperty AllowStepSelectionProperty = BindableProperty.Create(
        nameof(AllowStepSelection), typeof(bool), typeof(Wizard), false, propertyChanged: Rebuild);

    public static readonly BindableProperty LinearNavigationProperty = BindableProperty.Create(
        nameof(LinearNavigation), typeof(bool), typeof(Wizard), true);


    // -------------------------------------------------------------------------------------------
    // Chrome
    // -------------------------------------------------------------------------------------------

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(View), typeof(Wizard), null, propertyChanged: Relayout);

    public static readonly BindableProperty ProgressStyleProperty = BindableProperty.Create(
        nameof(ProgressStyle), typeof(WizardProgressStyle), typeof(Wizard), WizardProgressStyle.Chevron,
        propertyChanged: Relayout);

    public static readonly BindableProperty ProgressPositionProperty = BindableProperty.Create(
        nameof(ProgressPosition), typeof(WizardProgressPosition), typeof(Wizard), WizardProgressPosition.Top,
        propertyChanged: Relayout);

    public static readonly BindableProperty ProgressHeightProperty = BindableProperty.Create(
        nameof(ProgressHeight), typeof(double), typeof(Wizard), 44d, propertyChanged: Relayout);

    public static readonly BindableProperty ShowStepTitlesProperty = BindableProperty.Create(
        nameof(ShowStepTitles), typeof(bool), typeof(Wizard), true, propertyChanged: Relayout);

    public static readonly BindableProperty NavigationBarProperty = BindableProperty.Create(
        nameof(NavigationBar), typeof(View), typeof(Wizard), null, propertyChanged: Relayout);

    public static readonly BindableProperty ShowNavigationBarProperty = BindableProperty.Create(
        nameof(ShowNavigationBar), typeof(bool), typeof(Wizard), true, propertyChanged: Relayout);

    public static readonly BindableProperty ShowCancelProperty = BindableProperty.Create(
        nameof(ShowCancel), typeof(bool), typeof(Wizard), false, propertyChanged: Rebuild);

    public static readonly BindableProperty ShowBackOnFirstStepProperty = BindableProperty.Create(
        nameof(ShowBackOnFirstStep), typeof(bool), typeof(Wizard), false, propertyChanged: Rebuild);

    public static readonly BindableProperty BackTextProperty = BindableProperty.Create(
        nameof(BackText), typeof(string), typeof(Wizard), "Back", propertyChanged: Rebuild);

    public static readonly BindableProperty NextTextProperty = BindableProperty.Create(
        nameof(NextText), typeof(string), typeof(Wizard), "Next", propertyChanged: Rebuild);

    public static readonly BindableProperty FinishTextProperty = BindableProperty.Create(
        nameof(FinishText), typeof(string), typeof(Wizard), "Finish", propertyChanged: Rebuild);

    public static readonly BindableProperty CancelTextProperty = BindableProperty.Create(
        nameof(CancelText), typeof(string), typeof(Wizard), "Cancel", propertyChanged: Rebuild);

    public static readonly BindableProperty TransitionProperty = BindableProperty.Create(
        nameof(Transition), typeof(StateTransition), typeof(Wizard), StateTransition.Slide,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Wizard), () =>
            ((Wizard)b).stateView.Transition = (StateTransition)n));

    public static readonly BindableProperty TransitionDurationProperty = BindableProperty.Create(
        nameof(TransitionDuration), typeof(uint), typeof(Wizard), 220u,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Wizard), () =>
            ((Wizard)b).stateView.TransitionDuration = (uint)n));


    // -------------------------------------------------------------------------------------------
    // Commands raised outward
    // -------------------------------------------------------------------------------------------

    public static readonly BindableProperty StepChangedCommandProperty = BindableProperty.Create(
        nameof(StepChangedCommand), typeof(ICommand), typeof(Wizard), null);

    public static readonly BindableProperty FinishedCommandProperty = BindableProperty.Create(
        nameof(FinishedCommand), typeof(ICommand), typeof(Wizard), null);

    public static readonly BindableProperty CancelledCommandProperty = BindableProperty.Create(
        nameof(CancelledCommand), typeof(ICommand), typeof(Wizard), null);


    /// <summary>
    /// The <see cref="WizardStep.Name"/> on screen. Two-way: the wizard writes it back as the user
    /// moves, and assigning it navigates (subject to <c>StepChanging</c> and the step being
    /// visible and enabled — a rejected assignment is reverted).
    /// </summary>
    public string? CurrentStep
    {
        get => (string?)this.GetValue(CurrentStepProperty);
        set => this.SetValue(CurrentStepProperty, value);
    }

    /// <summary>Zero-based index of the current step among the <em>visible</em> steps. Two-way.</summary>
    public int CurrentStepIndex
    {
        get => (int)this.GetValue(CurrentStepIndexProperty);
        set => this.SetValue(CurrentStepIndexProperty, value);
    }

    /// <summary>How many steps are visible.</summary>
    public int StepCount
    {
        get => (int)this.GetValue(StepCountProperty);
        private set => this.SetValue(StepCountPropertyKey, value);
    }

    /// <summary>One-based position of the current step, for "Step 2 of 5" captions.</summary>
    public int StepNumber
    {
        get => (int)this.GetValue(StepNumberProperty);
        private set => this.SetValue(StepNumberPropertyKey, value);
    }

    public bool IsFirstStep
    {
        get => (bool)this.GetValue(IsFirstStepProperty);
        private set => this.SetValue(IsFirstStepPropertyKey, value);
    }

    public bool IsLastStep
    {
        get => (bool)this.GetValue(IsLastStepProperty);
        private set => this.SetValue(IsLastStepPropertyKey, value);
    }

    /// <summary>0..1 completion, for binding your own progress visual.</summary>
    public double ProgressFraction
    {
        get => (double)this.GetValue(ProgressFractionProperty);
        private set => this.SetValue(ProgressFractionPropertyKey, value);
    }

    /// <summary>
    /// A consumer gate on going backwards, ANDed with the wizard's own "is there a previous step".
    /// Leave it alone and Back is enabled everywhere but the first step.
    /// </summary>
    public bool CanGoBack
    {
        get => (bool)this.GetValue(CanGoBackProperty);
        set => this.SetValue(CanGoBackProperty, value);
    }

    /// <summary>
    /// A consumer gate on going forwards, ANDed with the current step's <see cref="WizardStep.IsValid"/>.
    /// </summary>
    public bool CanGoNext
    {
        get => (bool)this.GetValue(CanGoNextProperty);
        set => this.SetValue(CanGoNextProperty, value);
    }

    public bool CanCancel
    {
        get => (bool)this.GetValue(CanCancelProperty);
        set => this.SetValue(CanCancelProperty, value);
    }

    /// <summary>Let the user tap the progress indicator to jump between steps.</summary>
    public bool AllowStepSelection
    {
        get => (bool)this.GetValue(AllowStepSelectionProperty);
        set => this.SetValue(AllowStepSelectionProperty, value);
    }

    /// <summary>
    /// With step selection on, restrict taps to steps already completed (plus the current one), so the
    /// user can review but not skip ahead. Programmatic <c>GoTo</c> is never restricted by this.
    /// </summary>
    public bool LinearNavigation
    {
        get => (bool)this.GetValue(LinearNavigationProperty);
        set => this.SetValue(LinearNavigationProperty, value);
    }

    /// <summary>Replaces the built-in progress indicator with your own view.</summary>
    public View? Progress
    {
        get => (View?)this.GetValue(ProgressProperty);
        set => this.SetValue(ProgressProperty, value);
    }

    /// <summary>Which built-in indicator to draw when <see cref="Progress"/> is not set.</summary>
    public WizardProgressStyle ProgressStyle
    {
        get => (WizardProgressStyle)this.GetValue(ProgressStyleProperty);
        set => this.SetValue(ProgressStyleProperty, value);
    }

    public WizardProgressPosition ProgressPosition
    {
        get => (WizardProgressPosition)this.GetValue(ProgressPositionProperty);
        set => this.SetValue(ProgressPositionProperty, value);
    }

    /// <summary>Height reserved for the built-in indicator.</summary>
    public double ProgressHeight
    {
        get => (double)this.GetValue(ProgressHeightProperty);
        set => this.SetValue(ProgressHeightProperty, value);
    }

    /// <summary>Draw step titles on the built-in indicator. Turn off for a compact marker strip.</summary>
    public bool ShowStepTitles
    {
        get => (bool)this.GetValue(ShowStepTitlesProperty);
        set => this.SetValue(ShowStepTitlesProperty, value);
    }

    /// <summary>Replaces the built-in Back/Next bar with your own view.</summary>
    public View? NavigationBar
    {
        get => (View?)this.GetValue(NavigationBarProperty);
        set => this.SetValue(NavigationBarProperty, value);
    }

    /// <summary>Turn off when each step carries its own buttons.</summary>
    public bool ShowNavigationBar
    {
        get => (bool)this.GetValue(ShowNavigationBarProperty);
        set => this.SetValue(ShowNavigationBarProperty, value);
    }

    public bool ShowCancel
    {
        get => (bool)this.GetValue(ShowCancelProperty);
        set => this.SetValue(ShowCancelProperty, value);
    }

    /// <summary>Keep Back on screen (disabled) on the first step, so the bar does not reflow.</summary>
    public bool ShowBackOnFirstStep
    {
        get => (bool)this.GetValue(ShowBackOnFirstStepProperty);
        set => this.SetValue(ShowBackOnFirstStepProperty, value);
    }

    public string BackText
    {
        get => (string)this.GetValue(BackTextProperty);
        set => this.SetValue(BackTextProperty, value);
    }

    public string NextText
    {
        get => (string)this.GetValue(NextTextProperty);
        set => this.SetValue(NextTextProperty, value);
    }

    /// <summary>Replaces <see cref="NextText"/> on the last step.</summary>
    public string FinishText
    {
        get => (string)this.GetValue(FinishTextProperty);
        set => this.SetValue(FinishTextProperty, value);
    }

    public string CancelText
    {
        get => (string)this.GetValue(CancelTextProperty);
        set => this.SetValue(CancelTextProperty, value);
    }

    /// <summary>How step content animates. Defaults to <see cref="StateTransition.Slide"/>.</summary>
    public StateTransition Transition
    {
        get => (StateTransition)this.GetValue(TransitionProperty);
        set => this.SetValue(TransitionProperty, value);
    }

    public uint TransitionDuration
    {
        get => (uint)this.GetValue(TransitionDurationProperty);
        set => this.SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Invoked with the new step's name after every move.</summary>
    public ICommand? StepChangedCommand
    {
        get => (ICommand?)this.GetValue(StepChangedCommandProperty);
        set => this.SetValue(StepChangedCommandProperty, value);
    }

    /// <summary>Invoked when Finish is taken on the last step.</summary>
    public ICommand? FinishedCommand
    {
        get => (ICommand?)this.GetValue(FinishedCommandProperty);
        set => this.SetValue(FinishedCommandProperty, value);
    }

    public ICommand? CancelledCommand
    {
        get => (ICommand?)this.GetValue(CancelledCommandProperty);
        set => this.SetValue(CancelledCommandProperty, value);
    }
}
