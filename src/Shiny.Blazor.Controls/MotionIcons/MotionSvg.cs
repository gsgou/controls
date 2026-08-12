using System.Text;
using Shiny.Controls.MotionIcons;

namespace Shiny.Blazor.Controls.MotionIcons;

/// <summary>
/// Builds the SVG body an icon's stylesheet expects to animate.
/// </summary>
/// <remarks>
/// The markup depends only on the artwork and the compiled plan — colour, size and stroke width all
/// arrive as CSS custom properties — so it is generated once alongside the stylesheet and reused by
/// every instance of that icon on the page, whatever they each look like.
/// </remarks>
static class MotionSvg
{
    /// <summary>Builds the contents of the <c>&lt;svg&gt;</c> element.</summary>
    public static string Build(MotionIconDefinition icon, MotionPlan plan)
    {
        var svg = new StringBuilder();
        var depth = 0;

        depth += OpenGroup(svg, plan.RootOpacity, "ro");
        depth += OpenGroup(svg, plan.RootTranslate, "rt");
        depth += OpenGroup(svg, plan.RootRotate, "rr");
        depth += OpenGroup(svg, plan.RootScale, "rs");

        for (var i = 0; i < icon.Parts.Count; i++)
            AppendPart(svg, icon.Parts[i], plan.Parts[i], i);

        for (var i = 0; i < depth; i++)
            svg.Append("</g>");

        return svg.ToString();
    }

    static void AppendPart(StringBuilder svg, MotionIconPart part, MotionPartPlan plan, int index)
    {
        var suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var depth = 0;

        depth += OpenGroup(svg, plan.Translate, "t" + suffix);
        depth += OpenGroup(svg, plan.Rotate, "r" + suffix);
        depth += OpenGroup(svg, plan.Scale, "s" + suffix);

        svg.Append("<path class=\"");

        if (plan.Animated)
            svg.Append("mi-a ");

        svg.Append("mi-p").Append(suffix).Append('"');
        svg.Append(" d=\"").Append(Escape(part.Path)).Append('"');
        svg.Append(" fill=\"").Append(Paint(part.Fill)).Append('"');
        svg.Append(" stroke=\"").Append(Paint(part.Stroke)).Append('"');

        if (part.Stroke.IsPainted)
        {
            svg.Append(" stroke-width=\"calc(var(--shiny-mi-stroke) * ")
                .Append(MotionCss.Num(part.StrokeScale)).Append(")\"");

            svg.Append(" stroke-linecap=\"").Append(Cap(part.LineCap)).Append('"');
            svg.Append(" stroke-linejoin=\"").Append(Join(part.LineJoin)).Append('"');
        }

        // Normalising the path's length to 1 turns "how much of this stroke is drawn" into a plain
        // 0-to-1 dash offset, whatever the geometry actually measures — the browser's equivalent of
        // the MAUI renderer measuring and rebuilding the path.
        if (plan.Trim)
            svg.Append(" pathLength=\"1\" stroke-dasharray=\"1 1\"");

        svg.Append("/>");

        for (var i = 0; i < depth; i++)
            svg.Append("</g>");
    }

    static int OpenGroup(StringBuilder svg, bool required, string key)
    {
        if (!required)
            return 0;

        svg.Append("<g class=\"mi-g mi-a mi-").Append(key).Append("\">");
        return 1;
    }

    static string Paint(IconPaint paint) => paint.Kind switch
    {
        IconPaintKind.Current => "var(--shiny-mi-color)",
        IconPaintKind.Accent => "var(--shiny-mi-accent)",
        IconPaintKind.Fixed => Escape(paint.Value!),
        _ => "none"
    };

    static string Cap(MotionLineCap cap) => cap switch
    {
        MotionLineCap.Butt => "butt",
        MotionLineCap.Square => "square",
        _ => "round"
    };

    static string Join(MotionLineJoin join) => join switch
    {
        MotionLineJoin.Miter => "miter",
        MotionLineJoin.Bevel => "bevel",
        _ => "round"
    };

    // Artwork can be supplied at runtime, so it is escaped rather than trusted — a path string is
    // being written straight into markup.
    static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}
