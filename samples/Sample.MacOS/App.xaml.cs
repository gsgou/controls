namespace Sample.MacOS;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
        CentreButtonsForAppKit();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell()) { Title = "Shiny Controls (macOS)" };

    /// <summary>
    /// A native control laid out with <c>VerticalOptions="Fill"</c> — the default for a child of a
    /// <c>HorizontalStackLayout</c> or a <c>FlexLayout</c> — measures 96pt tall on the macOS AppKit
    /// head instead of its natural height, which turns every toolbar row in this sample into a row of
    /// tall blocks. Centring them is a no-op on the other heads (controls sharing a row share a font,
    /// so their natural heights already match), so it is applied only here.
    /// </summary>
    /// <remarks>
    /// The setter is appended to the shared implicit styles from <c>Resources/Styles/Styles.xaml</c>
    /// rather than declared as second implicit styles in App.xaml: a nearer implicit style
    /// *replaces* the merged one outright, which would silently drop its padding, corner radius and
    /// minimum size. Nothing in the control library is affected — ShinyButton, Fab and the TableView
    /// cells are composed views, not native controls.
    /// </remarks>
    static void CentreButtonsForAppKit()
    {
        if (Current?.Resources is null)
            return;

        foreach (var type in new[] { typeof(Button), typeof(ImageButton), typeof(Switch), typeof(CheckBox), typeof(RadioButton) })
        {
            if (Current.Resources.TryGetValue(type.FullName!, out var found) && found is Style style)
                style.Setters.Add(new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center });
        }
    }
}
