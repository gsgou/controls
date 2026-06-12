using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Chat.Internal;

class ChatInputBar : ContentView
{
    readonly BorderlessEntry entry;
    readonly Button sendButton;
    readonly Button attachButton;
    readonly Button toolsButton;
    readonly Grid rootGrid;
    readonly BoxView separator;

    public event Action<string>? SendRequested;
    public event Action? AttachRequested;
    public event Action? ToolsRequested;

    public ChatInputBar()
    {
        entry = new BorderlessEntry
        {
            Placeholder = "Type a message...",
            FontSize = 15,
            ReturnType = ReturnType.Send,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(4, 0)
        };
        entry.Completed += OnEntryCompleted;

        sendButton = new Button
        {
            Text = "Send",
            FontSize = 14,
            CornerRadius = 18,
            HeightRequest = 36,
            Padding = new Thickness(16, 0),
            VerticalOptions = LayoutOptions.Center
        };
        // Theme defaults \u2014 overridden by ChatView.SendButton* properties when set.
        sendButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnPrimary);
        sendButton.SetDynamicResource(Button.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        sendButton.Clicked += OnSendClicked;

        attachButton = new Button
        {
            Text = "+",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        attachButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.Primary);
        attachButton.Clicked += OnAttachClicked;

        toolsButton = new Button
        {
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            Text = "\u2026",
            FontSize = 18,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        toolsButton.SetDynamicResource(Button.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        toolsButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnPrimary);
        toolsButton.Clicked += OnToolsClicked;

        rootGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 4,
            Padding = new Thickness(8, 6)
        };
        // Input bar background \u2014 overridden by ChatView.InputBarBackgroundColor when set.
        rootGrid.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Surface);

        // Top border line
        separator = new BoxView
        {
            HeightRequest = 0.5,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(-8, -6, -8, 0)
        };
        // Input bar separator \u2014 overridden by ChatView.InputBarBorderColor when set.
        separator.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.OutlineVariant);

        var wrapper = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };

        rootGrid.Add(toolsButton, 0, 0);
        rootGrid.Add(attachButton, 1, 0);
        rootGrid.Add(entry, 2, 0);
        rootGrid.Add(sendButton, 3, 0);

        wrapper.Add(separator, 0, 0);
        wrapper.Add(rootGrid, 0, 1);

        Content = wrapper;
    }

    public string PlaceholderText
    {
        get => entry.Placeholder ?? string.Empty;
        set => entry.Placeholder = value;
    }

    public string SendButtonText
    {
        get => sendButton.Text ?? string.Empty;
        set => sendButton.Text = value;
    }

    public Color SendButtonBackgroundColor
    {
        get => sendButton.BackgroundColor;
        set => sendButton.BackgroundColor = value;
    }

    public Color SendButtonTextColor
    {
        get => sendButton.TextColor;
        set => sendButton.TextColor = value;
    }

    public Color BarBackgroundColor
    {
        get => rootGrid.BackgroundColor;
        set => rootGrid.BackgroundColor = value;
    }

    public Color BarBorderColor
    {
        get => separator.Color;
        set => separator.Color = value;
    }

    public bool ShowAttachButton
    {
        get => attachButton.IsVisible;
        set => attachButton.IsVisible = value;
    }

    public bool ShowToolsButton
    {
        get => toolsButton.IsVisible;
        set => toolsButton.IsVisible = value;
    }

    public Color ToolsButtonBackgroundColor
    {
        get => toolsButton.BackgroundColor;
        set => toolsButton.BackgroundColor = value;
    }

    public string? ToolsButtonText
    {
        get => toolsButton.Text;
        set => toolsButton.Text = value;
    }

    public ImageSource? ToolsButtonIcon
    {
        get => toolsButton.ImageSource;
        set => toolsButton.ImageSource = value;
    }

    public string EntryText
    {
        get => entry.Text ?? string.Empty;
        set => entry.Text = value;
    }

    public void ClearText() => entry.Text = string.Empty;

    void OnEntryCompleted(object? sender, EventArgs e)
    {
        TrySend();
    }

    void OnSendClicked(object? sender, EventArgs e)
    {
        TrySend();
    }

    void OnAttachClicked(object? sender, EventArgs e)
    {
        AttachRequested?.Invoke();
    }

    void OnToolsClicked(object? sender, EventArgs e)
    {
        ToolsRequested?.Invoke();
    }

    void TrySend()
    {
        var text = entry.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            SendRequested?.Invoke(text);
            ClearText();
        }
    }
}
