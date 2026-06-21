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

    public DialogContext(DialogConfig config, Action confirm, Action cancel, Action<string> select)
    {
        this.Kind = config.Kind;
        this.Title = config.Title;
        this.Message = config.Message;
        this.OkText = config.OkText;
        this.CancelText = config.CancelText;
        this.Placeholder = config.Placeholder;
        this.PromptValue = config.InitialValue;
        this.Actions = config.Actions;
        this.DestructiveAction = config.DestructiveAction;
        this.ConfirmCommand = new Command(confirm);
        this.CancelCommand = new Command(cancel);
        this.SelectCommand = new Command<string>(select);
    }

    public DialogKind Kind { get; }
    public string Title { get; }
    public string Message { get; }
    public string OkText { get; }
    public string? CancelText { get; }
    public string? Placeholder { get; }

    /// <summary>ActionSheet options, in display order.</summary>
    public IReadOnlyList<string> Actions { get; }

    /// <summary>The option to render destructively, if any.</summary>
    public string? DestructiveAction { get; }

    public bool IsPrompt => this.Kind == DialogKind.Prompt;
    public bool IsActionSheet => this.Kind == DialogKind.ActionSheet;
    public bool HasCancel => !string.IsNullOrEmpty(this.CancelText);

    /// <summary>Two-way bound text for Prompt dialogs.</summary>
    public string? PromptValue
    {
        get => (string?)this.GetValue(PromptValueProperty);
        set => this.SetValue(PromptValueProperty, value);
    }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>Selects an ActionSheet option (pass the option text); resolves the dialog with that value.</summary>
    public ICommand SelectCommand { get; }
}
