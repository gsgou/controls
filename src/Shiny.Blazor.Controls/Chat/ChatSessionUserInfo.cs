namespace Shiny.Blazor.Controls.Chat;

public record ChatSessionUserInfo(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string? BubbleColor,        // CSS color
    DateTimeOffset JoinedDate
);
