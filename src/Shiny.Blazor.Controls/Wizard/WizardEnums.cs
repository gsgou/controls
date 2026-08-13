namespace Shiny.Blazor.Controls;

/// <summary>The built-in progress indicators a <see cref="Wizard"/> can draw for itself.</summary>
public enum WizardProgressStyle
{
    /// <summary>Pointed breadcrumb segments, one per step, each carrying the step title.</summary>
    Chevron,

    /// <summary>A connected row of numbered markers with the step title underneath.</summary>
    Dots,

    /// <summary>A single filled bar with a "Step 2 of 5 — Title" caption.</summary>
    Bar,

    /// <summary>Draw nothing.</summary>
    None
}

/// <summary>Where the progress indicator sits relative to the step content.</summary>
public enum WizardProgressPosition
{
    Top,
    Bottom,
    None
}

/// <summary>Which way a step change is moving.</summary>
public enum WizardDirection
{
    Forward,
    Backward,
    None
}
