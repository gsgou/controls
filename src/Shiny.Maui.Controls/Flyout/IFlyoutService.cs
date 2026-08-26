namespace Shiny.Maui.Controls.Flyout;

/// <summary>
/// Drives the flyout on whichever page is showing, from code that has no reference to it — a view
/// model, a toolbar command, a shortcut handler.
/// </summary>
public interface IFlyoutService
{
    /// <summary>Raised whenever any flyout settles into a new state.</summary>
    event EventHandler<FlyoutStateChangedEventArgs>? StateChanged;

    /// <summary>The panel on that side of the current page's flyout, or null if there is not one.</summary>
    FlyoutPanel? GetPanel(FlyoutSide side = FlyoutSide.Start);

    FlyoutPanelState GetState(FlyoutSide side = FlyoutSide.Start);

    /// <summary>Expands the panel, or returns it to its <see cref="FlyoutPanel.CollapsedState"/>.</summary>
    Task ToggleAsync(FlyoutSide side = FlyoutSide.Start);

    Task SetStateAsync(FlyoutSide side, FlyoutPanelState state);
}
