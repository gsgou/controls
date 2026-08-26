namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// Draws an <see cref="SvgDocument"/> into a <c>GraphicsView</c>.
/// </summary>
/// <remarks>
/// The drawable holds no copy of the artwork - only a reference to the shared, immutable document -
/// so pointing a hundred cells at the same drawing costs a hundred references and one parse.
/// </remarks>
/// <example>
/// <code>
/// var view = new GraphicsView { Drawable = new SvgDrawable { Document = doc, TintColor = Colors.Teal } };
/// </code>
/// </example>
public sealed class SvgDrawable : IDrawable
{
    /// <summary>The artwork. Null draws nothing.</summary>
    public SvgDocument? Document { get; set; }

    /// <summary>How the artwork is scaled into the view.</summary>
    public Aspect Aspect { get; set; } = Aspect.AspectFit;

    /// <summary>
    /// What <c>currentColor</c> resolves to - the tint an icon drawn with <c>fill="currentColor"</c>
    /// takes on. Artwork with its own explicit colours ignores it.
    /// </summary>
    public Color TintColor { get; set; } = Colors.Black;


    /// <inheritdoc />
    public void Draw(ICanvas canvas, RectF dirtyRect)
        => this.Document?.Draw(canvas, dirtyRect, this.Aspect, this.TintColor ?? Colors.Black);
}
