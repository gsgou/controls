namespace Shiny.Maui.Controls.Chat;

[Flags]
public enum ChatSessionPermissions
{
    None = 0,
    CanSendMessages     = 1,
    CanEditMessages     = 2,   // own messages only
    CanDeleteMessages   = 4,   // own messages only
    CanReactToMessages  = 8,
    CanInviteUsers      = 16,
    CanLeaveSession     = 32,
    CanChangeSessionName= 64,
    CanSendImages       = 128, // gates the gallery/camera attach affordance (independent of text send)

    Default = CanSendMessages | CanSendImages | CanReactToMessages | CanInviteUsers | CanLeaveSession,
    All = CanSendMessages | CanEditMessages | CanDeleteMessages | CanReactToMessages
        | CanInviteUsers | CanLeaveSession | CanChangeSessionName | CanSendImages
}
