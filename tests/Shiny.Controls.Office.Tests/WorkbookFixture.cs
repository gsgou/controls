using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Builds .xlsx packages in memory for tests.
/// </summary>
/// <remarks>
/// The fixture deliberately includes parts the editor has no model for — a custom XML part and custom
/// document properties. They exist so the round-trip tests have something to prove survived: a diff
/// over a package containing only parts the editor understands proves nothing.
/// </remarks>
public static class WorkbookFixture
{
    public const string ForeignXml = """<?xml version="1.0" encoding="utf-8"?><thing xmlns="urn:shiny:test"><keep me="yes"/></thing>""";

    public static byte[] Build(bool includeForeignParts = true)
    {
        using var buffer = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(buffer, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = BuildStylesheet();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(BuildSheetData());

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Data"
            });

            if (includeForeignParts)
            {
                var customXml = workbookPart.AddCustomXmlPart(CustomXmlPartType.CustomXml);
                using (var writer = new StreamWriter(customXml.GetStream(FileMode.Create)))
                    writer.Write(ForeignXml);

                var properties = document.AddCustomFilePropertiesPart();
                properties.Properties = new DocumentFormat.OpenXml.CustomProperties.Properties(
                    new DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty(
                        new DocumentFormat.OpenXml.VariantTypes.VTLPWSTR("shiny-round-trip"))
                    {
                        FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}",
                        PropertyId = 2,
                        Name = "Marker"
                    });
            }

            workbookPart.Workbook.Save();
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A workbook with three sheets, one of them hidden, cross-sheet formulas in both the plain and
    /// quoted spelling, and a sheet-scoped defined name.
    /// </summary>
    /// <remarks>
    /// The quoted sheet and the defined name are the two things a rename or a delete is most likely to
    /// get wrong, and neither is visible in a single-sheet fixture.
    /// </remarks>
    public static byte[] BuildMultiSheet()
    {
        using var buffer = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(buffer, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = BuildStylesheet();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            Add("Data", 1, BuildSheetData(), null);

            Add("Q1 Sales", 2, new SheetData(
                new Row(new Cell { CellReference = "A1", CellValue = new CellValue("10") }) { RowIndex = 1u }), null);

            Add("Summary", 3, new SheetData(
                new Row(
                    new Cell { CellReference = "A1", CellFormula = new CellFormula("Data!B1*2"), CellValue = new CellValue("84") },
                    new Cell { CellReference = "B1", CellFormula = new CellFormula("SUM('Q1 Sales'!A1:A3)"), CellValue = new CellValue("10") },
                    new Cell { CellReference = "C1", CellFormula = new CellFormula("\"Data!\"&Data!B1"), CellValue = new CellValue("Data!42") })
                { RowIndex = 1u }), null);

            Add("Scratch", 4, new SheetData(), SheetStateValues.Hidden);

            // Scoped to Summary, which is index 2 in the sheet list - the value a delete or a move has
            // to keep pointing at the right sheet.
            workbookPart.Workbook.AppendChild(new DefinedNames(
                new DefinedName("Data!$A$1:$A$5") { Name = "Region", LocalSheetId = 2u }));

            workbookPart.Workbook.Save();

            void Add(string name, uint id, SheetData data, SheetStateValues? state)
            {
                var part = workbookPart.AddNewPart<WorksheetPart>();
                part.Worksheet = new Worksheet(data);

                var sheet = new Sheet { Id = workbookPart.GetIdOfPart(part), SheetId = id, Name = name };
                if (state is { } value)
                    sheet.State = value;

                sheets.AppendChild(sheet);
            }
        }

        return buffer.ToArray();
    }

    static SheetData BuildSheetData()
    {
        var data = new SheetData();

        // A1 text (inline, so the fixture does not depend on a shared string table existing yet),
        // B1 number, C1 formula with a cached result, A2 styled number, D5 leaves a gap on purpose.
        data.AppendChild(new Row(
            new Cell { CellReference = "A1", DataType = CellValues.InlineString, InlineString = new InlineString(new DocumentFormat.OpenXml.Spreadsheet.Text("Widget")) },
            new Cell { CellReference = "B1", CellValue = new CellValue("42") },
            new Cell { CellReference = "C1", CellFormula = new CellFormula("B1*2"), CellValue = new CellValue("84") })
        { RowIndex = 1u });

        data.AppendChild(new Row(
            new Cell { CellReference = "A2", CellValue = new CellValue("1234.5"), StyleIndex = 1u },
            new Cell { CellReference = "B2", DataType = CellValues.Boolean, CellValue = new CellValue("1") })
        { RowIndex = 2u });

        data.AppendChild(new Row(
            new Cell { CellReference = "D5", DataType = CellValues.Error, CellValue = new CellValue("#DIV/0!") })
        { RowIndex = 5u });

        return data;
    }

    static Stylesheet BuildStylesheet() => new(
        new NumberingFormats(
            new NumberingFormat { NumberFormatId = 164u, FormatCode = "#,##0.00" })
        { Count = 1u },
        new Fonts(
            new Font(new FontSize { Val = 11d }, new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 11d }, new FontName { Val = "Calibri" }, new Color { Rgb = "FFCC0000" }))
        { Count = 2u },
        new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
            new Fill(new PatternFill(new ForegroundColor { Rgb = "FFFFF2CC" }) { PatternType = PatternValues.Solid }))
        { Count = 3u },
        new Borders(new Border()) { Count = 1u },
        new CellFormats(
            new CellFormat { NumberFormatId = 0u, FontId = 0u, FillId = 0u, BorderId = 0u },
            new CellFormat
            {
                NumberFormatId = 164u,
                FontId = 1u,
                FillId = 2u,
                BorderId = 0u,
                ApplyNumberFormat = true,
                ApplyFont = true,
                ApplyFill = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, WrapText = true }
            })
        { Count = 2u });
}
