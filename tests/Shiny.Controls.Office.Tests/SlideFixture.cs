using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Builds .pptx packages in memory for tests.
/// </summary>
/// <remarks>
/// The point of this fixture is inheritance. The title placeholder on slide 1 carries text and nothing
/// else — no position, no size, no font — so a reader that does not walk slide → layout → master
/// produces an unpositioned, unstyled shape. The deck also puts slides in a deliberately different
/// order in the package than in the slide id list, to catch readers that use package order.
/// </remarks>
public static class SlideFixture
{
    public const string ThemeAccent1 = "4472C4";

    public static byte[] Build()
    {
        using var buffer = new MemoryStream();
        using (var document = PresentationDocument.Create(buffer, PresentationDocumentType.Presentation, autoSave: false))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();

            var masterPart = presentationPart.AddNewPart<SlideMasterPart>();
            masterPart.SlideMaster = BuildMaster();

            var themePart = masterPart.AddNewPart<ThemePart>();
            themePart.Theme = BuildTheme();

            var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
            layoutPart.SlideLayout = BuildLayout();
            layoutPart.AddPart(masterPart);

            masterPart.SlideMaster.SlideLayoutIdList = new SlideLayoutIdList(
                new SlideLayoutId { Id = 2147483649U, RelationshipId = masterPart.GetIdOfPart(layoutPart) });

            // Created in one order, listed in another, so package order and running order disagree.
            var second = presentationPart.AddNewPart<SlidePart>();
            second.Slide = BuildContentSlide();
            second.AddPart(layoutPart);

            var first = presentationPart.AddNewPart<SlidePart>();
            first.Slide = BuildTitleSlide();
            first.AddPart(layoutPart);

            var notes = second.AddNewPart<NotesSlidePart>();
            notes.NotesSlide = BuildNotes();

            presentationPart.Presentation.SlideMasterIdList = new SlideMasterIdList(
                new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(masterPart) });

