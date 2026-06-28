namespace Shiny.Blazor.Controls.Chat;

public record ChatSessionInfo(
    string SessionId,
    string SessionName,
    ChatSessionUserInfo[] Users,
    string[]? PermittedEmojis,
    MessageBodyPermissions BodyPermissions,
    ChatSessionPermissions Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastReadDate,
    int UnreadMessageCount
);
