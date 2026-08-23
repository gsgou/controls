namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Registers system-wide hotkeys — combinations that fire while your app is in the background.
/// Useful on its own (media keys, capture shortcuts), and what <see cref="IQuickEntryService"/>
/// uses to open the popup from anywhere.
/// </summary>
/// <remarks>
/// <para>Platform backing:</para>
/// <list type="bullet">
/// <item><description><b>Windows</b> — <c>RegisterHotKey</c> against a message-only window. Reliable; a combination already claimed by another process fails.</description></item>
/// <item><description><b>macOS (AppKit)</b> — Carbon <c>RegisterEventHotKey</c>. Works with no accessibility permission prompt, unlike an <c>NSEvent</c> global monitor.</description></item>
/// <item><description><b>Linux/X11</b> — <c>XGrabKey</c> on the root window, watched from a dedicated display connection.</description></item>
/// <item><description><b>Linux/Wayland</b> — the <c>org.freedesktop.portal.GlobalShortcuts</c> desktop portal, where the compositor implements it (GNOME 45+, KDE Plasma 6+). Binding shows the user a system confirmation prompt, so the hotkey may only start working after they accept. Compositors without the portal cannot support global hotkeys at all — <see cref="IsSupported"/> is false and you must fall back to a tray icon.</description></item>
/// <item><description><b>MacCatalyst</b> — not supported.</description></item>
/// </list>
/// </remarks>
public interface IGlobalHotKeyService
{
    /// <summary>False where the platform (or the running Wayland compositor) offers no global hotkey mechanism.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Claim a hotkey. <paramref name="accelerator"/> uses the tray accelerator grammar —
    /// modifiers joined with '+', then the key: <c>"Ctrl+Alt+Space"</c>, <c>"Cmd+Shift+K"</c>,
    /// <c>"Ctrl+F12"</c>.
    /// </summary>
    /// <param name="accelerator">The combination to claim.</param>
    /// <param name="pressed">
    /// Invoked when the combination fires. Marshalled to the UI thread for you, so it is safe to
    /// touch MAUI objects directly.
    /// </param>
    /// <returns>
    /// A registration to dispose when you no longer want the hotkey, or <c>null</c> when the
    /// combination could not be claimed — unparseable, unsupported platform, or already owned by
    /// another application. Registration failure is a normal outcome, not an exception.
    /// </returns>
    IDisposable? Register(string accelerator, Action pressed);
}
