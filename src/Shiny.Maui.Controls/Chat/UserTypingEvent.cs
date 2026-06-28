namespace Shiny.Maui.Controls.Chat;

public record UserTypingEvent(string UserId, bool IsTyping, DateTimeOffset Timestamp);
