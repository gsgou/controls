using Shiny.Maui.Controls.Themes;

namespace Shiny;

public static class MaterialThemeExtensions
{
    /// <summary>Apply the Material theme (a Material Design 3 purple palette).</summary>
    public static ShinyControlConfiguration UseMaterialTheme(this ShinyControlConfiguration cfg)
        => cfg.UseTheme(new MaterialTheme());
}
