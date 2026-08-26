using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

public partial class ProgressLine
{
    static void Relayout(BindableObject b, object? _, object? __)
        => StyleGuard.WhenReady(b, typeof(ProgressLine), () => ((ProgressLine)b).RefreshLayout());

    // Placement
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(
        nameof(Position), typeof(ProgressLinePosition), typeof(ProgressLine), ProgressLinePosition.Top,
        propertyChanged: Relayout);
    /// <summary>Which page edge the line runs along. Defaults to <see cref="ProgressLinePosition.Top"/>.</summary>
    public ProgressLinePosition Position { get => (ProgressLinePosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    public static readonly BindableProperty DockProperty = BindableProperty.Create(
        nameof(Dock), typeof(bool), typeof(ProgressLine), true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ProgressLine), () =>
        {
            var line = (ProgressLine)b;

            // Turning it on after construction has to run the move, not just re-measure.
            if ((bool)n!)
                line.ScheduleDock();
            else
                line.RefreshLayout();
        }));
    /// <summary>
    /// Whether the line moves itself out of wherever it was declared and onto the page edge named by
    /// <see cref="Position"/>. On by default — the line is page chrome, so "across the top of the
    /// window" is the whole point of it.
    /// </summary>
    /// <remarks>
    /// The consequence is that the control does not render where you wrote it, which is surprising
    /// exactly once. Set this to <c>False</c> to keep it inline and get an ordinary thin bar that
    /// fills the slot you gave it — useful under a header you are drawing yourself.
    /// </remarks>
    public bool Dock { get => (bool)GetValue(DockProperty); set => SetValue(DockProperty, value); }

    public static readonly BindableProperty AutoInsetProperty = BindableProperty.Create(
        nameof(AutoInset), typeof(bool), typeof(ProgressLine), true,
        propertyChanged: Relayout);
    /// <summary>
    /// Whether the line offsets itself past the navigation bar (top) or tab bar (bottom) and the
    /// safe area, instead of sitting hard against the page edge.
    /// </summary>
    public bool AutoInset { get => (bool)GetValue(AutoInsetProperty); set => SetValue(AutoInsetProperty, value); }

