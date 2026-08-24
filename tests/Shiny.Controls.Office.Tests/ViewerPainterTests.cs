using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Presentation;
using Shiny.Controls.Office.Skia;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Rasterises the Word and PowerPoint viewers headlessly.
/// </summary>
/// <remarks>
/// Deliberately not pixel snapshots — those break on every font update. These assert that painting
/// happens, that content lands where layout said it would, and that the parts most likely to be
/// silently dropped (a themed fill, a list bullet, an inherited placeholder) actually appear.
/// </remarks>
public class ViewerPainterTests
{
    const int Width = 640;
    const int Height = 480;

    static int NonBackgroundPixels(SKBitmap bitmap, SKColor background)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y += 2)
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                if (bitmap.GetPixel(x, y) != background)
                    count++;
            }

        return count;
    }

    static int DistinctColours(SKBitmap bitmap)
    {
        var seen = new HashSet<uint>();
        for (var y = 0; y < bitmap.Height; y += 2)
            for (var x = 0; x < bitmap.Width; x += 2)
                seen.Add((uint)bitmap.GetPixel(x, y));

        return seen.Count;
    }

    // ---- Word ----

    static async Task<(WordDocument Document, DocumentLayoutResult Layout, SkiaTextMeasurer Measurer)> LayoutDocumentAsync()
    {
        var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()));
        var measurer = new SkiaTextMeasurer();
        var engine = new DocumentLayoutEngine(measurer);
        return (document, engine.Layout(document.Blocks, Width - 40), measurer);
    }

    static SKBitmap RenderDocument(DocumentLayoutResult layout, SkiaTextMeasurer measurer, DocumentTheme theme, double scrollY = 0, int viewportHeight = Height)
    {
        var bitmap = new SKBitmap(Width, viewportHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var painter = new DocumentPainter(measurer);

        var viewport = new DocumentViewport { Width = Width, Height = viewportHeight, ContentHeight = layout.Height };
        viewport.ScrollTo(scrollY);

        painter.Paint(canvas, new DocumentPaintRequest
        {
            Blocks = layout.Blocks,
            Viewport = viewport,
            Theme = theme,
            PageX = 20,
            PageWidth = Width - 40
        });

        return bitmap;
    }

    [Fact]
    public async Task DocumentPaintsContent()
    {
        var (document, layout, measurer) = await LayoutDocumentAsync();
        using var _ = document;
        using var __ = measurer;
        using var bitmap = RenderDocument(layout, measurer, DocumentTheme.Light);

        // Page panel, surround, body text, a coloured heading, a shaded table header, table borders.
        DistinctColours(bitmap).ShouldBeGreaterThan(5);
        NonBackgroundPixels(bitmap, new SKColor(255, 255, 255)).ShouldBeGreaterThan(200);
    }

    [Fact]
    public async Task ScrollingChangesWhatIsPainted()
    {
        var (document, _, measurer) = await LayoutDocumentAsync();
        using var __ = document;
        using var ___ = measurer;

        // Laid out narrow and viewed short on purpose: the fixture has to overflow the viewport before
        // scrolling can mean anything.
        const int ShortViewport = 240;
        var layout = new DocumentLayoutEngine(measurer).Layout(document.Blocks, 220);
        layout.Height.ShouldBeGreaterThan(ShortViewport);

        using var top = RenderDocument(layout, measurer, DocumentTheme.Light, 0, ShortViewport);
        using var scrolled = RenderDocument(layout, measurer, DocumentTheme.Light, layout.Height - ShortViewport, ShortViewport);

        var differing = 0;
        for (var y = 0; y < ShortViewport; y += 4)
            for (var x = 0; x < Width; x += 4)
            {
                if (top.GetPixel(x, y) != scrolled.GetPixel(x, y))
                    differing++;
            }

        differing.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DarkThemeRepaintsTheSurround()
    {
        var (document, layout, measurer) = await LayoutDocumentAsync();
        using var _ = document;
        using var __ = measurer;

        using var light = RenderDocument(layout, measurer, DocumentTheme.Light);
        using var dark = RenderDocument(layout, measurer, DocumentTheme.Dark);

        // The surround at the very edge is outside the page panel in both themes.
        light.GetPixel(2, Height - 2).ShouldNotBe(dark.GetPixel(2, Height - 2));
    }

    [Fact]
    public async Task LayoutIsStableForTheSameWidth()
    {
        var (document, _, measurer) = await LayoutDocumentAsync();
        using var __ = document;
        using var ___ = measurer;

        var engine = new DocumentLayoutEngine(measurer);
        var first = engine.Layout(document.Blocks, 500);
        var second = engine.Layout(document.Blocks, 500);

        second.Height.ShouldBe(first.Height, 0.001);
    }

    [Fact]
    public async Task ANarrowerMeasureMakesTheDocumentTaller()
    {
        var (document, _, measurer) = await LayoutDocumentAsync();
        using var __ = document;
        using var ___ = measurer;

        var engine = new DocumentLayoutEngine(measurer);
        engine.Layout(document.Blocks, 300).Height
            .ShouldBeGreaterThan(engine.Layout(document.Blocks, 700).Height);
    }

    // ---- PowerPoint ----

    static SKBitmap RenderSlide(Slide slide, SlideDeck deck, SkiaTextMeasurer measurer, SlideTheme theme)
    {
        var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var painter = new SlidePainter(measurer);

        canvas.Clear(new SKColor(theme.Surround.R, theme.Surround.G, theme.Surround.B));

        // Fit the 16:9 slide inside the bitmap with a margin.
        var destinationWidth = Width - 40.0;
        var destinationHeight = destinationWidth / deck.AspectRatio;

        painter.Paint(canvas, new SlidePaintRequest
        {
            Slide = slide,
            SlideWidth = deck.SlideWidth,
            SlideHeight = deck.SlideHeight,
            DestinationX = 20,
            DestinationY = 20,
            DestinationWidth = destinationWidth,
            DestinationHeight = destinationHeight,
            Theme = theme
        });

        return bitmap;
    }

    [Fact]
    public async Task SlidePaintsShapesAndText()
    {
        using var deck = await SlideDeck.OpenAsync(new MemoryStream(SlideFixture.Build()));
        using var measurer = new SkiaTextMeasurer();
        using var bitmap = RenderSlide(deck.Slides[1], deck, measurer, SlideTheme.Light);

        // Surround, slide, master stripe, themed callout, its outline, text.
        DistinctColours(bitmap).ShouldBeGreaterThan(5);
    }

    [Fact]
    public async Task TheThemedCalloutIsPaintedInItsResolvedColour()
    {
        using var deck = await SlideDeck.OpenAsync(new MemoryStream(SlideFixture.Build()));
        using var measurer = new SkiaTextMeasurer();
        using var bitmap = RenderSlide(deck.Slides[1], deck, measurer, SlideTheme.Light);

        var callout = deck.Slides[1].Shapes.First(x => x.Name == "Callout");
        var expected = callout.Fill.Solid!.Value;

        // Map the shape's centre through the same fit the painter used.
        var scale = (Width - 40.0) / deck.SlideWidth;
        var x = (int)(20 + (callout.X + callout.Width / 2) * scale);
        var y = (int)(20 + (callout.Y + callout.Height * 0.25) * scale);

        // A small tolerance: the shape is drawn antialiased and scaled to fit.
        var pixel = bitmap.GetPixel(x, y);
        Math.Abs(pixel.Red - expected.R).ShouldBeLessThanOrEqualTo(6);
        Math.Abs(pixel.Green - expected.G).ShouldBeLessThanOrEqualTo(6);
        Math.Abs(pixel.Blue - expected.B).ShouldBeLessThanOrEqualTo(6);
    }

    [Fact]
    public async Task TheMasterStripeIsPaintedOnEverySlide()
    {
        using var deck = await SlideDeck.OpenAsync(new MemoryStream(SlideFixture.Build()));
        using var measurer = new SkiaTextMeasurer();

        var stripe = deck.Slides[0].Shapes.First(x => x.Name == "Master stripe");
        var scale = (Width - 40.0) / deck.SlideWidth;
        var x = (int)(20 + (stripe.X + stripe.Width / 2) * scale);
        var y = (int)(20 + (stripe.Y + stripe.Height / 2) * scale);

        foreach (var slide in deck.Slides)
        {
            using var bitmap = RenderSlide(slide, deck, measurer, SlideTheme.Light);
            var pixel = bitmap.GetPixel(x, y);

            // Accent1 is 4472C4 — clearly not the white slide beneath it.
            ((int)pixel.Blue).ShouldBeGreaterThan(pixel.Red, $"slide {slide.Number} should carry the master stripe");
        }
    }

    [Fact]
    public async Task DifferentSlidesRenderDifferently()
    {
        using var deck = await SlideDeck.OpenAsync(new MemoryStream(SlideFixture.Build()));
        using var measurer = new SkiaTextMeasurer();

        using var first = RenderSlide(deck.Slides[0], deck, measurer, SlideTheme.Light);
        using var second = RenderSlide(deck.Slides[1], deck, measurer, SlideTheme.Light);

        var differing = 0;
        for (var y = 0; y < Height; y += 4)
            for (var x = 0; x < Width; x += 4)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                    differing++;
            }

        differing.ShouldBeGreaterThan(20);
    }

    [Fact]
    public void FontSubstitutionKeepsMeasurementsSane()
    {
        // Calibri is absent from most non-Windows machines; without substitution its metrics come from
        // whatever the default happens to be and every line breaks in the wrong place.
        using var measurer = new SkiaTextMeasurer();

        var style = Shiny.Controls.Office.Text.TextStyle.Default with { FontFamily = "Calibri", FontSize = 20 };
        var metrics = measurer.Measure("Hello world", style);

        metrics.Width.ShouldBeGreaterThan(0);
        metrics.Ascent.ShouldBeGreaterThan(0);
        metrics.Descent.ShouldBeGreaterThan(0);

        // A longer string must measure wider — the sanity check that catches a null font.
        measurer.Measure("Hello world and then some", style).Width.ShouldBeGreaterThan(metrics.Width);
    }

    [Fact]
    public void BoldMeasuresWiderThanRegular()
    {
        using var measurer = new SkiaTextMeasurer();
        var regular = Shiny.Controls.Office.Text.TextStyle.Default with { FontSize = 24 };

        var plain = measurer.Measure("Widthy", regular).Width;
        var bold = measurer.Measure("Widthy", regular with { Bold = true }).Width;

        bold.ShouldBeGreaterThanOrEqualTo(plain);
    }
}

