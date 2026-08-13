using Shiny.Maui.Controls.Themes;

namespace Shiny;

public static class AuroraThemeExtensions
{
    /// <summary>Apply the Aurora theme (a vivid violet/cyan palette that glows instead of casting shadows).</summary>
    public static ShinyControlConfiguration UseAuroraTheme(this ShinyControlConfiguration cfg)
        => cfg.UseTheme(new AuroraTheme());
}
