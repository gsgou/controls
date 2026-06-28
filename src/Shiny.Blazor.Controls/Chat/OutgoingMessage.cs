namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// The payload the control hands to <see cref="IChatSession.SendMessageAsync"/>.
/// </summary>
public record OutgoingMessage(
    string? Body,
    OutgoingAttachment? Attachment = null,
    string ClientMessageId = ""
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
}
