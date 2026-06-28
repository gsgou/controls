namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// A live, session-scoped handle owned by the control for the lifetime of a conversation. The
/// control subscribes to the events on attach and disposes the session on detach.
/// </summary>
public interface IChatSession : IAsyncDisposable
{
    /// <summary>Always current — refreshed before <see cref="SessionUpdated"/> fires.</summary>
    ChatSessionInfo Info { get; }

    /// <summary>The id of the local user — drives bubble alignment and ownership checks.</summary>
    string CurrentUserId { get; }

    Task<MessagePage> GetMessagesAsync(
        string? cursorMessageId,
        MessagePageDirection direction,
        int count,
        CancellationToken cancellationToken = default
    );

    Task<ChatMessage> SendMessageAsync(OutgoingMessage message, CancellationToken cancellationToken = default);
    Task<ChatMessage> ResendMessageAsync(string clientMessageId, CancellationToken cancellationToken = default);
    Task EditMessageAsync(string messageId, string body, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>add == true toggles the emoji on; add == false removes it. Users may hold multiple distinct reactions.</summary>
    Task ReactToMessageAsync(string messageId, string emoji, bool add, CancellationToken cancellationToken = default);

    /// <summary>Batch; the control passes only visible, not-mine, unread ids.</summary>
    Task MarkReadAsync(string[] messageIds, CancellationToken cancellationToken = default);

    Task ToggleTypingAsync(bool isTyping, CancellationToken cancellationToken = default);
    Task InviteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task LeaveAsync(CancellationToken cancellationToken = default);
    Task RenameAsync(string sessionName, CancellationToken cancellationToken = default);

    event EventHandler<ChatMessage> MessageReceived;
    event EventHandler<MessageChanged> MessageUpdated;
    event EventHandler<string> MessageDeleted;
    event EventHandler<UserTypingEvent> UserTyping;
    event EventHandler<ChatSessionUserInfo> UserJoined;
    event EventHandler<ChatSessionUserInfo> UserLeft;
    event EventHandler<ChatSessionInfo> SessionUpdated;
    event EventHandler<ChatConnectionState> ConnectionStateChanged;
}
