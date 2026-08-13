using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// A multi-step flow built on the same registration model as <see cref="StateView"/>: the steps are
/// named branches, and the wizard adds an order, a progress indicator, a Back/Next bar that knows
/// where it is, and a validation gate on leaving a step.
/// </summary>
/// <example>
/// <code>
/// &lt;Wizard @@bind-CurrentStep="step" ShowCancel="true" Finished="SubmitAsync"&gt;
///     &lt;Steps&gt;
///         &lt;WizardStep Name="Cart" Title="Cart"&gt;…&lt;/WizardStep&gt;
///         &lt;WizardStep Name="Pay" Title="Payment" IsValid="@@cardValid"&gt;…&lt;/WizardStep&gt;
///     &lt;/Steps&gt;
/// &lt;/Wizard&gt;
/// </code>
/// </example>
public partial class Wizard
{
    readonly List<WizardStep> steps = new();

    WizardStep? current;
    int direction = 1;
    bool resolved;

    /// <summary>The steps, as markup. Children are <c>WizardStep</c> components.</summary>
    [Parameter] public RenderFragment? Steps { get; set; }

    /// <summary>Alias for <see cref="Steps"/>, so steps can be declared without the wrapper tag.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Replaces the built-in progress indicator with your own markup.</summary>
    [Parameter] public RenderFragment? Progress { get; set; }

    /// <summary>Replaces the built-in Back/Next bar with your own markup.</summary>
    [Parameter] public RenderFragment? NavigationBar { get; set; }


    // ---------------------------------------------------------------------------------------------
    // Position
    // ---------------------------------------------------------------------------------------------

    /// <summary>The <c>Name</c> of the step on screen. Two-way.</summary>
    [Parameter] public string? CurrentStep { get; set; }

    [Parameter] public EventCallback<string?> CurrentStepChanged { get; set; }

    /// <summary>Zero-based index of the current step among the visible steps. Two-way.</summary>
    [Parameter] public int CurrentStepIndex { get; set; } = -1;

    [Parameter] public EventCallback<int> CurrentStepIndexChanged { get; set; }


    // ---------------------------------------------------------------------------------------------
    // Gates
    // ---------------------------------------------------------------------------------------------

    /// <summary>A consumer gate on going backwards, ANDed with "is there a previous step".</summary>
    [Parameter] public bool CanGoBack { get; set; } = true;

    /// <summary>A consumer gate on going forwards, ANDed with the current step's validity.</summary>
    [Parameter] public bool CanGoNext { get; set; } = true;

    [Parameter] public bool CanCancel { get; set; } = true;

    /// <summary>Let the user click the progress indicator to jump between steps.</summary>
    [Parameter] public bool AllowStepSelection { get; set; }

    /// <summary>
    /// With step selection on, restrict clicks to steps already completed (plus the current one), so
    /// the user can review but not skip ahead. Programmatic <c>GoToAsync</c> is never restricted.
    /// </summary>
    [Parameter] public bool LinearNavigation { get; set; } = true;


    // ---------------------------------------------------------------------------------------------
    // Chrome
    // ---------------------------------------------------------------------------------------------

    [Parameter] public WizardProgressStyle ProgressStyle { get; set; } = WizardProgressStyle.Chevron;

    [Parameter] public WizardProgressPosition ProgressPosition { get; set; } = WizardProgressPosition.Top;

    /// <summary>Draw step titles on the built-in indicator. Turn off for a compact marker strip.</summary>
    [Parameter] public bool ShowStepTitles { get; set; } = true;

    /// <summary>Turn off when each step carries its own buttons.</summary>
    [Parameter] public bool ShowNavigationBar { get; set; } = true;

    [Parameter] public bool ShowCancel { get; set; }

    /// <summary>Keep Back on screen (disabled) on the first step, so the bar does not reflow.</summary>
    [Parameter] public bool ShowBackOnFirstStep { get; set; }

    [Parameter] public string BackText { get; set; } = "Back";

    [Parameter] public string NextText { get; set; } = "Next";

    /// <summary>Replaces <see cref="NextText"/> on the last step.</summary>
    [Parameter] public string FinishText { get; set; } = "Finish";

    [Parameter] public string CancelText { get; set; } = "Cancel";

    /// <summary>How step content animates in. Defaults to <see cref="StateTransition.Slide"/>.</summary>
    [Parameter] public StateTransition Transition { get; set; } = StateTransition.Slide;

