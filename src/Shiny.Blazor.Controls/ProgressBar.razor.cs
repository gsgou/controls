using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

public partial class ProgressBar : IDisposable
{
    Timer? pulseTimer;
    bool isPulsing;
    int pulseKey;

    // Value
    [Parameter] public double Value { get; set; }
    [Parameter] public EventCallback<double> ValueChanged { get; set; }
    [Parameter] public double Minimum { get; set; } = 0;
    [Parameter] public double Maximum { get; set; } = 100;

    // Appearance
    [Parameter] public string TrackColor { get; set; } = "var(--shiny-color-surface-container-highest, #E5E7EB)";
    [Parameter] public string BarColor { get; set; } = "var(--shiny-color-primary, #3B82F6)";
    [Parameter] public double TrackHeight { get; set; } = 8;
    [Parameter] public string CornerRadius { get; set; } = "var(--shiny-shape-corner-extra-small, 4px)";

    // Gradient
    [Parameter] public bool UseGradient { get; set; }
    [Parameter] public string GradientStartColor { get; set; } = "var(--shiny-color-primary, #3B82F6)";
    [Parameter] public string GradientEndColor { get; set; } = "var(--shiny-color-tertiary, #8B5CF6)";

    // Pulse (Vista-style shimmer sweep)
    [Parameter] public bool PulseEnabled { get; set; }
    [Parameter] public bool PulseOnValueChange { get; set; } = true;
    [Parameter] public TimeSpan PulseInterval { get; set; } = TimeSpan.Zero;
    [Parameter] public string PulseColor { get; set; } = "rgba(255,255,255,0.4)";
    [Parameter] public double PulseLength { get; set; } = 0.4;
    [Parameter] public int PulseSpeed { get; set; } = 800;

    // Text
    [Parameter] public bool ShowText { get; set; }
    [Parameter] public string TextFormat { get; set; } = "{0:0}%";
    [Parameter] public string TextColor { get; set; } = "var(--shiny-color-on-primary, #FFFFFF)";
    /// <summary>Overlay label size in px. The default, <c>-1</c>, follows the theme type scale.</summary>
    [Parameter] public double FontSize { get; set; } = -1;

    // Indeterminate
    [Parameter] public bool IsIndeterminate { get; set; }

    // General
    [Parameter] public string? CssClass { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    double Percentage => Maximum > Minimum
        ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) * 100
        : 0;

    string FormattedText => string.Format(TextFormat, Percentage);

    string RootStyle => "";

    string TrackStyle => $"height: {TrackHeight}px; border-radius: {CornerRadius}; background: {TrackColor};";

    string FillStyle
    {
        get
        {
            var width = IsIndeterminate ? "30%" : $"{Percentage}%";
            var bg = UseGradient
                ? $"linear-gradient(to right, {GradientStartColor}, {GradientEndColor})"
                : BarColor;
            var lengthPct = Math.Clamp(PulseLength, 0.05, 1.0) * 100;
            var pulseVars = $"--pulse-color: {PulseColor}; --pulse-speed: {PulseSpeed}ms; --pulse-length: {lengthPct}%;";
            return $"width: {width}; background: {bg}; border-radius: {CornerRadius}; {pulseVars}";
        }
    }

    string TextStyle => $"color: {TextColor}; font-size: {(FontSize >= 0 ? $"{FontSize}px" : "var(--shiny-type-label-small-size, 11px)")};";

    string PulseClass => isPulsing ? "shiny-pb-pulse" : "";

    string IndeterminateClass => IsIndeterminate ? "shiny-pb--indeterminate" : "";

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var oldValue = Value;
        var oldPulseInterval = PulseInterval;
        var oldPulseEnabled = PulseEnabled;

        await base.SetParametersAsync(parameters);

        if (PulseEnabled && PulseOnValueChange && Math.Abs(Value - oldValue) > double.Epsilon && oldValue != 0)
            TriggerPulse();

        if (PulseEnabled != oldPulseEnabled || PulseInterval != oldPulseInterval)
            ConfigurePulseTimer();
    }

    void TriggerPulse()
    {
        isPulsing = false;
        pulseKey++;
        var capturedKey = pulseKey;
        _ = Task.Run(async () =>
        {
            await Task.Yield();
            if (capturedKey != pulseKey) return;
            isPulsing = true;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(PulseSpeed + 100);
            if (capturedKey != pulseKey) return;
            isPulsing = false;
            await InvokeAsync(StateHasChanged);
        });
    }

    void ConfigurePulseTimer()
    {
        StopPulseTimer();

        if (!PulseEnabled || PulseInterval <= TimeSpan.Zero)
            return;

        pulseTimer = new Timer(_ =>
        {
            isPulsing = true;
            InvokeAsync(StateHasChanged);

            Task.Delay(PulseSpeed + 100).ContinueWith(_ =>
            {
                isPulsing = false;
                InvokeAsync(StateHasChanged);
            });
        }, null, PulseInterval, PulseInterval);
    }

    void StopPulseTimer()
    {
        pulseTimer?.Dispose();
        pulseTimer = null;
    }

    public void Dispose()
    {
        StopPulseTimer();
        GC.SuppressFinalize(this);
    }
}
