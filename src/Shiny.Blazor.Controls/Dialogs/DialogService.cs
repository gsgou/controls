namespace Shiny.Blazor.Controls.Dialogs;

public sealed class DialogService : IDialogService
{
    readonly DialogOptions options;
    readonly List<DialogEntry> activeDialogs = new();
    readonly Queue<DialogEntry> queue = new();
    bool isProcessingQueue;

    public DialogService(DialogOptions? options = null)
        => this.options = options ?? new DialogOptions();

    public event Action? OnChanged;

    /// <summary>The dialogs currently on screen (modal, so normally one).</summary>
    public IReadOnlyList<DialogEntry> ActiveDialogs => this.activeDialogs.AsReadOnly();

    public async Task Alert(string title, string message, string okText = "OK", Action<DialogConfig>? configure = null)
    {
        var entry = this.Enqueue(DialogKind.Alert, title, message, okText, null, null, configure);
        await entry.ResultTcs.Task;
    }

    public async Task<bool> Confirm(string title, string message, string okText = "Yes", string cancelText = "No", Action<DialogConfig>? configure = null)
    {
        var entry = this.Enqueue(DialogKind.Confirm, title, message, okText, cancelText, null, configure);
        var outcome = await entry.ResultTcs.Task;
        return outcome.Ok;
    }

    public async Task<PromptResult> Prompt(string title, string message, string? placeholder = null, string okText = "OK", string cancelText = "Cancel", Action<DialogConfig>? configure = null)
    {
        var entry = this.Enqueue(DialogKind.Prompt, title, message, okText, cancelText, placeholder, configure);
        var outcome = await entry.ResultTcs.Task;
        return new PromptResult(outcome.Ok, outcome.Value);
    }

    DialogEntry Enqueue(DialogKind kind, string title, string message, string okText, string? cancelText, string? placeholder, Action<DialogConfig>? configure)
    {
        var config = new DialogConfig
        {
            Kind = kind,
            Title = title,
            Message = message,
            OkText = okText,
            CancelText = cancelText,
            Placeholder = placeholder,
            Animation = this.options.DefaultAnimation
        };

        this.options.ConfigureDefaults?.Invoke(config);
        configure?.Invoke(config);

        return this.Enqueue(config);
    }

    DialogEntry Enqueue(DialogConfig config)
    {
        var entry = new DialogEntry(config, Guid.NewGuid().ToString("N"));
        this.queue.Enqueue(entry);
        _ = this.ProcessQueueAsync();
        return entry;
    }

    async Task ProcessQueueAsync()
    {
        if (this.isProcessingQueue)
            return;

        this.isProcessingQueue = true;
        try
        {
            while (this.queue.Count > 0)
            {
                var entry = this.queue.Dequeue();
                this.activeDialogs.Add(entry);
                this.OnChanged?.Invoke();

                await entry.RemovedTcs.Task;
                this.activeDialogs.Remove(entry);
                this.OnChanged?.Invoke();
            }
        }
        finally
        {
            this.isProcessingQueue = false;
        }
    }

    /// <summary>Confirm the dialog (OK button / Enter). For Prompt, returns the current input value.</summary>
    public void Confirm(DialogEntry entry)
        => this.Complete(entry, true, entry.Config.Kind == DialogKind.Prompt ? entry.PromptValue : null);

    /// <summary>Cancel the dialog (Cancel button / backdrop / Escape).</summary>
    public void Cancel(DialogEntry entry)
        => this.Complete(entry, false, null);

    void Complete(DialogEntry entry, bool ok, string? value)
    {
        if (entry.IsDismissing)
            return;

        entry.IsDismissing = true;
        entry.ResultTcs.TrySetResult(new DialogOutcome(ok, value));
        this.OnChanged?.Invoke();

        // Delay removal to allow CSS exit animation
        _ = Task.Delay(200).ContinueWith(_ => entry.RemovedTcs.TrySetResult());
    }
}
