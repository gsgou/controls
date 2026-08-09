using AView = Android.Views.View;
using MotionEventActions = Android.Views.MotionEventActions;

namespace Shiny.Maui.Controls.Infrastructure;

partial class DragTouchHook
{
    AView? hooked;

    partial void AttachPlatform()
    {
        var native = this.view.Handler?.PlatformView as AView;
        if (ReferenceEquals(native, this.hooked))
            return;

        if (this.hooked is not null)
            this.hooked.Touch -= this.OnTouch;

        this.hooked = native;

        // MAUI's own gesture plumbing subscribes to this same event; the .NET for Android binding
        // reuses one listener implementor per view, so this composes with it rather than replacing it.
        if (this.hooked is not null)
            this.hooked.Touch += this.OnTouch;
    }


    void OnTouch(object? sender, AView.TouchEventArgs e)
    {
        // Deliberately never assigns e.Handled - MAUI's handler owns that value and the last
        // writer wins.
        switch (e.Event?.ActionMasked)
        {
            case MotionEventActions.Down:
                this.Pressed?.Invoke();
                break;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                this.Released?.Invoke();
                break;
        }
    }


    /// <remarks>
    /// The standard Android answer to "a scroller is eating my child's gesture". The request walks
    /// the whole ancestor chain, and the framework clears the flag on the next ACTION_DOWN - so it
    /// only ever covers the one gesture that armed a drag; every other touch still scrolls.
    /// </remarks>
    partial void SetPlatformScrollLock(bool locked)
        => this.hooked?.Parent?.RequestDisallowInterceptTouchEvent(locked);


    static partial void SetPlatformRaised(View target, bool raised)
    {
        // bringChildToFront only shuffles the parent's child array - unlike the remove/re-add that
        // MAUI's ZIndex performs, it never detaches the view, so the in-flight touch lives.
        if (raised && target.Handler?.PlatformView is AView native)
            native.BringToFront();
    }
}
