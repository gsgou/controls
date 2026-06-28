namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// A lightweight, consumer-supplied bubble action appended to the built-in (permission-driven)
/// action set shown when a message bubble's action affordance is tapped. The handler receives
/// the <see cref="ChatMessage"/> the action targets.
/// </summary>
public class ChatBubbleAction : BindableObject
{
    /// <summary>Display text shown in the bubble action sheet.</summary>
    public string? Text { get; set; }

    /// <summary>Optional icon (reserved for richer renderers).</summary>
    public ImageSource? Icon { get; set; }

    /// <summary>Async handler invoked with the targeted message.</summary>
    public Func<ChatMessage, Task>? Handler { get; set; }

    /// <summary>Raised when the action is invoked.</summary>
    public event EventHandler<ChatMessage>? Clicked;

    /// <summary>Invoked by the control when the user selects this action for a message.</summary>
    public virtual async Task InvokeAsync(ChatMessage message)
    {
        this.Clicked?.Invoke(this, message);
        if (this.Handler is not null)
            await this.Handler.Invoke(message).ConfigureAwait(true);
    }
}
