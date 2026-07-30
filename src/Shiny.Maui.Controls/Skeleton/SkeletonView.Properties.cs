using Shiny.Maui.Controls.Infrastructure;
namespace Shiny.Maui.Controls;

public partial class SkeletonView
{
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(SkeletonView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnContentChanged();
            }));
    /// <summary>The real content shown when <see cref="IsBusy"/> is false.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(SkeletonView), false,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnIsBusyChanged((bool)n);
            }));
    /// <summary>When true, the content is hidden and animated shimmer placeholders are shown.</summary>
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    public static readonly BindableProperty SkeletonTemplateProperty = BindableProperty.Create(
        nameof(SkeletonTemplate), typeof(DataTemplate), typeof(SkeletonView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Optional custom placeholder layout. When null, built-in line placeholders are used.</summary>
    public DataTemplate? SkeletonTemplate { get => (DataTemplate?)GetValue(SkeletonTemplateProperty); set => SetValue(SkeletonTemplateProperty, value); }

    public static readonly BindableProperty ItemCountProperty = BindableProperty.Create(
        nameof(ItemCount), typeof(int), typeof(SkeletonView), 3,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Number of placeholder lines in the built-in skeleton (ignored when SkeletonTemplate is set).</summary>
    public int ItemCount { get => (int)GetValue(ItemCountProperty); set => SetValue(ItemCountProperty, value); }

    public static readonly BindableProperty BaseColorProperty = BindableProperty.Create(
        nameof(BaseColor), typeof(Color), typeof(SkeletonView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Fill color of the built-in placeholder shapes. When null, the theme SurfaceContainerHigh token is used.</summary>
    public Color? BaseColor { get => (Color?)GetValue(BaseColorProperty); set => SetValue(BaseColorProperty, value); }

    public static readonly BindableProperty ShimmerColorProperty = BindableProperty.Create(
        nameof(ShimmerColor), typeof(Color), typeof(SkeletonView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnShimmerColorChanged();
            }));
    /// <summary>Color of the sweeping shimmer highlight. When null, the theme SurfaceContainerHighest token is used.</summary>
    public Color? ShimmerColor { get => (Color?)GetValue(ShimmerColorProperty); set => SetValue(ShimmerColorProperty, value); }

    public static readonly BindableProperty ShimmerEnabledProperty = BindableProperty.Create(
        nameof(ShimmerEnabled), typeof(bool), typeof(SkeletonView), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnShimmerEnabledChanged();
            }));
    /// <summary>When false, placeholders are shown statically with no sweeping animation.</summary>
    public bool ShimmerEnabled { get => (bool)GetValue(ShimmerEnabledProperty); set => SetValue(ShimmerEnabledProperty, value); }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration), typeof(uint), typeof(SkeletonView), (uint)1200);
    /// <summary>Duration in ms of a single shimmer sweep across the control.</summary>
    public uint AnimationDuration { get => (uint)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(SkeletonView), 6d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Corner radius of the built-in placeholder shapes.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(SkeletonView), 16d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Height of each built-in placeholder line.</summary>
    public double ItemHeight { get => (double)GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

    public static readonly BindableProperty ItemSpacingProperty = BindableProperty.Create(
        nameof(ItemSpacing), typeof(double), typeof(SkeletonView), 12d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(SkeletonView), () =>
            {
                ((SkeletonView)b).OnSkeletonAppearanceChanged();
            }));
    /// <summary>Vertical spacing between built-in placeholder lines.</summary>
    public double ItemSpacing { get => (double)GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }
}
