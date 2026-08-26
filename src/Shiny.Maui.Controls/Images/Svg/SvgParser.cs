using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// Turns SVG markup into the frozen node tree <see cref="SvgDocument"/> draws.
/// </summary>
/// <remarks>
/// <para>One instance per document. The element index, the stylesheet and the resolved paint servers
/// all live for the length of a parse and are then discarded - what survives is the node tree, which
/// holds no XML at all.</para>
///
/// <para>The parser does not throw on bad content. Artwork arrives from designers and CDNs, and one
/// malformed attribute should cost that attribute rather than the page the drawing sits on; only
/// markup that is not XML at all, or whose root is not an <c>svg</c> element, is refused.</para>
/// </remarks>
sealed class SvgParser
{
    // A `use` pointing at its own ancestor is a cycle. Real artwork nests two or three deep, so a
    // dozen is generous and still terminates.
    const int MaxDepth = 12;

    static readonly XName XLinkHref = XNamespace.Get("http://www.w3.org/1999/xlink") + "href";

    readonly Dictionary<string, XElement> byId = new(StringComparer.Ordinal);
    readonly SvgStylesheet stylesheet = new();
    readonly Dictionary<string, SvgPaintServer?> paintServers = new(StringComparer.Ordinal);
    readonly Dictionary<string, PathF?> clipPaths = new(StringComparer.Ordinal);
    readonly PathBuilder pathBuilder = new();

    SizeF viewport;


    /// <summary>
    /// Parses markup into a document.
    /// </summary>
    /// <exception cref="FormatException">The content is not XML, or its root is not an <c>svg</c> element.</exception>
    public static SvgDocument Parse(string markup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markup);

        XDocument xml;
        try
        {
            // DTD processing off and no resolver: artwork can come off the network, and an external
            // entity or a nested-entity bomb inside an image file must not become a fetch or an OOM.
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };

