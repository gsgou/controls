namespace Shiny.Blazor.Controls;

/// <summary>What a two-pane component does when a fold divides the viewport.</summary>
/// <remarks>Mirrors <c>Shiny.Maui.Controls.FoldBehavior</c>, so the same option reads the same on both hosts.</remarks>
public enum FoldBehavior
{
    /// <summary>Ignore the fold.</summary>
    None,

    /// <summary>Line the split up with the fold so neither pane draws under the hinge. The default.</summary>
    SnapToFold
}
