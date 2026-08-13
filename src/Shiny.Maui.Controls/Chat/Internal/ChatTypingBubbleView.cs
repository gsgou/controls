using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Chat.Internal;

class ChatTypingBubbleView : ContentView
{
    readonly ChatView chatView;
    readonly string userId;
    readonly Grid avatarNameRow;
    readonly Border avatarBorder;
    readonly Label avatarLabel;
    readonly Image avatarImage;
    readonly Label nameLabel;
    readonly Border bubbleBorder;
    readonly BoxView dot1;
    readonly BoxView dot2;
    readonly BoxView dot3;
    bool isAnimating;

    public ChatTypingBubbleView(ChatView chatView, string userId)
    {
        this.chatView = chatView;
        this.userId = userId;

        this.avatarImage = new Image
        {
            WidthRequest = 32,
            HeightRequest = 32,
            Aspect = Aspect.AspectFill
        };
        this.avatarLabel = new Label
        {
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }.WithFontSize(ShinyThemeKeys.Type.BodySmallSize);
        this.avatarBorder = new Border
        {
            WidthRequest = 32,
            HeightRequest = 32,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerLargeRadius),
            Padding = 0,
            VerticalOptions = LayoutOptions.Center
        };
        this.nameLabel = new Label
        {
            Margin = new Thickness(4, 0, 0, 2),
            VerticalOptions = LayoutOptions.Center
        }.WithFontSize(ShinyThemeKeys.Type.BodySmallSize);
        this.nameLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        this.avatarNameRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 6,
            Margin = new Thickness(0, 0, 0, 2)
        };
        this.avatarNameRow.Add(this.avatarBorder, 0, 0);
        this.avatarNameRow.Add(this.nameLabel, 1, 0);

        this.dot1 = CreateDot();
        this.dot2 = CreateDot();
        this.dot3 = CreateDot();

        var dotsLayout = new HorizontalStackLayout
        {
            Spacing = 4,
            Children = { this.dot1, this.dot2, this.dot3 }
        };

        this.bubbleBorder = new Border
        {
            Padding = new Thickness(14, 10),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerLargeRadius),
            Content = dotsLayout,
            HorizontalOptions = LayoutOptions.Start
        };

        var rootLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            Padding = new Thickness(12, 0),
            HorizontalOptions = LayoutOptions.Start
        };
        rootLayout.Add(this.avatarNameRow, 0, 0);
        rootLayout.Add(this.bubbleBorder, 0, 1);

        this.Margin = new Thickness(0, 4, 0, 0);
        this.Content = rootLayout;

        this.Configure();
    }

    static BoxView CreateDot()
    {
        var dot = new BoxView
        {
            WidthRequest = 8,
            HeightRequest = 8,
            CornerRadius = 4,
            VerticalOptions = LayoutOptions.Center
        };
        dot.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        return dot;
    }

    void Configure()
    {
        var user = this.chatView.GetUser(this.userId);
        var bubbleColor = user?.BubbleColor ?? this.chatView.OtherBubbleColor;
        this.bubbleBorder.BackgroundColor = bubbleColor;

        var showAvatar = this.chatView.IsMultiPerson;
        this.avatarNameRow.IsVisible = showAvatar;

        if (showAvatar)
        {
            this.nameLabel.Text = user?.DisplayName ?? "Unknown";
            this.avatarBorder.BackgroundColor = bubbleColor;

            if (user?.Avatar is not null)
            {
                this.avatarImage.Source = user.Avatar;
                this.avatarBorder.Content = this.avatarImage;
            }
            else
            {
                this.avatarLabel.Text = ChatGroupHelper.GetInitials(user?.DisplayName);
                this.avatarBorder.Content = this.avatarLabel;
            }
        }

        this.StartAnimation();
    }

    void StartAnimation()
    {
        if (this.isAnimating)
            return;
        this.isAnimating = true;

        var animation = new Animation();
        animation.Add(0.0, 0.4, new Animation(v => this.dot1.TranslationY = v, 0, -4));
        animation.Add(0.4, 0.8, new Animation(v => this.dot1.TranslationY = v, -4, 0));
        animation.Add(0.15, 0.55, new Animation(v => this.dot2.TranslationY = v, 0, -4));
        animation.Add(0.55, 0.95, new Animation(v => this.dot2.TranslationY = v, -4, 0));
        animation.Add(0.3, 0.7, new Animation(v => this.dot3.TranslationY = v, 0, -4));
        animation.Add(0.7, 1.0, new Animation(v => this.dot3.TranslationY = v, -4, 0));

        animation.Commit(this, "TypingDots", length: 1000, repeat: () => this.isAnimating);
    }
}
