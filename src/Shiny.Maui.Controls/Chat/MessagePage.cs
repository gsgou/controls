namespace Shiny.Maui.Controls.Chat;

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
    /// <summary>Load history above the cursor (scroll up) — the default LoadMore path.</summary>
    Older,

    /// <summary>Load messages below the cursor — used for jump-to-first-unread then scroll down.</summary>
    Newer
}
