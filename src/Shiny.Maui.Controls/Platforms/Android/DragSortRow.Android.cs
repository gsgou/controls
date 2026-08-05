using Android.Views;
using AView = Android.Views.View;

namespace Shiny.Maui.Controls.Infrastructure;

partial class DragSortRow
{
    AView? hookedHandle;

    /// <remarks>
    /// This is the reason drag sort never worked on Android. A ScrollView calls
    /// onInterceptTouchEvent on every move and takes the gesture away from its children the
    /// moment the touch crosses the touch slop - the handle's pan is cancelled before it ever
    /// reaches GestureStatus.Started. ScrollOrientation.Neither does not help either:
    /// MauiScrollView only tests it in OnTouchEvent, never in OnInterceptTouchEvent.
    ///
    /// The fix is the standard Android one - on ACTION_DOWN over the handle, tell the ancestor
    /// chain not to intercept. The flag is reset by the framework on the next ACTION_DOWN, so
    /// it only ever covers the one gesture that started on a drag handle; touches anywhere else
    /// in the table still scroll normally.
    /// </remarks>
    partial void HookPlatformScrollLock()
    {
        this.DragHandle.HandlerChanged += (_, _) => AttachTouchHook();
        AttachTouchHook();
    }


    void AttachTouchHook()
    {
        var native = this.DragHandle.Handler?.PlatformView as AView;
        if (ReferenceEquals(native, this.hookedHandle))
            return;

        if (this.hookedHandle != null)
            this.hookedHandle.Touch -= OnHandleTouch;

        this.hookedHandle = native;

        // MAUI's own gesture plumbing subscribes to this same event; the .NET for Android
        // binding reuses one listener implementor per view, so this composes with it rather
        // than replacing it.
        if (this.hookedHandle != null)
            this.hookedHandle.Touch += OnHandleTouch;

        Android.Util.Log.Debug("ShinyDragSort", $"attach hook native={native?.GetType().Name ?? "null"}");
    }


    void OnHandleTouch(object? sender, AView.TouchEventArgs e)
    {
        // Deliberately never assigns e.Handled - MAUI's handler owns that value and the
        // last writer wins.
        Android.Util.Log.Debug("ShinyDragSort", $"touch {e.Event?.ActionMasked}");

        if (e.Event?.ActionMasked == MotionEventActions.Down && sender is AView view)
        {
            var parent = view.Parent;
            while (parent != null)
            {
                Android.Util.Log.Debug("ShinyDragSort", $"ancestor {parent.GetType().FullName}");
                parent.RequestDisallowInterceptTouchEvent(true);
                parent = parent.Parent;
            }
        }
    }
}
