namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// Payload for <see cref="IChatSession.MessageUpdated"/>. <see cref="Change"/> tells the control
/// what changed so it can update only the affected sub-view rather than re-rendering the bubble.
/// </summary>
public record MessageChanged(ChatMessage Message, MessageChangeKind Change);

public enum MessageChangeKind
{
    Edited,
    ReactionChanged,
    ReadReceiptChanged,
    StatusChanged
}
