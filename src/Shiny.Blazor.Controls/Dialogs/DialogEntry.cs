namespace Shiny.Blazor.Controls.Dialogs;

public sealed class DialogEntry
{
    internal DialogEntry(DialogConfig config, string id)
    {
        this.Config = config;
        this.Id = id;
        this.PromptValue = config.InitialValue ?? string.Empty;
    }

    public DialogConfig Config { get; }
    public string Id { get; }

    /// <summary>Two-way bound input value for Prompt dialogs.</summary>
    public string PromptValue { get; set; }

    internal bool IsDismissing { get; set; }
    internal TaskCompletionSource<DialogOutcome> ResultTcs { get; } = new();
    internal TaskCompletionSource RemovedTcs { get; } = new();
}

internal readonly struct DialogOutcome
{
    public DialogOutcome(bool ok, string? value)
    {
        this.Ok = ok;
        this.Value = value;
    }

    public bool Ok { get; }
    public string? Value { get; }
}
