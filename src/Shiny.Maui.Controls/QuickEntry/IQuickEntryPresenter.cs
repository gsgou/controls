namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Puts the quick entry popup on screen. One implementation draws it over the current page and ships
/// in this package; the other opens a borderless OS window and ships in
/// <c>Shiny.Maui.Controls.Desktop</c>, which is why this is public rather than internal.
/// </summary>
/// <remarks>
/// Everything about <em>what</em> is shown — building the content, clamping the height, the glow
/// triggers, the Escape ladder — stays in <see cref="IQuickEntryService"/>. A presenter only knows
/// how to make a surface appear, disappear and change size, and how to report back that it lost
/// focus or saw a navigation key.
/// </remarks>
public interface IQuickEntryPresenter
{
    /// <summary>Which presentation this is. Used to match it against <see cref="QuickEntryOptions.Presentation"/>.</summary>
    QuickEntryPresentation Kind { get; }

    /// <summary>False when this presenter cannot run here — the desktop one on mobile, either one before the app has a window.</summary>
    bool IsSupported { get; }

    /// <summary>Called when the popup loses focus (window deactivated, or the scrim tapped in-app).</summary>
    Action? Deactivated { get; set; }

    /// <summary>Called for a navigation key. Return true if it was handled and should be swallowed.</summary>
    Func<QuickEntryKey, bool>? KeyPressed { get; set; }

    /// <summary>
    /// Called when the hosted content reports a new height. Only a presenter that has to size a
    /// surface itself — the desktop window — raises this; the in-app overlay is laid out by the page.
    /// </summary>
    Action<double>? ContentHeightChanged { get; set; }

    /// <summary>Build the surface and leave it hidden. Called once, before the first <see cref="Show"/>.</summary>
    Task PrepareAsync(QuickEntryOptions options, View content);

    /// <summary>Swap the hosted content without rebuilding the surface.</summary>
    void SetContent(View content);

    /// <summary>Make the popup visible at the given size.</summary>
    void Show(QuickEntryOptions options, double width, double height);

    /// <summary>Hide the popup, keeping the surface for the next open.</summary>
    void Hide();

    /// <summary>Resize the visible popup.</summary>
    void Resize(QuickEntryOptions options, double width, double height);

    /// <summary>Release the surface entirely.</summary>
    void Teardown();
}

/// <summary>
/// Draws the screen-edge glow. Split from <see cref="IQuickEntryPresenter"/> because the two are
/// available independently — Wayland can host the popup but not a click-through overlay above other
/// windows — and combined again behind <see cref="IQuickEntryService"/>, which is the only place a
/// consumer sees them.
/// </summary>
public interface IScreenGlowPresenter
{
    /// <summary>Which presentation this is.</summary>
    QuickEntryPresentation Kind { get; }

    /// <summary>False where a click-through overlay cannot be drawn.</summary>
    bool IsSupported { get; }

    /// <summary>Fade the glow in.</summary>
    Task ShowAsync(ScreenGlowOptions options);

    /// <summary>Fade the glow out.</summary>
    Task HideAsync(ScreenGlowOptions options);

    /// <summary>Release the surface entirely.</summary>
    void Teardown();
}
