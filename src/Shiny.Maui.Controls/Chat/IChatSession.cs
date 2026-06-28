namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// A live, session-scoped handle owned by the control for the lifetime of a conversation. The
/// control subscribes to the events on attach and disposes the session on detach. Events may fire
/// off the UI thread; the control marshals them.
/// </summary>
public interface IChatSession : IAsyncDisposable
{
    /// <summary>Always current — refreshed before <see cref="SessionUpdated"/> fires.</summary>
    ChatSessionInfo Info { get; }

    /// <summary>The id of the local user — drives bubble alignment and ownership checks.</summary>
    string CurrentUserId { get; }

    // ---- paging (cursor-based, stable under live inserts; pages older OR newer) ----

    /// <param name="cursorMessageId">Anchor message id; null + <see cref="MessagePageDirection.Older"/> = newest page.</param>
    Task<MessagePage> GetMessagesAsync(
        string? cursorMessageId,
        MessagePageDirection direction,
        int count,
        CancellationToken cancellationToken = default
    );

    // ---- outgoing ----

    Task<ChatMessage> SendMessageAsync(OutgoingMessage message, CancellationToken cancellationToken = default);
    Task<ChatMessage> ResendMessageAsync(string clientMessageId, CancellationToken cancellationToken = default);
    Task EditMessageAsync(string messageId, string body, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>add == true toggles the emoji on; add == false removes it. Users may hold multiple distinct reactions.</summary>
    Task ReactToMessageAsync(string messageId, string emoji, bool add, CancellationToken cancellationToken = default);

    /// <summary>Batch; the control passes only visible, not-mine, unread ids.</summary>
    Task MarkReadAsync(string[] messageIds, CancellationToken cancellationToken = default);

    // ---- session management (gated by Info.Permissions) ----

    Task ToggleTypingAsync(bool isTyping, CancellationToken cancellationToken = default);
    Task InviteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task LeaveAsync(CancellationToken cancellationToken = default);
    Task RenameAsync(string sessionName, CancellationToken cancellationToken = default);

    // ---- live (may fire off the UI thread; the control marshals) ----

    event EventHandler<ChatMessage> MessageReceived;        // includes echoes of own sends (multi-device)
    event EventHandler<MessageChanged> MessageUpdated;      // carries WHAT changed
    event EventHandler<string> MessageDeleted;              // messageId
    event EventHandler<UserTypingEvent> UserTyping;
    event EventHandler<ChatSessionUserInfo> UserJoined;
    event EventHandler<ChatSessionUserInfo> UserLeft;
    event EventHandler<ChatSessionInfo> SessionUpdated;     // name / users / permitted emojis / permissions
    event EventHandler<ChatConnectionState> ConnectionStateChanged;
}
