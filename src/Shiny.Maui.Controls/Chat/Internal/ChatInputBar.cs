using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Chat.Internal;

class ChatInputBar : ContentView
{
    readonly BorderlessEntry entry;
    readonly Button sendButton;
    readonly Button attachButton;
    readonly Button actionsButton;
    readonly Grid rootGrid;
    readonly BoxView separator;
    readonly HorizontalStackLayout markdownToolbar;
    readonly Border editBanner;
    readonly Label editBannerLabel;

    string defaultSendText = "Send";

    public event Action<string>? SendRequested;
    public event Action? AttachRequested;
    public event Action? ActionsRequested;
    public event Action? LinkRequested;
    public event Action? EditCancelled;

    public ChatInputBar()
    {
        this.entry = new BorderlessEntry
        {
            Placeholder = "Type a message...",
            FontSize = 15,
            ReturnType = ReturnType.Send,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(4, 0)
        };
        this.entry.Completed += this.OnEntryCompleted;

        this.sendButton = new Button
        {
            Text = "Send",
            FontSize = 14,
            CornerRadius = 18,
            HeightRequest = 36,
            Padding = new Thickness(16, 0),
            VerticalOptions = LayoutOptions.Center
        };
        this.sendButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnPrimary);
        this.sendButton.SetDynamicResource(Button.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        this.sendButton.Clicked += this.OnSendClicked;

        this.attachButton = new Button
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
        this.attachButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.Primary);
        this.attachButton.Clicked += (_, _) => this.AttachRequested?.Invoke();

        this.actionsButton = new Button
        {
            Text = "⋯",
            FontSize = 20,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 40,
            HeightRequest = 44,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        this.actionsButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.Primary);
        this.actionsButton.Clicked += (_, _) => this.ActionsRequested?.Invoke();

