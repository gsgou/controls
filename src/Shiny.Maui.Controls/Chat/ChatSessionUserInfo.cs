namespace Shiny.Maui.Controls.Chat;

public record ChatSessionUserInfo(
    string UserId,
    string DisplayName,
    ImageSource? Avatar,
    Color? BubbleColor,
    DateTimeOffset JoinedDate
);
