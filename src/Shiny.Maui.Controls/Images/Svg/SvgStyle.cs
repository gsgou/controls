using System.Xml.Linq;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// The presentation properties an element inherits from its ancestors.
/// </summary>
/// <remarks>
/// A struct copied down the tree rather than a stack that is pushed and popped: inheritance in SVG
/// is a pure "parent's values with mine layered on top", and copying eight fields is cheaper than
/// the bookkeeping of unwinding them.
/// </remarks>
readonly record struct SvgStyle
{
    /// <summary>The state an SVG root starts in - notably, <c>fill</c> begins as black.</summary>
    public static SvgStyle Root { get; } = new()
    {
        Fill = SvgSolidPaint.Black,
        FillOpacity = 1f,
        FillRule = WindingMode.NonZero,
        Stroke = null,
        StrokeOpacity = 1f,
        StrokeWidth = 1f,
        DashOffset = 0f,
        LineCap = LineCap.Butt,
        LineJoin = LineJoin.Miter,
        MiterLimit = 4f,
        CurrentColor = Colors.Black,
        FontSize = 16f,
        FontWeight = 400,
        TextAnchor = HorizontalAlignment.Left,
        Visible = true
    };

    /// <summary>The fill paint, or null for <c>fill="none"</c>.</summary>
    public SvgPaintServer? Fill { get; init; }

    /// <summary><c>fill-opacity</c>.</summary>
    public float FillOpacity { get; init; }

    /// <summary><c>fill-rule</c>.</summary>
    public WindingMode FillRule { get; init; }

    /// <summary>The stroke paint, or null for <c>stroke="none"</c>.</summary>
    public SvgPaintServer? Stroke { get; init; }

    /// <summary><c>stroke-opacity</c>.</summary>
    public float StrokeOpacity { get; init; }

    /// <summary><c>stroke-width</c> in user units.</summary>
    public float StrokeWidth { get; init; }

    /// <summary>The raw <c>stroke-dasharray</c>, resolved against the stroke width per shape.</summary>
    public string? DashArray { get; init; }

    /// <summary><c>stroke-dashoffset</c> in user units.</summary>
    public float DashOffset { get; init; }

    /// <summary><c>stroke-linecap</c>.</summary>
    public LineCap LineCap { get; init; }

    /// <summary><c>stroke-linejoin</c>.</summary>
    public LineJoin LineJoin { get; init; }

    /// <summary><c>stroke-miterlimit</c>.</summary>
    public float MiterLimit { get; init; }

    /// <summary>The CSS <c>color</c> property, which is what <c>currentColor</c> refers to.</summary>
    public Color CurrentColor { get; init; }

    /// <summary><c>font-size</c> in user units.</summary>
    public float FontSize { get; init; }

    /// <summary><c>font-family</c>, first entry only.</summary>
    public string? FontFamily { get; init; }

    /// <summary><c>font-weight</c> as a numeric weight.</summary>
    public int FontWeight { get; init; }

    /// <summary>True for <c>font-style: italic</c> or <c>oblique</c>.</summary>
    public bool Italic { get; init; }

    /// <summary><c>text-anchor</c>, mapped onto the canvas alignment it corresponds to.</summary>
    public HorizontalAlignment TextAnchor { get; init; }

    /// <summary>False once <c>display:none</c> or <c>visibility:hidden</c> has been seen.</summary>
    public bool Visible { get; init; }
}


/// <summary>
/// The little of CSS that matters for artwork: the rules inside <c>&lt;style&gt;</c> elements.
/// </summary>
/// <remarks>
/// <para>Illustrator, Figma and Sketch all export shared appearance as classes rather than as
/// per-element attributes, so an SVG stack that ignores <c>&lt;style&gt;</c> renders a large share of
/// real-world files as flat black silhouettes. This handles what those exporters actually emit:
/// type, class and id selectors, comma-separated, with plain declarations.</para>
///
/// <para>Not handled, and deliberately: combinators, attribute and pseudo selectors, <c>@media</c>,
/// <c>@import</c> and specificity beyond the type &lt; class &lt; id ordering. A rule that uses them
/// is skipped rather than mis-applied.</para>
/// </remarks>
sealed class SvgStylesheet
{
    static readonly char[] DeclarationSeparators = [';'];