    public static readonly BindableProperty OffsetProperty = BindableProperty.Create(
        nameof(Offset), typeof(Thickness), typeof(ProgressLine), new Thickness(0),
        propertyChanged: Relayout);
    /// <summary>Extra margin added on top of whatever <see cref="AutoInset"/> resolved.</summary>
    public Thickness Offset { get => (Thickness)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    // Value
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(ProgressLine), 0.0, BindingMode.TwoWay);
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
        nameof(Minimum), typeof(double), typeof(ProgressLine), 0.0);
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(double), typeof(ProgressLine), 100.0);
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly BindableProperty IsIndeterminateProperty = BindableProperty.Create(
        nameof(IsIndeterminate), typeof(bool), typeof(ProgressLine), false);
    public bool IsIndeterminate { get => (bool)GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    // Appearance
    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(ProgressLine), null);
    /// <summary>Fill color. When null, the theme Primary token is used.</summary>
    public Color? BarColor { get => (Color?)GetValue(BarColorProperty); set => SetValue(BarColorProperty, value); }

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(ProgressLine), Colors.Transparent);
    /// <summary>
    /// The unfilled remainder. Transparent by default, unlike <see cref="ProgressBar"/> — a page-edge
    /// line reads as a moving accent, and a visible rail across the whole window reads as a border
    /// that has appeared for no reason.
    /// </summary>
    public Color? TrackColor { get => (Color?)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }

    public static readonly BindableProperty LineHeightProperty = BindableProperty.Create(
        nameof(LineHeight), typeof(double), typeof(ProgressLine), 3.0,
        propertyChanged: Relayout);
    /// <summary>Thickness of the line in device-independent units. Defaults to 3.</summary>
    public double LineHeight { get => (double)GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ProgressLine), 0.0);
    /// <summary>Corner radius of the fill. Square by default, so the line meets the window edges.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly BindableProperty UseGradientProperty = BindableProperty.Create(
        nameof(UseGradient), typeof(bool), typeof(ProgressLine), false);
    public bool UseGradient { get => (bool)GetValue(UseGradientProperty); set => SetValue(UseGradientProperty, value); }

    public static readonly BindableProperty GradientStartColorProperty = BindableProperty.Create(
        nameof(GradientStartColor), typeof(Color), typeof(ProgressLine), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ProgressLine), () =>
            ((ProgressLine)b).ApplyGradientColors()));
    /// <summary>
    /// Where the gradient starts. When null the theme Primary token is used, so a gradient line
    /// follows a theme pack change instead of pinning one product's blue into every app.
    /// </summary>
    public Color? GradientStartColor { get => (Color?)GetValue(GradientStartColorProperty); set => SetValue(GradientStartColorProperty, value); }

    public static readonly BindableProperty GradientEndColorProperty = BindableProperty.Create(
        nameof(GradientEndColor), typeof(Color), typeof(ProgressLine), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ProgressLine), () =>
            ((ProgressLine)b).ApplyGradientColors()));
    /// <summary>Where the gradient ends. When null the theme Tertiary token is used.</summary>
    public Color? GradientEndColor { get => (Color?)GetValue(GradientEndColorProperty); set => SetValue(GradientEndColorProperty, value); }

    /// <summary>
    /// Pushes the gradient ends onto the inner bar, falling back to the theme tokens. Done here
    /// rather than with a plain binding because <see cref="ProgressBar"/>'s gradient properties are
    /// non-nullable — binding null onto them would paint the gradient transparent rather than leave
    /// the token in place. <see cref="ShinyThemeKeys"/> is what the fallback resolves through.
    /// </summary>
    void ApplyGradientColors()
    {
        if (this.GradientStartColor is Color start)
            this.Bar.GradientStartColor = start;
        else
            this.Bar.SetDynamicResource(ProgressBar.GradientStartColorProperty, ShinyThemeKeys.Color.Primary);

        if (this.GradientEndColor is Color end)
            this.Bar.GradientEndColor = end;
        else
            this.Bar.SetDynamicResource(ProgressBar.GradientEndColorProperty, ShinyThemeKeys.Color.Tertiary);
    }

    // Fill animation — forwarded to the inner bar, which owns the slide.
    public static readonly BindableProperty AnimateProgressProperty = BindableProperty.Create(
        nameof(AnimateProgress), typeof(bool), typeof(ProgressLine), true);
    /// <inheritdoc cref="ProgressBar.AnimateProgress"/>
    public bool AnimateProgress { get => (bool)GetValue(AnimateProgressProperty); set => SetValue(AnimateProgressProperty, value); }

    public static readonly BindableProperty ProgressAnimationDurationProperty = BindableProperty.Create(
        nameof(ProgressAnimationDuration), typeof(int), typeof(ProgressLine), 250);
    /// <inheritdoc cref="ProgressBar.ProgressAnimationDuration"/>
    public int ProgressAnimationDuration { get => (int)GetValue(ProgressAnimationDurationProperty); set => SetValue(ProgressAnimationDurationProperty, value); }

    public static readonly BindableProperty ProgressAnimationEasingProperty = BindableProperty.Create(
        nameof(ProgressAnimationEasing), typeof(Easing), typeof(ProgressLine), Easing.CubicOut);
    /// <inheritdoc cref="ProgressBar.ProgressAnimationEasing"/>
    public Easing ProgressAnimationEasing { get => (Easing)GetValue(ProgressAnimationEasingProperty); set => SetValue(ProgressAnimationEasingProperty, value); }

    // Pulse
    public static readonly BindableProperty PulseEnabledProperty = BindableProperty.Create(
        nameof(PulseEnabled), typeof(bool), typeof(ProgressLine), false);
    public bool PulseEnabled { get => (bool)GetValue(PulseEnabledProperty); set => SetValue(PulseEnabledProperty, value); }

    public static readonly BindableProperty PulseColorProperty = BindableProperty.Create(
        nameof(PulseColor), typeof(Color), typeof(ProgressLine), Colors.White);
    public Color PulseColor { get => (Color)GetValue(PulseColorProperty); set => SetValue(PulseColorProperty, value); }

    public static readonly BindableProperty PulseLengthProperty = BindableProperty.Create(
        nameof(PulseLength), typeof(double), typeof(ProgressLine), 0.4);
    public double PulseLength { get => (double)GetValue(PulseLengthProperty); set => SetValue(PulseLengthProperty, value); }

    public static readonly BindableProperty PulseSpeedProperty = BindableProperty.Create(
        nameof(PulseSpeed), typeof(int), typeof(ProgressLine), 800);
    public int PulseSpeed { get => (int)GetValue(PulseSpeedProperty); set => SetValue(PulseSpeedProperty, value); }

    // Visibility
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(ProgressLine), true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ProgressLine), () =>
            ((ProgressLine)b).OnActiveChanged((bool)n!)));
    /// <summary>
    /// Whether the line is showing. This is the animated switch — bind it, not <c>IsVisible</c>,
    /// which is <c>VisualElement</c>'s and cuts straight to hidden with no fade.
    /// </summary>
    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

    public static readonly BindableProperty FadeDurationProperty = BindableProperty.Create(
        nameof(FadeDuration), typeof(int), typeof(ProgressLine), 200);
    /// <summary>Length of the <see cref="IsActive"/> fade in milliseconds. Zero or less cuts.</summary>
    public int FadeDuration { get => (int)GetValue(FadeDurationProperty); set => SetValue(FadeDurationProperty, value); }
}
