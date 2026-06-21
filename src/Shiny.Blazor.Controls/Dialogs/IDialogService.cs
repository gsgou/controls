namespace Shiny.Blazor.Controls.Dialogs;

/// <summary>
/// A service-first dialog system that emulates the browser <c>alert</c>, <c>confirm</c>, and
/// <c>prompt</c> primitives. Inject <see cref="IDialogService"/> anywhere and await a result. Requires
/// <c>AddShinyDialogs()</c> in DI and a single <c>&lt;DialogHost /&gt;</c> placed in the layout.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a message with a single dismiss button. Completes when the button is tapped.
    /// </summary>
    Task Alert(string title, string message, string okText = "OK", Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a message with confirm/cancel buttons. Returns <c>true</c> when the confirm button is
    /// tapped, <c>false</c> when cancelled (button, backdrop, or Escape).
    /// </summary>
    Task<bool> Confirm(string title, string message, string okText = "Yes", string cancelText = "No", Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a message with a text field and confirm/cancel buttons. Returns a <see cref="PromptResult"/>
    /// carrying whether the user confirmed and the entered text. <paramref name="initialValue"/> pre-fills the
    /// input, <paramref name="maxLength"/> caps the input length (use <c>null</c> for no limit), and
    /// <paramref name="inputType"/> sets the HTML input type (e.g. "text", "password", "email", "number").
    /// Pass <c>null</c> for <paramref name="cancelText"/> to hide the cancel button.
    /// </summary>
    Task<PromptResult> Prompt(string title, string message, string? placeholder = null, string okText = "OK", string? cancelText = "Cancel", string? initialValue = null, int? maxLength = null, string? inputType = null, Action<DialogConfig>? configure = null);

    /// <summary>
    /// Shows a list of options (an action sheet). Returns the chosen option's text, or <c>null</c> when
    /// cancelled (button, backdrop, or Escape). Pass <paramref name="destructive"/> (matching one of the
    /// <paramref name="options"/>) to render that option in red. Pass <c>null</c> for
    /// <paramref name="cancelText"/> to hide the cancel button entirely. Defaults to a bottom slide-up.
    /// </summary>
    Task<string?> ActionSheet(string title, IReadOnlyList<string> options, string? cancelText = "Cancel", string? destructive = null, Action<DialogConfig>? configure = null);
}
