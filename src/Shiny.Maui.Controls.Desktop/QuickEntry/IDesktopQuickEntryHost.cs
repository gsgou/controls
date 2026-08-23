using Shiny.Maui.Controls.QuickEntry;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// What the per-platform window code calls back into. Implemented by
/// <see cref="DesktopQuickEntryPresenter"/>, which forwards to the presenter callbacks the core
/// service wired up.
/// </summary>
/// <remarks>
/// A named interface rather than the presenter type directly, so the AppKit / Win32 / GTK files hold
/// one small contract instead of the whole presenter — they are the files that already have to know
/// about window handles and event masks.
/// </remarks>
interface IDesktopQuickEntryHost
{
    /// <summary>The window lost focus to another application.</summary>
    void NotifyDeactivated();

    /// <summary>A navigation key arrived. Returns true when it was consumed.</summary>
    bool NotifyKey(QuickEntryKey key);
}
