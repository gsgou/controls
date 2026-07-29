using System.Windows.Input;
using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

/// <summary>The lower/upper pair produced by a <see cref="RangeSlider"/>.</summary>
public readonly record struct SliderRange(double Lower, double Upper);

public partial class RangeSlider
{
    // LowerValue
    public static readonly BindableProperty LowerValueProperty = BindableProperty.Create(
        nameof(LowerValue), typeof(double), typeof(RangeSlider), 0.0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double LowerValue { get => (double)GetValue(LowerValueProperty); set => SetValue(LowerValueProperty, value); }

    // UpperValue
    public static readonly BindableProperty UpperValueProperty = BindableProperty.Create(
        nameof(UpperValue), typeof(double), typeof(RangeSlider), 100.0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double UpperValue { get => (double)GetValue(UpperValueProperty); set => SetValue(UpperValueProperty, value); }

    // Minimum
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
        nameof(Minimum), typeof(double), typeof(RangeSlider), 0.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    // Maximum
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(double), typeof(RangeSlider), 100.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    // Step
    public static readonly BindableProperty StepProperty = BindableProperty.Create(
        nameof(Step), typeof(double), typeof(RangeSlider), 1.0);
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    // MinimumRange — minimum gap allowed between the two thumbs (hard stop). 0 = no constraint.
    public static readonly BindableProperty MinimumRangeProperty = BindableProperty.Create(
        nameof(MinimumRange), typeof(double), typeof(RangeSlider), 0.0);
    /// <summary>Minimum distance the thumbs may be apart. The dragged thumb stops rather than crossing this gap. 0 disables the constraint.</summary>
    public double MinimumRange { get => (double)GetValue(MinimumRangeProperty); set => SetValue(MinimumRangeProperty, value); }

    // MaximumRange — maximum gap allowed between the two thumbs. Dragging past it pushes the other thumb. 0 = no constraint.
    public static readonly BindableProperty MaximumRangeProperty = BindableProperty.Create(
        nameof(MaximumRange), typeof(double), typeof(RangeSlider), 0.0);
    /// <summary>Maximum distance the thumbs may be apart. Dragging one thumb past this pushes the other along. 0 disables the constraint.</summary>
    public double MaximumRange { get => (double)GetValue(MaximumRangeProperty); set => SetValue(MaximumRangeProperty, value); }

    // ColdColor
    public static readonly BindableProperty ColdColorProperty = BindableProperty.Create(
        nameof(ColdColor), typeof(Color), typeof(RangeSlider), Color.FromArgb("#3B82F6"),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public Color ColdColor { get => (Color)GetValue(ColdColorProperty); set => SetValue(ColdColorProperty, value); }

    // HotColor
    public static readonly BindableProperty HotColorProperty = BindableProperty.Create(
        nameof(HotColor), typeof(Color), typeof(RangeSlider), Color.FromArgb("#EF4444"),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public Color HotColor { get => (Color)GetValue(HotColorProperty); set => SetValue(HotColorProperty, value); }

    // TrackHeight
    public static readonly BindableProperty TrackHeightProperty = BindableProperty.Create(
        nameof(TrackHeight), typeof(double), typeof(RangeSlider), 8.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double TrackHeight { get => (double)GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    // ThumbSize
    public static readonly BindableProperty ThumbSizeProperty = BindableProperty.Create(
        nameof(ThumbSize), typeof(double), typeof(RangeSlider), 24.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double ThumbSize { get => (double)GetValue(ThumbSizeProperty); set => SetValue(ThumbSizeProperty, value); }

    // ThumbColor
    public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(
        nameof(ThumbColor), typeof(Color), typeof(RangeSlider), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var slider = (RangeSlider)b;
            if (n is Color c)
            {
                slider.lowerThumb.BackgroundColor = c;
                slider.upperThumb.BackgroundColor = c;
            }
            else
            {
                slider.lowerThumb.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.OnPrimary);
                slider.upperThumb.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.OnPrimary);
            }
            slider.UpdateVisuals();
        }));
    /// <summary>Thumb fill color. When null, the theme OnPrimary token is used.</summary>
    public Color? ThumbColor { get => (Color?)GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }

    // ThumbBorderWidth
    public static readonly BindableProperty ThumbBorderWidthProperty = BindableProperty.Create(
        nameof(ThumbBorderWidth), typeof(double), typeof(RangeSlider), 2.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double ThumbBorderWidth { get => (double)GetValue(ThumbBorderWidthProperty); set => SetValue(ThumbBorderWidthProperty, value); }

    // ShowTooltip
    public static readonly BindableProperty ShowTooltipProperty = BindableProperty.Create(
        nameof(ShowTooltip), typeof(bool), typeof(RangeSlider), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public bool ShowTooltip { get => (bool)GetValue(ShowTooltipProperty); set => SetValue(ShowTooltipProperty, value); }

    // TooltipBackgroundColor
    public static readonly BindableProperty TooltipBackgroundColorProperty = BindableProperty.Create(
        nameof(TooltipBackgroundColor), typeof(Color), typeof(RangeSlider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    /// <summary>Tooltip badge background color. When null, the theme SurfaceVariant token is used.</summary>
    public Color? TooltipBackgroundColor { get => (Color?)GetValue(TooltipBackgroundColorProperty); set => SetValue(TooltipBackgroundColorProperty, value); }

    // TooltipTextColor
    public static readonly BindableProperty TooltipTextColorProperty = BindableProperty.Create(
        nameof(TooltipTextColor), typeof(Color), typeof(RangeSlider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    /// <summary>Tooltip text color. When null, the theme OnSurfaceVariant token is used.</summary>
    public Color? TooltipTextColor { get => (Color?)GetValue(TooltipTextColorProperty); set => SetValue(TooltipTextColorProperty, value); }

    // TooltipFontSize
    public static readonly BindableProperty TooltipFontSizeProperty = BindableProperty.Create(
        nameof(TooltipFontSize), typeof(double), typeof(RangeSlider), 12.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public double TooltipFontSize { get => (double)GetValue(TooltipFontSizeProperty); set => SetValue(TooltipFontSizeProperty, value); }

    // ValueFormat
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(
        nameof(ValueFormat), typeof(string), typeof(RangeSlider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public string? ValueFormat { get => (string?)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    // TooltipTemplate
    public static readonly BindableProperty TooltipTemplateProperty = BindableProperty.Create(
        nameof(TooltipTemplate), typeof(DataTemplate), typeof(RangeSlider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((RangeSlider)b).UpdateVisuals();
            }));
    public DataTemplate? TooltipTemplate { get => (DataTemplate?)GetValue(TooltipTemplateProperty); set => SetValue(TooltipTemplateProperty, value); }

    // RangeChangedCommand
    public static readonly BindableProperty RangeChangedCommandProperty = BindableProperty.Create(
        nameof(RangeChangedCommand), typeof(ICommand), typeof(RangeSlider));
    public ICommand? RangeChangedCommand { get => (ICommand?)GetValue(RangeChangedCommandProperty); set => SetValue(RangeChangedCommandProperty, value); }

    /// <summary>Fired whenever the lower and/or upper value changes through interaction.</summary>
    public event EventHandler<SliderRange>? RangeChanged;
}
