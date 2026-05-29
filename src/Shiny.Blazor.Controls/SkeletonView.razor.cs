using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

public partial class SkeletonView
{
    /// <summary>When true, the content is hidden and animated shimmer placeholders are shown.</summary>
    [Parameter] public bool IsBusy { get; set; }

    /// <summary>The real content shown when <see cref="IsBusy"/> is false.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional custom placeholder markup. Apply the <c>shiny-skeleton__shape</c> class to shimmer your own shapes.</summary>
    [Parameter] public RenderFragment? SkeletonContent { get; set; }

    /// <summary>Number of placeholder lines in the built-in skeleton (ignored when SkeletonContent is set).</summary>
    [Parameter] public int ItemCount { get; set; } = 3;

    /// <summary>Height in px of each built-in placeholder line.</summary>
    [Parameter] public double ItemHeight { get; set; } = 16;

    /// <summary>Vertical spacing in px between built-in placeholder lines.</summary>
    [Parameter] public double ItemSpacing { get; set; } = 12;

    /// <summary>Corner radius in px of the built-in placeholder shapes.</summary>
    [Parameter] public double CornerRadius { get; set; } = 6;

    /// <summary>Base fill color of placeholder shapes.</summary>
    [Parameter] public string BaseColor { get; set; } = "#e1e1e6";

    /// <summary>Color of the sweeping shimmer highlight.</summary>
    [Parameter] public string HighlightColor { get; set; } = "rgba(255, 255, 255, 0.6)";

    /// <summary>Duration in seconds of a single shimmer sweep.</summary>
    [Parameter] public double AnimationDuration { get; set; } = 1.4;

    /// <summary>When false, placeholders are shown statically with no sweeping animation.</summary>
    [Parameter] public bool ShimmerEnabled { get; set; } = true;

    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    string RootStyle =>
        $"--shiny-skeleton-base: {BaseColor};" +
        $" --shiny-skeleton-highlight: {HighlightColor};" +
        $" --shiny-skeleton-duration: {AnimationDuration.ToString(System.Globalization.CultureInfo.InvariantCulture)}s;" +
        $" --shiny-skeleton-gap: {ItemSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;";
}
