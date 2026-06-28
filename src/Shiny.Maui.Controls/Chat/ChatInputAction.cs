namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// A lightweight, consumer-supplied input-bar action surfaced from the input bar's overflow
/// button. The handler receives the owning <see cref="ChatView"/> so it can read/write
/// <see cref="ChatView.EntryText"/> or call <see cref="ChatView.SubmitEntry"/>.
/// </summary>
public class ChatInputAction : BindableObject
{
    /// <summary>Display text shown in the input-bar action sheet.</summary>
    public string? Text { get; set; }

    /// <summary>Optional icon (reserved for richer renderers).</summary>
    public ImageSource? Icon { get; set; }

    /// <summary>Async handler invoked when the action is chosen.</summary>
    public Func<ChatView, Task>? Handler { get; set; }

    /// <summary>Raised when the action is invoked.</summary>
    public event EventHandler<ChatView>? Clicked;

    /// <summary>Invoked by the control when the user selects this action.</summary>
    public virtual async Task InvokeAsync(ChatView chatView)
    {
        this.Clicked?.Invoke(this, chatView);
        if (this.Handler is not null)
            await this.Handler.Invoke(chatView).ConfigureAwait(true);
    }
}
