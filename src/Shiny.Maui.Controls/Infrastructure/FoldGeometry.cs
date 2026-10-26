namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>Where a view is in its <b>window</b>, which is the space a fold rectangle is reported in.</summary>
static class FoldGeometry
{
    /// <summary>
    /// The view's rectangle in window coordinates (device-independent units), or null when it has no
    /// size yet. Asks the platform view first — it knows about toolbars, navigation bars and anything
    /// else MAUI does not position — and falls back to adding up the MAUI frames, which is right for a
    /// view laid out without a handler (tests) and close enough on the platforms that have no fold.
    /// </summary>
    public static Rect? WindowBounds(VisualElement view)
    {
        if (view.Width <= 0 || view.Height <= 0)
            return null;

#if ANDROID
        if (view.Handler?.PlatformView is Android.Views.View native && native.IsAttachedToWindow)
        {
            var location = new int[2];
            native.GetLocationInWindow(location);
            var density = native.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;

            return new Rect(location[0] / density, location[1] / density, view.Width, view.Height);
        }
#elif IOS || MACCATALYST
        if (view.Handler?.PlatformView is UIKit.UIView native && native.Window is not null)
        {
            var frame = native.ConvertRectToView(native.Bounds, null);
            return new Rect(frame.X, frame.Y, view.Width, view.Height);
        }
#endif

        return Walk(view);
    }


    /// <summary>
    /// The fold (grown by the hinge margin) in <paramref name="view"/>'s own coordinates, or null when
    /// there is no posture source, no fold, or the fold does not divide this view. A one-off read; a
    /// control that has to follow the fold uses a <see cref="FoldTracker"/>.
    /// </summary>
    public static Rect? FoldIn(this VisualElement view)
    {
        if (!FoldPosture.HasSource || FoldPosture.Current.Avoid is not { } avoid)
            return null;

        return WindowBounds(view) is { } bounds ? FoldLayout.FoldIn(bounds, avoid) : null;
    }


    /// <summary>The sum of every ancestor's frame, scroll offsets taken back off.</summary>
    internal static Rect Walk(VisualElement view)
    {
        double x = 0;
        double y = 0;
        Element? current = view;

        while (current is not null)
        {
            if (current is VisualElement ve)
            {
                x += ve.X;
                y += ve.Y;

                if (!ReferenceEquals(current, view) && current is ScrollView scroll)
                {
                    x -= scroll.ScrollX;
                    y -= scroll.ScrollY;
                }
            }

            current = current.Parent;
        }

        return new Rect(x, y, view.Width, view.Height);
    }
}
