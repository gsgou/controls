namespace Shiny.Blazor.Controls;

/// <summary>An element's rectangle in viewport coordinates.</summary>
/// <remarks>
/// A named class rather than an anonymous type or a tuple on purpose. Anonymous types come back from
/// published WebAssembly as <c>ConstructorContainsNullParameterNames</c> once the trimmer has been
/// through them, and the failure only shows up in a Release publish.
/// </remarks>
public sealed class TooltipRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public double Left => this.X;
    public double Top => this.Y;
    public double Right => this.X + this.Width;
    public double Bottom => this.Y + this.Height;
    public double CenterX => this.X + (this.Width / 2);
    public double CenterY => this.Y + (this.Height / 2);
}


/// <summary>What the JS placement engine settled on.</summary>
public sealed class TooltipPlacementResult
{
    /// <summary>One of <c>top</c>, <c>bottom</c>, <c>left</c>, <c>right</c>, <c>center</c>.</summary>
    public string? Placement { get; set; }

    /// <summary>Distance from the bubble's leading edge to the centre of the tail.</summary>
    public double TailOffset { get; set; }

    public double Left { get; set; }

    public double Top { get; set; }
}
