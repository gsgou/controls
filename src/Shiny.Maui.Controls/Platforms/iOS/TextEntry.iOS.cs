using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace Shiny.Maui.Controls;

public partial class TextEntry
{
    UIView? accessoryPlatformView;

    // iOS is the one platform with a real API for this: a view assigned to the responder's
    // InputAccessoryView is docked to the keyboard by the OS and rides its animation exactly.
    partial void ApplyAccessory(KeyboardAccessoryView? bar)
    {
        if (entry.Handler?.PlatformView is not UITextField field)
            return;

        if (bar is null)
        {
            field.InputAccessoryView = null;
            accessoryPlatformView = null;
            if (entry.IsFocused)
                field.ReloadInputViews();
            return;
        }

        var context = entry.Handler.MauiContext;
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

        field.InputAccessoryView = native;
        accessoryPlatformView = native;

        // Swapping the accessory while the field already has focus does nothing until the responder
        // is told to reload its input views.
        if (entry.IsFocused)
            field.ReloadInputViews();
    }

    partial void OnAccessoryFocusChangedPlatform(bool focused)
    {
        if (!focused || accessoryPlatformView is null)
            return;

        // Re-measure on focus: rotation while the field was unfocused leaves a stale width.
        if (entry.Handler?.PlatformView is UITextField field && ResolvedAccessory is KeyboardAccessoryView bar)
        {
            var width = field.Window?.Bounds.Width ?? UIScreen.MainScreen.Bounds.Width;
            if (Math.Abs(accessoryPlatformView.Frame.Width - width) > 0.5)
            {
                ((IView)bar).Measure(width, bar.BarHeight);
                ((IView)bar).Arrange(new Rect(0, 0, width, bar.BarHeight));
                accessoryPlatformView.Frame = new CGRect(0, 0, width, bar.BarHeight);
            }
        }
    }
}
