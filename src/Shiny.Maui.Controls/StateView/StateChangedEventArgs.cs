namespace Shiny.Maui.Controls;

/// <summary>Raised by <see cref="StateView.StateChanged"/> once the new state is on screen.</summary>
public class StateChangedEventArgs(string? previousState, string? currentState) : EventArgs
{
    public string? PreviousState { get; } = previousState;
    public string? CurrentState { get; } = currentState;
}
