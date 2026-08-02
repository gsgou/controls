namespace Shiny.Blazor.Controls.Splash;

/// <summary>
/// Drives the pre-boot splash screen rendered by <c>splash.js</c> in index.html.
/// </summary>
/// <remarks>
/// The splash is already on screen by the time any of this runs - Blazor cannot render it,
/// only finish it. Use this to report startup progress and to decide the exact moment the
/// app is ready enough to be shown.
/// </remarks>
public interface ISplashScreen
{
    /// <summary>
    /// False once <see cref="HideAsync"/> has completed, or if no splash was ever started.
    /// </summary>
    ValueTask<bool> IsVisibleAsync();

    /// <summary>
    /// Sets the status line (the <c>[data-shiny-splash-status]</c> element).
    /// </summary>
    ValueTask SetStatusAsync(string? text);

    /// <summary>
    /// Sets determinate progress. Pass null for indeterminate.
    /// </summary>
    /// <param name="value">0.0 to 1.0. Values outside the range are clamped.</param>
    ValueTask SetProgressAsync(double? value);

    /// <summary>
    /// Fades the splash out and removes it. Idempotent - safe to call more than once, and
    /// safe to call when no splash was started.
    /// </summary>
    /// <param name="fadeMs">Overrides the fade duration configured at show time.</param>
    ValueTask HideAsync(int? fadeMs = null);
}
