namespace Shiny.Blazor.Controls.OnScreenKeyboard;

/// <summary>
/// Drives the on-screen keyboard's visibility from anywhere — a component, a service, a kiosk mode
/// switch. Rendering is <see cref="OnScreenKeyboardHost"/>'s job; place one of those once, near the
/// root of the layout.
/// </summary>
public interface IOnScreenKeyboardService
{
    bool IsVisible { get; }

    /// <summary>Raised whenever <see cref="IsVisible"/> changes, including from auto-show on focus.</summary>
    event EventHandler<bool>? VisibilityChanged;

    void Show();
    void Hide();
    void Toggle();
}
