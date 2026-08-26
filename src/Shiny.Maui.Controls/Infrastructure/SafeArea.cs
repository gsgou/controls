namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// The window's top and bottom safe-area insets — the status bar / notch and the home indicator.
/// </summary>
/// <remarks>
/// Only Apple's heads report a non-zero inset here. Android draws its system bars outside the MAUI
/// content area unless the app opts into edge-to-edge, Windows and GTK4 have no concept of one, and
/// the AppKit head's title bar is handled by <c>ContentLayoutRect</c> rather than an inset — so
/// returning zero everywhere else is the right answer, not a gap.
/// </remarks>
static class SafeArea
{
    public static double Top() => Insets().Top;

    public static double Bottom() => Insets().Bottom;

    static (double Top, double Bottom) Insets()
    {
#if IOS || MACCATALYST
        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIKit.UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault();

        return window is null
            ? (0, 0)
            : (window.SafeAreaInsets.Top, window.SafeAreaInsets.Bottom);
#else
        return (0, 0);
#endif
    }
}
