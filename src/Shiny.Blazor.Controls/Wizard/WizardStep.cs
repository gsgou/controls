using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// One step of a <see cref="Wizard"/>. A <see cref="StateViewState"/> that also carries what a wizard
/// needs: a title for the progress indicator, whether the step can be left, and whether it counts
/// towards the run at all.
/// </summary>
public class WizardStep : StateViewState
{
    bool lastCompletedParameter;

    /// <summary>Shown on the progress indicator. Falls back to <see cref="StateViewState.Name"/>.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Sub-caption, shown by the <see cref="WizardProgressStyle.Bar"/> indicator.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Whether this step is part of the run at all. A hidden step is skipped by Next/Back and is not
    /// drawn on the progress indicator — this is how a branch that only applies to some inputs is
    /// modelled.
    /// </summary>
    [Parameter] public bool IsVisible { get; set; } = true;

    /// <summary>A disabled step is drawn but cannot be navigated to.</summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the step is happy to be left going forwards. Combined with <see cref="Validate"/>, and
    /// bypassed entirely when <see cref="IsOptional"/> is set.
    /// </summary>
    [Parameter] public bool IsValid { get; set; } = true;

    /// <summary>An optional step may be left even when <see cref="IsValid"/> is false.</summary>
    [Parameter] public bool IsOptional { get; set; }

    /// <summary>Whether the step has been completed. Two-way — the wizard sets it on the way forward.</summary>
    [Parameter] public bool IsCompleted { get; set; }

    [Parameter] public EventCallback<bool> IsCompletedChanged { get; set; }

    /// <summary>Overrides the wizard's Next button text while this step is showing.</summary>
    [Parameter] public string? NextText { get; set; }

    /// <summary>Overrides the wizard's Back button text while this step is showing.</summary>
    [Parameter] public string? BackText { get; set; }

    /// <summary>
    /// Run before the step is left going forwards; returning false keeps the wizard here. Async, so a
    /// server round-trip is a first-class validator rather than something bolted onto an event.
    /// </summary>
    [Parameter] public Func<Task<bool>>? Validate { get; set; }


    /// <summary>The live completion flag, which the wizard owns between parameter assignments.</summary>
    internal bool Completed { get; private set; }

    /// <summary>What the progress indicator labels this step.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(this.Title) ? (this.Name ?? string.Empty) : this.Title!;


    protected override void OnParametersSet()
    {
        // Only take the parameter when the consumer actually changed it; otherwise a re-render would
        // undo the completion the wizard just recorded.
        if (this.IsCompleted != this.lastCompletedParameter)
        {
            this.lastCompletedParameter = this.IsCompleted;
            this.Completed = this.IsCompleted;
        }

        base.OnParametersSet();
    }

    internal void SetCompleted(bool value)
    {
        if (this.Completed == value)
            return;

        this.Completed = value;
        this.lastCompletedParameter = value;
        this.IsCompleted = value;

        if (this.IsCompletedChanged.HasDelegate)
            _ = this.IsCompletedChanged.InvokeAsync(value);
    }
}
