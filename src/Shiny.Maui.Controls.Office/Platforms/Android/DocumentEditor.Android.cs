using Android.Views;
using AndroidX.Core.View;

namespace Shiny.Maui.Controls.Office;

public partial class DocumentEditor
{
    Android.Views.View? insetHost;
    ViewTreeObserver.IOnGlobalLayoutListener? layoutListener;

    /// <summary>
    /// Measures the keyboard from the window's visible frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not an <c>OnApplyWindowInsetsListener</c> on this control's own view: insets are dispatched
    /// down the tree and stop wherever something consumes them, so a listener on a nested view is
    /// simply never called in a host whose root already handles them — which is what happened here,
    /// and it fails silently.
    /// </para>
    /// <para>
    /// The decor view's visible frame is the one measurement that is true whatever the host has
    /// configured: it shrinks under <c>adjustResize</c> and stays put under <c>adjustPan</c>, and the
    /// IME inset off the root window covers the edge-to-edge case where neither applies.
    /// </para>
    /// </remarks>
    partial void HookKeyboard()
    {
        if (this.Handler?.PlatformView is not Android.Views.View platform)
            return;

        var root = platform.RootView;
        if (root is null)
            return;

        this.UnhookKeyboard();

        this.insetHost = root;
        this.layoutListener = new LayoutListener(this, platform, root);
        root.ViewTreeObserver?.AddOnGlobalLayoutListener(this.layoutListener);
    }

    partial void UnhookKeyboard()
    {
        if (this.layoutListener is { } listener && this.insetHost?.ViewTreeObserver is { IsAlive: true } observer)
            observer.RemoveOnGlobalLayoutListener(listener);

        this.layoutListener = null;
        this.insetHost = null;
        this.ApplyKeyboardInset(0);
    }

    sealed class LayoutListener(DocumentEditor owner, Android.Views.View platform, Android.Views.View root)
        : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        public void OnGlobalLayout()
        {
            var density = platform.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;

            var ime = ViewCompat.GetRootWindowInsets(root)?.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom ?? 0;

            var location = new int[2];
            platform.GetLocationInWindow(location);

            var bottom = location[1] + platform.Height;
            var keyboardTop = root.Height - ime;

            // Only the part that actually covers this control: an editor with a status line under it
            // is overlapped by less than the keyboard's whole height, and one above the fold not at all.
            owner.ApplyKeyboardInset(ime <= 0 ? 0 : Math.Max(0, (bottom - keyboardTop) / density));
        }
    }
}