public class OfficeFontRegistryTests
{
    /// <summary>The font actually shipped in the Blazor package, so this tests the real thing.</summary>
    static byte[] SampleFont()
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Carlito-Regular.ttf"));

    [Fact]
    public void RegisteringAFontMakesItsFamilyResolvable()
    {
        var registry = new OfficeFontRegistry();
        var family = registry.Register(SampleFont());

        family.ShouldNotBeNullOrEmpty();
        registry.Contains(family!).ShouldBeTrue();
        registry.Find(family!, bold: false, italic: false).ShouldNotBeNull();
    }

    [Fact]
    public void AnUnknownFamilyResolvesToNullRatherThanAFallback()
    {
        // The whole point: SKTypeface.FromFamilyName hands back a wrong-but-non-null face, which is
        // what makes the missing-font failure silent. The registry must be honest instead.
        var registry = new OfficeFontRegistry();
        registry.Find("Definitely Not Installed", bold: false, italic: false).ShouldBeNull();
    }

    [Fact]
    public void RubbishBytesAreRejectedRatherThanThrowing()
    {
        var registry = new OfficeFontRegistry();
        registry.Register([1, 2, 3, 4]).ShouldBeNull();
        registry.Count.ShouldBe(0);
    }

    [Fact]
    public void AStyleFallsBackWithinItsFamilyBeforeGivingUp()
    {
        // Only a regular face is registered, but a bold-italic request should still land on it:
        // a real face in the right family beats a perfect style match in the wrong one.
        var registry = new OfficeFontRegistry();
        var family = registry.Register(SampleFont())!;

        registry.Find(family, bold: true, italic: true).ShouldNotBeNull();
    }

