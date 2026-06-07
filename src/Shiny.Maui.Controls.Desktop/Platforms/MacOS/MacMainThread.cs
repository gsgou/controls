using Foundation;

namespace Shiny.Maui.Controls.Desktop.TrayIcon;

// MAUI's MainThread/Dispatcher abstraction is not wired up on the
// new Microsoft.Maui.Platforms.MacOS host. AppKit demands that
// NSStatusBar / NSStatusItem / NSMenu work happens on the AppKit
// main thread, so we drive it directly through NSObject.
static class MacMainThread
{
    static readonly NSObject Dispatcher = new();

    public static void Invoke(Action action)
    {
        if (NSThread.IsMain)
        {
            action();
        }
        else
        {
            Dispatcher.InvokeOnMainThread(action);
        }
    }

    public static T Invoke<T>(Func<T> func)
    {
        if (NSThread.IsMain)
            return func();

        T result = default!;
        Dispatcher.InvokeOnMainThread(() => result = func());
        return result;
    }
}
