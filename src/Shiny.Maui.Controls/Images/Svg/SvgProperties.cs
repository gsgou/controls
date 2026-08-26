using System.Xml.Linq;

namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// One element's presentation properties, wherever they were written.
/// </summary>
/// <remarks>
/// SVG lets the same property arrive three ways - as an XML attribute, as a stylesheet rule, or in a
/// <c>style</c> attribute - and they do not all carry the same weight. Presentation attributes are
/// the weakest, a matched rule beats them, and the inline <c>style</c> beats everything. Resolving
/// that once per element here keeps the ordering out of every caller.
/// </remarks>
readonly struct SvgProperties
{
    readonly XElement element;
    readonly Dictionary<string, string>? rules;
    readonly Dictionary<string, string>? inline;

    /// <summary>Resolves an element against the document's stylesheet.</summary>
    public SvgProperties(XElement element, SvgStylesheet stylesheet)
    {
        this.element = element;
        this.rules = stylesheet.Lookup(element);

        var style = (string?)element.Attribute("style");
        this.inline = String.IsNullOrWhiteSpace(style) ? null : SvgStylesheet.ParseDeclarations(style);
    }


    /// <summary>Returns the winning value for a property, or null when it was never written.</summary>
    public string? Get(string name)
    {
        if (this.inline?.TryGetValue(name, out var declared) == true)
            return declared;

        if (this.rules?.TryGetValue(name, out var ruled) == true)
            return ruled;

        return (string?)this.element.Attribute(name);
    }
}
