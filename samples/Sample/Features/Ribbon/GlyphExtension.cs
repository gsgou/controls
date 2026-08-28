using System.Globalization;

namespace Sample.Features.Ribbon;

/// <summary>
/// <c>{local:Glyph ✂}</c> — a one-character icon, so the sample needs no image assets.
/// </summary>
/// <remarks>
/// Sample scaffolding, not something the control needs. <c>RibbonItem.Icon</c> takes any
/// <see cref="ImageSource"/>, so a real app usually hands it a PNG or SVG out of its resources, or an
/// icon-font <see cref="FontImageSource"/> of its own. What it does show is why <c>Icon</c> is an
/// <c>ImageSource</c> rather than a string: a glyph is one kind of image source among several, and the
/// ribbon does not have to know which.
/// </remarks>
[ContentProperty(nameof(Glyph))]
public class GlyphExtension : IMarkupExtension<ImageSource>
{
    public string Glyph { get; set; } = string.Empty;

    public double Size { get; set; } = 22;

    public ImageSource ProvideValue(IServiceProvider serviceProvider)
    {
        var source = new FontImageSource
        {
            Glyph = this.Glyph,
            Size = this.Size
        };

        // The icon has to survive a theme swap, and a literal colour would not.
        source.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#1B1B1F"), Colors.White);
        return source;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => this.ProvideValue(serviceProvider);
}
