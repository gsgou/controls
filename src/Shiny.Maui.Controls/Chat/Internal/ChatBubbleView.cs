using System.Text.RegularExpressions;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Chat.Internal;

partial class ChatBubbleView : ContentView
{
    static readonly Regex UrlRegex = CreateUrlRegex();

    readonly ChatView chatView;
    readonly bool isMe;

    readonly Grid rootLayout;
    readonly Grid avatarNameRow;
    readonly Border avatarBorder;
    readonly Label avatarLabel;
    readonly Image avatarImage;
    readonly Label nameLabel;
    readonly Grid bubbleRow;
    readonly Border bubbleBorder;
    readonly VerticalStackLayout defaultContentLayout;
    readonly Label textLabel;
    readonly Image imageView;
    readonly Label timestampLabel;
    readonly HorizontalStackLayout acknowledgementLayout;
    readonly Button toolsButton;
    View? customTemplateView;

    public ChatBubbleView(ChatView chatView, bool isMe)
    {
        this.chatView = chatView;
        this.isMe = isMe;

        avatarImage = new Image
        {
            WidthRequest = 32,
            HeightRequest = 32,
            Aspect = Aspect.AspectFill
        };

        avatarLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        avatarBorder = new Border
        {
            WidthRequest = 32,
            HeightRequest = 32,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Padding = 0,

            VerticalOptions = LayoutOptions.Center
        };

        nameLabel = new Label
        {
            FontSize = 12,
            Margin = new Thickness(4, 0, 0, 2),
            VerticalOptions = LayoutOptions.Center
        };
        nameLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        avatarNameRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 6,
            Margin = new Thickness(0, 0, 0, 2)
        };
        avatarNameRow.Add(avatarBorder, 0, 0);
        avatarNameRow.Add(nameLabel, 1, 0);

