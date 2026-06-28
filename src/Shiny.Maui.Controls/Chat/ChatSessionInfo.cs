namespace Shiny.Maui.Controls.Chat;

public record ChatSessionInfo(
    string SessionId,
    string SessionName,
    ChatSessionUserInfo[] Users,
    string[]? PermittedEmojis,                 // null => control falls back to its default set; empty => no reactions
    MessageBodyPermissions BodyPermissions,    // drives the markdown toolbar
    ChatSessionPermissions Permissions,        // drives every action affordance
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastReadDate,
    int UnreadMessageCount
);
