namespace Shiny.Blazor.Controls.Chat;

public record Reaction(string UserId, string Emoji, DateTimeOffset Timestamp);
