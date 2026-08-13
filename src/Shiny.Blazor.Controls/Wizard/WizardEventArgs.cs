namespace Shiny.Blazor.Controls;

/// <summary>
/// Passed to <see cref="Wizard.StepChanging"/> before a step is left. Set <see cref="Cancel"/> to keep
/// the wizard where it is — the hook for validation that a simple <c>IsValid</c> flag cannot express.
/// </summary>
public class WizardStepChangingArgs(WizardStep? from, WizardStep? to, WizardDirection direction)
{
    public WizardStep? From { get; } = from;
    public WizardStep? To { get; } = to;
    public WizardDirection Direction { get; } = direction;

    /// <summary>Set to true to abandon the move.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Passed to <see cref="Wizard.StepChanged"/> once the wizard has settled on a new step.</summary>
public class WizardStepChangedArgs(WizardStep? from, WizardStep? to, WizardDirection direction)
{
    public WizardStep? From { get; } = from;
    public WizardStep? To { get; } = to;
    public WizardDirection Direction { get; } = direction;
}

/// <summary>
/// Passed to <see cref="Wizard.Finishing"/>. Cancelling leaves the wizard on the final step, which is
/// what a submit that fails server-side needs.
/// </summary>
public class WizardFinishingArgs(WizardStep? step)
{
    public WizardStep? Step { get; } = step;
    public bool Cancel { get; set; }
}
