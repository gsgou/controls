using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Builds .docx packages in memory for tests.
/// </summary>
/// <remarks>
/// The fixture exercises the things that are actually hard: a style with a <c>basedOn</c> ancestor that
/// only overrides part of it, a numbered list with two levels, a table with a column span and a
/// vertical merge, and direct formatting layered on top of a named style.
/// </remarks>
public static class DocumentFixture
{
    public static byte[] Build()
    {
        using var buffer = new MemoryStream();
        using (var document = WordprocessingDocument.Create(buffer, WordprocessingDocumentType.Document, autoSave: false))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

            AddStyles(main);
            AddNumbering(main);

            var body = main.Document.Body!;

            body.AppendChild(Heading("Quarterly Report"));
            body.AppendChild(Paragraph("Plain body text that should wrap when the measure is narrow enough to force it."));
            body.AppendChild(FormattedParagraph());
            body.AppendChild(ListItem("First item", level: 0));
            body.AppendChild(ListItem("Second item", level: 0));
            body.AppendChild(ListItem("Nested item", level: 1));
            body.AppendChild(BuildTable());
            body.AppendChild(StyledParagraph("Accent", "Text using a style derived from another."));

            body.AppendChild(new SectionProperties(
                new PageSize { Width = 12240, Height = 15840 },
                new PageMargin { Left = 1440, Right = 1440, Top = 1440, Bottom = 1440 }));

            main.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    static void AddStyles(MainDocumentPart main)
    {
        var part = main.AddNewPart<StyleDefinitionsPart>();

        part.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Calibri" },
                    new FontSize { Val = "22" })),      // 11pt
                new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                    new SpacingBetweenLines { After = "160", Line = "259", LineRule = LineSpacingRuleValues.Auto }))),

            new Style(
                new StyleName { Val = "Normal" },
                new StyleRunProperties(new FontSize { Val = "22" }))
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            },

            new Style(
                new StyleName { Val = "heading 1" },
                new StyleParagraphProperties(
                    new OutlineLevel { Val = 0 },
                    new SpacingBetweenLines { Before = "240", After = "120" }),
                new StyleRunProperties(
                    new Bold(),
                    new FontSize { Val = "32" },
                    new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "2F5496" }))
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1"
            },

            // Base carries the font and size; Accent only overrides the colour, so a resolver that
            // ignores basedOn renders it at the wrong size in the wrong face.
            new Style(
                new StyleName { Val = "Base" },
                new StyleRunProperties(
                    new RunFonts { Ascii = "Georgia" },
                    new FontSize { Val = "28" },
                    new Italic()))
            {
                Type = StyleValues.Paragraph,
                StyleId = "Base"
            },

            new Style(
                new StyleName { Val = "Accent" },
                new BasedOn { Val = "Base" },
                new StyleRunProperties(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "C00000" }))
            {
                Type = StyleValues.Paragraph,
                StyleId = "Accent"
            });
    }

    static void AddNumbering(MainDocumentPart main)
    {
        var part = main.AddNewPart<NumberingDefinitionsPart>();

        part.Numbering = new Numbering(
            new AbstractNum(
                new Level(
                    new StartNumberingValue { Val = 1 },
                    new NumberingFormat { Val = NumberFormatValues.Decimal },
                    new LevelText { Val = "%1." },
                    new PreviousParagraphProperties(new Indentation { Left = "720", Hanging = "360" }))
                { LevelIndex = 0 },
                new Level(
                    new StartNumberingValue { Val = 1 },
                    new NumberingFormat { Val = NumberFormatValues.LowerLetter },
                    new LevelText { Val = "%1.%2." },
                    new PreviousParagraphProperties(new Indentation { Left = "1440", Hanging = "360" }))
                { LevelIndex = 1 })
            { AbstractNumberId = 1 },

            new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 });
    }

    static Paragraph Heading(string text) => new(
        new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
        new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)));

    static Paragraph Paragraph(string text) => new(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)));

    static Paragraph StyledParagraph(string styleId, string text) => new(
        new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
        new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)));

    /// <summary>Direct formatting layered over the default style, plus a hyperlink-less link colour.</summary>
    static Paragraph FormattedParagraph() => new(
        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
        new Run(new RunProperties(new Bold()), new DocumentFormat.OpenXml.Wordprocessing.Text("Bold ")),
        new Run(new RunProperties(new Italic()), new DocumentFormat.OpenXml.Wordprocessing.Text("italic ")),
        new Run(
            new RunProperties(new Underline { Val = UnderlineValues.Single }, new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "FF0000" }),
            new DocumentFormat.OpenXml.Wordprocessing.Text("underlined red")));

    static Paragraph ListItem(string text, int level) => new(
        new ParagraphProperties(new NumberingProperties(
            new NumberingLevelReference { Val = level },
            new NumberingId { Val = 1 })),
        new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)));

    static Table BuildTable()
    {
        var table = new Table(
            new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 })),
            new TableGrid(
                new GridColumn { Width = "3000" },
                new GridColumn { Width = "3000" },
                new GridColumn { Width = "3000" }));

        table.AppendChild(new TableRow(
            new TableRowProperties(new TableHeader()),
            HeaderCell("Region"),
            HeaderCell("Q1"),
            HeaderCell("Q2")));

        // First column starts a vertical merge that the next row continues.
        table.AppendChild(new TableRow(
            new TableCell(
                new TableCellProperties(new VerticalMerge { Val = MergedCellValues.Restart }),
                Paragraph("North")),
            new TableCell(Paragraph("100")),
            new TableCell(Paragraph("120"))));

        table.AppendChild(new TableRow(
            new TableCell(
                new TableCellProperties(new VerticalMerge()),
                Paragraph(string.Empty)),
            new TableCell(Paragraph("90")),
            new TableCell(Paragraph("95"))));

        // A cell spanning two grid columns.
        table.AppendChild(new TableRow(
            new TableCell(Paragraph("Total")),
            new TableCell(
                new TableCellProperties(new GridSpan { Val = 2 }),
                Paragraph("405"))));

        return table;
    }

    static TableCell HeaderCell(string text) => new(
        new TableCellProperties(new Shading { Fill = "DDEBF7", Val = ShadingPatternValues.Clear }),
        new Paragraph(new Run(new RunProperties(new Bold()), new DocumentFormat.OpenXml.Wordprocessing.Text(text))));
}
