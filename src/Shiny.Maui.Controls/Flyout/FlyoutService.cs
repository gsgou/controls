namespace Shiny.Maui.Controls.Flyout;

/// <inheritdoc cref="IFlyoutService"/>
public class FlyoutService : IFlyoutService
{
    public event EventHandler<FlyoutStateChangedEventArgs>? StateChanged
    {
        add => FlyoutRegistry.StateChanged += value;
        remove => FlyoutRegistry.StateChanged -= value;
    }

    public FlyoutPanel? GetPanel(FlyoutSide side = FlyoutSide.Start)
        => FlyoutRegistry.Current()?.GetPanel(side);

    public FlyoutPanelState GetState(FlyoutSide side = FlyoutSide.Start)
        => FlyoutRegistry.Current()?.GetState(side) ?? FlyoutPanelState.Hidden;

    public Task ToggleAsync(FlyoutSide side = FlyoutSide.Start)
        => FlyoutRegistry.Current()?.ToggleAsync(side) ?? Task.CompletedTask;

    public Task SetStateAsync(FlyoutSide side, FlyoutPanelState state)
        => FlyoutRegistry.Current()?.SetStateAsync(side, state) ?? Task.CompletedTask;
}
