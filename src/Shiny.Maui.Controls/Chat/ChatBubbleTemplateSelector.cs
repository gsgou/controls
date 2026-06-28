using Shiny.Maui.Controls.Chat.Internal;

namespace Shiny.Maui.Controls.Chat;

class ChatBubbleTemplateSelector : DataTemplateSelector
{
    readonly ChatView chatView;
    readonly DataTemplate myTemplate;
    readonly DataTemplate otherTemplate;

    public ChatBubbleTemplateSelector(ChatView chatView)
    {
        this.chatView = chatView;
        this.myTemplate = new DataTemplate(() => new ChatBubbleView(chatView, isMe: true));
        this.otherTemplate = new DataTemplate(() => new ChatBubbleView(chatView, isMe: false));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ChatMessage msg && this.chatView.IsOwnMessage(msg)
            ? this.myTemplate
            : this.otherTemplate;
}
