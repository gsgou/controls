using Shiny.Maui.Controls.Infrastructure;
namespace Shiny.Maui.Controls;

public partial class BadgeView
{
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(BadgeView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnContentChanged();
            }));
    /// <summary>The view the badge is overlaid on top of.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(BadgeView), string.Empty,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge text. When empty/null and <see cref="IsDot"/> is false, the badge is hidden.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public static readonly BindableProperty PositionProperty = BindableProperty.Create(
        nameof(Position), typeof(BadgePosition), typeof(BadgeView), BadgePosition.TopRight,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Corner of the wrapped content where the badge is anchored.</summary>
    public BadgePosition Position { get => (BadgePosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(
        nameof(BadgeColor), typeof(Color), typeof(BadgeView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge fill color. When null, the theme Error token is used.</summary>
    public Color? BadgeColor { get => (Color?)GetValue(BadgeColorProperty); set => SetValue(BadgeColorProperty, value); }

    public static readonly BindableProperty BadgeTextColorProperty = BindableProperty.Create(
        nameof(BadgeTextColor), typeof(Color), typeof(BadgeView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge text color. When null, the theme OnError token is used.</summary>
    public Color? BadgeTextColor { get => (Color?)GetValue(BadgeTextColorProperty); set => SetValue(BadgeTextColorProperty, value); }

    public static readonly BindableProperty BadgeBorderColorProperty = BindableProperty.Create(
        nameof(BadgeBorderColor), typeof(Color), typeof(BadgeView), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge border (stroke) color. When null, the theme Surface token is used, creating a clean separation from the wrapped content.</summary>
    public Color? BadgeBorderColor { get => (Color?)GetValue(BadgeBorderColorProperty); set => SetValue(BadgeBorderColorProperty, value); }

    public static readonly BindableProperty BadgeBorderThicknessProperty = BindableProperty.Create(
        nameof(BadgeBorderThickness), typeof(double), typeof(BadgeView), 1.5,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Thickness of the badge border.</summary>
    public double BadgeBorderThickness { get => (double)GetValue(BadgeBorderThicknessProperty); set => SetValue(BadgeBorderThicknessProperty, value); }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(BadgeView), 10.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge text font size.</summary>
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(BadgeView), Microsoft.Maui.Controls.FontAttributes.Bold,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge text font attributes.</summary>
    public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(BadgeView), 999.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Badge corner radius. Default is a fully rounded pill.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly BindableProperty BadgePaddingProperty = BindableProperty.Create(
        nameof(BadgePadding), typeof(Thickness), typeof(BadgeView), new Thickness(6, 2),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Inner padding of the badge.</summary>
    public Thickness BadgePadding { get => (Thickness)GetValue(BadgePaddingProperty); set => SetValue(BadgePaddingProperty, value); }

    public static readonly BindableProperty OffsetXProperty = BindableProperty.Create(
        nameof(OffsetX), typeof(double), typeof(BadgeView), 4.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Horizontal nudge from the corner. Positive values push the badge outward (away from the content).</summary>
    public double OffsetX { get => (double)GetValue(OffsetXProperty); set => SetValue(OffsetXProperty, value); }

    public static readonly BindableProperty OffsetYProperty = BindableProperty.Create(
        nameof(OffsetY), typeof(double), typeof(BadgeView), -4.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Vertical nudge from the corner. Positive values push the badge downward; negative pushes upward.</summary>
    public double OffsetY { get => (double)GetValue(OffsetYProperty); set => SetValue(OffsetYProperty, value); }

    public static readonly BindableProperty IsDotProperty = BindableProperty.Create(
        nameof(IsDot), typeof(bool), typeof(BadgeView), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>When true, the badge is rendered as a small dot (text is ignored).</summary>
    public bool IsDot { get => (bool)GetValue(IsDotProperty); set => SetValue(IsDotProperty, value); }

    public static readonly BindableProperty DotSizeProperty = BindableProperty.Create(
        nameof(DotSize), typeof(double), typeof(BadgeView), 10.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>Diameter (px) of the badge in dot mode.</summary>
    public double DotSize { get => (double)GetValue(DotSizeProperty); set => SetValue(DotSizeProperty, value); }

    public static readonly BindableProperty MaxCountProperty = BindableProperty.Create(
        nameof(MaxCount), typeof(int), typeof(BadgeView), 0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnBadgeVisualChanged();
            }));
    /// <summary>When greater than zero and <see cref="Text"/> parses as a number above this limit, the badge displays "{MaxCount}+".</summary>
    public int MaxCount { get => (int)GetValue(MaxCountProperty); set => SetValue(MaxCountProperty, value); }

    public static readonly BindableProperty IsAnimatedProperty = BindableProperty.Create(
        nameof(IsAnimated), typeof(bool), typeof(BadgeView), true);
    /// <summary>When true, the badge scales in/out as it appears or disappears.</summary>
    public bool IsAnimated { get => (bool)GetValue(IsAnimatedProperty); set => SetValue(IsAnimatedProperty, value); }

    public static readonly BindableProperty IsPulsingProperty = BindableProperty.Create(
        nameof(IsPulsing), typeof(bool), typeof(BadgeView), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((BadgeView)b).OnPulsingChanged();
            }));
    /// <summary>When true, the badge continuously pulses to draw attention.</summary>
    public bool IsPulsing { get => (bool)GetValue(IsPulsingProperty); set => SetValue(IsPulsingProperty, value); }
}
