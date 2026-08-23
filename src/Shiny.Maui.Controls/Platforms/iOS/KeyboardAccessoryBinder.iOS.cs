using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace Shiny.Maui.Controls;

partial class KeyboardAccessoryBinder
{
    UIView? accessoryPlatformView;

    // iOS is the one platform with a real API for this: a view assigned to the responder's
    // InputAccessoryView is docked to the keyboard by the OS and rides its animation exactly.
    // InputAccessoryView is only settable on UITextField and UITextView - on UIResponder itself it is
    // get-only - so both are handled by hand rather than through the base type.
    partial void ApplyPlatform(KeyboardAccessoryView? bar)
    {
        if (PlatformInput is not UIView field)
            return;

        if (bar is null)
        {
            SetInputAccessory(field, null);
            accessoryPlatformView = null;
            if (input.IsFocused)
                field.ReloadInputViews();
            return;
        }

        var context = input.Handler?.MauiContext;
        if (context is null)
            return;

        var native = bar.ToPlatform(context);

        // A view handed to InputAccessoryView never gets a MAUI layout pass, so it has to be
        // measured and arranged by hand. Skip this and the bar is there but zero points tall -
        // which reads as "the accessory never appeared".
        var width = field.Window?.Bounds.Width ?? UIScreen.MainScreen.Bounds.Width;
        var height = bar.BarHeight;
        ((IView)bar).Measure(width, height);
        ((IView)bar).Arrange(new Rect(0, 0, width, height));

        native.Frame = new CGRect(0, 0, width, height);
        native.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

        SetInputAccessory(field, native);
        accessoryPlatformView = native;

        // Swapping the accessory while the field already has focus does nothing until the responder
        // is told to reload its input views.
        if (input.IsFocused)
            field.ReloadInputViews();
    }

    // Either half of the pair, or null for anything else - a handler that has not been created yet,
    // or a platform view that is neither (which simply cannot carry an accessory).
    UIView? PlatformInput => input.Handler?.PlatformView switch
    {
        UITextField field => field,
        UITextView text => text,
        _ => null
    };

    static void SetInputAccessory(UIView view, UIView? accessory)
    {
        switch (view)
        {
            case UITextField field:
                field.InputAccessoryView = accessory;
                break;

            case UITextView text:
                text.InputAccessoryView = accessory;
                break;
        }
    }

    partial void OnFocusChangedPlatform(bool focused)
    {
        if (!focused || accessoryPlatformView is null)
            return;

        // Re-measure on focus: rotation while the field was unfocused leaves a stale width.
        if (PlatformInput is UIView field && bar is KeyboardAccessoryView current)
        {
            var width = field.Window?.Bounds.Width ?? UIScreen.MainScreen.Bounds.Width;
            if (Math.Abs(accessoryPlatformView.Frame.Width - width) > 0.5)
            {
                ((IView)current).Measure(width, current.BarHeight);
                ((IView)current).Arrange(new Rect(0, 0, width, current.BarHeight));
                accessoryPlatformView.Frame = new CGRect(0, 0, width, current.BarHeight);
            }
        }
    }
}
