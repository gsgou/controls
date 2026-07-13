namespace Shiny.Maui.Controls;

public partial class Overlay
{
    public static readonly BindableProperty IsShownProperty = BindableProperty.Create(
        nameof(IsShown), typeof(bool), typeof(Overlay), false,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((Overlay)b).OnIsShownChanged((bool)n));
    public bool IsShown { get => (bool)GetValue(IsShownProperty); set => SetValue(IsShownProperty, value); }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration), typeof(uint), typeof(Overlay), (uint)250);
    public uint AnimationDuration { get => (uint)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    public static readonly BindableProperty BlurRadiusProperty = BindableProperty.Create(
        nameof(BlurRadius), typeof(double), typeof(Overlay), 0d,
        propertyChanged: (b, _, _) => ((Overlay)b).OnBlurRadiusChanged());
    /// <summary>
    /// When set to a value greater than 0, applies a frosted glass blur effect to the backdrop.
    /// Uses native platform blur (UIVisualEffectView on iOS, RenderEffect on Android 12+).
    /// </summary>
    public double BlurRadius { get => (double)GetValue(BlurRadiusProperty); set => SetValue(BlurRadiusProperty, value); }

    public static readonly BindableProperty OverlayContentTemplateProperty = BindableProperty.Create(
        nameof(OverlayContentTemplate), typeof(DataTemplate), typeof(Overlay),
        propertyChanged: (b, _, _) => ((Overlay)b).UpdateOverlayContent());
    public DataTemplate? OverlayContentTemplate { get => (DataTemplate?)GetValue(OverlayContentTemplateProperty); set => SetValue(OverlayContentTemplateProperty, value); }

    public static readonly BindableProperty CloseOnBackdropTapProperty = BindableProperty.Create(
        nameof(CloseOnBackdropTap), typeof(bool), typeof(Overlay), true);
    /// <summary>
    /// When true (the default), tapping the dimmed backdrop hides the overlay (sets <see cref="IsShown"/> to false).
    /// Set false for overlays that must remain until dismissed programmatically — e.g. a loading overlay.
    /// </summary>
    public bool CloseOnBackdropTap { get => (bool)GetValue(CloseOnBackdropTapProperty); set => SetValue(CloseOnBackdropTapProperty, value); }
}
