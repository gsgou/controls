namespace Shiny.Maui.Controls.Flyout;

/// <summary>Raised after a panel has settled into a new state.</summary>
public sealed class FlyoutStateChangedEventArgs(FlyoutSide side, FlyoutPanelState oldState, FlyoutPanelState newState) : EventArgs
{
    public FlyoutSide Side { get; } = side;
    public FlyoutPanelState OldState { get; } = oldState;
    public FlyoutPanelState NewState { get; } = newState;
}
