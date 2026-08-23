namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Not used on Windows.
/// </summary>
/// <remarks>
/// A WinUI 3 window has no per-pixel alpha, so the glow cannot be a transparent MAUI window the way
/// it is on macOS and Linux. <c>WindowsScreenGlow</c> is registered instead and renders the same
/// frames with GDI+ into layered Win32 windows.
/// </remarks>
static class ScreenGlowPlatform
{
    public static bool IsSupported => false;

    public static void Initialize(object platformWindow) { }
    public static void Show(object platformWindow) { }
    public static void Hide(object platformWindow) { }
    public static void Teardown(object platformWindow) { }
    public static Size GetScreenSize() => new(0, 0);
}
