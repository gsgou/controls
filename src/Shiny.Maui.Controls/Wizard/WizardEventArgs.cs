namespace Shiny.Maui.Controls;

/// <summary>
/// Raised before a <see cref="Wizard"/> leaves a step. Set <see cref="Cancel"/> to keep it where it is —
/// this is the hook for validation that cannot be expressed as a <see cref="WizardStep.IsValid"/> flag.
/// </summary>
public class WizardStepChangingEventArgs(WizardStep? from, WizardStep? to, WizardDirection direction) : EventArgs
{
    /// <summary>The step being left, or null on the wizard's first step.</summary>
    public WizardStep? From { get; } = from;

    /// <summary>The step being entered.</summary>
    public WizardStep? To { get; } = to;

    public WizardDirection Direction { get; } = direction;

    /// <summary>Set to true to abandon the move.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Raised once a <see cref="Wizard"/> has settled on a new step.</summary>
public class WizardStepChangedEventArgs(WizardStep? from, WizardStep? to, WizardDirection direction) : EventArgs
{
    public WizardStep? From { get; } = from;
    public WizardStep? To { get; } = to;
    public WizardDirection Direction { get; } = direction;
}

/// <summary>
/// Raised when the last step's Finish is taken. Cancelling leaves the wizard on the final step, which
/// is what a submit that fails server-side needs.
/// </summary>
public class WizardFinishingEventArgs(WizardStep? step) : EventArgs
{
    public WizardStep? Step { get; } = step;
    public bool Cancel { get; set; }
}
