using AndroidX.ViewPager2.Widget;
using AView = Android.Views.View;
using ARect = Android.Graphics.Rect;

namespace Shiny.Maui.Controls.SignaturePad;

public partial class SignaturePad
{
    ViewPager2? lockedPager;
    bool pagerWasInputEnabled;
    EventHandler<AView.LayoutChangeEventArgs>? exclusionLayoutHandler;

    partial void SetBackGestureEnabled(bool enabled)
    {
        if (graphicsView?.Handler?.PlatformView is not AView native)
            return;

        if (!enabled)
        {
            // 1) Stop the Android 10+ (API 29+) system back gesture from
            //    stealing strokes that begin near the left/right screen edges.
            //    The exclusion rect is in the view's own coordinate space, so
            //    re-apply it on every layout pass (size isn't known until the
            //    panel has finished opening).
            ApplyGestureExclusion(native);
            exclusionLayoutHandler = (_, _) => ApplyGestureExclusion(native);
            native.LayoutChange += exclusionLayoutHandler;

            // 2) If we're hosted inside a TabbedPage, freeze its ViewPager2 so
            //    horizontal strokes don't swipe to the next/previous tab.
            lockedPager = FindViewPager(native);
            if (lockedPager is not null)
            {
                pagerWasInputEnabled = lockedPager.UserInputEnabled;
                lockedPager.UserInputEnabled = false;
            }
        }
        else
        {
            if (exclusionLayoutHandler is not null)
            {
                native.LayoutChange -= exclusionLayoutHandler;
                exclusionLayoutHandler = null;
            }
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                native.SystemGestureExclusionRects = new List<ARect>();

            if (lockedPager is not null)
            {
                lockedPager.UserInputEnabled = pagerWasInputEnabled;
                lockedPager = null;
            }
        }
    }

    static void ApplyGestureExclusion(AView view)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            return;
        if (view.Width <= 0 || view.Height <= 0)
            return;

        view.SystemGestureExclusionRects = new List<ARect>
        {
            new ARect(0, 0, view.Width, view.Height)
        };
    }

    static ViewPager2? FindViewPager(AView view)
    {
        var parent = view.Parent;
        while (parent is not null)
        {
            if (parent is ViewPager2 pager)
                return pager;

            parent = (parent as AView)?.Parent;
        }
        return null;
    }
}
