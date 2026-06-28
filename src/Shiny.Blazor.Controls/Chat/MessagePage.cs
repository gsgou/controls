namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// A page of messages returned by <see cref="IChatSession.GetMessagesAsync"/>. Messages are
/// always chronological ascending regardless of paging direction.
/// </summary>
public record MessagePage(
    IReadOnlyList<ChatMessage> Messages,
    bool HasMore
);

public enum MessagePageDirection
{
    Older,
    Newer
}
