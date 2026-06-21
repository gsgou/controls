namespace Shiny.Maui.Controls.Dialogs;

/// <summary>
/// A service-first dialog system that emulates the classic <c>alert</c>, <c>confirm</c>, and
/// <c>prompt</c> primitives with owned (non-native), animated, themeable dialogs. Inject
/// <see cref="IDialogService"/> anywhere and await a result — the overlay auto-attaches to the current
/// page (no XAML or OverlayHost required). Registered by <c>UseShinyControls()</c>.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a message with a single dismiss button. Completes when the button is tapped.
    /// </summary>
    Task Alert(string title, string message, string okText = "OK", Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a message with confirm/cancel buttons. Returns <c>true</c> when the confirm button is
    /// tapped, <c>false</c> when cancelled (button or backdrop).
    /// </summary>
    Task<bool> Confirm(string title, string message, string okText = "Yes", string cancelText = "No", Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a message with a text field and confirm/cancel buttons. Returns a <see cref="PromptResult"/>
    /// carrying whether the user confirmed and the entered text.
    /// </summary>
    Task<PromptResult> Prompt(string title, string message, string? placeholder = null, string okText = "OK", string cancelText = "Cancel", Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a list of options (an iOS-style action sheet) with a cancel button. Returns the chosen option's
    /// text, or <c>null</c> when cancelled (button or backdrop). Pass <paramref name="destructive"/> (matching
    /// one of the <paramref name="options"/>) to render that option in red. Defaults to a bottom slide-up.
    /// </summary>
    Task<string?> ActionSheet(string title, IReadOnlyList<string> options, string cancelText = "Cancel", string? destructive = null, Action<DialogConfig>? configure = null);
}
