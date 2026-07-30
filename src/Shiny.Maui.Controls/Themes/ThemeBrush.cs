namespace Shiny.Maui.Controls.Themes;

/// <summary>
/// Builds brushes whose colour tracks a theme token via <c>SetDynamicResource</c>.
/// </summary>
static class ThemeBrush
{
    /// <summary>
    /// A <see cref="SolidColorBrush"/> bound to <paramref name="tokenKey"/>, seeded with a
    /// non-null fallback. A brush handed to a handler with a null Color throws inside
    /// MAUI's Windows stroke mapper, and a token is null until the theme dictionary merges,
    /// so the seed keeps early handler creation alive until the token resolves.
    /// </summary>
    public static SolidColorBrush FromToken(string tokenKey, Color? fallback = null)
    {
        var brush = new SolidColorBrush(fallback ?? Colors.Transparent);
        brush.SetDynamicResource(SolidColorBrush.ColorProperty, tokenKey);
        return brush;
    }
}
