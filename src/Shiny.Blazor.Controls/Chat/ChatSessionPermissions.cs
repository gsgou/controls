namespace Shiny.Blazor.Controls.Chat;

[Flags]
public enum ChatSessionPermissions
{
    None = 0,
    CanSendMessages     = 1,
    CanEditMessages     = 2,
    CanDeleteMessages   = 4,
    CanReactToMessages  = 8,
    CanInviteUsers      = 16,
    CanLeaveSession     = 32,
    CanChangeSessionName= 64,
    CanSendImages       = 128,

    Default = CanSendMessages | CanSendImages | CanReactToMessages | CanInviteUsers | CanLeaveSession,
    All = CanSendMessages | CanEditMessages | CanDeleteMessages | CanReactToMessages
        | CanInviteUsers | CanLeaveSession | CanChangeSessionName | CanSendImages
}
