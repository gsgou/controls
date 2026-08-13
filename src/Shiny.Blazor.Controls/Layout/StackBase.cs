using System.Text;
using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Shared flexbox plumbing for <see cref="VStack"/> and <see cref="HStack"/>. Everything is an
/// inline style — there is no scoped stylesheet to load and no class the consumer has to know
/// about beyond the <c>shiny-vstack</c> / <c>shiny-hstack</c> hooks.
/// </summary>
public abstract class StackBase : ComponentBase
{
    /// <summary>Gap between children, in pixels.</summary>
    [Parameter] public double Spacing { get; set; }

    /// <summary>Cross-axis alignment. Defaults to <see cref="StackAlign.Stretch"/>.</summary>
    [Parameter] public StackAlign Align { get; set; } = StackAlign.Stretch;

    /// <summary>Main-axis distribution. Defaults to <see cref="StackJustify.Start"/>.</summary>
    [Parameter] public StackJustify Justify { get; set; } = StackJustify.Start;

    /// <summary>Wrap children onto additional lines when they overflow.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Lay children out in reverse order.</summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>Fill the remaining space of a flex parent (and allow the stack to shrink below its content).</summary>
    [Parameter] public bool Grow { get; set; }

    /// <summary>CSS padding shorthand, e.g. <c>"16px"</c> or <c>"8px 16px"</c>.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>CSS background shorthand applied to the stack.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Scroll the stack's own overflow along its main axis.</summary>
    [Parameter] public bool Scrollable { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    protected abstract bool IsVertical { get; }

    protected IDictionary<string, object>? ExtraAttributes { get; private set; }
    protected string? UserClass { get; private set; }
    protected string LayoutStyle { get; private set; } = string.Empty;

    protected override void OnParametersSet()
    {
        this.ExtraAttributes = LayoutAttributes.Split(this.AdditionalAttributes, out var userClass, out var userStyle);
        this.UserClass = userClass;

        var direction = (this.IsVertical, this.Reverse) switch
        {
            (true, false) => "column",
            (true, true) => "column-reverse",
            (false, false) => "row",
            _ => "row-reverse"
        };

        var sb = new StringBuilder("display:flex;flex-direction:")
            .Append(direction)
            .Append(";align-items:")
            .Append(this.Align.ToCss())
            .Append(";justify-content:")
            .Append(this.Justify.ToCss())
            .Append(';');

        if (this.Spacing > 0)
            sb.Append("gap:").Append(LayoutAttributes.Px(this.Spacing)).Append(';');

        if (this.Wrap)
            sb.Append("flex-wrap:wrap;");

        if (this.Grow)
            sb.Append("flex:1 1 0;min-width:0;min-height:0;");

        if (this.Scrollable)
            sb.Append(this.IsVertical ? "overflow-y:auto;min-height:0;" : "overflow-x:auto;min-width:0;");

        if (!string.IsNullOrWhiteSpace(this.Padding))
            sb.Append("padding:").Append(LayoutAttributes.Spacing(this.Padding)).Append(';');

        if (!string.IsNullOrWhiteSpace(this.Background))
            sb.Append("background:").Append(this.Background).Append(';');

        this.LayoutStyle = LayoutAttributes.Append(sb.ToString(), userStyle);
    }
}