    [Parameter] public int TransitionDuration { get; set; } = 220;

    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    // A splat clobbers attributes the component wrote itself, so a consumer passing style= would drop
    // the transition-duration custom property. Pull class/style out of the splat and merge them.
    IDictionary<string, object>? ExtraAttributes { get; set; }
    string? UserClass { get; set; }
    string? UserStyle { get; set; }


    // ---------------------------------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------------------------------

    /// <summary>Raised before a step is left. Set <c>Cancel</c> on the args to stay put.</summary>
    [Parameter] public EventCallback<WizardStepChangingArgs> StepChanging { get; set; }

    /// <summary>Raised once the new step is on screen.</summary>
    [Parameter] public EventCallback<WizardStepChangedArgs> StepChanged { get; set; }

    /// <summary>Raised when Finish is taken. Set <c>Cancel</c> on the args to stay on the last step.</summary>
    [Parameter] public EventCallback<WizardFinishingArgs> Finishing { get; set; }

    /// <summary>Raised after <see cref="Finishing"/> was not cancelled.</summary>
    [Parameter] public EventCallback Finished { get; set; }

    /// <summary>Raised when the run is abandoned.</summary>
    [Parameter] public EventCallback Cancelled { get; set; }


    // ---------------------------------------------------------------------------------------------
    // Read-only surface
    // ---------------------------------------------------------------------------------------------

    /// <summary>The step on screen, or null before the wizard has settled on one.</summary>
    public WizardStep? CurrentStepItem => this.current;

    /// <summary>Only the steps that count — visible ones, in order.</summary>
    public IReadOnlyList<WizardStep> VisibleSteps => this.Visible();

    public int StepCount => this.Visible().Count;

    /// <summary>One-based position of the current step, for "Step 2 of 5" captions.</summary>
    public int StepNumber => this.CurrentStepIndex + 1;

    public bool IsFirstStep => this.CurrentStepIndex <= 0;

    public bool IsLastStep
    {
        get
        {
            var visible = this.Visible();
            return this.CurrentStepIndex < 0 || this.CurrentStepIndex == visible.Count - 1;
        }
    }

    /// <summary>0..1 completion.</summary>
    public double ProgressFraction
    {
        get
        {
            var visible = this.Visible();
            return visible.Count == 0 ? 0d : (this.CurrentStepIndex + 1) / (double)visible.Count;
        }
    }


    // ---------------------------------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Validate the current step and move forward — or, on the last step, finish. Returns false when a
    /// gate, the step's validity, or a cancelled <see cref="StepChanging"/> stopped the move.
    /// </summary>
    public async Task<bool> GoNextAsync()
    {
        if (this.current == null || !this.CanGoNext)
            return false;

        if (!await this.ValidateCurrentAsync().ConfigureAwait(true))
            return false;

        var next = this.Adjacent(this.current, forward: true);
        if (next == null)
            return await this.FinishAsync().ConfigureAwait(true);

        return await this.NavigateAsync(next, WizardDirection.Forward, complete: true).ConfigureAwait(true);
    }

    /// <summary>Move back a step.</summary>
    public async Task<bool> GoBackAsync()
    {
        if (this.current == null || !this.CanGoBack)
            return false;

        var previous = this.Adjacent(this.current, forward: false);
        return previous != null &&
               await this.NavigateAsync(previous, WizardDirection.Backward, complete: false).ConfigureAwait(true);
    }

    /// <summary>Jump to a named step. Not subject to <see cref="LinearNavigation"/>.</summary>
    public async Task<bool> GoToAsync(string name)
    {
        var target = this.Find(name);
        return target != null &&
               await this.NavigateAsync(target, this.DirectionTo(target), complete: false).ConfigureAwait(true);
    }

    /// <summary>Jump to a step by its zero-based index among the visible steps.</summary>
    public async Task<bool> GoToAsync(int visibleIndex)
    {
        var visible = this.Visible();
        if (visibleIndex < 0 || visibleIndex >= visible.Count)
            return false;

        var target = visible[visibleIndex];
        return target.IsEnabled &&
               await this.NavigateAsync(target, this.DirectionTo(target), complete: false).ConfigureAwait(true);
    }

    /// <summary>Take the finish. Raises <see cref="Finishing"/> (cancellable) and then <see cref="Finished"/>.</summary>
    public async Task<bool> FinishAsync()
    {
        var args = new WizardFinishingArgs(this.current);
        if (this.Finishing.HasDelegate)
            await this.Finishing.InvokeAsync(args).ConfigureAwait(true);

        if (args.Cancel)
            return false;

        this.current?.SetCompleted(true);
        this.StateHasChanged();

        if (this.Finished.HasDelegate)
            await this.Finished.InvokeAsync().ConfigureAwait(true);

        return true;
    }

