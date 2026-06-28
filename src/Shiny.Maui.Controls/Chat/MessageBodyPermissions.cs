namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// Text formatting the session permits in a message body. Drives the markdown composition toolbar.
/// </summary>
[Flags]
public enum MessageBodyPermissions
{
    None = 0,
    Links = 1,
    Bold = 2,
    Italics = 4,
    Underline = 8,
    Strikethrough = 16,
    Codeblocks = 32,
    All = Links | Bold | Italics | Underline | Strikethrough | Codeblocks
}
