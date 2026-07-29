using Shiny.Maui.Controls.Infrastructure;
namespace Shiny.Maui.Controls;

public interface ITextEntryAwareTool
{
    void Attach(TextEntry entry);
    void Detach();
}

public class TextEntryTool : ContentView
{
    readonly Image iconImage;
    readonly Label textLabel;
    readonly TapGestureRecognizer tap;

    public TextEntryTool()
    {
        iconImage = new Image
        {
            WidthRequest = 20,
            HeightRequest = 20,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };

        textLabel = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };

        var layout = new HorizontalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children = { iconImage, textLabel }
        };

        tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        layout.GestureRecognizers.Add(tap);

        Content = layout;
        Padding = new Thickness(12, 0);
        VerticalOptions = LayoutOptions.Fill;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(TextEntryTool));
    }

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(ImageSource), typeof(TextEntryTool), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var t = (TextEntryTool)b;
            t.iconImage.Source = n as ImageSource;
            t.iconImage.IsVisible = n is not null;
        }));
    public ImageSource? Icon { get => (ImageSource?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(TextEntryTool), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var t = (TextEntryTool)b;
            t.textLabel.Text = n as string;
            t.textLabel.IsVisible = !string.IsNullOrEmpty(n as string);
        }));
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public static readonly BindableProperty ToolColorProperty = BindableProperty.Create(
        nameof(ToolColor), typeof(Color), typeof(TextEntryTool), Colors.Grey,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            if (n is Color c)
                ((TextEntryTool)b).textLabel.TextColor = c;
        }));
    public Color ToolColor { get => (Color)GetValue(ToolColorProperty); set => SetValue(ToolColorProperty, value); }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(System.Windows.Input.ICommand), typeof(TextEntryTool));
    public System.Windows.Input.ICommand? Command { get => (System.Windows.Input.ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(TextEntryTool));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public event EventHandler? Clicked;

    internal TextEntry? ParentEntry { get; set; }

    internal void Invoke()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }

    void OnTapped(object? sender, TappedEventArgs e) => Invoke();
}
