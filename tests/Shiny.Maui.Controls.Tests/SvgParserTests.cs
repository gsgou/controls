using System.IO.Compression;
using System.Numerics;
using System.Text;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Images.Svg;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// What has to be true for real artwork to render: the coordinate system the file declares, the
/// inheritance rules its exporter relied on, and the references it makes to its own definitions.
/// </summary>
public class SvgParserTests
{
    static SvgDocument Parse(string body, string attributes = "viewBox=\"0 0 100 100\"")
        => SvgDocument.Parse($"<svg xmlns=\"http://www.w3.org/2000/svg\" {attributes}>{body}</svg>");

    static IEnumerable<SvgNode> Flatten(SvgNode node)
    {
        yield return node;

        if (node is not SvgGroup group)
            yield break;

        foreach (var child in group.Children)
        {
            foreach (var descendant in Flatten(child))
                yield return descendant;
        }
    }

    static SvgShape[] Shapes(SvgDocument document) => [.. Flatten(document.Root).OfType<SvgShape>()];

    static SvgShape Single(SvgDocument document)
    {
        var shapes = Shapes(document);
        shapes.Length.ShouldBe(1);
        return shapes[0];
    }


    [Fact]
    public void ViewBox_DefinesTheCoordinateSpace()
    {
        var document = Parse("<rect width='10' height='10' />", "width='48' height='24' viewBox='-5 -5 20 10'");

        document.ViewBox.ShouldBe(new RectF(-5f, -5f, 20f, 10f));
        document.Size.ShouldBe(new SizeF(48f, 24f));
    }


    [Fact]
    public void Size_FallsBackToViewBox_WhenExtentsArePercentages()
    {
        // An SVG sized 100%/100% has no intrinsic size of its own - a browser would take it from the
        // element it sits in. There is no such element here, so the viewBox is the only measurement.
        var document = Parse("<rect width='10' height='10' />", "width='100%' height='100%' viewBox='0 0 32 16'");

        document.Size.ShouldBe(new SizeF(32f, 16f));
    }


    [Fact]
    public void Size_FallsBackToASquare_WhenNothingIsDeclared()
    {
        var document = SvgDocument.Parse("<svg xmlns='http://www.w3.org/2000/svg'><rect width='4' height='4'/></svg>");

        document.Size.ShouldBe(new SizeF(100f, 100f));
        document.ViewBox.ShouldBe(new RectF(0f, 0f, 100f, 100f));
    }


