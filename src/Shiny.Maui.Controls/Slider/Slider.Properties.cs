using System.Windows.Input;
using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Slider
{
    // Value
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(Slider), 0.0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    // Minimum
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
        nameof(Minimum), typeof(double), typeof(Slider), 0.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    // Maximum
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(double), typeof(Slider), 100.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    // Step
    public static readonly BindableProperty StepProperty = BindableProperty.Create(
        nameof(Step), typeof(double), typeof(Slider), 1.0);
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    // ColdColor
    public static readonly BindableProperty ColdColorProperty = BindableProperty.Create(
        nameof(ColdColor), typeof(Color), typeof(Slider), Color.FromArgb("#3B82F6"),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public Color ColdColor { get => (Color)GetValue(ColdColorProperty); set => SetValue(ColdColorProperty, value); }

    // HotColor
    public static readonly BindableProperty HotColorProperty = BindableProperty.Create(
        nameof(HotColor), typeof(Color), typeof(Slider), Color.FromArgb("#EF4444"),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public Color HotColor { get => (Color)GetValue(HotColorProperty); set => SetValue(HotColorProperty, value); }

    // TrackHeight
    public static readonly BindableProperty TrackHeightProperty = BindableProperty.Create(
        nameof(TrackHeight), typeof(double), typeof(Slider), 8.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double TrackHeight { get => (double)GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    // ThumbSize
    public static readonly BindableProperty ThumbSizeProperty = BindableProperty.Create(
        nameof(ThumbSize), typeof(double), typeof(Slider), 24.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double ThumbSize { get => (double)GetValue(ThumbSizeProperty); set => SetValue(ThumbSizeProperty, value); }

    // ThumbColor
    public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(
        nameof(ThumbColor), typeof(Color), typeof(Slider), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, () =>
            {
            var slider = (Slider)b;
            if (n is Color c)
                slider.thumb.BackgroundColor = c;
            else
                slider.thumb.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.OnPrimary);
            slider.UpdateVisuals();
        }));
    /// <summary>Thumb fill color. When null, the theme OnPrimary token is used.</summary>
    public Color? ThumbColor { get => (Color?)GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }

    // ThumbBorderWidth
    public static readonly BindableProperty ThumbBorderWidthProperty = BindableProperty.Create(
        nameof(ThumbBorderWidth), typeof(double), typeof(Slider), 2.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double ThumbBorderWidth { get => (double)GetValue(ThumbBorderWidthProperty); set => SetValue(ThumbBorderWidthProperty, value); }

    // ShowTooltip
    public static readonly BindableProperty ShowTooltipProperty = BindableProperty.Create(
        nameof(ShowTooltip), typeof(bool), typeof(Slider), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public bool ShowTooltip { get => (bool)GetValue(ShowTooltipProperty); set => SetValue(ShowTooltipProperty, value); }

    // TooltipBackgroundColor
    public static readonly BindableProperty TooltipBackgroundColorProperty = BindableProperty.Create(
        nameof(TooltipBackgroundColor), typeof(Color), typeof(Slider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    /// <summary>Tooltip badge background color. When null, the theme SurfaceVariant token is used.</summary>
    public Color? TooltipBackgroundColor { get => (Color?)GetValue(TooltipBackgroundColorProperty); set => SetValue(TooltipBackgroundColorProperty, value); }

    // TooltipTextColor
    public static readonly BindableProperty TooltipTextColorProperty = BindableProperty.Create(
        nameof(TooltipTextColor), typeof(Color), typeof(Slider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    /// <summary>Tooltip text color. When null, the theme OnSurfaceVariant token is used.</summary>
    public Color? TooltipTextColor { get => (Color?)GetValue(TooltipTextColorProperty); set => SetValue(TooltipTextColorProperty, value); }

    // TooltipFontSize
    public static readonly BindableProperty TooltipFontSizeProperty = BindableProperty.Create(
        nameof(TooltipFontSize), typeof(double), typeof(Slider), 12.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public double TooltipFontSize { get => (double)GetValue(TooltipFontSizeProperty); set => SetValue(TooltipFontSizeProperty, value); }

    // ValueFormat
    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(
        nameof(ValueFormat), typeof(string), typeof(Slider), null);
    public string? ValueFormat { get => (string?)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    // TooltipTemplate
    public static readonly BindableProperty TooltipTemplateProperty = BindableProperty.Create(
        nameof(TooltipTemplate), typeof(DataTemplate), typeof(Slider), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, () =>
            {
                ((Slider)b).UpdateVisuals();
            }));
    public DataTemplate? TooltipTemplate { get => (DataTemplate?)GetValue(TooltipTemplateProperty); set => SetValue(TooltipTemplateProperty, value); }

    // ValueChangedCommand
    public static readonly BindableProperty ValueChangedCommandProperty = BindableProperty.Create(
        nameof(ValueChangedCommand), typeof(ICommand), typeof(Slider));
    public ICommand? ValueChangedCommand { get => (ICommand?)GetValue(ValueChangedCommandProperty); set => SetValue(ValueChangedCommandProperty, value); }

    // Event
    public event EventHandler<double>? ValueChangedEvent;
}
