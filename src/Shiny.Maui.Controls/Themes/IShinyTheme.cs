namespace Shiny.Maui.Controls.Themes;

/// <summary>
/// A Shiny Controls theme: a paired light/dark set of token resources (colors, shape,
/// elevation, type, spacing) keyed by <see cref="ShinyThemeKeys"/>. The built-in
/// <see cref="BasicTheme"/> ships in the core package; additional themes (Ocean, Material,
/// …) ship as separate NuGet packs. Apply one via
/// <c>UseShinyControls(cfg =&gt; cfg.UseTheme(new OceanTheme()))</c> or switch at runtime
/// with <see cref="ShinyThemeManager.SetTheme"/>.
/// </summary>
public interface IShinyTheme
{
    /// <summary>Display name of the theme, e.g. "Ocean".</summary>
    string Name { get; }

    /// <summary>Resource dictionary applied when the app is in light appearance.</summary>
    ResourceDictionary Light { get; }

    /// <summary>Resource dictionary applied when the app is in dark appearance.</summary>
    ResourceDictionary Dark { get; }
}
