using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Shiny.Controls.Office.Icons;

namespace Shiny.Blazor.Controls.Office;

/// <summary>
/// The Word and PowerPoint toolbar icons, as inline SVG.
/// </summary>
/// <remarks>
/// <para>
/// Rendered from the same <see cref="OfficeIcons"/> geometry the MAUI toolbar paints on a canvas, so
/// there is one definition of what each button looks like rather than two that drift. Nothing here
/// parses a path string: the shapes arrive as commands and are written straight out as SVG elements.
/// </para>
/// <para>
/// <c>stroke="currentColor"</c> is the whole point of the set being monochrome — the icons inherit
/// the toolbar's text colour, follow the theme and dim with a disabled button, which is exactly what
/// the emoji they replaced could not do.
/// </para>
/// </remarks>
internal static class OfficeToolbarIcons
{
    /// <summary>The rendered size in the toolbar, in CSS pixels.</summary>
    const int Size = 20;

    static readonly Dictionary<OfficeIcon, MarkupString> Cache = [];
    static readonly Lock Gate = new();


    /// <summary>The inline SVG for an icon, built once and reused.</summary>
    public static MarkupString Get(OfficeIcon icon)
    {
        lock (Gate)
        {
            if (!Cache.TryGetValue(icon, out var markup))
            {
                markup = new MarkupString(Build(icon, Size));
                Cache[icon] = markup;
            }

            return markup;
        }
    }


    /// <summary>An icon at a size other than the toolbar's — the split-button chevron, for one.</summary>
    public static MarkupString Get(OfficeIcon icon, int size)
        => size == Size ? Get(icon) : new MarkupString(Build(icon, size));


    static string Build(OfficeIcon icon, int size)
    {
        var svg = new StringBuilder(256);

        // The dimensions are attributes rather than CSS: this markup is injected as a MarkupString, so
        // it carries no CSS-isolation scope attribute and a scoped `... svg` rule would not match it.
        svg.Append(CultureInfo.InvariantCulture, $"""<svg width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{N(OfficeIcons.StrokeWidth)}" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">""");

        foreach (var shape in OfficeIcons.Shapes(icon))
            Append(svg, shape);

        svg.Append("</svg>");

        return svg.ToString();
    }


    static void Append(StringBuilder svg, OfficeIconShape shape)
    {
        switch (shape.Primitive)
        {
            case OfficeIconPrimitive.Rectangle:
                svg.Append($"""<rect x="{N(shape.X)}" y="{N(shape.Y)}" width="{N(shape.Width)}" height="{N(shape.Height)}" rx="{N(shape.CornerRadius)}"{Paint(shape)}/>""");
                break;

            case OfficeIconPrimitive.Ellipse:
                svg.Append($"""<ellipse cx="{N(shape.X + shape.Width / 2)}" cy="{N(shape.Y + shape.Height / 2)}" rx="{N(shape.Width / 2)}" ry="{N(shape.Height / 2)}"{Paint(shape)}/>""");
                break;

            default:
                svg.Append($"""<path d="{Data(shape)}"{Paint(shape)}/>""");
                break;
        }
    }


    static string Data(OfficeIconShape shape)
    {
        var data = new StringBuilder(64);

        foreach (var vertex in shape.Vertices)
        {
            if (data.Length > 0)
                data.Append(' ');

            switch (vertex.Verb)
            {
                case OfficeIconVerb.Move:
                    data.Append($"M {N(vertex.X)} {N(vertex.Y)}");
                    break;

                case OfficeIconVerb.Line:
                    data.Append($"L {N(vertex.X)} {N(vertex.Y)}");
                    break;

                case OfficeIconVerb.Cubic:
                    data.Append($"C {N(vertex.C1X)} {N(vertex.C1Y)} {N(vertex.C2X)} {N(vertex.C2Y)} {N(vertex.X)} {N(vertex.Y)}");
                    break;

                case OfficeIconVerb.Close:
                    data.Append('Z');
                    break;
            }
        }

        return data.ToString();
    }


    static string Paint(OfficeIconShape shape) => shape.IsFilled ? """ fill="currentColor" stroke="none" """.TrimEnd() : string.Empty;

    static string N(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
