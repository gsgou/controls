using System.Collections.ObjectModel;
using Shiny.Maui.Controls.Chat.Internal;

namespace Shiny.Maui.Controls.Chat;

/// <summary>
/// A provider-driven chat surface. Bind <see cref="Provider"/> + <see cref="SessionId"/> and the
/// control resolves an <see cref="IChatSession"/>, subscribes to its live events, performs cursor
/// paging, renders bubbles, and disposes the session on detach. The control is styles + layout
/// only; all data and behavior live behind the provider.
/// </summary>
public partial class ChatView : ContentView
{
    /// <summary>Raised when a non-image bubble is tapped (image taps open the viewer).</summary>
    public event EventHandler<ChatMessage>? MessageTapped;

    readonly ObservableCollection<ChatMessage> messages = new();

    readonly CollectionView collectionView;
    readonly ChatInputBar inputBar;
    readonly VerticalStackLayout typingBubbleHost;
    readonly Border toastPill;
    readonly Label toastNewMessagesLabel;
    readonly Label toastTypingLabel;
    readonly Grid messageArea;
    readonly Grid rootGrid;
    readonly ImageViewer imageViewer;

    // connection banner
    readonly Border connectionBanner;
    readonly Label connectionBannerLabel;

    // loader / error overlays
    readonly Grid loaderOverlay;
    readonly VerticalStackLayout errorOverlay;
    readonly Label errorLabel;

    // session state
    IChatSession? session;

    // scroll / unread
    bool isNearBottom = true;
    int unreadCount;

    // paging
    bool isLoadingOlder;
    bool hasMoreOlder = true;
    CancellationTokenSource? loadCts;

    // typing
    readonly Dictionary<string, DateTimeOffset> typingUsers = new();
    IDispatcherTimer? typingExpiryTimer;

    // read receipts
    readonly HashSet<string> markReadRequested = new();

    // edit mode
    string? editingMessageId;

    public ChatView()
    {
        this.collectionView = new CollectionView
        {
            ItemsSource = this.messages,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 0 },
            ItemTemplate = new ChatBubbleTemplateSelector(this),
            RemainingItemsThreshold = 5
        };
        this.collectionView.RemainingItemsThresholdReached += this.OnRemainingItemsThresholdReached;
        this.collectionView.Scrolled += this.OnCollectionViewScrolled;

