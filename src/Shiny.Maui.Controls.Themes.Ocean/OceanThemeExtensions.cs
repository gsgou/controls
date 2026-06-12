using Shiny.Maui.Controls.Themes;

namespace Shiny;

public static class OceanThemeExtensions
{
    /// <summary>Apply the Ocean theme (a cool teal/cyan Material 3 palette).</summary>
    public static ShinyControlConfiguration UseOceanTheme(this ShinyControlConfiguration cfg)
        => cfg.UseTheme(new OceanTheme());
}
