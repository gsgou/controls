using Shiny;
using Shiny.Maui.Controls.Themes;

namespace Sample;

public partial class AppShell : ShinyShell
{
    const string ThemePreferenceKey = "shiny.sample.theme";
    const string DarkPreferenceKey = "shiny.sample.dark";

    public AppShell()
    {
        InitializeComponent();
        this.RestorePreferences();
    }

    void OnBasicTheme(object? sender, EventArgs e) => this.SelectTheme("basic");
    void OnOceanTheme(object? sender, EventArgs e) => this.SelectTheme("ocean");
    void OnMaterialTheme(object? sender, EventArgs e) => this.SelectTheme("material");
    void OnTerminalTheme(object? sender, EventArgs e) => this.SelectTheme("terminal");
    void OnAuroraTheme(object? sender, EventArgs e) => this.SelectTheme("aurora");

    void OnDarkToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;

        Preferences.Set(DarkPreferenceKey, e.Value);
    }

    void SelectTheme(string key)
    {
        ShinyThemeManager.SetTheme(ThemeFor(key));
        Preferences.Set(ThemePreferenceKey, key);
        this.ShowSelectedTheme(key);

        // Close the flyout, otherwise the menu stays over the whole screen and the theme looks
        // like it did nothing - the flyout's own chrome is deliberately branded, not themed.
        this.FlyoutIsPresented = false;
    }

    static IShinyTheme ThemeFor(string key) => key switch
    {
        "ocean" => new OceanTheme(),
        "material" => new MaterialTheme(),
        "terminal" => new TerminalTheme(),
        "aurora" => new AuroraTheme(),
        _ => new BasicTheme()
    };

    void RestorePreferences()
    {
        var key = Preferences.Get(ThemePreferenceKey, "basic");
        ShinyThemeManager.SetTheme(ThemeFor(key));
        this.ShowSelectedTheme(key);

        var dark = Preferences.Get(DarkPreferenceKey, false);
        this.darkSwitch.IsToggled = dark;
        if (dark && Application.Current is not null)
            Application.Current.UserAppTheme = AppTheme.Dark;
    }

    /// <summary>Marks the active theme so the three buttons aren't indistinguishable.</summary>
    void ShowSelectedTheme(string key)
    {
        Mark(this.basicThemeButton, key == "basic");
        Mark(this.oceanThemeButton, key == "ocean");
        Mark(this.materialThemeButton, key == "material");
        Mark(this.terminalThemeButton, key == "terminal");
        Mark(this.auroraThemeButton, key == "aurora");

        static void Mark(Button button, bool selected)
        {
            button.Opacity = selected ? 1 : 0.55;
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    async void OnFooterTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(new Uri("https://shinylib.net"));
        }
        catch
        {
            // Platform may not support launching URLs
        }
    }
}