        // Shared toast pill — new messages on top, typing below
        this.toastNewMessagesLabel = new Label
        {
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };
        this.toastTypingLabel = new Label
        {
            TextColor = Color.FromArgb("#D0E8FF"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };
        this.toastPill = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            BackgroundColor = Color.FromArgb("#007AFF"),
            Padding = new Thickness(16, 8),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 12),
            IsVisible = false,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children = { this.toastNewMessagesLabel, this.toastTypingLabel }
            }
        };
        var pillTap = new TapGestureRecognizer();
        pillTap.Tapped += this.OnToastPillTapped;
        this.toastPill.GestureRecognizers.Add(pillTap);

        // connection banner (top)
        this.connectionBannerLabel = new Label
        {
            TextColor = Colors.White,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        this.connectionBanner = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#B00020"),
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Start,
            IsVisible = false,
            Content = this.connectionBannerLabel
        };

        // loader overlay
        this.loaderOverlay = new Grid
        {
            IsVisible = false,
            Children =
            {
                new ActivityIndicator
                {
                    IsRunning = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };

        // error overlay
        this.errorLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(24, 0)
        };
        var errorRetry = new Button { Text = "Retry" };
        errorRetry.Clicked += (_, _) => this.ReloadSession();
        this.errorOverlay = new VerticalStackLayout
        {
            IsVisible = false,
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { this.errorLabel, errorRetry }
        };

        this.messageArea = new Grid
        {
            IsClippedToBounds = true,
            Children = { this.collectionView, this.connectionBanner, this.toastPill, this.loaderOverlay, this.errorOverlay }
        };

        this.typingBubbleHost = new VerticalStackLayout { IsVisible = false, Spacing = 0 };

        this.inputBar = new ChatInputBar();
        this.inputBar.SendRequested += this.OnSendRequested;
        this.inputBar.AttachRequested += this.OnAttachRequested;
        this.inputBar.ActionsRequested += this.OnInputActionsRequested;
        this.inputBar.LinkRequested += this.OnLinkRequested;
        this.inputBar.EditCancelled += this.OnEditCancelled;

        this.imageViewer = new ImageViewer { OpenViewerOnTap = false };

        this.rootGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };
        this.rootGrid.Add(this.messageArea, 0, 0);
        this.rootGrid.Add(this.typingBubbleHost, 0, 1);
        this.rootGrid.Add(this.inputBar, 0, 2);

        // image viewer overlay spans all rows
        this.rootGrid.Add(this.imageViewer, 0, 0);
        Grid.SetRowSpan(this.imageViewer, 3);

        this.Content = this.rootGrid;

        // collections initialised so XAML can Add() directly
        this.InputActions = new ObservableCollection<ChatInputAction>();
        this.CustomBubbleActions = new ObservableCollection<ChatBubbleAction>();

        this.Loaded += (_, _) => this.OnLoaded();
        this.Unloaded += (_, _) => this.OnUnloaded();
    }

    // ------- public entry helpers -------

    /// <summary>Gets or sets the current text in the input bar entry field.</summary>
    public string EntryText
    {
        get => this.inputBar.EntryText;
        set => this.inputBar.EntryText = value;
    }

    /// <summary>Programmatically submits the current entry text as if Send was pressed.</summary>
    public void SubmitEntry()
    {
        var text = this.inputBar.EntryText?.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        this.OnSendRequested(text);
    }

    // ------- internal accessors used by bubbles -------

    internal IList<ChatMessage> Items => this.messages;
    internal ChatSessionInfo? Info => this.session?.Info;
    internal string? CurrentUserId => this.session?.CurrentUserId;
    internal bool IsMultiPerson => (this.session?.Info.Users.Length ?? 0) > 2;

    internal bool IsOwnMessage(ChatMessage message)
        => this.session is not null && message.SenderId == this.session.CurrentUserId;

    internal ChatSessionUserInfo? GetUser(string userId)
    {
        var users = this.session?.Info.Users;
        if (users is null)
            return null;

        for (var i = 0; i < users.Length; i++)
        {
            if (users[i].UserId == userId)
                return users[i];
        }
        return null;
    }

    internal void RefreshBubbles()
    {
        this.collectionView.ItemsSource = null;
        this.Dispatcher.Dispatch(() => this.collectionView.ItemsSource = this.messages);
    }

    Page? GetPage()
    {
        Element? e = this;
        while (e is not null)
        {
            if (e is Page page)
                return page;
            e = e.Parent;
        }
        return Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
    }

    /// <summary>
    /// Displays a centered grid of tappable glyphs over the chat area and returns the glyph the
    /// user selects, or null if the backdrop is tapped to cancel.
    /// </summary>
    internal Task<string?> ShowGlyphPickerAsync(IReadOnlyList<string> glyphs, int columns = 6)
    {
        var tcs = new TaskCompletionSource<string?>();

        var backdrop = new BoxView
        {
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.4),
            Opacity = 0
        };
        var backdropTap = new TapGestureRecognizer();
        backdrop.GestureRecognizers.Add(backdropTap);

        var pickerGrid = new Grid
        {
            ColumnSpacing = 4,
            RowSpacing = 4,
            Padding = new Thickness(8)
        };
        for (var c = 0; c < columns; c++)
            pickerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var rowCount = (int)Math.Ceiling(glyphs.Count / (double)columns);
        for (var r = 0; r < rowCount; r++)
            pickerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var container = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            BackgroundColor = Colors.White,
            Padding = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0,
            Scale = 0.85,
            Content = pickerGrid
        };

        void Close(string? result)
        {
            backdrop.GestureRecognizers.Remove(backdropTap);
            if (this.rootGrid.Children.Contains(backdrop))
                this.rootGrid.Children.Remove(backdrop);
            if (this.rootGrid.Children.Contains(container))
                this.rootGrid.Children.Remove(container);
            tcs.TrySetResult(result);
        }

        backdropTap.Tapped += (_, _) => Close(null);

        for (var i = 0; i < glyphs.Count; i++)
        {
            var glyph = glyphs[i];
            var label = new Label
            {
                Text = glyph,
                FontSize = 28,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var cell = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(8, 6),
                Content = label
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => Close(glyph);
            cell.GestureRecognizers.Add(tap);

            pickerGrid.Add(cell, i % columns, i / columns);
        }

        this.rootGrid.Add(backdrop, 0, 0);
        Grid.SetRowSpan(backdrop, 3);
        this.rootGrid.Add(container, 0, 0);
        Grid.SetRowSpan(container, 3);

        _ = Task.WhenAll(
            backdrop.FadeToAsync(1, 150u),
            container.FadeToAsync(1, 150u),
            container.ScaleToAsync(1, 150u, Easing.SpringOut));

        return tcs.Task;
    }

    partial void HookKeyboard();
    partial void UnhookKeyboard();

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.OldHandler != null)
            this.UnhookKeyboard();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler != null)
            this.HookKeyboard();
    }
}
