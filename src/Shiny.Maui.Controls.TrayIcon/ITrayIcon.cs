namespace Shiny.Maui.Controls.TrayIcon;

/// <summary>
/// Represents a cross-platform tray (Windows), status bar (macOS / MacCatalyst), or
/// status notifier (Linux) icon. Always create on the UI thread; dispose to remove.
/// </summary>
public interface ITrayIcon : IDisposable
{
    /// <summary>Hover tooltip on Windows / macOS, accessible description on Linux.</summary>
    string? Tooltip { get; set; }

    /// <summary>Optional label shown beside (or instead of) the icon on macOS and Linux. Ignored on Windows.</summary>
    string? Title { get; set; }

    /// <summary>Show or hide the icon without disposing it.</summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// On macOS, marks the icon as a template image so it auto-tints for light/dark menu bars.
    /// No effect on Windows; on Linux uses the "monochrome" theme when available.
    /// </summary>
    bool IsTemplateImage { get; set; }

    /// <summary>Set the icon. Pass a function that returns a fresh stream each time so the host can re-read it for DPI/theme changes.</summary>
    void SetIcon(Func<Stream> iconStreamFactory);

    /// <summary>Set the menu shown on right-click (Windows/Linux) or primary-click (macOS default).</summary>
    void SetMenu(TrayMenu menu);

    /// <summary>Programmatically open the menu — useful from left-click handlers on Windows.</summary>
    void ShowMenu();

    /// <summary>Raised on primary click. Suppressed on platforms where primary click opens the menu (macOS).</summary>
    event EventHandler<TrayClickEventArgs>? PrimaryClick;

    /// <summary>Raised on secondary click. On macOS this fires for control-click / right-click only.</summary>
    event EventHandler<TrayClickEventArgs>? SecondaryClick;

    /// <summary>Raised on double-click of the icon (Windows / macOS only).</summary>
    event EventHandler<TrayClickEventArgs>? DoubleClick;
}

public sealed class TrayClickEventArgs : EventArgs
{
    public TrayClickEventArgs(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    /// <summary>Screen X coordinate of the click in device-independent pixels (best-effort across platforms).</summary>
    public int X { get; }

    /// <summary>Screen Y coordinate of the click.</summary>
    public int Y { get; }
}
