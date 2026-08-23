namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// Where the quick entry popup sits over the page.
/// </summary>
/// <remarks>
/// The MAUI twin of this enum also carries <c>NearCursor</c> and <c>Manual</c>, which only mean
/// anything to a real OS window placed in screen coordinates. In a browser the popup is an element
/// in the page, so those are left out rather than accepted and ignored.
/// </remarks>
public enum QuickEntryPlacement
{
    /// <summary>Horizontally centred, in the upper third — the Spotlight position. Offset by <see cref="QuickEntryOptions.TopMarginRatio"/>.</summary>
    TopCenter,

    /// <summary>Horizontally centred, near the bottom. Offset by <see cref="QuickEntryOptions.BottomMarginRatio"/>.</summary>
    BottomCenter,

    /// <summary>Dead centre of the viewport.</summary>
    Center
}

/// <summary>When the quick entry popup lights the screen-edge glow.</summary>
public enum ScreenGlowTrigger
{
    /// <summary>Never. The glow can still be driven by hand through <see cref="IQuickEntryService"/>.</summary>
    None,

    /// <summary>The whole time the popup is open.</summary>
    WhileOpen,

    /// <summary>
    /// Only while the content reports itself busy — the closest match to Siri, which lights the edge
    /// while listening and thinking rather than the whole time it is up. <see cref="PromptView"/>
    /// reports this from its <c>IsBusy</c> parameter.
    /// </summary>
    WhileBusy
}
