namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Drives the quick entry popup — a prompt surface summoned over whatever the user is looking at —
/// and the screen-edge glow that goes with it. Resolve it from DI after <c>UseQuickEntry()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The popup is presented one of two ways (see <see cref="QuickEntryOptions.Presentation"/>): as an
/// overlay over the current page, which works everywhere, or as a borderless always-on-top OS window
/// that opens over <em>other applications</em>, which needs the <c>Shiny.Maui.Controls.Desktop</c>
/// add-on and a desktop platform. Everything on this interface behaves the same either way.
/// </para>
/// <para>
/// The glow lives here rather than on a service of its own because the two are almost always used
/// together, and splitting them meant an app wiring up an assistant had to resolve, configure and
/// keep two objects in step for one visible behaviour.
/// </para>
/// </remarks>
public interface IQuickEntryService
{
    /// <summary>The live options object. Most values can be changed between opens.</summary>
    QuickEntryOptions Options { get; }

    /// <summary>
    /// True when the popup can be shown at all. Only false where there is no page to draw on and no
    /// window to open — in practice, before the app has a window.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// How the popup will actually be presented right now, with <see cref="QuickEntryPresentation.Auto"/>
    /// and any unavailable-platform fallback already resolved. Read this rather than
    /// <c>Options.Presentation</c> when the answer matters.
    /// </summary>
    QuickEntryPresentation ResolvedPresentation { get; }

    /// <summary>True while the popup is on screen.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// The popup's content view, built on first use from <see cref="QuickEntryOptions.ContentFactory"/>.
    /// Cast it to <see cref="PromptView"/> (the default) to wire up <c>Submitted</c>. Null before the
    /// popup has ever been shown or <see cref="PreloadAsync"/> called.
    /// </summary>
    View? Content { get; }

    /// <summary>
    /// Build the popup ahead of time so the first open is instant. Optional — it builds itself on
    /// first <see cref="Show"/> either way — but it is also how you get at <see cref="Content"/>
    /// before the user has opened anything.
    /// </summary>
    Task PreloadAsync();

    /// <summary>Open the popup and give it focus (unless <see cref="QuickEntryOptions.ActivateOnShow"/> is off).</summary>
    void Show();

    /// <summary>Close the popup. Content is kept alive unless <see cref="QuickEntryOptions.RecreateContentOnShow"/> is set.</summary>
    void Hide();

    /// <summary>Open the popup if closed, close it if open. This is what a hotkey or tray click binds to.</summary>
    void Toggle();

    /// <summary>
    /// Grow or shrink the popup. Height is clamped to <see cref="QuickEntryOptions.MaxHeight"/>.
    /// Content that implements <see cref="IQuickEntryAutoSize"/> drives this for you.
    /// </summary>
    void Resize(double? width = null, double? height = null);

    /// <summary>Raised after the popup becomes visible.</summary>
    event EventHandler? Opened;

    /// <summary>Raised after the popup is hidden, whichever way it was dismissed.</summary>
    event EventHandler? Closed;

    // -------------------------------------------------------------------------------------
    // Screen glow
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// False where the glow cannot be drawn: MacCatalyst and Linux under Wayland in desktop
    /// presentation, and any host with no page to overlay in-app.
    /// </summary>
    bool IsGlowSupported { get; }

    /// <summary>True while the glow is lit.</summary>
    bool IsGlowVisible { get; }

    /// <summary>Fade the glow in and leave it running. Independent of the popup — use it for any listening or recording state.</summary>
    void ShowGlow();

    /// <summary>Fade the glow out.</summary>
    void HideGlow();

    /// <summary>Light the glow for a fixed period, then put it out — a one-shot acknowledgement.</summary>
    Task PulseGlowAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}
