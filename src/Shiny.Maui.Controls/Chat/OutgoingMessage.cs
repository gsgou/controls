namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// The payload the control hands to <see cref="IChatSession.SendMessageAsync"/>.
/// </summary>
public record OutgoingMessage(
    string? Body,                       // markdown; null when attachment-only
    OutgoingAttachment? Attachment = null,
    string ClientMessageId = ""         // control-generated; correlates the optimistic bubble with the echo
    // string? ReplyToMessageId          // FUTURE — reserved
);

/// <summary>
/// An outgoing attachment. The provider OWNS and DISPOSES <see cref="Content"/> after upload.
/// </summary>
public record OutgoingAttachment(
    ChatAttachmentKind Kind,
    Stream Content,
    string FileName,
    string ContentType
);

public enum ChatAttachmentKind
{
    Image
    // Video, Audio, File — reserved
}
