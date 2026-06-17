namespace Shiny.Maui.Controls.Dialogs;

/// <summary>
/// Global defaults and customization for <see cref="IDialogService"/>. Configure via
/// <c>UseShinyControls(c =&gt; c.ConfigureDialogs(...))</c>.
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

    /// <summary>
    /// Optional template that fully replaces the default dialog card. Its <c>BindingContext</c> is a
    /// <see cref="DialogContext"/> exposing <c>Title</c>, <c>Message</c>, <c>OkText</c>,
    /// <c>CancelText</c>, <c>Placeholder</c>, <c>IsPrompt</c>, <c>HasCancel</c>, two-way
    /// <c>PromptValue</c>, and <c>ConfirmCommand</c>/<c>CancelCommand</c>. The dimmed backdrop and
    /// entry/exit animation are still applied by the host.
    /// </summary>
    public DataTemplate? ContentTemplate { get; set; }
}
