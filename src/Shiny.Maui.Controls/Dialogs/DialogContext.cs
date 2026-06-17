using System.Windows.Input;

namespace Shiny.Maui.Controls.Dialogs;

/// <summary>
/// Binding context handed to a custom dialog <see cref="DialogOptions.ContentTemplate"/> (and used by
/// the built-in card). Bind your template to these members.
/// </summary>
public sealed class DialogContext : BindableObject
{
    public static readonly BindableProperty PromptValueProperty = BindableProperty.Create(
        nameof(PromptValue), typeof(string), typeof(DialogContext), default(string), BindingMode.TwoWay);

    public DialogContext(DialogConfig config, Action confirm, Action cancel)
    {
        this.Kind = config.Kind;
        this.Title = config.Title;
        this.Message = config.Message;
        this.OkText = config.OkText;
        this.CancelText = config.CancelText;
        this.Placeholder = config.Placeholder;
        this.PromptValue = config.InitialValue;
        this.ConfirmCommand = new Command(confirm);
        this.CancelCommand = new Command(cancel);
    }

    public DialogKind Kind { get; }
    public string Title { get; }
    public string Message { get; }
    public string OkText { get; }
    public string? CancelText { get; }
    public string? Placeholder { get; }

    public bool IsPrompt => this.Kind == DialogKind.Prompt;
    public bool HasCancel => !string.IsNullOrEmpty(this.CancelText);

    /// <summary>Two-way bound text for Prompt dialogs.</summary>
    public string? PromptValue
    {
        get => (string?)this.GetValue(PromptValueProperty);
        set => this.SetValue(PromptValueProperty, value);
    }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
}
