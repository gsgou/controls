using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.QuickEntry;
namespace Shiny.Maui.Controls;

public partial class Overlay
{
    public static readonly BindableProperty IsShownProperty = BindableProperty.Create(
        nameof(IsShown), typeof(bool), typeof(Overlay), false,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(Overlay), () =>
            {
                ((Overlay)b).OnIsShownChanged((bool)n);
            }));
    public bool IsShown { get => (bool)GetValue(IsShownProperty); set => SetValue(IsShownProperty, value); }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration), typeof(uint), typeof(Overlay), (uint)250);
    public uint AnimationDuration { get => (uint)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    public static readonly BindableProperty BlurRadiusProperty = BindableProperty.Create(
        nameof(BlurRadius), typeof(double), typeof(Overlay), 0d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Overlay), () =>
            {
                ((Overlay)b).OnBlurRadiusChanged();
            }));
    /// <summary>
    /// When set to a value greater than 0, applies a frosted glass blur effect to the backdrop.
    /// Uses native platform blur (UIVisualEffectView on iOS, RenderEffect on Android 12+).
    /// </summary>
    public double BlurRadius { get => (double)GetValue(BlurRadiusProperty); set => SetValue(BlurRadiusProperty, value); }

    public static readonly BindableProperty OverlayContentTemplateProperty = BindableProperty.Create(
        nameof(OverlayContentTemplate), typeof(DataTemplate), typeof(Overlay),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Overlay), () =>
            {
                ((Overlay)b).UpdateOverlayContent();
            }));
    public DataTemplate? OverlayContentTemplate { get => (DataTemplate?)GetValue(OverlayContentTemplateProperty); set => SetValue(OverlayContentTemplateProperty, value); }

    public static readonly BindableProperty ShowEdgeGlowProperty = BindableProperty.Create(
        nameof(ShowEdgeGlow), typeof(bool), typeof(Overlay), false);
    /// <summary>
    /// Rims the page with an animated colour wash while the overlay is shown — the Siri-style glow.
    /// Sits behind the content and in front of the backdrop, and is click-through, so it is purely a
    /// signal that something is happening. Configure it with <see cref="GlowOptions"/>.
    /// </summary>
    public bool ShowEdgeGlow { get => (bool)GetValue(ShowEdgeGlowProperty); set => SetValue(ShowEdgeGlowProperty, value); }

    public static readonly BindableProperty GlowOptionsProperty = BindableProperty.Create(
        nameof(GlowOptions), typeof(ScreenGlowOptions), typeof(Overlay), null);
    /// <summary>
    /// Appearance of the <see cref="ShowEdgeGlow"/> wash — thickness, palette, speed, pulse and
    /// intensity. Leave null for the defaults.
    /// </summary>
    public ScreenGlowOptions? GlowOptions { get => (ScreenGlowOptions?)GetValue(GlowOptionsProperty); set => SetValue(GlowOptionsProperty, value); }

    public static readonly BindableProperty ContentAlignmentProperty = BindableProperty.Create(
        nameof(ContentAlignment), typeof(LayoutOptions), typeof(Overlay), LayoutOptions.Center,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Overlay), () =>
            {
                ((Overlay)b).UpdateContentPlacement();
            }));
    /// <summary>
    /// Where the overlay's content sits vertically. Centred by default, which is right for a dialog;
    /// <c>Start</c> and <c>End</c> put it near the top or bottom edge, for a command bar or a prompt
    /// summoned over the page.
    /// </summary>
    public LayoutOptions ContentAlignment { get => (LayoutOptions)GetValue(ContentAlignmentProperty); set => SetValue(ContentAlignmentProperty, value); }

    public static readonly BindableProperty ContentMarginProperty = BindableProperty.Create(
        nameof(ContentMargin), typeof(Thickness), typeof(Overlay), new Thickness(0),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Overlay), () =>
            {
                ((Overlay)b).UpdateContentPlacement();
            }));
    /// <summary>Inset applied to the content, so a <see cref="ContentAlignment"/> of Start or End can be offset from the edge.</summary>
    public Thickness ContentMargin { get => (Thickness)GetValue(ContentMarginProperty); set => SetValue(ContentMarginProperty, value); }

    public static readonly BindableProperty CloseOnBackdropTapProperty = BindableProperty.Create(
        nameof(CloseOnBackdropTap), typeof(bool), typeof(Overlay), true);
    /// <summary>
    /// When true (the default), tapping the dimmed backdrop hides the overlay (sets <see cref="IsShown"/> to false).
    /// Set false for overlays that must remain until dismissed programmatically — e.g. a loading overlay.
    /// </summary>
    public bool CloseOnBackdropTap { get => (bool)GetValue(CloseOnBackdropTapProperty); set => SetValue(CloseOnBackdropTapProperty, value); }
}
