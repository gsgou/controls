namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Where the quick entry popup appears on screen. Placement is always resolved against the
/// screen that currently has the mouse pointer, so the popup follows the user across a
/// multi-monitor setup rather than pinning itself to the app's window.
/// </summary>
public enum QuickEntryPlacement
{
    /// <summary>
    /// Horizontally centred, sitting in the upper third of the screen — the Spotlight /
    /// Claude Desktop / Copilot position. Vertical offset is controlled by
    /// <see cref="QuickEntryOptions.TopMarginRatio"/>.
    /// </summary>
    TopCenter,

    /// <summary>
    /// Horizontally centred, sitting near the bottom of the screen — where a dock-style command bar
    /// or a "listening" prompt belongs. Vertical offset is controlled by
    /// <see cref="QuickEntryOptions.BottomMarginRatio"/>.
    /// </summary>
    BottomCenter,

    /// <summary>Dead centre of the active screen.</summary>
    Center,

    /// <summary>Anchored just below and right of the mouse pointer, clamped to stay on screen.</summary>
    NearCursor,

    /// <summary>
    /// Leaves positioning entirely to you. Set <see cref="QuickEntryOptions.X"/> and
    /// <see cref="QuickEntryOptions.Y"/> (screen coordinates, top-left origin, device-independent pixels).
    /// </summary>
    Manual
}