    [Fact]
    public void Fill_DefaultsToBlack()
    {
        var shape = Single(Parse("<rect width='10' height='10' />"));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Black);
        shape.Stroke.ShouldBeNull();
    }


    [Fact]
    public void Shape_IsDropped_WhenItHasNeitherFillNorStroke()
    {
        // Nothing to draw, so it should not survive into a tree that is walked once per frame.
        Shapes(Parse("<rect width='10' height='10' fill='none' />")).ShouldBeEmpty();
    }


    [Fact]
    public void Stroke_SurvivesWithoutAFill()
    {
        var shape = Single(Parse("<path d='M0 0 L10 10' fill='none' stroke='red' stroke-width='3' />"));

        shape.Fill.ShouldBeNull();
        shape.Stroke.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Red);
        shape.StrokeWidth.ShouldBe(3f);
    }


    [Fact]
    public void RootPresentationAttributes_ReachTheShapes()
    {
        // How every stroke-based icon set is authored: the <svg> carries the paint and the glyph
        // carries only geometry.
        var document = SvgDocument.Parse(
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' " +
            "stroke='currentColor' stroke-width='2' stroke-linecap='round'>" +
            "<path d='M5 12 L19 12' /></svg>"
        );

        var shape = Single(document);

        shape.Fill.ShouldBeNull();
        shape.Stroke.ShouldBeSameAs(SvgCurrentColorPaint.Instance);
        shape.StrokeWidth.ShouldBe(2f);
        shape.LineCap.ShouldBe(LineCap.Round);
    }


    [Fact]
    public void CurrentColor_IsResolvedAtDrawTime()
    {
        var shape = Single(Parse("<rect width='10' height='10' fill='currentColor' />"));

        // Baking the tint in at parse time would make the parsed document tint-specific, and the
        // cache exists precisely so one parse serves placements that disagree about the colour.
        var paint = shape.Fill.ShouldBeOfType<SvgCurrentColorPaint>();
        paint.ColorFor(new SvgDrawContext(Colors.Teal)).ShouldBe(Colors.Teal);
        paint.ColorFor(new SvgDrawContext(Colors.Red)).ShouldBe(Colors.Red);
    }


    [Fact]
    public void ColorProperty_IsWhatCurrentColorMeans()
    {
        var shape = Single(Parse("<g color='#ff0000'><rect width='10' height='10' fill='currentColor' /></g>"));

        shape.Fill.ShouldBeOfType<SvgCurrentColorPaint>()
            .ColorFor(new SvgDrawContext(Colors.Blue))
            .ShouldBe(Colors.Blue);
    }


    [Theory]
    [InlineData("#f00")]
    [InlineData("#ff0000")]
    [InlineData("#ff0000ff")]
    [InlineData("rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)")]
    [InlineData("rgba(255, 0, 0, 1)")]
    [InlineData("hsl(0, 100%, 50%)")]
    [InlineData("red")]
    public void Colors_ParseInEveryCssSpelling(string value)
    {
        var color = Single(Parse($"<rect width='10' height='10' fill='{value}' />")).Fill.ShouldBeOfType<SvgSolidPaint>().Color;

        color.Red.ShouldBe(1f, 0.01f);
        color.Green.ShouldBe(0f, 0.01f);
        color.Blue.ShouldBe(0f, 0.01f);
        color.Alpha.ShouldBe(1f, 0.01f);
    }


    [Fact]
    public void ShortHexAlpha_DoublesTheDigit()
    {
        // #f008 is #ff000088, not #0f000008 - a detail that silently halves opacity when missed.
        var color = Single(Parse("<rect width='10' height='10' fill='#f008' />")).Fill.ShouldBeOfType<SvgSolidPaint>().Color;

        color.Alpha.ShouldBe(0x88 / 255f, 0.01f);
    }


    [Fact]
    public void StyleAttribute_BeatsPresentationAttribute()
    {
        var shape = Single(Parse("<rect width='10' height='10' fill='red' style='fill:blue' />"));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Blue);
    }


    [Fact]
    public void StylesheetRules_BeatPresentationAttributes()
    {
        // Illustrator and Figma both export shared appearance as classes; ignoring <style> renders a
        // large share of real files as flat black.
        var shape = Single(Parse(
            "<style>.brand { fill: #00ff00; }</style><rect class='brand' width='10' height='10' fill='red' />"
        ));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Lime);
    }


    [Fact]
    public void StylesheetSelectors_AreOrderedBySpecificity()
    {
        var shape = Single(Parse(
            "<style>rect { fill: red } .brand { fill: green } #one { fill: blue }</style>" +
            "<rect id='one' class='brand' width='10' height='10' />"
        ));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Blue);
    }


    [Fact]
    public void StructuralSelectors_AreSkippedRatherThanMisapplied()
    {
        // "g rect" is a descendant selector this does not evaluate. Applying it as if it named an
        // element would paint every rect in the document.
        var shape = Single(Parse("<style>g rect { fill: red }</style><rect width='10' height='10' />"));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Black);
    }


    [Fact]
    public void DisplayNone_DropsTheSubtree()
    {
        Shapes(Parse("<g display='none'><rect width='10' height='10' /></g>")).ShouldBeEmpty();
        Shapes(Parse("<rect width='10' height='10' visibility='hidden' />")).ShouldBeEmpty();
    }


    [Fact]
    public void Opacity_BelongsToTheNode_NotToInheritance()
    {
        // `opacity` composites an element as a unit rather than inheriting - two nested 0.5 groups
        // are 0.25, which only works if each keeps its own value.
        var document = Parse("<g opacity='0.5'><g opacity='0.5'><rect width='10' height='10' /></g></g>");
        var groups = Flatten(document.Root).OfType<SvgGroup>().Where(g => g.Opacity < 1f).ToArray();

        groups.Length.ShouldBe(2);
        groups.ShouldAllBe(g => g.Opacity == 0.5f);
        Single(document).Opacity.ShouldBe(1f);
    }


    [Fact]
    public void Transforms_ComposeLeftToRight()
    {
        var shape = Single(Parse("<g transform='translate(10 20) scale(2)'><rect width='10' height='10' /></g>"));
        var group = Flatten(Parse("<g transform='translate(10 20) scale(2)'><rect width='10' height='10' /></g>").Root)
            .OfType<SvgGroup>()
            .First(g => !g.Transform.IsIdentity);

        // A point at the origin lands at the translation; a point at (1,0) lands two units further.
        Vector2.Transform(Vector2.Zero, group.Transform).ShouldBe(new Vector2(10f, 20f));
        Vector2.Transform(Vector2.UnitX, group.Transform).ShouldBe(new Vector2(12f, 20f));
        shape.ShouldNotBeNull();
    }


    [Fact]
    public void Rotate_AboutAPoint_UsesTheThreeArgumentForm()
    {
        var group = Flatten(Parse("<g transform='rotate(90 5 5)'><rect width='10' height='10' /></g>").Root)
            .OfType<SvgGroup>()
            .First(g => !g.Transform.IsIdentity);

        var centre = Vector2.Transform(new Vector2(5f, 5f), group.Transform);

        centre.X.ShouldBe(5f, 0.001f);
        centre.Y.ShouldBe(5f, 0.001f);
    }


    [Fact]
    public void Rect_WithASingleRadius_ImpliesTheOther()
    {
        var square = Single(Parse("<rect width='20' height='20' />"));
        var rounded = Single(Parse("<rect width='20' height='20' rx='5' />"));

        // Both cover the same extent; the rounded one simply needs more segments to say so. The
        // tolerance is for the flattening MAUI measures curves with, not for the geometry.
        rounded.Bounds.Width.ShouldBe(square.Bounds.Width, 0.05f);
        rounded.Path.OperationCount.ShouldBeGreaterThan(square.Path.OperationCount);
    }


    [Fact]
    public void PrimitiveShapes_AllProduceGeometry()
    {
        Single(Parse("<circle cx='10' cy='10' r='5' />")).Bounds.Width.ShouldBe(10f, 0.05f);
        Single(Parse("<ellipse cx='10' cy='10' rx='8' ry='4' />")).Bounds.Height.ShouldBe(8f, 0.05f);
        Single(Parse("<polygon points='0,0 10,0 10,10' fill='red' />")).Bounds.Width.ShouldBe(10f, 0.05f);
        Single(Parse("<line x1='0' y1='0' x2='10' y2='0' stroke='red' />")).Bounds.Width.ShouldBe(10f, 0.05f);
    }


    [Fact]
    public void MalformedPathData_CostsOnlyThatShape()
    {
        // Artwork arrives from designers and CDNs. One bad `d` should leave a gap, not take out the
        // page the drawing sits on.
        var document = Parse("<path d='M z z q q q' fill='red' /><rect width='10' height='10' fill='blue' />");

        Shapes(document).Length.ShouldBe(1);
        Shapes(document)[0].Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Blue);
    }


    [Fact]
    public void Use_ExpandsItsTargetAndOffsetsIt()
    {
        var document = Parse(
            "<defs><rect id='box' width='10' height='10' fill='red' /></defs>" +
            "<use href='#box' x='20' y='30' />"
        );

        var shape = Single(document);
        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Red);

        var offset = Flatten(document.Root).OfType<SvgGroup>().First(g => !g.Transform.IsIdentity).Transform;
        Vector2.Transform(Vector2.Zero, offset).ShouldBe(new Vector2(20f, 30f));
    }


    [Fact]
    public void Use_PointingAtItsOwnAncestor_DoesNotRecurse()
    {
        var document = Parse("<g id='loop'><use href='#loop' /><rect width='10' height='10' /></g>");

        Shapes(document).Length.ShouldBe(1);
    }


    [Fact]
    public void Defs_AreNotDrawnWhereTheyAreDefined()
    {
        Shapes(Parse("<defs><rect width='10' height='10' /></defs>")).ShouldBeEmpty();
        Shapes(Parse("<symbol id='s'><rect width='10' height='10' /></symbol>")).ShouldBeEmpty();
    }


    [Fact]
    public void LinearGradient_ResolvesItsStopsAndGeometry()
    {
        var shape = Single(Parse(
            "<defs><linearGradient id='g' x1='0%' y1='0%' x2='100%' y2='0%'>" +
            "<stop offset='0%' stop-color='red' /><stop offset='100%' stop-color='blue' />" +
            "</linearGradient></defs>" +
            "<rect width='10' height='10' fill='url(#g)' />"
        ));

        var gradient = shape.Fill.ShouldBeOfType<SvgGradientPaint>();
        gradient.IsRadial.ShouldBeFalse();
        gradient.Units.ShouldBe(SvgGradientUnits.ObjectBoundingBox);
        gradient.Stops.Length.ShouldBe(2);
        gradient.Stops[0].Color.ShouldBe(Colors.Red);
        gradient.Stops[1].Offset.ShouldBe(1f);

        // In bounding-box units "100%" and "1" have to mean the same thing.
        gradient.End.X.ShouldBe(1f, 0.001f);
    }


    [Fact]
    public void Gradient_InheritsStopsThroughHref()
    {
        // The shape every exporter emits: one gradient holds the ramp, the rest re-aim it.
        var shape = Single(Parse(
            "<defs>" +
            "<linearGradient id='ramp'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient>" +
            "<linearGradient id='aimed' href='#ramp' x1='0' y1='0' x2='0' y2='1' />" +
            "</defs>" +
            "<rect width='10' height='10' fill='url(#aimed)' />"
        ));

        var gradient = shape.Fill.ShouldBeOfType<SvgGradientPaint>();
        gradient.Stops.Length.ShouldBe(2);
        gradient.End.Y.ShouldBe(1f, 0.001f);
    }


    [Fact]
    public void Gradient_WithOneStop_BecomesAFlatColour()
    {
        var shape = Single(Parse(
            "<defs><linearGradient id='g'><stop offset='0' stop-color='red' stop-opacity='0.5' /></linearGradient></defs>" +
            "<rect width='10' height='10' fill='url(#g)' />"
        ));

        var solid = shape.Fill.ShouldBeOfType<SvgSolidPaint>();
        solid.Color.Alpha.ShouldBe(0.5f, 0.01f);
    }


    [Fact]
    public void GradientStops_NeverRunBackwards()
    {
        var shape = Single(Parse(
            "<defs><linearGradient id='g'>" +
            "<stop offset='0.6' stop-color='red' /><stop offset='0.2' stop-color='blue' /><stop offset='2' stop-color='lime' />" +
            "</linearGradient></defs><rect width='10' height='10' fill='url(#g)' />"
        ));

        var offsets = shape.Fill.ShouldBeOfType<SvgGradientPaint>().Stops.Select(s => s.Offset).ToArray();

        offsets.ShouldBe([0.6f, 0.6f, 1f]);
    }


    [Fact]
    public void MissingPaintReference_FallsBackToWhatTheAuthorNamed()
    {
        var shape = Single(Parse("<rect width='10' height='10' fill='url(#nope) red' />"));

        shape.Fill.ShouldBeOfType<SvgSolidPaint>().Color.ShouldBe(Colors.Red);
    }


    [Fact]
    public void RadialGradient_ResolvesCentreAndRadius()
    {
        var shape = Single(Parse(
            "<defs><radialGradient id='g' cx='30%' cy='40%' r='60%'>" +
            "<stop offset='0' stop-color='white'/><stop offset='1' stop-color='black'/>" +
            "</radialGradient></defs><rect width='10' height='10' fill='url(#g)' />"
        ));

        var gradient = shape.Fill.ShouldBeOfType<SvgGradientPaint>();
        gradient.IsRadial.ShouldBeTrue();
        gradient.Center.X.ShouldBe(0.3f, 0.001f);
        gradient.Radius.ShouldBe(0.6f, 0.001f);
    }


    [Fact]
    public void ClipPath_BecomesGeometryOnTheNodeThatReferencesIt()
    {
        var shape = Single(Parse(
            "<defs><clipPath id='c'><circle cx='5' cy='5' r='5' /></clipPath></defs>" +
            "<rect width='10' height='10' clip-path='url(#c)' />"
        ));

        shape.Clip.ShouldNotBeNull();
        shape.Clip!.OperationCount.ShouldBeGreaterThan(0);
    }


    [Fact]
    public void ClipPath_WithSeveralShapes_UnionsThem()
    {
        // Two ClipPath calls would intersect, which is the opposite of what a multi-shape clipPath
        // means - so the geometry has to arrive as one path.
        var shape = Single(Parse(
            "<defs><clipPath id='c'><rect width='4' height='4' /><rect x='6' width='4' height='4' /></clipPath></defs>" +
            "<rect width='10' height='10' clip-path='url(#c)' />"
        ));

        shape.Clip.ShouldNotBeNull();
        shape.Clip!.GetBoundsByFlattening().Width.ShouldBe(10f, 0.01f);
    }


    [Fact]
    public void DashArray_IsExpressedInStrokeWidths()
    {
        // ICanvas measures a dash pattern in multiples of the stroke; SVG measures it in user units.
        var shape = Single(Parse("<path d='M0 0 L10 0' stroke='red' stroke-width='2' stroke-dasharray='4 2' />"));

        shape.DashPattern.ShouldBe([2f, 1f]);
    }


    [Fact]
    public void DashArray_WithAnOddCount_Repeats()
    {
        var shape = Single(Parse("<path d='M0 0 L10 0' stroke='red' stroke-width='1' stroke-dasharray='3' />"));

        shape.DashPattern.ShouldBe([3f, 3f]);
    }


    [Fact]
    public void FillRule_IsCarriedOntoTheShape()
    {
        Single(Parse("<path d='M0 0 L10 10' fill='red' fill-rule='evenodd' />")).Winding.ShouldBe(WindingMode.EvenOdd);
        Single(Parse("<path d='M0 0 L10 10' fill='red' />")).Winding.ShouldBe(WindingMode.NonZero);
    }


    [Fact]
    public void Text_TakesItsAnchorAndFont()
    {
        var text = Flatten(Parse("<text x='5' y='9' font-size='12' font-weight='bold' text-anchor='middle'>Hi</text>").Root)
            .OfType<SvgText>()
            .Single();

        text.Text.ShouldBe("Hi");
        text.Origin.ShouldBe(new PointF(5f, 9f));
        text.FontSize.ShouldBe(12f);
        text.Alignment.ShouldBe(HorizontalAlignment.Center);
    }


    [Fact]
    public void NonSvgContent_IsRefused()
    {
        Should.Throw<FormatException>(() => SvgDocument.Parse("<html><body/></html>"));
        Should.Throw<FormatException>(() => SvgDocument.Parse("not xml at all"));
    }


    [Fact]
    public void ExternalEntities_AreNotResolved()
    {
        // An image file must never become a file read or a network fetch, however it is authored.
        const string markup =
            "<!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>" +
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'><text x='0' y='0'>&xxe;</text></svg>";

        // Either the entity is ignored outright or the parse is refused; what must not happen is the
        // file being read.
        try
        {
            var document = SvgDocument.Parse(markup);
            Flatten(document.Root).OfType<SvgText>().ShouldAllBe(t => !t.Text.Contains("root:"));
        }
        catch (FormatException)
        {
            // Refusing is an equally good answer.
        }
    }


    [Fact]
    public void LooksLikeSvg_SniffsThePayload()
    {
        SvgDocument.LooksLikeSvg("<svg xmlns='http://www.w3.org/2000/svg'/>"u8).ShouldBeTrue();
        SvgDocument.LooksLikeSvg("<?xml version='1.0'?>\n<!-- a comment -->\n<svg />"u8).ShouldBeTrue();

        // PNG's magic number, which no amount of extension guessing should override.
        SvgDocument.LooksLikeSvg([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]).ShouldBeFalse();
        SvgDocument.LooksLikeSvg([]).ShouldBeFalse();
    }


    [Fact]
    public void Parse_HandlesGzippedSvgz()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 8 8'><rect width='8' height='8'/></svg>";

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, true))
            gzip.Write(Encoding.UTF8.GetBytes(markup));

        var compressed = buffer.ToArray();

        SvgDocument.LooksLikeSvg(compressed).ShouldBeTrue();
        SvgDocument.Parse(compressed).ViewBox.Width.ShouldBe(8f);
    }


    [Fact]
    public void Parse_HandlesUtf16AndByteOrderMarks()
    {
        var markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 6 6'><rect width='6' height='6'/></svg>";

        SvgDocument.Parse(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(markup)).ToArray())
            .ViewBox.Width.ShouldBe(6f);

        SvgDocument.Parse(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(markup)).ToArray())
            .ViewBox.Width.ShouldBe(6f);
    }


    [Fact]
    public void NodeCount_ReflectsWhatWasParsed()
    {
        Parse("<rect width='1' height='1'/><rect x='2' width='1' height='1'/>").NodeCount.ShouldBeGreaterThanOrEqualTo(3);
    }
}
