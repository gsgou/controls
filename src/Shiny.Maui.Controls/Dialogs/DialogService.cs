using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Dialogs;

public sealed class DialogService(DialogOptions options) : IDialogService
{
    public async Task Alert(string title, string message, string okText = "OK", Action<DialogConfig>? configure = null)
    {
        var config = this.Build(DialogKind.Alert, title, message, okText, null, null, configure);
        await this.ShowAsync(config);
    }

    public async Task<bool> Confirm(string title, string message, string okText = "Yes", string cancelText = "No", Action<DialogConfig>? configure = null)
    {
        var config = this.Build(DialogKind.Confirm, title, message, okText, cancelText, null, configure);
        var outcome = await this.ShowAsync(config);
        return outcome.Ok;
    }

    public async Task<PromptResult> Prompt(string title, string message, string? placeholder = null, string okText = "OK", string? cancelText = "Cancel", string? initialValue = null, int? maxLength = null, Keyboard? keyboard = null, Action<DialogConfig>? configure = null)
    {
        var config = this.Build(DialogKind.Prompt, title, message, okText, cancelText, placeholder, c =>
        {
            c.InitialValue = initialValue;
            if (maxLength is > 0)
                c.MaxLength = maxLength.Value;
            if (keyboard is not null)
                c.Keyboard = keyboard;
            configure?.Invoke(c);
        });
        var outcome = await this.ShowAsync(config);
        return new PromptResult(outcome.Ok, outcome.Value);
    }

    public async Task<string?> ActionSheet(string title, IReadOnlyList<string> options, string? cancelText = "Cancel", string? destructive = null, Action<DialogConfig>? configure = null)
    {
        var config = this.Build(DialogKind.ActionSheet, title, string.Empty, "OK", cancelText, null, c =>
        {
            c.Actions = options;
            c.DestructiveAction = destructive;
            // action sheets read best sliding up from the bottom; per-call configure can still override
            c.Animation = DialogAnimation.SlideBottom;
            c.MaxWidth = 500;
            configure?.Invoke(c);
        });
        var outcome = await this.ShowAsync(config);
        return outcome.Ok ? outcome.Value : null;
    }

    DialogConfig Build(DialogKind kind, string title, string message, string okText, string? cancelText, string? placeholder, Action<DialogConfig>? configure)
    {
        var config = new DialogConfig
        {
            Kind = kind,
            Title = title,
            Message = message,
            OkText = okText,
            CancelText = cancelText,
            Placeholder = placeholder,
            Animation = options.DefaultAnimation
        };

        options.ConfigureDefaults?.Invoke(config);
        configure?.Invoke(config);
        return config;
    }

    Task<DialogOutcome> ShowAsync(DialogConfig config)
    {
        if (config.UseFeedback)
            FeedbackHelper.Execute(this, "Show", config.Title);

        return DialogManager.ShowAsync(config, options);
    }
}
