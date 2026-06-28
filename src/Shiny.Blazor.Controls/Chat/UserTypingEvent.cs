namespace Shiny.Blazor.Controls.Chat;

public record UserTypingEvent(string UserId, bool IsTyping, DateTimeOffset Timestamp);
