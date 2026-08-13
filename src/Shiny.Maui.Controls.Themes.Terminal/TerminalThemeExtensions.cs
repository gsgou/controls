using Shiny.Maui.Controls.Themes;

namespace Shiny;

public static class TerminalThemeExtensions
{
    /// <summary>Apply the Terminal theme (a dense, square, phosphor-green console look).</summary>
    /// <remarks>
    /// The theme asks for a monospace font family. On MAUI a family name only resolves if the host
    /// app registered it via <c>ConfigureFonts</c>; an unregistered name falls back to the system
    /// font, so the theme still applies — it just keeps the default typeface.
    /// </remarks>
    public static ShinyControlConfiguration UseTerminalTheme(this ShinyControlConfiguration cfg)
        => cfg.UseTheme(new TerminalTheme());
}
