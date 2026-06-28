namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// Thrown by <see cref="IChatSessionProvider.GetSessionAsync"/> when the session is missing or
/// the current user has no access. The control renders an error state.
/// </summary>
public class ChatSessionException : Exception
{
    public ChatSessionException(string message) : base(message) { }
    public ChatSessionException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown by send/edit operations when the provider REFUSES the content (too large, too many
/// images, unsupported, not permitted). The control flips the optimistic bubble to
/// <see cref="MessageStatus.Rejected"/>, surfaces <see cref="Exception.Message"/> as the reason,
/// and offers no retry.
/// </summary>
public class ChatSendRejectedException : Exception
{
    public ChatSendRejectedException(string reason, SendRejectionKind kind) : base(reason)
        => this.Kind = kind;

    public SendRejectionKind Kind { get; }
}

public enum SendRejectionKind
{
    MessageTooLarge,
    TooManyAttachments,
    AttachmentTooLarge,
    UnsupportedContent,
    NotPermitted,
    Other
}
