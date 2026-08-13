namespace Shiny.Maui.Controls;

/// <summary>The built-in progress indicators a <see cref="Wizard"/> can draw for itself.</summary>
public enum WizardProgressStyle
{
    /// <summary>Pointed breadcrumb segments, one per step, each carrying the step title.</summary>
    Chevron,

    /// <summary>A connected row of numbered markers with the step title underneath.</summary>
    Dots,

    /// <summary>A single filled bar with a "Step 2 of 5 — Title" caption.</summary>
    Bar,

    /// <summary>Draw nothing. Equivalent to <see cref="WizardProgressPosition.None"/>.</summary>
    None
}

/// <summary>Where the progress indicator sits relative to the step content.</summary>
public enum WizardProgressPosition
{
    Top,
    Bottom,
    None
}

/// <summary>Which way a step change is moving. Handed to <see cref="WizardStepChangingEventArgs"/>.</summary>
public enum WizardDirection
{
    /// <summary>Moving towards the end of the wizard.</summary>
    Forward,

    /// <summary>Moving towards the start.</summary>
    Backward,

    /// <summary>A jump that is neither (a direct <c>GoTo</c>, or the initial step).</summary>
    None
}