    readonly Dictionary<string, Dictionary<string, string>> byType = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Dictionary<string, string>> byClass = new(StringComparer.Ordinal);
    readonly Dictionary<string, Dictionary<string, string>> byId = new(StringComparer.Ordinal);

    /// <summary>True when nothing was parsed, so lookups can be skipped entirely.</summary>
    public bool IsEmpty => this.byType.Count == 0 && this.byClass.Count == 0 && this.byId.Count == 0;


    /// <summary>Adds every rule in one stylesheet's text.</summary>
    public void Add(string css)
    {
        var index = 0;

        while (index < css.Length)
        {
            var open = css.IndexOf('{', index);
            if (open < 0)
                break;

            var close = css.IndexOf('}', open + 1);
            if (close < 0)
                break;

            var selectors = css[index..open];
            var declarations = ParseDeclarations(css[(open + 1)..close]);
            index = close + 1;

            // An at-rule's body is itself a block of rules, and treating its prelude as a selector
            // would attach the whole thing to a bogus element name.
            if (declarations.Count == 0 || selectors.Contains('@', StringComparison.Ordinal))
                continue;

            foreach (var selector in selectors.Split(','))
                this.AddRule(selector.Trim(), declarations);
        }
    }


    /// <summary>
    /// Returns the declarations that apply to an element, or null when none do.
    /// </summary>
    public Dictionary<string, string>? Lookup(XElement element)
    {
        if (this.IsEmpty)
            return null;

        Dictionary<string, string>? merged = null;

        // Least specific first: each pass overwrites what the last one said.
        Merge(ref merged, this.byType, element.Name.LocalName);

        var classes = (string?)element.Attribute("class");
        if (!String.IsNullOrWhiteSpace(classes))
        {
            foreach (var name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                Merge(ref merged, this.byClass, name);
        }

        Merge(ref merged, this.byId, (string?)element.Attribute("id"));

        return merged;
    }


    void AddRule(string selector, Dictionary<string, string> declarations)
    {
        if (selector.Length == 0)
            return;

        // Anything with structure - descendants, attribute matches, pseudo-classes - is beyond what
        // this understands, and applying it anyway would paint elements the author never selected.
        if (selector.AsSpan().IndexOfAny(" >+~[:*") >= 0)
            return;

        var table = selector[0] switch
        {
            '.' => this.byClass,
            '#' => this.byId,
            _ => this.byType
        };

        var key = selector[0] is '.' or '#' ? selector[1..] : selector;
        if (key.Length == 0)
            return;

        if (!table.TryGetValue(key, out var existing))
        {
            table[key] = declarations;
            return;
        }

        // Later rules win, matching the cascade for equal specificity.
        foreach (var pair in declarations)
            existing[pair.Key] = pair.Value;
    }


    static void Merge(ref Dictionary<string, string>? target, Dictionary<string, Dictionary<string, string>> table, string? key)
    {
        if (String.IsNullOrEmpty(key) || !table.TryGetValue(key, out var source))
            return;

        target ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in source)
            target[pair.Key] = pair.Value;
    }


    /// <summary>Parses a <c>name: value; name: value</c> run, as found in a rule or a style attribute.</summary>
    public static Dictionary<string, string> ParseDeclarations(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var declaration in text.Split(DeclarationSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0)
                continue;

            var name = declaration[..colon].Trim();
            var value = declaration[(colon + 1)..].Trim();

            if (name.Length > 0 && value.Length > 0)
                result[name] = value;
        }

        return result;
    }
}
