using Shiny;
using Shiny.Maui.Controls.Themes;

namespace Sample;

public partial class AppShell : ShinyShell
{
    public AppShell()
    {
        InitializeComponent();
    }

    void OnBasicTheme(object? sender, EventArgs e) => ShinyThemeManager.SetTheme(new BasicTheme());
    void OnOceanTheme(object? sender, EventArgs e) => ShinyThemeManager.SetTheme(new OceanTheme());
    void OnMaterialTheme(object? sender, EventArgs e) => ShinyThemeManager.SetTheme(new MaterialTheme());

    void OnDarkToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
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