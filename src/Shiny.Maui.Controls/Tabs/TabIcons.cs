using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// Builds and maintains the one view an <see cref="ITabIcon"/> renders as.
/// </summary>
/// <remarks>
/// The choice between a <see cref="MotionIconView"/> and an <see cref="Image"/> is made once per
/// icon and can change when the source properties do, so callers hand the previous view back in and
/// take whatever comes out — which is the same instance whenever the kind has not changed. Building
/// a fresh view on every property write would drop the icon's playback state (and, on Android,
/// re-create a native view mid-gesture).
/// </remarks>
static class TabIcons
{
    /// <summary>True when the spec has motion artwork rather than a plain image.</summary>
    public static bool IsMotion(ITabIcon spec)
        => spec.IconSource is not null || !String.IsNullOrWhiteSpace(spec.IconPathData) || !String.IsNullOrWhiteSpace(spec.Icon);

    /// <summary>True when the spec names no artwork at all.</summary>
    public static bool IsEmpty(ITabIcon spec) => !IsMotion(spec) && spec.IconImage is null;

    /// <summary>
    /// Returns the view for <paramref name="spec"/>, reusing <paramref name="existing"/> when it is
    /// already of the right kind. Null when the spec names no artwork.
    /// </summary>
    public static View? Realize(ITabIcon spec, View? existing, double size)
    {
        if (IsEmpty(spec))
            return null;

        if (IsMotion(spec))
        {
            var view = existing as MotionIconView ?? new MotionIconView
            {
                // Manual: the tap target is the whole tab, not the 24pt icon inside it, so the bar
                // plays the icon itself. Left on the default Hover|Press only the presses that
                // happen to land on the glyph would animate.
                Trigger = MotionTrigger.Manual
            };

            view.Icon = spec.Icon;
            view.Source = spec.IconSource;
            view.PathData = spec.IconPathData;
            view.Motion = spec.Motion;
            view.WidthRequest = size;
            view.HeightRequest = size;
            return view;
        }

        var image = existing as Image ?? new Image { Aspect = Aspect.AspectFit };
        image.Source = spec.IconImage;
        image.WidthRequest = size;
        image.HeightRequest = size;
        return image;
    }

    /// <summary>Tints the icon, whichever kind it turned out to be.</summary>
    public static void Tint(View? view, Color? color, string themeKey)
    {
        // Only a motion icon is recoloured. An Image is left exactly as the app drew it: there is no
        // cross-platform tint in MAUI, and pushing the on-surface token through someone's
        // full-colour PNG would flatten it to a silhouette - never what supplying a bitmap meant.
        // Opacity carries the selected/unselected distinction for those instead; see
        // ShinyTabBar.ApplyCellState.
        if (view is not MotionIconView motion)
            return;

        if (color is not null)
            motion.Color = color;
        else
            motion.SetDynamicResource(MotionIconView.ColorProperty, themeKey);
    }


    /// <summary>Plays a motion icon, if it is one and the bar wants motion.</summary>
    public static void Play(View? view)
    {
        if (view is MotionIconView motion)
            motion.Play();
    }
}