        this.rootGrid = new Grid
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
        this.rootGrid.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Surface);

        this.separator = new BoxView
        {
            HeightRequest = 0.5,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(-8, -6, -8, 0)
        };
        this.separator.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.OutlineVariant);

        this.rootGrid.Add(this.actionsButton, 0, 0);
        this.rootGrid.Add(this.attachButton, 1, 0);
        this.rootGrid.Add(this.entry, 2, 0);
        this.rootGrid.Add(this.sendButton, 3, 0);

        // Markdown composition toolbar (above the entry row)
        this.markdownToolbar = new HorizontalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(8, 4, 8, 0),
            IsVisible = false
        };

        // Edit-mode banner
        this.editBannerLabel = new Label
        {
            Text = "Editing message",
            FontSize = 12,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Start
        };
        this.editBannerLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        var editCancel = new Button
        {
            Text = "✕",
            FontSize = 14,
            BackgroundColor = Colors.Transparent,
            Padding = 0,
            WidthRequest = 32,
            HeightRequest = 28,
            HorizontalOptions = LayoutOptions.End
        };
        editCancel.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        editCancel.Clicked += (_, _) => this.EditCancelled?.Invoke();

        var editBannerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        editBannerGrid.Add(this.editBannerLabel, 0, 0);
        editBannerGrid.Add(editCancel, 1, 0);

        this.editBanner = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(12, 4),
            IsVisible = false,
            Content = editBannerGrid
        };
        this.editBanner.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceContainerHigh);

        var wrapper = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { this.separator, this.editBanner, this.markdownToolbar, this.rootGrid }
        };

        this.Content = wrapper;
    }

    public string PlaceholderText
    {
        get => this.entry.Placeholder ?? string.Empty;
        set => this.entry.Placeholder = value;
    }

    public string SendButtonText
    {
        get => this.sendButton.Text ?? string.Empty;
        set
        {
            this.defaultSendText = value;
            if (!this.IsEditing)
                this.sendButton.Text = value;
        }
    }

    public Color SendButtonBackgroundColor
    {
        get => this.sendButton.BackgroundColor;
        set => this.sendButton.BackgroundColor = value;
    }

    public Color SendButtonTextColor
    {
        get => this.sendButton.TextColor;
        set => this.sendButton.TextColor = value;
    }

    public Color BarBackgroundColor
    {
        get => this.rootGrid.BackgroundColor;
        set => this.rootGrid.BackgroundColor = value;
    }

    public Color BarBorderColor
    {
        get => this.separator.Color;
        set => this.separator.Color = value;
    }

    public bool ShowAttachButton
    {
        get => this.attachButton.IsVisible;
        set => this.attachButton.IsVisible = value;
    }

    public bool ShowActionsButton
    {
        get => this.actionsButton.IsVisible;
        set => this.actionsButton.IsVisible = value;
    }

    public string EntryText
    {
        get => this.entry.Text ?? string.Empty;
        set => this.entry.Text = value;
    }

    public bool IsEditing { get; private set; }

    public void ClearText() => this.entry.Text = string.Empty;

    public void SetInputEnabled(bool enabled)
    {
        this.entry.IsEnabled = enabled;
        this.sendButton.IsEnabled = enabled;
        this.attachButton.IsEnabled = enabled;
        this.actionsButton.IsEnabled = enabled;
        this.Opacity = enabled ? 1 : 0.6;
    }

    public void EnterEditMode(string body)
    {
        this.IsEditing = true;
        this.editBanner.IsVisible = true;
        this.entry.Text = body;
        this.sendButton.Text = "Save";
        this.entry.Focus();
    }

    public void ExitEditMode()
    {
        this.IsEditing = false;
        this.editBanner.IsVisible = false;
        this.sendButton.Text = this.defaultSendText;
        this.ClearText();
    }

    // ------- Markdown toolbar -------

    public void SetBodyPermissions(MessageBodyPermissions permissions)
    {
        this.markdownToolbar.Children.Clear();

        if (permissions == MessageBodyPermissions.None)
        {
            this.markdownToolbar.IsVisible = false;
            return;
        }

        if (permissions.HasFlag(MessageBodyPermissions.Bold))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("B", FontAttributes.Bold, () => this.ApplyWrap("**", "**", "bold")));
        if (permissions.HasFlag(MessageBodyPermissions.Italics))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("I", FontAttributes.Italic, () => this.ApplyWrap("*", "*", "italic")));
        if (permissions.HasFlag(MessageBodyPermissions.Underline))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("U", FontAttributes.None, () => this.ApplyWrap("<u>", "</u>", "underline")));
        if (permissions.HasFlag(MessageBodyPermissions.Strikethrough))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("S", FontAttributes.None, () => this.ApplyWrap("~~", "~~", "strike")));
        if (permissions.HasFlag(MessageBodyPermissions.Codeblocks))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("</>", FontAttributes.None, () => this.ApplyWrap("`", "`", "code")));
        if (permissions.HasFlag(MessageBodyPermissions.Links))
            this.markdownToolbar.Children.Add(this.CreateFormatButton("🔗", FontAttributes.None, () => this.LinkRequested?.Invoke()));

        this.markdownToolbar.IsVisible = this.markdownToolbar.Children.Count > 0;
    }

    Button CreateFormatButton(string text, FontAttributes attrs, Action action)
    {
        var btn = new Button
        {
            Text = text,
            FontSize = 14,
            FontAttributes = attrs,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 40,
            HeightRequest = 32,
            Padding = 0
        };
        btn.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        btn.Clicked += (_, _) => action();
        return btn;
    }

    public void ApplyWrap(string prefix, string suffix, string placeholder)
    {
        var text = this.entry.Text ?? string.Empty;
        var start = this.entry.CursorPosition;
        if (start < 0 || start > text.Length)
            start = text.Length;

        var len = this.entry.SelectionLength;
        if (len < 0) len = 0;
        if (start + len > text.Length)
            len = Math.Max(0, text.Length - start);

        var selected = len > 0 ? text.Substring(start, len) : placeholder;
        var inserted = prefix + selected + suffix;
        this.entry.Text = text[..start] + inserted + text[(start + len)..];

        // place cursor at the end of the inserted segment
        this.entry.CursorPosition = start + inserted.Length;
        this.entry.Focus();
    }

    public void InsertLink(string displayText, string url)
    {
        var display = string.IsNullOrWhiteSpace(displayText) ? url : displayText;
        var text = this.entry.Text ?? string.Empty;
        var start = this.entry.CursorPosition;
        if (start < 0 || start > text.Length)
            start = text.Length;

        var len = this.entry.SelectionLength;
        if (len < 0) len = 0;
        if (start + len > text.Length)
            len = Math.Max(0, text.Length - start);

        var inserted = $"[{display}]({url})";
        this.entry.Text = text[..start] + inserted + text[(start + len)..];
        this.entry.CursorPosition = start + inserted.Length;
        this.entry.Focus();
    }

    void OnEntryCompleted(object? sender, EventArgs e) => this.TrySend();

    void OnSendClicked(object? sender, EventArgs e) => this.TrySend();

    void TrySend()
    {
        var text = this.entry.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            this.SendRequested?.Invoke(text);
            // The owner clears/exits edit mode as appropriate.
        }
    }
}