        textLabel = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap
        };

        imageView = new Image
        {
            Aspect = Aspect.AspectFit,
            MaximumHeightRequest = 250,
            MaximumWidthRequest = 250,
            IsVisible = false
        };

        defaultContentLayout = new VerticalStackLayout
        {
            Children = { textLabel, imageView }
        };

        bubbleBorder = new Border
        {
            Padding = new Thickness(12, 8),
            StrokeThickness = 0,
            MaximumWidthRequest = 280,
            Content = defaultContentLayout
        };

        var bubbleTap = new TapGestureRecognizer();
        bubbleTap.Tapped += OnBubbleTapped;
        bubbleBorder.GestureRecognizers.Add(bubbleTap);

        toolsButton = new Button
        {
            Text = "\u22ee",
            FontSize = 16,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 28,
            HeightRequest = 28,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        toolsButton.SetDynamicResource(Button.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        toolsButton.Clicked += OnToolsButtonClicked;

        bubbleRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 2
        };

        // Column order depends on isMe; set during Configure
        bubbleRow.Add(bubbleBorder, 0, 0);
        bubbleRow.Add(toolsButton, 1, 0);

        timestampLabel = new Label
        {
            FontSize = 11,
            Margin = new Thickness(4, 2, 4, 0)
        };
        timestampLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        acknowledgementLayout = new HorizontalStackLayout
        {
            Spacing = 4,
            Margin = new Thickness(4, 2, 4, 0),
            IsVisible = false
        };

        rootLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            Padding = new Thickness(12, 0)
        };
        rootLayout.Add(avatarNameRow, 0, 0);
        rootLayout.Add(bubbleRow, 0, 1);
        rootLayout.Add(acknowledgementLayout, 0, 2);
        rootLayout.Add(timestampLabel, 0, 3);

        Content = rootLayout;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not ChatMessage message)
            return;

        Configure(message);
    }

    void Configure(ChatMessage message)
    {
        var messages = chatView.Messages;
        if (messages is null or { Count: 0 })
            return;

        var index = -1;
        for (var i = 0; i < messages.Count; i++)
        {
            if (ReferenceEquals(messages[i], message))
            {
                index = i;
                break;
            }
        }

        var prev = index > 0 ? messages[index - 1] : null;
        var next = index < messages.Count - 1 ? messages[index + 1] : null;
        var isFirst = ChatGroupHelper.IsNewGroup(message, prev);
        var isLast = next is null || ChatGroupHelper.IsNewGroup(next, message);

        var showAvatar = ShouldShowAvatar(message, isFirst);
        var participant = GetParticipant(message.SenderId);

        // Alignment
        if (isMe)
        {
            rootLayout.HorizontalOptions = LayoutOptions.End;
            timestampLabel.HorizontalTextAlignment = TextAlignment.End;
            acknowledgementLayout.HorizontalOptions = LayoutOptions.End;
        }
        else
        {
            rootLayout.HorizontalOptions = LayoutOptions.Start;
            timestampLabel.HorizontalTextAlignment = TextAlignment.Start;
            acknowledgementLayout.HorizontalOptions = LayoutOptions.Start;
        }

        // Tools button: position relative to bubble
        var hasTools = chatView.HasBubbleTools(message);
        toolsButton.IsVisible = hasTools;

        // Reorder columns: tools button on the outside of the bubble
        // "my" messages (right-aligned): tools on the left, bubble on the right
        // "other" messages (left-aligned): bubble on the left, tools on the right
        Grid.SetColumn(bubbleBorder, isMe ? 1 : 0);
        Grid.SetColumn(toolsButton, isMe ? 0 : 1);

        // Avatar + Name
        avatarNameRow.IsVisible = showAvatar;
        if (showAvatar)
        {
            nameLabel.Text = participant?.DisplayName ?? "Unknown";

            var avatarColor = participant?.BubbleColor ?? chatView.OtherBubbleColor;
            avatarBorder.BackgroundColor = avatarColor;

            if (participant?.Avatar is not null)
            {
                avatarImage.Source = participant.Avatar;
                avatarBorder.Content = avatarImage;
            }
            else
            {
                avatarLabel.Text = ChatGroupHelper.GetInitials(participant?.DisplayName);
                avatarBorder.Content = avatarLabel;
            }
        }

        // Bubble colors
        var bubbleColor = isMe
            ? chatView.MyBubbleColor
            : (participant?.BubbleColor ?? chatView.OtherBubbleColor);
        var textColor = isMe ? chatView.MyTextColor : chatView.OtherTextColor;

        bubbleBorder.BackgroundColor = bubbleColor;

        // Corner radius: rounded with smaller tail corner
        var radius = chatView.BubbleCornerRadius;
        var tailRadius = isLast ? 4 : radius;
        bubbleBorder.StrokeShape = isMe
            ? new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(radius, radius, radius, tailRadius) }
            : new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(radius, radius, tailRadius, radius) };

        // Content: custom template, text, or image
        var template = chatView.MessageTemplateSelector?.SelectTemplate(message, this)
                    ?? chatView.MessageTemplate;

        if (template != null)
        {
            customTemplateView = (View)template.CreateContent();
            customTemplateView.BindingContext = message;
            bubbleBorder.Content = customTemplateView;
            bubbleBorder.Padding = new Thickness(12, 8);
        }
        else if (!string.IsNullOrEmpty(message.ImageUrl))
        {
            if (customTemplateView != null)
            {
                bubbleBorder.Content = defaultContentLayout;
                customTemplateView = null;
            }
            textLabel.IsVisible = false;
            imageView.IsVisible = true;
            imageView.Source = message.ImageUrl;
            bubbleBorder.Padding = new Thickness(4);
        }
        else
        {
            if (customTemplateView != null)
            {
                bubbleBorder.Content = defaultContentLayout;
                customTemplateView = null;
            }
            textLabel.IsVisible = true;
            imageView.IsVisible = false;
            textLabel.TextColor = textColor;
            textLabel.FontSize = chatView.BubbleFontSize;
            textLabel.FontFamily = chatView.BubbleFontFamily;
            SetTextWithLinks(textLabel, message.Text ?? string.Empty, textColor);
            bubbleBorder.Padding = new Thickness(12, 8);
        }

        // Acknowledgements
        ConfigureAcknowledgements(message);

        // Timestamp
        timestampLabel.IsVisible = isLast;
        timestampLabel.FontSize = chatView.TimestampFontSize;
        if (isLast)
            timestampLabel.Text = ChatGroupHelper.FormatTimestamp(message.Timestamp);

        // Dim unsent user messages
        bubbleBorder.Opacity = (message.IsFromMe && message.DateSent == null) ? 0.5 : 1.0;

        // Spacing
        Margin = new Thickness(0, isFirst ? 12 : 2, 0, 0);
    }

    void ConfigureAcknowledgements(ChatMessage message)
    {
        acknowledgementLayout.Children.Clear();

        if (message.Acknowledgements is not { Count: > 0 })
        {
            acknowledgementLayout.IsVisible = false;
            return;
        }

        // Group by glyph
        var groups = message.Acknowledgements
            .Where(a => !string.IsNullOrEmpty(a.Glyph))
            .GroupBy(a => a.Glyph)
            .ToList();

        if (groups.Count == 0)
        {
            acknowledgementLayout.IsVisible = false;
            return;
        }

        foreach (var group in groups)
        {
            var count = group.Count();
            var countLabel = new Label
            {
                Text = count > 1 ? count.ToString() : "",
                FontSize = 11,
                VerticalTextAlignment = TextAlignment.Center,
                IsVisible = count > 1
            };
            countLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

            var badge = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(6, 2),
                Content = new HorizontalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label
                        {
                            Text = group.Key,
                            FontSize = 12,
                            VerticalTextAlignment = TextAlignment.Center
                        },
                        countLabel
                    }
                }
            };
            badge.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceContainerHigh);
            acknowledgementLayout.Children.Add(badge);
        }

        acknowledgementLayout.IsVisible = true;
    }

    bool ShouldShowAvatar(ChatMessage message, bool isFirstInGroup)
    {
        if (message.IsFromMe)
            return false;

        if (!isFirstInGroup)
            return false;

        if (chatView.IsMultiPerson)
            return true;

        return chatView.ShowAvatarsInSingleChat;
    }

    void OnBubbleTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is ChatMessage msg)
            chatView.OnMessageTapped(msg);
    }

    void OnToolsButtonClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ChatMessage msg)
            chatView.ShowBubbleTools(msg);
    }

    ChatParticipant? GetParticipant(string senderId)
    {
        var participants = chatView.Participants;
        if (participants is null)
            return null;

        for (var i = 0; i < participants.Count; i++)
        {
            if (participants[i].Id == senderId)
                return participants[i];
        }
        return null;
    }

    void SetTextWithLinks(Label label, string text, Color textColor)
    {
        var matches = UrlRegex.Matches(text);
        if (matches.Count == 0)
        {
            label.FormattedText = null;
            label.Text = text;
            return;
        }

        var formatted = new FormattedString();
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                formatted.Spans.Add(new Span
                {
                    Text = text[lastIndex..match.Index],
                    TextColor = textColor
                });
            }

            var urlSpan = new Span
            {
                Text = match.Value,
                TextColor = Colors.CornflowerBlue,
                TextDecorations = TextDecorations.Underline
            };

            var url = match.Value;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => _ = Launcher.OpenAsync(new Uri(url));
            urlSpan.GestureRecognizers.Add(tap);

            formatted.Spans.Add(urlSpan);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            formatted.Spans.Add(new Span
            {
                Text = text[lastIndex..],
                TextColor = textColor
            });
        }

        label.FormattedText = formatted;
    }

    [GeneratedRegex(@"(https?://[^\s]+|www\.[^\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreateUrlRegex();
}
