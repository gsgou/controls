using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

public partial class BadgeView
{
    /// <summary>The view the badge is overlaid on top of.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Badge text. When empty/null and <see cref="IsDot"/> is false, the badge is hidden.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Corner of the wrapped content where the badge is anchored.</summary>
    [Parameter] public BadgePosition Position { get; set; } = BadgePosition.TopRight;

    /// <summary>Badge fill color (any valid CSS color).</summary>
    [Parameter] public string BadgeColor { get; set; } = "#DC2626";

    /// <summary>Badge text color (any valid CSS color).</summary>
    [Parameter] public string BadgeTextColor { get; set; } = "#FFFFFF";

    /// <summary>Badge border color (any valid CSS color). Defaults to white for clean separation from the wrapped content.</summary>
    [Parameter] public string BadgeBorderColor { get; set; } = "#FFFFFF";

    /// <summary>Badge border thickness in px.</summary>
    [Parameter] public double BadgeBorderThickness { get; set; } = 1.5;

    /// <summary>Badge text font size in px.</summary>
    [Parameter] public double FontSize { get; set; } = 10;

    /// <summary>Badge text font weight (CSS value).</summary>
    [Parameter] public string FontWeight { get; set; } = "700";

    /// <summary>Badge corner radius in px. Default is a fully rounded pill.</summary>
    [Parameter] public double CornerRadius { get; set; } = 999;

    /// <summary>Inner padding of the badge as a CSS value (e.g. "2px 6px").</summary>
    [Parameter] public string BadgePadding { get; set; } = "2px 6px";

    /// <summary>Horizontal nudge in px from the corner. Positive pushes outward (away from content).</summary>
    [Parameter] public double OffsetX { get; set; } = 4;

    /// <summary>Vertical nudge in px from the corner. Positive pushes downward; negative pushes upward.</summary>
    [Parameter] public double OffsetY { get; set; } = -4;

    /// <summary>When true, the badge is rendered as a small dot (text is ignored).</summary>
    [Parameter] public bool IsDot { get; set; }

    /// <summary>Diameter (px) of the badge in dot mode.</summary>
    [Parameter] public double DotSize { get; set; } = 10;

    /// <summary>When greater than zero and <see cref="Text"/> parses as a number above this limit, the badge displays "{MaxCount}+".</summary>
    [Parameter] public int MaxCount { get; set; }

    /// <summary>When true, the badge scales in/out as it appears or disappears.</summary>
    [Parameter] public bool IsAnimated { get; set; } = true;

    /// <summary>When true, the badge continuously pulses to draw attention.</summary>
    [Parameter] public bool IsPulsing { get; set; }

    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    bool ShouldShow => IsDot || !string.IsNullOrEmpty(Text);

    string DisplayText
    {
        get
        {
            var text = Text ?? string.Empty;
            if (MaxCount > 0 && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > MaxCount)
                return $"{MaxCount}+";
            return text;
        }
    }

    string PositionClass => Position switch
    {
        BadgePosition.TopLeft => "tl",
        BadgePosition.TopRight => "tr",
        BadgePosition.BottomLeft => "bl",
        BadgePosition.BottomRight => "br",
        _ => "tr"
    };

    string HostStyle => "position:relative; display:inline-block;";

    string BadgeStyle
    {
        get
        {
            var ci = CultureInfo.InvariantCulture;
            var common =
                $"background:{BadgeColor};" +
                $"color:{BadgeTextColor};" +
                $"border:{BadgeBorderThickness.ToString(ci)}px solid {BadgeBorderColor};" +
                $"border-radius:{CornerRadius.ToString(ci)}px;" +
                $"font-weight:{FontWeight};";

            if (IsDot)
            {
                common +=
                    $"width:{DotSize.ToString(ci)}px;" +
                    $"height:{DotSize.ToString(ci)}px;" +
                    "padding:0;";
            }
            else
            {
                common +=
                    $"padding:{BadgePadding};" +
                    $"font-size:{FontSize.ToString(ci)}px;" +
                    "min-width:1em; line-height:1;";
            }

            // Set --offset variables consumed by the position-specific CSS classes.
            common +=
                $"--shiny-badge-offset-x:{OffsetX.ToString(ci)}px;" +
                $"--shiny-badge-offset-y:{OffsetY.ToString(ci)}px;";

            return common;
        }
    }
}
