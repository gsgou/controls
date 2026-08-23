namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// MacCatalyst cannot host a click-through overlay above other applications, so the screen glow is
/// unavailable for the same reason the quick entry popup is.
/// </summary>
static class ScreenGlowPlatform
{
    public static bool IsSupported => false;

    public static void Initialize(object platformWindow) { }
    public static void Show(object platformWindow) { }
    public static void Hide(object platformWindow) { }
    public static void Teardown(object platformWindow) { }
    public static Size GetScreenSize() => new(0, 0);
}
