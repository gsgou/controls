using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class Slider : IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = default!;

    ElementReference trackRef;
    IJSObjectReference? module;
    DotNetObjectReference<Slider>? selfRef;

    // Parameters
    [Parameter] public double Value { get; set; }
    [Parameter] public EventCallback<double> ValueChanged { get; set; }
    [Parameter] public double Minimum { get; set; } = 0;
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public double Step { get; set; } = 1;
    [Parameter] public string ColdColor { get; set; } = "#3B82F6";
    [Parameter] public string HotColor { get; set; } = "#EF4444";
    [Parameter] public double TrackHeight { get; set; } = 8;
    [Parameter] public double ThumbSize { get; set; } = 24;
    [Parameter] public string ThumbColor { get; set; } = "var(--shiny-color-surface, #FFFFFF)";
    /// <summary>Thumb ring width in px. The default, <c>-1</c>, follows the theme border scale.</summary>
    [Parameter] public double ThumbBorderWidth { get; set; } = -1;
    [Parameter] public string CornerRadius { get; set; } = "var(--shiny-shape-corner-extra-small, 4px)";
    [Parameter] public bool ShowTooltip { get; set; } = true;
    [Parameter] public string TooltipBackgroundColor { get; set; } = "var(--shiny-color-inverse-surface, #1F2937)";
    [Parameter] public string TooltipTextColor { get; set; } = "var(--shiny-color-inverse-on-surface, #FFFFFF)";
    /// <summary>Tooltip label size in px. The default, <c>-1</c>, follows the theme type scale.</summary>
    [Parameter] public double TooltipFontSize { get; set; } = -1;
    [Parameter] public string? ValueFormat { get; set; }
    [Parameter] public RenderFragment<double>? TooltipTemplate { get; set; }
    [Parameter] public bool IsEnabled { get; set; } = true;
    [Parameter] public string? CssClass { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    double Percentage => Maximum > Minimum
        ? ((Value - Minimum) / (Maximum - Minimum)) * 100
        : 0;

    string BlendedColor => BlendColors(ColdColor, HotColor, Percentage / 100.0);

    string RootStyle => IsEnabled ? "" : "opacity: 0.5; pointer-events: none;";

    string TrackStyle => $"height: {TrackHeight}px; border-radius: {CornerRadius}; background: {BlendedColor};";

    string TrackFillStyle => $"width: {Percentage}%; height: 100%; border-radius: {CornerRadius}; background: transparent;";

    string ThumbStyle => $"left: {Percentage}%; width: {ThumbSize}px; height: {ThumbSize}px; border: {(ThumbBorderWidth >= 0 ? $"{ThumbBorderWidth}px" : "var(--shiny-border-medium, 2px)")} solid {BlendedColor}; background: {ThumbColor};";

    // Shift transform from -50% toward 0% at left edge and -100% at right edge to prevent overflow
    string TooltipTransformStyle
    {
        get
        {
            var pct = Percentage / 100.0;
            // At 0% → translateX(0%), at 50% → translateX(-50%), at 100% → translateX(-100%)
            var translatePct = -pct * 100;
            return $"transform: translateX({translatePct:0.#}%);";
        }
    }

    string TooltipBadgeStyle => $"background: {TooltipBackgroundColor}; color: {TooltipTextColor}; font-size: {(TooltipFontSize >= 0 ? $"{TooltipFontSize}px" : "var(--shiny-type-body-small-size, 12px)")};";

    string TooltipPointerStyle => $"border-top-color: {TooltipBackgroundColor};";

    string FormatValue(double val)
    {
        if (!string.IsNullOrEmpty(ValueFormat))
            return val.ToString(ValueFormat);
        return val % 1 == 0 ? val.ToString("0") : val.ToString("0.#");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Shiny.Blazor.Controls/slider.js");
            selfRef = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("init", trackRef, selfRef);
        }
    }

    async Task OnTrackClick(MouseEventArgs e)
    {
        if (!IsEnabled || module is null) return;

        var percent = await module.InvokeAsync<double>("getClickPercent", trackRef, e.ClientX);
        await SetValueFromPercent(percent);
    }

    void OnThumbPointerDown(PointerEventArgs e)
    {
        // Drag is handled via JS
    }

    [JSInvokable]
    public async Task OnDragUpdate(double percent)
    {
        await SetValueFromPercent(percent);
    }

    async Task SetValueFromPercent(double percent)
    {
        percent = Math.Clamp(percent, 0, 1);
        var rawValue = Minimum + (percent * (Maximum - Minimum));

        // Snap to step
        if (Step > 0)
            rawValue = Math.Round(rawValue / Step) * Step;

        rawValue = Math.Clamp(rawValue, Minimum, Maximum);

        if (Math.Abs(rawValue - Value) > double.Epsilon)
        {
            Value = rawValue;
            await ValueChanged.InvokeAsync(Value);
            StateHasChanged();
        }
    }

    static string BlendColors(string color1, string color2, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        var (r1, g1, b1) = ParseHex(color1);
        var (r2, g2, b2) = ParseHex(color2);

        var r = (int)(r1 + (r2 - r1) * ratio);
        var g = (int)(g1 + (g2 - g1) * ratio);
        var b = (int)(b1 + (b2 - b1) * ratio);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    static (int r, int g, int b) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";

        return (
            Convert.ToInt32(hex[..2], 16),
            Convert.ToInt32(hex[2..4], 16),
            Convert.ToInt32(hex[4..6], 16)
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try { await module.InvokeVoidAsync("dispose", trackRef); } catch { }
            await module.DisposeAsync();
        }
        selfRef?.Dispose();
    }
}