            presentationPart.Presentation.SlideIdList = new SlideIdList(
                new SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(first) },
                new SlideId { Id = 257U, RelationshipId = presentationPart.GetIdOfPart(second) });

            // 16:9 at 96dpi is 960x540.
            presentationPart.Presentation.SlideSize = new SlideSize { Cx = 12192000, Cy = 6858000 };
            presentationPart.Presentation.NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };

            presentationPart.Presentation.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    static D.Theme BuildTheme() => new(
        new D.ThemeElements(
            new D.ColorScheme(
                new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText, LastColor = "000000" }),
                new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window, LastColor = "FFFFFF" }),
                new D.Dark2Color(new D.RgbColorModelHex { Val = "44546A" }),
                new D.Light2Color(new D.RgbColorModelHex { Val = "E7E6E6" }),
                new D.Accent1Color(new D.RgbColorModelHex { Val = ThemeAccent1 }),
                new D.Accent2Color(new D.RgbColorModelHex { Val = "ED7D31" }),
                new D.Accent3Color(new D.RgbColorModelHex { Val = "A5A5A5" }),
                new D.Accent4Color(new D.RgbColorModelHex { Val = "FFC000" }),
                new D.Accent5Color(new D.RgbColorModelHex { Val = "5B9BD5" }),
                new D.Accent6Color(new D.RgbColorModelHex { Val = "70AD47" }),
                new D.Hyperlink(new D.RgbColorModelHex { Val = "0563C1" }),
                new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "954F72" }))
            { Name = "Office" },
            new D.FontScheme(
                new D.MajorFont(new D.LatinFont { Typeface = "Calibri Light" }),
                new D.MinorFont(new D.LatinFont { Typeface = "Calibri" }))
            { Name = "Office" },
            new D.FormatScheme(
                new D.FillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })),
                new D.LineStyleList(new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }))),
                new D.EffectStyleList(new D.EffectStyle(new D.EffectList())),
                new D.BackgroundFillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })))
            { Name = "Office" }))
    { Name = "Office Theme" };

    static SlideMaster BuildMaster() => new(
        new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()),

                // A decorative shape with no placeholder: this is inherited onto every slide.
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 9U, Name = "Master stripe" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 0L, Y = 6400800L },
                            new D.Extents { Cx = 12192000L, Cy = 457200L }),
                        new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle },
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Accent1 })),
                    new TextBody(new D.BodyProperties(), new D.ListStyle())))),

        new ColorMap
        {
            Background1 = D.ColorSchemeIndexValues.Light1,
            Text1 = D.ColorSchemeIndexValues.Dark1,
            Background2 = D.ColorSchemeIndexValues.Light2,
            Text2 = D.ColorSchemeIndexValues.Dark2,
            Accent1 = D.ColorSchemeIndexValues.Accent1,
            Accent2 = D.ColorSchemeIndexValues.Accent2,
            Accent3 = D.ColorSchemeIndexValues.Accent3,
            Accent4 = D.ColorSchemeIndexValues.Accent4,
            Accent5 = D.ColorSchemeIndexValues.Accent5,
            Accent6 = D.ColorSchemeIndexValues.Accent6,
            Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
            FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink
        },

        new TextStyles(
            new TitleStyle(new D.Level1ParagraphProperties(
                new D.DefaultRunProperties { FontSize = 4400 })),
            new BodyStyle(new D.Level1ParagraphProperties(
                new D.CharacterBullet { Char = "•" },
                new D.DefaultRunProperties { FontSize = 2000 })),
            new OtherStyle(new D.Level1ParagraphProperties(new D.DefaultRunProperties { FontSize = 1800 }))));

    /// <summary>
    /// The layout is where the title placeholder gets its geometry and size. A slide's title carries
    /// neither, so this is what a correct reader has to find.
    /// </summary>
    static SlideLayout BuildLayout() => new(
        new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()),

                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2U, Name = "Title Placeholder" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties(
                            new PlaceholderShape { Type = PlaceholderValues.Title })),
                    new ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 838200L, Y = 365125L },
                            new D.Extents { Cx = 10515600L, Cy = 1325563L }),
                        new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
                    new TextBody(
                        new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center },
                        new D.ListStyle(new D.Level1ParagraphProperties(
                            new D.DefaultRunProperties { FontSize = 4400, Bold = true })))),

                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 3U, Name = "Body Placeholder" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties(
                            new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1U })),
                    new ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 838200L, Y = 1825625L },
                            new D.Extents { Cx = 10515600L, Cy = 4351338L }),
                        new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
                    new TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(
                            new D.Level1ParagraphProperties(
                                new D.CharacterBullet { Char = "•" },
                                new D.DefaultRunProperties { FontSize = 2800 }),
                            new D.Level2ParagraphProperties(
                                new D.CharacterBullet { Char = "▪" },
                                new D.DefaultRunProperties { FontSize = 2400 })))))),
        new ColorMapOverride(new D.MasterColorMapping()))
    { Type = SlideLayoutValues.TitleOnly };

    /// <summary>A title placeholder carrying only text — everything else has to be inherited.</summary>
    static Slide BuildTitleSlide() => new(
        new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()),

                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2U, Name = "Title 1" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties(
                            new PlaceholderShape { Type = PlaceholderValues.Title })),
                    new ShapeProperties(),
                    new TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(new D.Run(new D.RunProperties { Language = "en-US" }, new D.Text("Deck Title"))))))),
        new ColorMapOverride(new D.MasterColorMapping()));

    static Slide BuildContentSlide() => new(
        new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()),

                // Bulleted body placeholder with two outline levels.
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2U, Name = "Content 1" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties(
                            new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1U })),
                    new ShapeProperties(),
                    new TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(new D.Run(new D.Text("Top level point"))),
                        new D.Paragraph(
                            new D.ParagraphProperties { Level = 1 },
                            new D.Run(new D.Text("Nested point"))))),

                // A shape with explicit geometry, a themed fill and centred bold text.
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 3U, Name = "Callout" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 914400L, Y = 4572000L },
                            new D.Extents { Cx = 2743200L, Cy = 914400L }),
                        new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.RoundRectangle },
                        new D.SolidFill(new D.SchemeColor(new D.LuminanceModulation { Val = 60000 }, new D.LuminanceOffset { Val = 40000 })
                        { Val = D.SchemeColorValues.Accent1 }),
                        new D.Outline(new D.SolidFill(new D.RgbColorModelHex { Val = "203864" })) { Width = 12700 }),
                    new TextBody(
                        new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center },
                        new D.ListStyle(),
                        new D.Paragraph(
                            new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
                            new D.Run(
                                new D.RunProperties { Bold = true, FontSize = 1800 },
                                new D.Text("Callout text"))))),

                // A shape whose preset the viewer does not draw natively.
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 4U, Name = "Exotic" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 8229600L, Y = 4572000L },
                            new D.Extents { Cx = 914400L, Cy = 914400L }),
                        new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Heart }),
                    new TextBody(new D.BodyProperties(), new D.ListStyle())))),
        new ColorMapOverride(new D.MasterColorMapping()));

    static NotesSlide BuildNotes() => new(
        new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()),
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2U, Name = "Notes Placeholder" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties(
                            new PlaceholderShape { Type = PlaceholderValues.Body })),
                    new ShapeProperties(),
                    new TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(new D.Run(new D.Text("Remember to mention the numbers."))))))));
}