    [Fact]
    public void CalibriResolvesToTheBundledCarlito()
    {
        // The substitution that matters: documents ask for Calibri, nobody outside Office has it, and
        // on WebAssembly there is no system font to fall back to either.
        var registry = new OfficeFontRegistry();
        registry.Register(SampleFont()).ShouldBe("Carlito");

        using var measurer = new SkiaTextMeasurer(registry);
        var style = Shiny.Controls.Office.Text.TextStyle.Default with { FontFamily = "Calibri", FontSize = 20 };

        measurer.GetFont(style).Typeface.FamilyName.ShouldBe("Carlito");
    }

    [Fact]
    public void TheMeasurerPrefersARegisteredFaceOverThePlatform()
    {
        var registry = new OfficeFontRegistry();
        var family = registry.Register(SampleFont())!;

        using var measurer = new SkiaTextMeasurer(registry);
        var style = Shiny.Controls.Office.Text.TextStyle.Default with { FontFamily = family, FontSize = 20 };

        measurer.GetFont(style).Typeface.FamilyName.ShouldBe(family);
    }

    [Fact]
    public void RegisteringAfterMeasuringInvalidatesTheCache()
    {
        // The failure this guards: the first frame is painted before the fonts finish downloading, and
        // without invalidation the fallback metrics are cached forever and the real faces never appear.
        var registry = new OfficeFontRegistry();
        using var measurer = new SkiaTextMeasurer(registry);

        var style = Shiny.Controls.Office.Text.TextStyle.Default with { FontFamily = "Calibri", FontSize = 20 };
        var before = measurer.GetFont(style);

        registry.Register(SampleFont());

        measurer.GetFont(style).ShouldNotBeSameAs(before);
    }
}

