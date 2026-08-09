using Shiny.Maui.Controls.Chat.Internal;

namespace Shiny.Maui.Controls.Chat;

public partial class ChatView
{
    void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        var lastIndex = this.messages.Count - 1;
        if (lastIndex < 0)
        {
            this.isNearBottom = true;
            this.SyncScrollMode();
            return;
        }

        // near-top → load older history. Driven off the scroll position rather than
        // RemainingItemsThresholdReached, which fires at the *end* of the list.
        if (e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex <= 2)
            this.LoadOlder();

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
            this.SyncScrollMode();
            this.typingBubbleHost.IsVisible = this.isNearBottom && this.typingBubbleHost.Children.Count > 0;
            this.UpdateToastPill();
        }
    }

    /// <summary>
    /// Chooses how the CollectionView reacts to its own collection changes. Pinning to the last item
    /// is what makes new messages land in view - the handler runs the scroll after the layout pass, so
    /// unlike ScrollTo it cannot fire against an unmeasured item. It is switched off while the user is
    /// reading back through history, or while older messages are being prepended.
    /// </summary>
    void SyncScrollMode()
    {
        var mode = this.isNearBottom && !this.isLoadingOlder
            ? ItemsUpdatingScrollMode.KeepLastItemInView
            : ItemsUpdatingScrollMode.KeepScrollOffset;

        if (this.collectionView.ItemsUpdatingScrollMode != mode)
            this.collectionView.ItemsUpdatingScrollMode = mode;
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
                    // Landing mid-history means the user is not at the bottom, so incoming
                    // messages must queue behind the unread pill instead of yanking the view.
                    this.isNearBottom = i >= this.messages.Count - 2;
                    this.SyncScrollMode();
                    this.ScrollWhenMeasured(i, ScrollToPosition.Start, animate: false);
                    return;
                }
            }
        }
        this.ScrollToEnd();
    }

    /// <summary>Scrolls to the newest message and re-arms auto-follow.</summary>
    public void ScrollToEnd(bool animate = false)
    {
        this.isNearBottom = true;
        this.SyncScrollMode();
        this.ScrollWhenMeasured(this.messages.Count - 1, ScrollToPosition.End, animate);
    }

    /// <summary>Scrolls the given message into view, or to the end when it is not loaded.</summary>
    public void ScrollToMessage(string messageId, bool animate = true)
    {
        for (var i = 0; i < this.messages.Count; i++)
        {
            if (this.messages[i].MessageId == messageId)
            {
                this.isNearBottom = i >= this.messages.Count - 2;
                this.SyncScrollMode();
                this.ScrollWhenMeasured(i, ScrollToPosition.Start, animate);
                return;
            }
        }
        this.ScrollToEnd(animate);
    }

    /// <summary>
    /// ScrollTo is dropped on the floor when the target item has not been measured yet, which is the
    /// normal state for a frame or two after the items source is populated or replaced - the single
    /// biggest reason a chat opens somewhere other than the bottom. Re-issue across the next couple of
    /// layout passes; repeats are no-ops once the first one lands.
    /// </summary>
    void ScrollWhenMeasured(int index, ScrollToPosition position, bool animate)
    {
        if (index < 0)
            return;

        var attempt = 0;

        void Try()
        {
            if (index >= this.messages.Count)
                return;

            this.collectionView.ScrollTo(index, position: position, animate: animate && attempt == 0);

            if (++attempt < 3)
                this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), Try);
        }

        this.Dispatcher.Dispatch(Try);
    }
}
