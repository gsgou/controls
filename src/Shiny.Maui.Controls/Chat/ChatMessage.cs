namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// An immutable chat message supplied by an <see cref="IChatSession"/>. The control merges
/// messages by <see cref="MessageId"/>; live updates replace the instance in place.
/// </summary>
public record ChatMessage(
    string MessageId,
    string? ClientMessageId,
    string SenderId,
    string? Body,
    string? ImageUrl,
    MessageStatus Status,
    string? StatusReason,
    DateTimeOffset Timestamp,
    DateTimeOffset? EditedTimestamp,
    IReadOnlyList<Reaction> Reactions,
    IReadOnlyList<ReadReceipt> ReadReceipts,
    string? Identifier = null,
    IReadOnlyDictionary<string, string>? Metadata = null
);

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed,   // transient (network/server) — control offers retry via ResendMessageAsync
    Rejected  // provider refused the message (too big, too many images, not permitted) — NOT retryable
}
