namespace Shiny.Maui.Controls.Dialogs;

/// <summary>
/// Result of <see cref="IDialogService.Prompt"/>.
/// </summary>
public sealed class PromptResult
{
    public PromptResult(bool ok, string? value)
    {
        this.Ok = ok;
        this.Value = value;
    }

    /// <summary>True when the user confirmed; false when cancelled.</summary>
    public bool Ok { get; }

    /// <summary>True when the user cancelled.</summary>
    public bool Cancelled => !this.Ok;

    /// <summary>The entered text. Null when cancelled.</summary>
    public string? Value { get; }
}

readonly struct DialogOutcome
{
    public DialogOutcome(bool ok, string? value)
    {
        this.Ok = ok;
        this.Value = value;
    }

    public bool Ok { get; }
    public string? Value { get; }
}
