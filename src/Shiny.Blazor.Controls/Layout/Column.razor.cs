using System.Text;
using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// A responsive column inside a <see cref="Row"/>. Each breakpoint parameter is a span measured in
/// the parent <see cref="Grid"/>'s columns (12 by default) and cascades upwards: a column with only
/// <c>Md</c> set uses that span at md and wider, and is full width below it.
/// <para>
/// Breakpoints: <c>Xs</c> &lt; 576px, <c>Sm</c> ≥ 576px, <c>Md</c> ≥ 768px, <c>Lg</c> ≥ 992px,
/// <c>Xl</c> ≥ 1200px, <c>Xxl</c> ≥ 1400px.
/// </para>
/// </summary>
public partial class Column : ComponentBase
{
    /// <summary>Span below 576px.</summary>
    [Parameter] public int? Xs { get; set; }
    /// <summary>Span at 576px and wider.</summary>
    [Parameter] public int? Sm { get; set; }
    /// <summary>Span at 768px and wider.</summary>
    [Parameter] public int? Md { get; set; }
    /// <summary>Span at 992px and wider.</summary>
    [Parameter] public int? Lg { get; set; }
    /// <summary>Span at 1200px and wider.</summary>
    [Parameter] public int? Xl { get; set; }
    /// <summary>Span at 1400px and wider.</summary>
    [Parameter] public int? Xxl { get; set; }

    /// <summary>Empty columns to leave to the left, below 576px.</summary>
    [Parameter] public int? OffsetXs { get; set; }
    [Parameter] public int? OffsetSm { get; set; }
    [Parameter] public int? OffsetMd { get; set; }
    [Parameter] public int? OffsetLg { get; set; }
    [Parameter] public int? OffsetXl { get; set; }
    [Parameter] public int? OffsetXxl { get; set; }

    /// <summary>Visual order below 576px — lets a sidebar drop below the content on phones.</summary>
    [Parameter] public int? OrderXs { get; set; }
    [Parameter] public int? OrderSm { get; set; }
    [Parameter] public int? OrderMd { get; set; }
    [Parameter] public int? OrderLg { get; set; }
    [Parameter] public int? OrderXl { get; set; }
    [Parameter] public int? OrderXxl { get; set; }

    /// <summary>Shrink to the content's width instead of taking a share of the row.</summary>
    [Parameter] public bool Fit { get; set; }

    /// <summary>CSS padding shorthand applied inside the column's gutter.</summary>
    [Parameter] public string? Padding { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    IDictionary<string, object>? ExtraAttributes;
    string? UserClass;
    string? ModeClass;
    string ColumnStyle = string.Empty;

    protected override void OnParametersSet()
    {
        this.ExtraAttributes = LayoutAttributes.Split(this.AdditionalAttributes, out var userClass, out var userStyle);
        this.UserClass = userClass;

        var hasSpan = this.Xs is not null || this.Sm is not null || this.Md is not null ||
                      this.Lg is not null || this.Xl is not null || this.Xxl is not null;

        // No span at all means "share the row equally with my siblings"; Fit always wins.
        this.ModeClass = this.Fit
            ? "shiny-col--fit"
            : hasSpan ? null : "shiny-col--auto";

        var sb = new StringBuilder();
        Var(sb, "--sc-xs", this.Xs);
        Var(sb, "--sc-sm", this.Sm);
        Var(sb, "--sc-md", this.Md);
        Var(sb, "--sc-lg", this.Lg);
        Var(sb, "--sc-xl", this.Xl);
        Var(sb, "--sc-xxl", this.Xxl);

        Var(sb, "--so-xs", this.OffsetXs);
        Var(sb, "--so-sm", this.OffsetSm);
        Var(sb, "--so-md", this.OffsetMd);
        Var(sb, "--so-lg", this.OffsetLg);
        Var(sb, "--so-xl", this.OffsetXl);
        Var(sb, "--so-xxl", this.OffsetXxl);

        Var(sb, "--sr-xs", this.OrderXs);
        Var(sb, "--sr-sm", this.OrderSm);
        Var(sb, "--sr-md", this.OrderMd);
        Var(sb, "--sr-lg", this.OrderLg);
        Var(sb, "--sr-xl", this.OrderXl);
        Var(sb, "--sr-xxl", this.OrderXxl);

        if (!string.IsNullOrWhiteSpace(this.Padding))
            sb.Append("padding:").Append(LayoutAttributes.Spacing(this.Padding)).Append(';');

        this.ColumnStyle = LayoutAttributes.Append(sb.ToString(), userStyle);
    }

    static void Var(StringBuilder sb, string name, int? value)
    {
        if (value is not null)
            sb.Append(name).Append(':').Append(LayoutAttributes.Num(value.Value)).Append(';');
    }
}
