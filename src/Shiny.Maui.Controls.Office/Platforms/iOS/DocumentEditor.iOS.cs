using Foundation;
using UIKit;

namespace Shiny.Maui.Controls.Office;

public partial class DocumentEditor
{
    NSObject? keyboardFrameToken;
    NSObject? keyboardHideToken;

    partial void HookKeyboard()
    {
        this.keyboardFrameToken ??= UIKeyboard.Notifications.ObserveWillChangeFrame(this.OnKeyboardFrame);
        this.keyboardHideToken ??= UIKeyboard.Notifications.ObserveWillHide(this.OnKeyboardHide);
    }

    partial void UnhookKeyboard()
    {
        this.keyboardFrameToken?.Dispose();
        this.keyboardHideToken?.Dispose();
        this.keyboardFrameToken = null;
        this.keyboardHideToken = null;

        this.ApplyKeyboardInset(0);
    }

    void OnKeyboardFrame(object? sender, UIKeyboardEventArgs e)
    {
        if (this.Handler?.PlatformView is not UIView platform || platform.Window is null)
            return;

        // The overlap is measured against this control's own bottom in window space, not the screen's:
        // an editor that only fills the top half of a page is not covered at all, and padding it for
        // the keyboard's full height would shrink it for nothing.
        var frame = platform.ConvertRectToView(platform.Bounds, null);
        var overlap = Math.Max(0, (frame.Y + frame.Height) - e.FrameEnd.Y);

        UIView.Animate(e.AnimationDuration, () => this.ApplyKeyboardInset(overlap));
    }

    void OnKeyboardHide(object? sender, UIKeyboardEventArgs e)
        => UIView.Animate(e.AnimationDuration, () => this.ApplyKeyboardInset(0));
}