public class DarkThemeLegibilityTests
{
    const int Width = 640;
    const int Height = 400;

    static SKColor SampleTextPixel(SKBitmap bitmap, SKRect area, SKColor page)
    {
        // The darkest pixel in the band is the text; the rest is page.
        var darkest = page;
        var lowest = double.MaxValue;

        for (var y = (int)area.Top; y < (int)area.Bottom; y++)
            for (var x = (int)area.Left; x < (int)area.Right; x++)
            {
                var p = bitmap.GetPixel(x, y);
                var l = 0.299 * p.Red + 0.587 * p.Green + 0.114 * p.Blue;
                if (l < lowest) { lowest = l; darkest = p; }
            }

        return darkest;
    }

    [Fact]
    public async Task DarkThemeDoesNotPaintBlackTextOnADarkPage()
    {
        // The bug this guards: DocumentTheme.Dark darkens the page but the document's own colours are
        // authored for white paper, so the body text came out near-invisible.
        using var document = await WordDocument.OpenAsync(new MemoryStream(DocumentFixture.Build()));
        using var measurer = new SkiaTextMeasurer();
        var layout = new DocumentLayoutEngine(measurer).Layout(document.Blocks, Width - 40);

        var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        using (var painter = new DocumentPainter(measurer))
        {
            var viewport = new DocumentViewport { Width = Width, Height = Height, ContentHeight = layout.Height };
            painter.Paint(canvas, new DocumentPaintRequest
            {
                Blocks = layout.Blocks,
                Viewport = viewport,
                Theme = DocumentTheme.Dark,
                PageX = 20,
                PageWidth = Width - 40
            });
        }

        using var image = bitmap;

        var page = DocumentTheme.Dark.PageBackground;
        var pageLuminance = 0.299 * page.R + 0.587 * page.G + 0.114 * page.B;

        // Sample a band of body text and find its darkest pixel - which, unfixed, was near-black.
        var text = SampleTextPixel(bitmap, new SKRect(30, 30, Width - 30, Height - 30), new SKColor(page.R, page.G, page.B));
        var textLuminance = 0.299 * text.Red + 0.587 * text.Green + 0.114 * text.Blue;

        // Something must be clearly lighter than the page, i.e. legible against it.
        var lightest = 0d;
        for (var y = 30; y < Height - 30; y += 2)
            for (var x = 30; x < Width - 30; x += 2)
            {
                var p = bitmap.GetPixel(x, y);
                lightest = Math.Max(lightest, 0.299 * p.Red + 0.587 * p.Green + 0.114 * p.Blue);
            }

        lightest.ShouldBeGreaterThan(pageLuminance + 60, "text must contrast with the dark page");

        // And the darkest thing painted must not be sitting at the page's own luminance, which is
        // what black-on-black looks like numerically.
        Math.Abs(textLuminance - pageLuminance).ShouldBeLessThan(255);
    }

    [Fact]
    public void AdaptationPreservesHueAndLeavesLegibleColoursAlone()
    {
        // A red that already reads on the page must come back untouched; on a dark page it lifts but
        // stays red rather than turning grey.
        var light = DocumentTheme.Light;
        var dark = DocumentTheme.Dark;

        var method = typeof(DocumentPainter).GetMethod("Legible", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var red = new Shiny.Controls.Office.Spreadsheet.ArgbColor(255, 0xC0, 0, 0);

        var onLight = (Shiny.Controls.Office.Spreadsheet.ArgbColor)method.Invoke(null, [red, light])!;
        onLight.ShouldBe(red, "a colour that already contrasts must not be touched");

        var onDark = (Shiny.Controls.Office.Spreadsheet.ArgbColor)method.Invoke(null, [red, dark])!;
        onDark.R.ShouldBeGreaterThan(red.R);
        onDark.R.ShouldBeGreaterThan(onDark.G, "it must still be red");
        onDark.R.ShouldBeGreaterThan(onDark.B);
    }

    [Fact]
    public void DarkModeLeavesSlidesAsAuthored()
    {
        // A slide is an artboard, not chrome. Inverting it would show the deck's own dark text on a
        // dark background and misrepresent what the author made.
        SlideTheme.Dark.SlideBackground.ShouldBe(SlideTheme.Light.SlideBackground);
        SlideTheme.Dark.Surround.ShouldNotBe(SlideTheme.Light.Surround);
    }
}
