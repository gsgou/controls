namespace Shiny.Blazor.Controls.Dialogs;

/// <summary>
/// Global defaults for <see cref="IDialogService"/>. Configure via <c>AddShinyDialogs(options =&gt; ...)</c>.
/// </summary>
public sealed class DialogOptions
{
    /// <summary>Animation applied to every dialog unless overridden per-call.</summary>
    public DialogAnimation DefaultAnimation { get; set; } = DialogAnimation.Pop;

    /// <summary>
    /// Runs against every dialog's config before the per-call <c>configure</c> delegate — use it to
    /// set app-wide colors, corner radius, backdrop opacity, etc.
    /// </summary>
    public Action<DialogConfig>? ConfigureDefaults { get; set; }
}
