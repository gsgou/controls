using Shiny.Maui.Controls.Chat.Internal;

namespace Shiny.Maui.Controls.Chat;

public partial class ChatView
{
    void OnRemainingItemsThresholdReached(object? sender, EventArgs e) => this.LoadOlder();

    void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        // near-top → load older history
        if (e.FirstVisibleItemIndex <= 2)
            this.LoadOlder();

        var lastIndex = this.messages.Count - 1;
        if (lastIndex < 0)
        {
            this.isNearBottom = true;
            return;
        }

        var wasNearBottom = this.isNearBottom;
        this.isNearBottom = e.LastVisibleItemIndex >= lastIndex - 1;

        if (this.isNearBottom)
        {
            if (this.unreadCount > 0)
            {
                this.unreadCount = 0;
                this.UpdateToastPill();
            }
            this.MarkVisibleRead();
        }

        if (wasNearBottom != this.isNearBottom)
        {
            this.typingBubbleHost.IsVisible = this.isNearBottom && this.typingBubbleHost.Children.Count > 0;
            this.UpdateToastPill();
        }
    }

    void OnToastPillTapped(object? sender, TappedEventArgs e)
    {
        if (this.unreadCount <= 0)
            return;

        this.unreadCount = 0;
        this.UpdateToastPill();
        this.ScrollToEnd(true);
    }

    void UpdateToastPill()
    {
        var hasUnread = this.unreadCount > 0;
        var hasTyping = !this.isNearBottom && this.ShowTypingIndicator && this.typingUsers.Count > 0;

        if (!hasUnread && !hasTyping)
        {
            this.toastPill.IsVisible = false;
            this.toastNewMessagesLabel.IsVisible = false;
            this.toastTypingLabel.IsVisible = false;
            return;
        }

        if (hasUnread)
        {
            this.toastNewMessagesLabel.Text = this.unreadCount == 1 ? "1 New Message" : $"{this.unreadCount} New Messages";
            this.toastNewMessagesLabel.IsVisible = true;
        }
        else
        {
            this.toastNewMessagesLabel.IsVisible = false;
        }

        if (hasTyping)
        {
            this.toastTypingLabel.Text = this.GetTypingText();
            this.toastTypingLabel.IsVisible = true;
        }
        else
        {
            this.toastTypingLabel.IsVisible = false;
        }

        this.toastPill.IsVisible = true;
    }

    string GetTypingText()
    {
        var names = this.typingUsers.Keys
            .Select(id => this.GetUser(id)?.DisplayName ?? "Someone")
            .ToList();

        return names.Count switch
        {
            0 => string.Empty,
            1 => $"{names[0]} is typing…",
            2 => $"{names[0]}, {names[1]} are typing…",
            3 => $"{names[0]}, {names[1]}, {names[2]} are typing…",
            _ => "Multiple users are typing…"
        };
    }

    void SyncTypingBubbles()
    {
        this.typingBubbleHost.Children.Clear();

        if (this.ShowTypingIndicator && this.typingUsers.Count > 0)
        {
            foreach (var userId in this.typingUsers.Keys)
            {
                var bubble = new ChatTypingBubbleView(this, userId);
                this.typingBubbleHost.Children.Add(bubble);
            }
            this.typingBubbleHost.IsVisible = this.isNearBottom;
        }
        else
        {
            this.typingBubbleHost.IsVisible = false;
        }

        this.UpdateToastPill();
    }

    void PerformInitialScroll()
    {
        if (this.messages.Count == 0)
            return;

        if (this.ScrollToFirstUnread && this.session?.Info.LastReadDate is DateTimeOffset last)
        {
            for (var i = 0; i < this.messages.Count; i++)
            {
                if (this.messages[i].Timestamp > last)
                {
                    this.collectionView.ScrollTo(i, position: ScrollToPosition.Start, animate: false);
                    return;
                }
            }
        }
        this.ScrollToEnd();
    }

    public void ScrollToEnd(bool animate = false)
    {
        if (this.messages.Count > 0)
            this.collectionView.ScrollTo(this.messages.Count - 1, position: ScrollToPosition.End, animate: animate);
    }

    public void ScrollToMessage(string messageId, bool animate = true)
    {
        for (var i = 0; i < this.messages.Count; i++)
        {
            if (this.messages[i].MessageId == messageId)
            {
                this.collectionView.ScrollTo(i, position: ScrollToPosition.Start, animate: animate);
                return;
            }
        }
        this.ScrollToEnd(animate);
    }
}
