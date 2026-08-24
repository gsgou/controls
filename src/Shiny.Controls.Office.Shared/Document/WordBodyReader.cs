using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Text;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Walks a document body and produces the reflowable block model.
/// </summary>
sealed class WordBodyReader(
    MainDocumentPart main,
    WordStyleResolver styles,
    WordNumbering numbering,
    IUnsupportedFeatureSink unsupported)
{
    /// <summary>Re-projects one paragraph after its XML has been edited.</summary>
    public DocumentParagraph Reread(Paragraph paragraph) => this.ReadParagraph(paragraph);

    public IReadOnlyList<DocumentBlock> ReadBody(Body? body)
    {
        var blocks = new List<DocumentBlock>();
        if (body is null)
            return blocks;

        foreach (var block in this.ReadBlocks(body))
            blocks.Add(block);

        return blocks;
    }

    IEnumerable<DocumentBlock> ReadBlocks(OpenXmlElement container)
    {
        foreach (var child in container.ChildElements)
        {
            switch (child)
            {
                case Paragraph paragraph:
                    yield return this.ReadParagraph(paragraph);
                    break;

                case Table table:
                    yield return this.ReadTable(table);
                    break;

                case SdtBlock structured:
                    // A content control is a wrapper; its content is ordinary blocks underneath.
                    var content = structured.Descendants<SdtContentBlock>().FirstOrDefault();
                    if (content is null)
                        break;

                    foreach (var inner in this.ReadBlocks(content))
                        yield return inner;

                    break;
            }
        }
    }

    DocumentParagraph ReadParagraph(Paragraph paragraph)
    {
        var properties = paragraph.ParagraphProperties;
        var styleId = properties?.ParagraphStyleId?.Val?.Value;

        var format = styles.ParagraphFormatFor(styleId);
        var runStyle = styles.RunStyleFor(styleId);

        if (properties is not null)
        {
            format = WordStyleResolver.ApplyParagraphProperties(format, properties);

            // rPr inside pPr is the paragraph mark's formatting, and Word uses it as the baseline for
            // runs that carry no formatting of their own.
            if (properties.ParagraphMarkRunProperties is { } markProperties)
                runStyle = WordStyleResolver.ApplyRunProperties(runStyle, markProperties, ThemeFonts.From(main.ThemePart));
        }

        var label = this.ReadListLabel(paragraph, properties, styleId, runStyle, ref format);
        var runs = this.ReadInlines(paragraph, runStyle).ToList();

        return new DocumentParagraph(runs, format)
        {
            Element = paragraph,
            List = label,
            StyleName = styles.StyleName(styleId)
        };
    }

    ListLabel? ReadListLabel(Paragraph paragraph, ParagraphProperties? properties, string? styleId, TextStyle runStyle, ref ParagraphFormat format)
    {
        var numberingProperties = properties?.NumberingProperties;
        var numId = numberingProperties?.NumberingId?.Val?.Value;
        var level = numberingProperties?.NumberingLevelReference?.Val?.Value ?? 0;

        if (numId is null || numId == 0 || numbering.IsEmpty)
            return null;

        var label = numbering.Next(numId.Value, level, runStyle);
        if (label is null)
            return null;

        // The level's own indent applies unless the paragraph overrides it directly.
        if (format.IndentLeft == 0 && label.Indent > 0)
            format = format with { IndentLeft = label.Indent };

        return label;
    }

    IEnumerable<StyledRun> ReadInlines(OpenXmlElement container, TextStyle inherited)
    {
        foreach (var child in container.ChildElements)
        {
            switch (child)
            {
                case Run run:
                    foreach (var piece in this.ReadRun(run, inherited, null))
                        yield return piece;

                    break;

                case Hyperlink hyperlink:
                    var target = this.ResolveHyperlink(hyperlink);
                    foreach (var run in hyperlink.Descendants<Run>())
                    {
                        foreach (var piece in this.ReadRun(run, inherited, target))
                            yield return piece;
                    }

                    break;

                case SdtRun structured:
                    foreach (var run in structured.Descendants<Run>())
                    {
                        foreach (var piece in this.ReadRun(run, inherited, null))
                            yield return piece;
                    }

                    break;

                case InsertedRun inserted:
                    // A tracked insertion is part of the text as it stands; a deletion is not.
                    foreach (var run in inserted.Elements<Run>())
                    {
                        foreach (var piece in this.ReadRun(run, inherited, null))
                            yield return piece;
                    }

                    break;

                case SimpleField field:
                    foreach (var run in field.Descendants<Run>())
                    {
                        foreach (var piece in this.ReadRun(run, inherited, null))
                            yield return piece;
                    }

                    break;
            }
        }
    }

    IEnumerable<StyledRun> ReadRun(Run run, TextStyle inherited, string? link)
    {
        var style = inherited;

        if (run.RunProperties?.RunStyle?.Val?.Value is { } runStyleId)
            style = styles.RunStyleFor(runStyleId, style);

        if (run.RunProperties is not null)
            style = WordStyleResolver.ApplyRunProperties(style, run.RunProperties, ThemeFonts.From(main.ThemePart));

        if (link is not null)
            style = style with { Link = link };

        foreach (var child in run.ChildElements)
        {
            switch (child)
            {
                case DocumentFormat.OpenXml.Wordprocessing.Text text:
                    yield return new StyledRun(text.Text ?? string.Empty, style);
                    break;

                case TabChar:
                    // A real tab needs tab stops the reflow view does not model; four spaces is the
                    // honest approximation and keeps indented text from collapsing.
                    yield return new StyledRun("    ", style);
                    break;

                case Break br:
                    yield return new StyledRun(string.Empty, style) { IsBreak = true };
                    _ = br;
                    break;

                case DocumentFormat.OpenXml.Wordprocessing.Drawing drawing:
                    var image = this.ReadImage(drawing);
                    if (image is not null)
                        yield return new StyledRun(string.Empty, style) { Image = image };

                    break;

                case Picture:
                    unsupported.Report(new UnsupportedFeature("document", "VML picture", UnsupportedSeverity.NotRendered));
                    break;

                case SymbolChar symbol when symbol.Char?.Value is { } code:
                    if (int.TryParse(code, System.Globalization.NumberStyles.HexNumber, null, out var value))
                        yield return new StyledRun(char.ConvertFromUtf32(value & 0xFF), style);

                    break;
            }
        }
    }

    InlineImage? ReadImage(DocumentFormat.OpenXml.Wordprocessing.Drawing drawing)
    {
        var blip = drawing.Descendants<Drawing.Blip>().FirstOrDefault();
        var extent = drawing.Descendants<Drawing.Wordprocessing.Extent>().FirstOrDefault();

        if (blip?.Embed?.Value is not { } relationshipId)
            return null;

        if (main.GetPartById(relationshipId) is not ImagePart part)
            return null;

        try
        {
            using var stream = part.GetStream();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);

            var width = extent?.Cx is { } cx ? OoxmlUnits.EmuToPixels(cx) : 0;
            var height = extent?.Cy is { } cy ? OoxmlUnits.EmuToPixels(cy) : 0;

            // An image with no stated extent still has to occupy something, or it vanishes.
            if (width <= 0 || height <= 0)
            {
                width = 200;
                height = 150;
            }

            return new InlineImage(copy.ToArray(), width, height, drawing.Descendants<Drawing.Wordprocessing.DocProperties>().FirstOrDefault()?.Description);
        }
        catch (Exception ex)
        {
            unsupported.Report(new UnsupportedFeature("media", "Image", UnsupportedSeverity.NotRendered, ex.Message));
            return null;
        }
    }

    string? ResolveHyperlink(Hyperlink hyperlink)
    {
        if (hyperlink.Anchor?.Value is { } anchor)
            return "#" + anchor;

        if (hyperlink.Id?.Value is not { } id)
            return null;

        try
        {
            return main.HyperlinkRelationships.FirstOrDefault(x => x.Id == id)?.Uri?.ToString();
        }
        catch (Exception)
        {
            // A malformed relationship should cost the link, not the paragraph.
            return null;
        }
    }

    DocumentTable ReadTable(Table table)
    {
        var rows = new List<DocumentTableRow>();

        foreach (var row in table.Elements<TableRow>())
        {
            var cells = new List<DocumentTableCell>();

            foreach (var cell in row.Elements<TableCell>())
            {
                var properties = cell.TableCellProperties;
                var merge = properties?.VerticalMerge;

                // vMerge with no val - or val="continue" - means this cell continues the one above.
                var isContinuation = merge is not null &&
                    (merge.Val?.Value is null || merge.Val.Value == MergedCellValues.Continue);

                var shading = properties?.Shading?.Fill?.Value is { } fill &&
                              !fill.Equals("auto", StringComparison.OrdinalIgnoreCase) &&
                              WordStyleResolver.TryParseHex(fill, out var color)
                    ? color
                    : (Spreadsheet.ArgbColor?)null;

                double? width = null;
                if (properties?.TableCellWidth?.Width?.Value is { } widthValue &&
                    properties.TableCellWidth.Type?.Value == TableWidthUnitValues.Dxa &&
                    double.TryParse(widthValue, out var twips))
                    width = OoxmlUnits.TwipsToPixels(twips);

                cells.Add(new DocumentTableCell(this.ReadBlocks(cell).ToList())
                {
                    ColumnSpan = (int)(properties?.GridSpan?.Val?.Value ?? 1),
                    IsVerticalContinuation = isContinuation,
                    Shading = shading,
                    Width = width
                });
            }

            var isHeader = row.TableRowProperties?.Elements<TableHeader>().Any() == true;
            rows.Add(new DocumentTableRow(cells) { IsHeader = isHeader });
        }

        var widths = table.Elements<TableGrid>()
            .FirstOrDefault()?
            .Elements<GridColumn>()
            .Select(x => x.Width?.Value is { } w && double.TryParse(w, out var twips) ? OoxmlUnits.TwipsToPixels(twips) : 0)
            .ToList() ?? [];

        var borders = table.GetFirstChild<TableProperties>()?.TableBorders;
        var hasBorders = borders is null || borders.TopBorder?.Val?.Value != BorderValues.None;

        return new DocumentTable(rows)
        {
            ColumnWidths = widths,
            HasBorders = hasBorders
        };
    }
}
