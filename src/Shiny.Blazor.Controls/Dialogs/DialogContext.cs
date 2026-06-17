namespace Shiny.Blazor.Controls.Dialogs;

/// <summary>
/// Context handed to a custom <c>DialogHost</c> <c>Template</c> so you can fully render the dialog
/// body while the host still provides the dimmed backdrop and entry/exit animation.
/// </summary>
public sealed class DialogContext
{
    readonly DialogEntry entry;
    readonly Action confirm;
    readonly Action cancel;

    internal DialogContext(DialogEntry entry, Action confirm, Action cancel)
    {
        this.entry = entry;
        this.confirm = confirm;
        this.cancel = cancel;
    }

    public DialogConfig Config => this.entry.Config;
    public DialogKind Kind => this.entry.Config.Kind;
    public string Title => this.entry.Config.Title;
    public string Message => this.entry.Config.Message;
    public string OkText => this.entry.Config.OkText;
    public string? CancelText => this.entry.Config.CancelText;
    public string? Placeholder => this.entry.Config.Placeholder;

    public bool IsPrompt => this.entry.Config.Kind == DialogKind.Prompt;
    public bool HasCancel => !string.IsNullOrEmpty(this.entry.Config.CancelText);

    /// <summary>Two-way bindable text for Prompt dialogs.</summary>
    public string PromptValue
    {
        get => this.entry.PromptValue;
        set => this.entry.PromptValue = value;
    }

    /// <summary>Confirm the dialog (OK). For Prompt, returns the current <see cref="PromptValue"/>.</summary>
    public void Confirm() => this.confirm();

    /// <summary>Cancel the dialog.</summary>
    public void Cancel() => this.cancel();
}