            using var reader = XmlReader.Create(new StringReader(markup), settings);
            xml = XDocument.Load(reader);
        }
        catch (Exception ex)
        {
            throw new FormatException("The content is not well-formed SVG.", ex);
        }

        var root = xml.Root;
        if (root is null || !root.Name.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("The content has no <svg> root element.");

        return new SvgParser().Build(root);
    }


    SvgDocument Build(XElement root)
    {
        foreach (var descendant in root.DescendantsAndSelf())
        {
            // First definition wins, which is what a browser does with duplicate ids too.
            if ((string?)descendant.Attribute("id") is { Length: > 0 } id)
                this.byId.TryAdd(id, descendant);

            if (descendant.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
                this.stylesheet.Add(descendant.Value);
        }

        var viewBox = SvgValues.ParseViewBox((string?)root.Attribute("viewBox"));

        // Width and height may be percentages of a viewport this drawing does not have one of. When
        // that happens the viewBox is the only real measurement in the file, so it becomes the size.
        var width = ResolveExtent((string?)root.Attribute("width"), viewBox?.Width);
        var height = ResolveExtent((string?)root.Attribute("height"), viewBox?.Height);

        var box = viewBox ?? new RectF(0f, 0f, width, height);
        this.viewport = box.Size;

        // The root element carries presentation attributes like any other, and icon sets lean on it
        // hard - a Lucide or Feather glyph puts stroke="currentColor" and stroke-width on the <svg>
        // and nothing at all on the shapes. Starting the walk from SvgStyle.Root rather than from
        // the root element's own style would render every one of them as a black silhouette.
        var properties = new SvgProperties(root, this.stylesheet);
        var style = this.Inherit(properties, SvgStyle.Root);

        var content = this.Attach(new SvgGroup { Children = this.VisitChildren(root, style, 0) }, properties);

        return new SvgDocument(
            new SizeF(width, height),
            box,
            ParseAlignment((string?)root.Attribute("preserveAspectRatio")),
            content as SvgGroup ?? new SvgGroup { Children = [content] }
        );
    }


    static float ResolveExtent(string? text, float? fromViewBox)
    {
        var value = String.IsNullOrWhiteSpace(text) || SvgValues.IsPercentage(text)
            ? fromViewBox ?? 100f
            : SvgValues.ParseLength(text, 0f, fromViewBox ?? 100f);

        return value > 0f ? value : 100f;
    }


    SvgNode[] VisitChildren(XElement parent, SvgStyle inherited, int depth)
    {
        var nodes = new List<SvgNode>();

        foreach (var child in parent.Elements())
        {
            if (this.Visit(child, inherited, depth) is { } node)
                nodes.Add(node);
        }

        return [.. nodes];
    }


    SvgNode? Visit(XElement element, SvgStyle inherited, int depth)
    {
        if (depth > MaxDepth)
            return null;

        var properties = new SvgProperties(element, this.stylesheet);
        var style = this.Inherit(properties, inherited);

        if (!style.Visible)
            return null;

        return element.Name.LocalName switch
        {
            // A nested `svg` gets its own viewport in the spec. Treating it as a group is the one
            // approximation here: content that relied on being re-scaled by it draws at the outer
            // scale instead, which is right far more often than dropping the subtree would be.
            "g" or "a" or "svg" => this.Attach(new SvgGroup { Children = this.VisitChildren(element, style, depth + 1) }, properties),
            "switch" => this.BuildSwitch(element, properties, style, depth),
            "use" => this.BuildUse(element, properties, style, depth),
            "path" => this.BuildShape(properties, style, this.ParsePathData(properties.Get("d"))),
            "rect" => this.BuildShape(properties, style, this.BuildRect(properties)),
            "circle" => this.BuildShape(properties, style, this.BuildCircle(properties)),
            "ellipse" => this.BuildShape(properties, style, this.BuildEllipse(properties)),
            "line" => this.BuildShape(properties, style, BuildLine(properties)),
            "polyline" => this.BuildShape(properties, style, BuildPolygon(properties, false)),
            "polygon" => this.BuildShape(properties, style, BuildPolygon(properties, true)),
            "text" => this.BuildText(element, properties, style),

            // Definitions, metadata, and the features this renderer does not draw - masks, filters,
            // patterns, markers, raster <image>. Skipping them silently is right: they are either
            // referenced from elsewhere or purely descriptive.
            _ => null
        };
    }


    // `switch` renders the first child whose system-language and required-features conditions pass.
    // Nothing here evaluates conditions, so the first drawable child wins - which is the one
    // exporters put first anyway.
    SvgNode? BuildSwitch(XElement element, SvgProperties properties, SvgStyle style, int depth)
    {
        foreach (var child in element.Elements())
        {
            if (this.Visit(child, style, depth + 1) is { } node)
                return this.Attach(new SvgGroup { Children = [node] }, properties);
        }

        return null;
    }


    SvgNode? BuildUse(XElement element, SvgProperties properties, SvgStyle style, int depth)
    {
        var href = properties.Get("href") ?? (string?)element.Attribute(XLinkHref);
        if (ResolveReference(href) is not { } reference || !this.byId.TryGetValue(reference, out var target))
            return null;

        // A `use` inside the thing it points at would recurse forever; the depth cap catches the
        // indirect cycles, this catches the obvious one before it costs twelve levels of work.
        if (target.DescendantsAndSelf().Contains(element))
            return null;

        // `symbol` is never drawn where it is defined, only where it is used - so its children are
        // visited directly rather than going through Visit, which would skip it.
        var node = target.Name.LocalName.Equals("symbol", StringComparison.OrdinalIgnoreCase)
            ? new SvgGroup { Children = this.VisitChildren(target, style, depth + 1) }
            : this.Visit(target, style, depth + 1);

        if (node is null)
            return null;

        // `use` offsets its target, and that offset composes inside the element's own transform.
        var offset = Matrix3x2.CreateTranslation(
            SvgValues.ParseLength(properties.Get("x"), this.viewport.Width, 0f),
            SvgValues.ParseLength(properties.Get("y"), this.viewport.Height, 0f)
        );

        return this.Attach(new SvgGroup { Children = [node] }, properties, offset);
    }


    SvgNode? BuildShape(SvgProperties properties, SvgStyle style, PathF? path)
    {
        if (path is null || path.OperationCount == 0)
            return null;

        var strokeWidth = style.StrokeWidth;

        var shape = new SvgShape
        {
            Path = path,
            Bounds = path.GetBoundsByFlattening(),
            Winding = style.FillRule,
            Fill = style.Fill,
            FillOpacity = style.FillOpacity,
            Stroke = style.Stroke,
            StrokeOpacity = style.StrokeOpacity,
            StrokeWidth = strokeWidth,
            DashPattern = SvgValues.ParseDashArray(style.DashArray, strokeWidth),
            DashOffset = strokeWidth > 0f ? style.DashOffset / strokeWidth : 0f,
            LineCap = style.LineCap,
            LineJoin = style.LineJoin,
            MiterLimit = style.MiterLimit
        };

        // A shape with neither fill nor stroke draws nothing; dropping it here keeps it out of every
        // future frame rather than re-deciding once per draw.
        return shape.Fill is null && shape.Stroke is null ? null : this.Attach(shape, properties);
    }


    SvgNode? BuildText(XElement element, SvgProperties properties, SvgStyle style)
    {
        // Every text run the element owns, joined. A tspan carrying its own x/y is laid out as if it
        // continued the line - close enough for a label, wrong for real typography.
        var text = String.Concat(element.DescendantNodes().OfType<XText>().Select(t => t.Value)).Trim();

        if (text.Length == 0 || style.Fill is null)
            return null;

        var node = new SvgText
        {
            Text = text,
            Origin = new PointF(
                SvgValues.ParseLength(properties.Get("x"), this.viewport.Width, 0f),
                SvgValues.ParseLength(properties.Get("y"), this.viewport.Height, 0f)
            ),
            FontSize = style.FontSize,
            Font = style.FontFamily is null
                ? null
                : new Microsoft.Maui.Graphics.Font(style.FontFamily, style.FontWeight, style.Italic ? FontStyleType.Italic : FontStyleType.Normal),
            Alignment = style.TextAnchor,
            Fill = style.Fill,
            FillOpacity = style.FillOpacity
        };

        return this.Attach(node, properties);
    }


    PathF? ParsePathData(string? data)
    {
        if (String.IsNullOrWhiteSpace(data))
            return null;

        try
        {
            return this.pathBuilder.BuildPath(data);
        }
        catch (Exception)
        {
            // A malformed `d` leaves a gap in the drawing rather than taking down the page it is on.
            return null;
        }
    }


    PathF BuildRect(SvgProperties properties)
    {
        var x = SvgValues.ParseLength(properties.Get("x"), this.viewport.Width, 0f);
        var y = SvgValues.ParseLength(properties.Get("y"), this.viewport.Height, 0f);
        var width = SvgValues.ParseLength(properties.Get("width"), this.viewport.Width, 0f);
        var height = SvgValues.ParseLength(properties.Get("height"), this.viewport.Height, 0f);

        // Either radius alone implies the other, which is how a browser reads a rect with only rx.
        var rx = SvgValues.ParseLength(properties.Get("rx"), width, Single.NaN);
        var ry = SvgValues.ParseLength(properties.Get("ry"), height, Single.NaN);

        if (Single.IsNaN(rx))
            rx = Single.IsNaN(ry) ? 0f : ry;

        if (Single.IsNaN(ry))
            ry = rx;

        var path = new PathF();
        SvgGeometry.AppendRectangle(path, new RectF(x, y, width, height), rx, ry);
        return path;
    }


    PathF BuildCircle(SvgProperties properties)
    {
        var radius = SvgValues.ParseLength(properties.Get("r"), this.Diagonal, 0f);
        var path = new PathF();

        SvgGeometry.AppendEllipse(
            path,
            SvgValues.ParseLength(properties.Get("cx"), this.viewport.Width, 0f),
            SvgValues.ParseLength(properties.Get("cy"), this.viewport.Height, 0f),
            radius,
            radius
        );

        return path;
    }


    PathF BuildEllipse(SvgProperties properties)
    {
        var path = new PathF();

        SvgGeometry.AppendEllipse(
            path,
            SvgValues.ParseLength(properties.Get("cx"), this.viewport.Width, 0f),
            SvgValues.ParseLength(properties.Get("cy"), this.viewport.Height, 0f),
            SvgValues.ParseLength(properties.Get("rx"), this.viewport.Width, 0f),
            SvgValues.ParseLength(properties.Get("ry"), this.viewport.Height, 0f)
        );

        return path;
    }


    static PathF BuildLine(SvgProperties properties)
    {
        var path = new PathF();

        path.MoveTo(SvgValues.ParseNumber(properties.Get("x1")) ?? 0f, SvgValues.ParseNumber(properties.Get("y1")) ?? 0f);
        path.LineTo(SvgValues.ParseNumber(properties.Get("x2")) ?? 0f, SvgValues.ParseNumber(properties.Get("y2")) ?? 0f);

        return path;
    }


    static PathF BuildPolygon(SvgProperties properties, bool close)
    {
        var path = new PathF();
        SvgGeometry.AppendPolygon(path, SvgValues.ParsePoints(properties.Get("points")), close);
        return path;
    }


    // The reference length for a radius, which SVG measures against the viewport's diagonal so that
    // a percentage radius stays circular in a viewport that is not square.
    float Diagonal
        => MathF.Sqrt(((this.viewport.Width * this.viewport.Width) + (this.viewport.Height * this.viewport.Height)) / 2f);


    SvgStyle Inherit(SvgProperties properties, SvgStyle style)
    {
        var currentColor = SvgValues.ParseColor(properties.Get("color"), style.CurrentColor) ?? style.CurrentColor;

        return style with
        {
            CurrentColor = currentColor,
            Visible = style.Visible && !IsNone(properties.Get("display")) && !IsHidden(properties.Get("visibility")),
            Fill = this.ResolvePaint(properties.Get("fill"), currentColor, style.Fill),
            FillOpacity = SvgValues.ParseOpacity(properties.Get("fill-opacity"), style.FillOpacity),
            FillRule = ParseWinding(properties.Get("fill-rule"), style.FillRule),
            Stroke = this.ResolvePaint(properties.Get("stroke"), currentColor, style.Stroke),
            StrokeOpacity = SvgValues.ParseOpacity(properties.Get("stroke-opacity"), style.StrokeOpacity),
            StrokeWidth = SvgValues.ParseLength(properties.Get("stroke-width"), this.Diagonal, style.StrokeWidth),
            DashArray = properties.Get("stroke-dasharray") ?? style.DashArray,
            DashOffset = SvgValues.ParseLength(properties.Get("stroke-dashoffset"), this.Diagonal, style.DashOffset),
            LineCap = ParseLineCap(properties.Get("stroke-linecap"), style.LineCap),
            LineJoin = ParseLineJoin(properties.Get("stroke-linejoin"), style.LineJoin),
            MiterLimit = SvgValues.ParseNumber(properties.Get("stroke-miterlimit")) ?? style.MiterLimit,
            FontSize = SvgValues.ParseLength(properties.Get("font-size"), style.FontSize, style.FontSize),
            FontFamily = ParseFontFamily(properties.Get("font-family")) ?? style.FontFamily,
            FontWeight = ParseFontWeight(properties.Get("font-weight"), style.FontWeight),
            Italic = ParseItalic(properties.Get("font-style"), style.Italic),
            TextAnchor = ParseTextAnchor(properties.Get("text-anchor"), style.TextAnchor)
        };
    }


    /// <summary>Applies the transform, clip and opacity any drawable element may carry.</summary>
    SvgNode Attach(SvgNode node, SvgProperties properties, Matrix3x2? extra = null)
    {
        var transform = SvgValues.ParseTransform(properties.Get("transform"));

        if (extra is { } inner)
            transform = inner * transform;

        return node with
        {
            Transform = transform,
            Clip = this.ResolveClipPath(properties.Get("clip-path")),

            // `opacity` is the one presentation property that does not inherit - it composites the
            // element as a unit, so it belongs to this node rather than to its children's style.
            Opacity = SvgValues.ParseOpacity(properties.Get("opacity"), 1f)
        };
    }


    /// <summary>Resolves a <c>fill</c> or <c>stroke</c> value, falling back to what was inherited.</summary>
    SvgPaintServer? ResolvePaint(string? value, Color currentColor, SvgPaintServer? inherited)
    {
        if (String.IsNullOrWhiteSpace(value))
            return inherited;

        var text = value.Trim();

        if (text.Equals("inherit", StringComparison.OrdinalIgnoreCase))
            return inherited;

        if (text.Equals("none", StringComparison.OrdinalIgnoreCase) || text.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            return null;

        if (text.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            return SvgCurrentColorPaint.Instance;

        if (text.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
        {
            var close = text.IndexOf(')');
            if (close < 0)
                return inherited;

            if (this.ResolvePaintServer(ResolveReference(text[4..close].Trim('"', '\'', ' '))) is { } server)
                return server;

            // `fill="url(#missing) red"` names its own fallback, which is exactly what to use when
            // the reference does not resolve - including when it names a paint kind not built here.
            var fallback = text[(close + 1)..].Trim();
            return fallback.Length == 0 ? null : this.ResolvePaint(fallback, currentColor, inherited);
        }

        return SvgValues.ParseColor(text, currentColor) is { } color ? new SvgSolidPaint(color) : inherited;
    }


    SvgPaintServer? ResolvePaintServer(string? id)
    {
        if (String.IsNullOrEmpty(id))
            return null;

        if (this.paintServers.TryGetValue(id, out var cached))
            return cached;

        // Written before the build so a gradient whose href chain loops back resolves to null once
        // rather than recursing.
        this.paintServers[id] = null;

        if (!this.byId.TryGetValue(id, out var element))
            return null;

        var built = this.BuildGradient(element);
        this.paintServers[id] = built;

        return built;
    }


    SvgPaintServer? BuildGradient(XElement element)
    {
        var isRadial = element.Name.LocalName.Equals("radialGradient", StringComparison.OrdinalIgnoreCase);
        if (!isRadial && !element.Name.LocalName.Equals("linearGradient", StringComparison.OrdinalIgnoreCase))
            return null;

        // A gradient may carry only stops, or only geometry, and inherit the rest from another one.
        var chain = new List<XElement>();
        var current = element;

        for (var i = 0; i < MaxDepth && current is not null; i++)
        {
            chain.Add(current);

            var href = ResolveReference((string?)current.Attribute("href") ?? (string?)current.Attribute(XLinkHref));
            if (href is null || !this.byId.TryGetValue(href, out var next) || chain.Contains(next))
                break;

            current = next;
        }

        var stops = this.BuildStops(chain);
        if (stops.Length == 0)
            return null;

        // MAUI's gradient paints want a real ramp; a one-stop gradient is a flat colour by any
        // other name, and saying so keeps the backends off a degenerate case.
        if (stops.Length == 1)
            return new SvgSolidPaint(stops[0].Color);

        var units = Attribute(chain, "gradientUnits")?.Trim().Equals("userSpaceOnUse", StringComparison.OrdinalIgnoreCase) == true
            ? SvgGradientUnits.UserSpaceOnUse
            : SvgGradientUnits.ObjectBoundingBox;

        // In bounding-box units the coordinates are already fractions, so "50%" and "0.5" have to
        // land on the same number - which is exactly what a percent basis of one gives.
        var basisX = units == SvgGradientUnits.UserSpaceOnUse ? this.viewport.Width : 1f;
        var basisY = units == SvgGradientUnits.UserSpaceOnUse ? this.viewport.Height : 1f;
        var basisR = units == SvgGradientUnits.UserSpaceOnUse ? this.Diagonal : 1f;

        var transform = SvgValues.ParseTransform(Attribute(chain, "gradientTransform"));

        if (isRadial)
        {
            return new SvgGradientPaint
            {
                IsRadial = true,
                Units = units,
                Stops = stops,
                Transform = transform,
                Center = new PointF(
                    SvgValues.ParseLength(Attribute(chain, "cx"), basisX, 0.5f * basisX),
                    SvgValues.ParseLength(Attribute(chain, "cy"), basisY, 0.5f * basisY)
                ),
                Radius = SvgValues.ParseLength(Attribute(chain, "r"), basisR, 0.5f * basisR)
            };
        }

        return new SvgGradientPaint
        {
            IsRadial = false,
            Units = units,
            Stops = stops,
            Transform = transform,
            Start = new PointF(
                SvgValues.ParseLength(Attribute(chain, "x1"), basisX, 0f),
                SvgValues.ParseLength(Attribute(chain, "y1"), basisY, 0f)
            ),
            End = new PointF(
                SvgValues.ParseLength(Attribute(chain, "x2"), basisX, basisX),
                SvgValues.ParseLength(Attribute(chain, "y2"), basisY, 0f)
            )
        };
    }


    SvgGradientStop[] BuildStops(List<XElement> chain)
    {
        foreach (var element in chain)
        {
            var stops = new List<SvgGradientStop>();
            var previousOffset = 0f;

            foreach (var stop in element.Elements())
            {
                if (!stop.Name.LocalName.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    continue;

                var properties = new SvgProperties(stop, this.stylesheet);
                var offsetText = properties.Get("offset");
                var offset = SvgValues.ParseNumber(offsetText) ?? 0f;

                if (SvgValues.IsPercentage(offsetText))
                    offset /= 100f;

                // Offsets must not go backwards; a stop that does is clamped to its predecessor,
                // which is what the spec asks for and what keeps MAUI's paints well-formed.
                offset = Math.Max(Math.Clamp(offset, 0f, 1f), previousOffset);
                previousOffset = offset;

                var color = SvgValues.ParseColor(properties.Get("stop-color"), Colors.Black) ?? Colors.Black;
                var opacity = SvgValues.ParseOpacity(properties.Get("stop-opacity"), 1f);

                stops.Add(new SvgGradientStop(offset, color.WithAlpha(color.Alpha * opacity)));
            }

            if (stops.Count > 0)
                return [.. stops];
        }

        return [];
    }


    PathF? ResolveClipPath(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        if (!text.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
            return null;

        var close = text.IndexOf(')');
        if (close < 0)
            return null;

        var id = ResolveReference(text[4..close].Trim('"', '\'', ' '));
        if (String.IsNullOrEmpty(id))
            return null;

        if (this.clipPaths.TryGetValue(id, out var cached))
            return cached;

        this.clipPaths[id] = null;

        if (!this.byId.TryGetValue(id, out var element) ||
            !element.Name.LocalName.Equals("clipPath", StringComparison.OrdinalIgnoreCase))
            return null;

        var built = this.BuildClipGeometry(element);
        this.clipPaths[id] = built;

        return built;
    }


    // Every shape in the clipPath, unioned into one path. `clipPathUnits="objectBoundingBox"` is not
    // honoured - a bounding-box clip needs the clipped shape's own extent, which is not known here -
    // so such a clip is applied in user space rather than dropped.
    PathF? BuildClipGeometry(XElement element)
    {
        var combined = new PathF();

        foreach (var child in element.Elements())
        {
            var properties = new SvgProperties(child, this.stylesheet);

            var geometry = child.Name.LocalName switch
            {
                "path" => this.ParsePathData(properties.Get("d")),
                "rect" => this.BuildRect(properties),
                "circle" => this.BuildCircle(properties),
                "ellipse" => this.BuildEllipse(properties),
                "polygon" => BuildPolygon(properties, true),
                "polyline" => BuildPolygon(properties, false),
                _ => null
            };

            if (geometry is null || geometry.OperationCount == 0)
                continue;

            var transform = SvgValues.ParseTransform(properties.Get("transform"));
            if (!transform.IsIdentity)
            {
                // Transform mutates in place, and this path was built here and goes nowhere else,
                // so there is nothing to copy first.
                geometry.Transform(transform);
            }

            SvgGeometry.Append(combined, geometry);
        }

        return combined.OperationCount == 0 ? null : combined;
    }


    static string? ResolveReference(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();

        // Same-document references only. An href to another file would be a fetch, which an image
        // renderer has no business starting on its own.
        return text.StartsWith('#') && text.Length > 1 ? text[1..] : null;
    }


    static string? Attribute(List<XElement> chain, string name)
    {
        foreach (var element in chain)
        {
            if ((string?)element.Attribute(name) is { } value)
                return value;
        }

        return null;
    }


    static PointF ParseAlignment(string? value)
    {
        var centered = new PointF(0.5f, 0.5f);

        if (String.IsNullOrWhiteSpace(value))
            return centered;

        var align = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (align is null || align.Equals("none", StringComparison.OrdinalIgnoreCase))
            return centered;

        static float Fraction(string text, char axis)
        {
            var index = text.IndexOf(axis);
            if (index < 0 || index + 4 > text.Length)
                return 0.5f;

            var rest = text.AsSpan(index + 1);

            return rest.StartsWith("Min", StringComparison.Ordinal) ? 0f
                : rest.StartsWith("Max", StringComparison.Ordinal) ? 1f
                : 0.5f;
        }

        return new PointF(Fraction(align, 'x'), Fraction(align, 'Y'));
    }


    static bool IsNone(string? value)
        => value?.Trim().Equals("none", StringComparison.OrdinalIgnoreCase) == true;


    static bool IsHidden(string? value)
        => value?.Trim() is { } text &&
           (text.Equals("hidden", StringComparison.OrdinalIgnoreCase) || text.Equals("collapse", StringComparison.OrdinalIgnoreCase));


    static WindingMode ParseWinding(string? value, WindingMode fallback) => value?.Trim().ToLowerInvariant() switch
    {
        "evenodd" => WindingMode.EvenOdd,
        "nonzero" => WindingMode.NonZero,
        _ => fallback
    };


    static LineCap ParseLineCap(string? value, LineCap fallback) => value?.Trim().ToLowerInvariant() switch
    {
        "butt" => LineCap.Butt,
        "round" => LineCap.Round,
        "square" => LineCap.Square,
        _ => fallback
    };


    static LineJoin ParseLineJoin(string? value, LineJoin fallback) => value?.Trim().ToLowerInvariant() switch
    {
        "miter" or "miter-clip" => LineJoin.Miter,
        "round" => LineJoin.Round,
        "bevel" => LineJoin.Bevel,
        _ => fallback
    };


    static HorizontalAlignment ParseTextAnchor(string? value, HorizontalAlignment fallback) => value?.Trim().ToLowerInvariant() switch
    {
        "start" => HorizontalAlignment.Left,
        "middle" => HorizontalAlignment.Center,
        "end" => HorizontalAlignment.Right,
        _ => fallback
    };


    static string? ParseFontFamily(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        // A family list is a fallback chain the platform resolves itself; MAUI takes one name, so
        // the first is the one that gets a say.
        var first = value.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim().Trim('"', '\'');
        return String.IsNullOrWhiteSpace(first) ? null : first;
    }


    static int ParseFontWeight(string? value, int fallback) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => fallback,
        "normal" => 400,
        "bold" => 700,
        "lighter" => Math.Max(100, fallback - 300),
        "bolder" => Math.Min(900, fallback + 300),
        var text => Int32.TryParse(text, out var weight) ? Math.Clamp(weight, 1, 1000) : fallback
    };


    static bool ParseItalic(string? value, bool fallback) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => fallback,
        "italic" or "oblique" => true,
        "normal" => false,
        _ => fallback
    };
}
