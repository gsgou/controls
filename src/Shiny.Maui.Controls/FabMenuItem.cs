using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

public class FabMenuItem : ContentView
{
    const double DefaultSize = 44;
    const double DefaultIconSize = 20;
    const double DefaultFontSize = 13;

    readonly HorizontalStackLayout rootLayout;
    readonly Border labelBorder;
    readonly Label label;
    readonly Border iconBorder;
    readonly Image iconImage;
    readonly TapGestureRecognizer iconTap;
    readonly TapGestureRecognizer labelTap;

    public FabMenuItem()
    {
        // Side label (appears to the left of the icon)
        label = new Label
        {
            FontSize = DefaultFontSize,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap
        };
        labelBorder = new Border
        {
            Padding = new Thickness(10, 6),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(6)
            },
            VerticalOptions = LayoutOptions.Center,
            Content = label,
            IsVisible = false,
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.15f,
                Radius = 4,
                Offset = new Point(0, 2)
            }
        };

        iconImage = new Image
        {
            WidthRequest = DefaultIconSize,
            HeightRequest = DefaultIconSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Aspect = Aspect.AspectFit
        };
        iconBorder = new Border
        {
            WidthRequest = DefaultSize,
            HeightRequest = DefaultSize,
            Padding = 0,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(DefaultSize / 2)
            },
            Content = iconImage,
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.2f,
                Radius = 6,
                Offset = new Point(0, 3)
            }
        };

        rootLayout = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        rootLayout.Add(labelBorder);
        rootLayout.Add(iconBorder);

        // Theme defaults — overridden if the consumer sets the explicit color properties.
        iconBorder.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        labelBorder.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Surface);
        label.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurface);

        iconTap = new TapGestureRecognizer();
        iconTap.Tapped += OnTapped;
        iconBorder.GestureRecognizers.Add(iconTap);

        labelTap = new TapGestureRecognizer();
        labelTap.Tapped += OnTapped;
        labelBorder.GestureRecognizers.Add(labelTap);

        Content = rootLayout;
        HorizontalOptions = LayoutOptions.End;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(FabMenuItem));
    }


    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon),
        typeof(ImageSource),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            item.iconImage.Source = n as ImageSource;
            item.iconImage.IsVisible = n is not null;
        }));
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            item.label.Text = n as string ?? string.Empty;
            item.labelBorder.IsVisible = !string.IsNullOrEmpty(item.label.Text);
        }));
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(FabMenuItem),
        null);
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter),
        typeof(object),
        typeof(FabMenuItem),
        null);
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly BindableProperty FabBackgroundColorProperty = BindableProperty.Create(
        nameof(FabBackgroundColor),
        typeof(Color),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            if (n is Color c)
                item.iconBorder.BackgroundColor = c;
            else
                item.iconBorder.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        }));
    public Color? FabBackgroundColor
    {
        get => (Color?)GetValue(FabBackgroundColorProperty);
        set => SetValue(FabBackgroundColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor),
        typeof(Color),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            if (n is Color c)
                ((FabMenuItem)b).iconBorder.Stroke = c;
        }));
    public Color? BorderColor
    {
        get => (Color?)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness),
        typeof(double),
        typeof(FabMenuItem),
        0.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
                ((FabMenuItem)b).iconBorder.StrokeThickness = (double)n;
            }));
    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor),
        typeof(Color),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            if (n is Color c)
                item.label.TextColor = c;
            else
                item.label.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurface);
        }));
    public Color? TextColor
    {
        get => (Color?)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty LabelBackgroundColorProperty = BindableProperty.Create(
        nameof(LabelBackgroundColor),
        typeof(Color),
        typeof(FabMenuItem),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            if (n is Color c)
                item.labelBorder.BackgroundColor = c;
            else
                item.labelBorder.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Surface);
        }));
    public Color? LabelBackgroundColor
    {
        get => (Color?)GetValue(LabelBackgroundColorProperty);
        set => SetValue(LabelBackgroundColorProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(FabMenuItem),
        DefaultFontSize,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
                ((FabMenuItem)b).label.FontSize = (double)n;
            }));
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size),
        typeof(double),
        typeof(FabMenuItem),
        DefaultSize,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            var s = (double)n;
            item.iconBorder.WidthRequest = s;
            item.iconBorder.HeightRequest = s;
            item.iconBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(s / 2)
            };
        }));
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize),
        typeof(double),
        typeof(FabMenuItem),
        DefaultIconSize,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var item = (FabMenuItem)b;
            var s = (double)n;
            item.iconImage.WidthRequest = s;
            item.iconImage.HeightRequest = s;
        }));
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }


    public static readonly BindableProperty UseFeedbackProperty = BindableProperty.Create(
        nameof(UseFeedback),
        typeof(bool),
        typeof(FabMenuItem),
        true);
    public bool UseFeedback
    {
        get => (bool)GetValue(UseFeedbackProperty);
        set => SetValue(UseFeedbackProperty, value);
    }


    public event EventHandler? Clicked;


    internal void Invoke()
    {
        if (UseFeedback)
            FeedbackHelper.Execute(this, nameof(Clicked));

        Clicked?.Invoke(this, EventArgs.Empty);
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }

    void OnTapped(object? sender, TappedEventArgs e) => Invoke();
}