    /// <summary>Abandon the run.</summary>
    public async Task CancelAsync()
    {
        if (!this.CanCancel)
            return;

        if (this.Cancelled.HasDelegate)
            await this.Cancelled.InvokeAsync().ConfigureAwait(true);
    }

    /// <summary>Clear every step's completion and return to the first one.</summary>
    public async Task ResetAsync()
    {
        foreach (var step in this.steps)
            step.SetCompleted(false);

        var first = this.Visible().FirstOrDefault(s => s.IsEnabled);
        if (first != null)
            await this.NavigateAsync(first, WizardDirection.Backward, complete: false).ConfigureAwait(true);

        this.StateHasChanged();
    }


    async Task<bool> ValidateCurrentAsync()
    {
        var step = this.current;
        if (step == null)
            return true;

        if (step.IsOptional)
            return true;

        if (step.Validate != null && !await step.Validate().ConfigureAwait(true))
            return false;

        return step.IsValid;
    }

    async Task<bool> NavigateAsync(WizardStep target, WizardDirection wizardDirection, bool complete)
    {
        if (ReferenceEquals(target, this.current))
            return true;

        if (!target.IsVisible || !target.IsEnabled)
            return false;

        var from = this.current;
        var args = new WizardStepChangingArgs(from, target, wizardDirection);
        if (this.StepChanging.HasDelegate)
            await this.StepChanging.InvokeAsync(args).ConfigureAwait(true);

        if (args.Cancel)
            return false;

        if (complete)
            from?.SetCompleted(true);

        this.direction = wizardDirection == WizardDirection.Backward ? -1 : 1;
        this.current = target;
        await this.PublishPositionAsync().ConfigureAwait(true);
        this.StateHasChanged();

        if (this.StepChanged.HasDelegate)
            await this.StepChanged.InvokeAsync(new WizardStepChangedArgs(from, target, wizardDirection)).ConfigureAwait(true);

        return true;
    }

    async Task SelectAsync(int visibleIndex)
    {
        if (!this.AllowStepSelection)
            return;

        var visible = this.Visible();
        if (visibleIndex < 0 || visibleIndex >= visible.Count)
            return;

        var target = visible[visibleIndex];
        if (this.LinearNavigation && !target.Completed && !ReferenceEquals(target, this.current))
            return;

        await this.NavigateAsync(target, this.DirectionTo(target), complete: false).ConfigureAwait(true);
    }


    // ---------------------------------------------------------------------------------------------
    // Host
    // ---------------------------------------------------------------------------------------------

    void IStateViewHost.RegisterState(StateViewState state)
    {
        if (state is not WizardStep step || this.steps.Contains(step))
            return;

        this.steps.Add(step);
        this.QueueResolve();
    }

    void IStateViewHost.UnregisterState(StateViewState state)
    {
        if (state is not WizardStep step || !this.steps.Remove(step))
            return;

        if (ReferenceEquals(step, this.current))
            this.current = null;

        this.QueueResolve();
    }

    void IStateViewHost.NotifyStateChanged(StateViewState state) => this.QueueResolve();

    void QueueResolve() =>
        // Steps register while this component is already rendering them, so anything their arrival
        // changes cannot be shown in this pass - queue another one.
        _ = this.InvokeAsync(async () =>
        {
            await this.ResolveAsync().ConfigureAwait(true);
            this.StateHasChanged();
        });

    protected override async Task OnParametersSetAsync()
    {
        this.ExtraAttributes = LayoutAttributes.Split(this.AdditionalAttributes, out var userClass, out var userStyle);
        this.UserClass = userClass;
        this.UserStyle = userStyle;

        // A CurrentStep pushed in from outside is a navigation request; honour it through the same
        // pipeline so StepChanging still gets a say.
        if (this.resolved && this.current != null && !string.Equals(this.CurrentStep, this.current.Name, StringComparison.OrdinalIgnoreCase))
        {
            var target = this.Find(this.CurrentStep);
            if (target != null && target.IsEnabled)
            {
                await this.NavigateAsync(target, this.DirectionTo(target), complete: false).ConfigureAwait(true);
                return;
            }
        }

        await this.ResolveAsync().ConfigureAwait(true);
    }

