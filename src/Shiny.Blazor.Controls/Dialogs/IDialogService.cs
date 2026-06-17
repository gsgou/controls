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
    /// carrying whether the user confirmed and the entered text.
    /// </summary>
    Task<PromptResult> Prompt(string title, string message, string? placeholder = null, string okText = "OK", string cancelText = "Cancel", Action<DialogConfig>? configure = null);
}
