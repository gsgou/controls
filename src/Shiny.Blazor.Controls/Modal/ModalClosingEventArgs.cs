namespace Shiny.Blazor.Controls;

/// <summary>
/// Raised by <see cref="ModalView.Closing"/> before the modal goes away, and the one chance to stop
/// it - set <see cref="Cancel"/> and the modal stays open.
/// </summary>
/// <example>
/// <code>
/// async Task OnClosing(ModalClosingEventArgs e)
/// {
///     if (e.Reason != ModalCloseReason.Button &amp;&amp; this.form.IsDirty)
///         e.Cancel = !await this.Confirm("Discard your changes?");
/// }
/// </code>
/// </example>
public sealed class ModalClosingEventArgs(ModalCloseReason reason)
{
    /// <summary>What is trying to close the modal.</summary>
    public ModalCloseReason Reason { get; } = reason;

    /// <summary>Set to true to keep the modal open.</summary>
    public bool Cancel { get; set; }
}