    async Task ResolveAsync()
    {
        var visible = this.Visible();

        // A step can be hidden or disabled out from under the wizard; land somewhere sensible rather
        // than showing a step that is no longer part of the run.
        if (this.current != null && (!this.current.IsVisible || !this.current.IsEnabled))
            this.current = null;

        this.current ??= this.Find(this.CurrentStep) ?? visible.FirstOrDefault(s => s.IsEnabled);

        if (this.current != null)
            this.resolved = true;

        await this.PublishPositionAsync().ConfigureAwait(true);
    }

    async Task PublishPositionAsync()
    {
        var visible = this.Visible();
        var index = this.current == null ? -1 : visible.IndexOf(this.current);

        if (!string.Equals(this.CurrentStep, this.current?.Name, StringComparison.Ordinal))
        {
            this.CurrentStep = this.current?.Name;
            if (this.CurrentStepChanged.HasDelegate)
                await this.CurrentStepChanged.InvokeAsync(this.CurrentStep).ConfigureAwait(true);
        }

        if (this.CurrentStepIndex != index)
        {
            this.CurrentStepIndex = index;
            if (this.CurrentStepIndexChanged.HasDelegate)
                await this.CurrentStepIndexChanged.InvokeAsync(index).ConfigureAwait(true);
        }
    }


    // ---------------------------------------------------------------------------------------------
    // Lookups + rendering
    // ---------------------------------------------------------------------------------------------

    List<WizardStep> Visible()
    {
        var result = new List<WizardStep>(this.steps.Count);
        foreach (var step in this.steps)
        {
            if (step.IsVisible)
                result.Add(step);
        }
        return result;
    }

    WizardStep? Find(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var step in this.steps)
        {
            if (step.IsVisible && string.Equals(step.Name, name, StringComparison.OrdinalIgnoreCase))
                return step;
        }
        return null;
    }

    WizardStep? Adjacent(WizardStep from, bool forward)
    {
        var visible = this.Visible();
        var index = visible.IndexOf(from);
        if (index < 0)
            return null;

        var step = forward ? 1 : -1;
        for (var i = index + step; i >= 0 && i < visible.Count; i += step)
        {
            if (visible[i].IsEnabled)
                return visible[i];
        }
        return null;
    }

    WizardDirection DirectionTo(WizardStep target)
    {
        if (this.current == null)
            return WizardDirection.None;

        var visible = this.Visible();
        var fromIndex = visible.IndexOf(this.current);
        var toIndex = visible.IndexOf(target);
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
            return WizardDirection.None;

        return toIndex > fromIndex ? WizardDirection.Forward : WizardDirection.Backward;
    }

    bool IsBackEnabled => this.CanGoBack && this.current != null && this.Adjacent(this.current, forward: false) != null;

    bool IsNextEnabled => this.current != null && this.CanGoNext && (this.current.IsOptional || this.current.IsValid);

    bool ShowProgress => this.ProgressPosition != WizardProgressPosition.None &&
                         (this.Progress != null || this.ProgressStyle != WizardProgressStyle.None);

    string BackLabel => this.current?.BackText ?? this.BackText;

    string NextLabel => this.current?.NextText ?? (this.IsLastStep ? this.FinishText : this.NextText);

    string Caption => this.current == null
        ? string.Empty
        : $"Step {this.StepNumber} of {this.StepCount} — {this.current.DisplayTitle}";

    double ProgressPercent => Math.Round(this.ProgressFraction * 100, 2);

    bool IsSelectable(WizardStep step, int position)
    {
        if (!this.AllowStepSelection || !step.IsEnabled)
            return false;

        return !this.LinearNavigation || step.Completed || position == this.CurrentStepIndex;
    }

    string StateClass(WizardStep step, int position)
    {
        if (position == this.CurrentStepIndex)
            return "is-current";

        if (step.Completed)
            return "is-complete";

        return step.IsEnabled ? "is-upcoming" : "is-upcoming is-disabled";
    }

    string RootStyle => LayoutAttributes.Append(
        $"--shiny-wizard-duration:{Math.Max(0, this.TransitionDuration)}ms;",
        this.UserStyle
    );

    string PaneClass => this.TransitionDuration <= 0
        ? string.Empty
        : this.Transition switch
        {
            StateTransition.Fade => "is-fade",
            StateTransition.Slide => this.direction >= 0 ? "is-slide-left" : "is-slide-right",
            StateTransition.SlideLeft => "is-slide-left",
            StateTransition.SlideRight => "is-slide-right",
            StateTransition.SlideUp => "is-slide-up",
            StateTransition.SlideDown => "is-slide-down",
            StateTransition.Scale => "is-scale",
            _ => string.Empty
        };
}
