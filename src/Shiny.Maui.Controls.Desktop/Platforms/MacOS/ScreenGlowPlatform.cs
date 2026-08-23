using AppKit;
using CoreGraphics;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// AppKit backing for the screen-edge glow: a borderless, transparent, click-through window pinned
/// above everything else and present on every Space.
/// </summary>
/// <remarks>
/// Unlike the quick entry popup this window must never take focus, so the borderless style mask is
/// exactly right here — a borderless <c>NSWindow</c> cannot become key, which is the behaviour we
/// want rather than the problem it is for the popup.
/// </remarks>
static class ScreenGlowPlatform
{
    public static bool IsSupported => true;

    public static void Initialize(object platformWindow)
    {
        if (platformWindow is not NSWindow window)
            return;

        MacMainThread.Invoke(() =>
        {
            window.StyleMask = NSWindowStyle.Borderless;
            window.IsOpaque = false;
            window.BackgroundColor = NSColor.Clear;
            window.HasShadow = false;
            window.IgnoresMouseEvents = true;
            window.Level = NSWindowLevel.ScreenSaver;
            window.HidesOnDeactivate = false;
            window.SetExcludedFromWindowsMenu(true);
            window.CollectionBehavior =
                NSWindowCollectionBehavior.CanJoinAllSpaces |
                NSWindowCollectionBehavior.FullScreenAuxiliary |
                NSWindowCollectionBehavior.Stationary |
                NSWindowCollectionBehavior.IgnoresCycle;

            if (window.ContentView is { } content)
            {
                content.WantsLayer = true;
                if (content.Layer != null)
                    content.Layer.BackgroundColor = NSColor.Clear.CGColor;
            }

            window.AlphaValue = 0f;
        });
    }

    public static void Show(object platformWindow)
    {
        if (platformWindow is not NSWindow window)
            return;

        MacMainThread.Invoke(() =>
        {
            var screen = QuickEntryPlatform.ScreenForPointer();
            window.SetFrame(screen.Frame, true);
            window.AlphaValue = 1f;
            // OrderFrontRegardless, never MakeKey: the glow must not steal focus from whatever the
            // user is typing into.
            window.OrderFrontRegardless();
        });
    }

    public static void Hide(object platformWindow)
    {
        if (platformWindow is not NSWindow window)
            return;

        MacMainThread.Invoke(() =>
        {
            window.AlphaValue = 0f;
            window.OrderOut(null);
        });
    }

    public static void Teardown(object platformWindow) { }

    public static Size GetScreenSize()
        => MacMainThread.Invoke(() =>
        {
            var frame = QuickEntryPlatform.ScreenForPointer().Frame;
            return new Size(frame.Width, frame.Height);
        });
}
