using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Spreadsheet.Calc;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// An open <c>.xlsx</c> workbook.
/// </summary>
public sealed class Workbook : OfficeDocument
{
    readonly SpreadsheetDocument document;
    readonly List<Worksheet> sheets = new();
    readonly WorkbookCalcContext calcContext;
    bool contentChanged;
    bool formulasLoaded;

    Workbook(MemoryStream buffer, string? path, SpreadsheetDocument document, IUnsupportedFeatureSink unsupported)
        : base(buffer, path, unsupported)
    {
        this.document = document;
        this.Undo = new UndoStack<Workbook>(this);

        var workbookPart = document.WorkbookPart
            ?? throw new InvalidDataException("The package has no workbook part.");

        var workbookElement = workbookPart.Workbook
            ?? throw new InvalidDataException("The workbook part has no workbook element.");

        this.SharedStrings = new SharedStrings(workbookPart);
        this.Styles = new StyleResolver(workbookPart, unsupported);

        foreach (var sheet in workbookElement.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>())
        {
            if (sheet.Id?.Value is not { } relationshipId)
                continue;

            if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
            {
                // Chart sheets, dialog sheets and macro sheets all live in the same <sheets> list.
                // They are preserved on save; they are simply not something the grid can show.
                this.Unsupported.Report(new UnsupportedFeature(
                    sheet.Name?.Value ?? relationshipId,
                    "Non-worksheet sheet",
                    UnsupportedSeverity.NotRendered));
                continue;
            }

            this.sheets.Add(new Worksheet(
                this,
                worksheetPart,
                sheet.Name?.Value ?? $"Sheet{this.sheets.Count + 1}",
                sheet.SheetId?.Value ?? 0,
                IsSheetVisible(sheet)));
        }

        this.calcContext = new WorkbookCalcContext(this, TimeProvider.System);
        this.ReportUnsupported(workbookElement);
    }

    public IReadOnlyList<Worksheet> Sheets => this.sheets;

    public UndoStack<Workbook> Undo { get; }

    internal SharedStrings SharedStrings { get; }

    public StyleResolver Styles { get; }

    /// <summary>The calculation engine. Formulas are indexed lazily on first use — see <see cref="EnsureFormulasLoaded"/>.</summary>
    public CalcEngine Calc { get; } = new();

    public Worksheet this[string name]
        => this.sheets.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"No sheet named '{name}'.");

    public static async Task<Workbook> OpenAsync(
        string path,
        IUnsupportedFeatureSink? unsupported = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(path, cancellationToken).ConfigureAwait(false);
        return Create(buffer, path, unsupported);
    }

    public static async Task<Workbook> OpenAsync(
        Stream source,
        IUnsupportedFeatureSink? unsupported = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(source, cancellationToken).ConfigureAwait(false);
        return Create(buffer, null, unsupported);
    }


    /// <summary>
    /// Creates an empty workbook with a single sheet, held in memory until it is saved.
    /// </summary>
    public static Workbook Create(string sheetName = "Sheet1", IUnsupportedFeatureSink? unsupported = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var buffer = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(buffer, SpreadsheetDocumentType.Workbook, autoSave: false))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

            // A minimal but complete stylesheet. Excel rejects a styles part whose default font, fill and
            // border entries are missing, and every cell format indexes into them.
            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new Stylesheet(
                new Fonts(new DocumentFormat.OpenXml.Spreadsheet.Font(
                    new FontSize { Val = 11d },
                    new DocumentFormat.OpenXml.Spreadsheet.FontName { Val = "Calibri" })) { Count = 1u },
                new Fills(
                    new DocumentFormat.OpenXml.Spreadsheet.Fill(new PatternFill { PatternType = PatternValues.None }),
                    new DocumentFormat.OpenXml.Spreadsheet.Fill(new PatternFill { PatternType = PatternValues.Gray125 })) { Count = 2u },
                new Borders(new Border()) { Count = 1u },
                new CellFormats(new CellFormat { NumberFormatId = 0u, FontId = 0u, FillId = 0u, BorderId = 0u }) { Count = 1u });

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(new SheetData());

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1u,
                Name = sheetName
            });

            workbookPart.Workbook.Save();
            document.Save();
        }

        // An expandable buffer, not new MemoryStream(bytes): that overload is fixed-capacity, and a
        // package opened for editing on it fails the moment saving needs one more byte than it holds.
        var reopened = new MemoryStream();
        reopened.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
        reopened.Position = 0;
        buffer.Dispose();

        return Create(reopened, null, unsupported);
    }

    static Workbook Create(MemoryStream buffer, string? path, IUnsupportedFeatureSink? unsupported)
    {
        var sink = unsupported ?? NullUnsupportedFeatureSink.Instance;
        SpreadsheetDocument document;
        try
        {
            // AutoSave off: OpenXml otherwise rewrites every part it materialised when the document is
            // disposed, which would silently modify a file that was only ever opened and read.
            document = SpreadsheetDocument.Open(buffer, isEditable: true, new OpenSettings { AutoSave = false });
        }
        catch
        {
            buffer.Dispose();
            throw;
        }

        try
        {
            return new Workbook(buffer, path, document, sink);
        }
        catch
        {
            document.Dispose();
            buffer.Dispose();
            throw;
        }
    }


    /// <summary>
    /// Indexes every formula in the workbook and computes them all. Called automatically the first time
    /// a calculated value is needed, so opening a workbook stays cheap for read-only use.
    /// </summary>
    public void EnsureFormulasLoaded()
    {
        if (this.formulasLoaded)
            return;

        this.formulasLoaded = true;

        foreach (var sheet in this.sheets)
        {
            foreach (var (cell, formula) in sheet.Formulas())
                this.Calc.SetFormula(new CellAddress(sheet.Name, cell), formula);
        }

        this.Calc.RecalculateAll(this.calcContext);

        if (this.Calc.CircularCells.Count > 0)
        {
            this.Unsupported.Report(new UnsupportedFeature(
                "workbook", "Circular reference", UnsupportedSeverity.NotEditable,
                string.Join(", ", this.Calc.CircularCells.Take(5))));
        }
    }

    /// <summary>The value a cell should display: the computed result for a formula, otherwise the literal.</summary>
    public CellValue GetEffectiveValue(string sheetName, CellRef cell)
    {
        this.EnsureFormulasLoaded();

        var address = new CellAddress(sheetName, cell.Relative());
        if (this.Calc.TryGetComputed(address, out var computed))
            return computed;

        var sheet = this.sheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        return sheet?.GetValue(cell) ?? CellValue.Blank;
    }

    /// <summary>Evaluates an expression against the live workbook without storing it.</summary>
    public CellValue Evaluate(string formula, string sheetName, CellRef origin)
    {
        this.EnsureFormulasLoaded();
        return this.Calc.EvaluateOnce(formula, new RebasedCalcContext(this.calcContext, sheetName, origin));
    }

    internal void OnCellChanged(Worksheet sheet, CellRef cell)
    {
        this.OnContentChanged();

        // The first edit brings the engine online. Deferring it until something reads a calculated
        // value would leave the engine blind to edits made before that first read - so a circular
        // reference or a broken dependency would not surface until much later, if at all.
        this.EnsureFormulasLoaded();

        var address = new CellAddress(sheet.Name, cell.Relative());
        var formula = sheet.GetFormula(cell);

        if (formula is null)
            this.Calc.RemoveFormula(address);
        else
            this.Calc.SetFormula(address, formula);

        this.Calc.Recalculate([address], this.calcContext);
    }

    /// <summary>Pushes freshly computed results into the formula cells' cached values.</summary>
    void FlushCalculatedValues()
    {
        if (!this.formulasLoaded)
            return;

        foreach (var sheet in this.sheets)
        {
            foreach (var (cell, _) in sheet.Formulas().ToList())
            {
                if (this.Calc.TryGetComputed(new CellAddress(sheet.Name, cell), out var value))
                    sheet.WriteCachedValue(cell, value);
            }
        }

    }

    /// <summary>Applies an edit through the undo stack.</summary>
    public void Execute(IEditCommand<Workbook> command) => this.Undo.Execute(command);

    internal void OnContentChanged()
    {
        this.contentChanged = true;
        this.MarkDirty();
    }

    protected override void FlushToPackage()
    {
        // Saving is skipped entirely when nothing changed. Merely opening a workbook materialises
        // workbook.xml, styles.xml and the sheet parts, and serialising them back rewrites bytes that
        // no edit asked to change — so an unmodified document must never reach document.Save().
        if (!this.contentChanged)
            return;

        // Any edit invalidates cached formula results somewhere, so the engine has to be live before
        // the file is written - otherwise the saved document shows stale numbers to every reader.
        this.EnsureFormulasLoaded();
        this.FlushCalculatedValues();
        this.SharedStrings.UpdateCounts();
        this.MarkFullCalculationOnLoad();
        this.document.Save();
        this.contentChanged = false;
    }

    /// <summary>
    /// Tells Excel to recalculate everything when the file is opened.
    /// </summary>
    /// <remarks>
    /// Editing a cell invalidates the cached result of every formula that depends on it, anywhere in the
    /// workbook. Until the calc engine lands there is no way to know which those are, and leaving stale
    /// cached values in the file is the one failure mode worse than recalculating too much: the numbers
    /// look authoritative and are wrong.
    /// </remarks>
    void MarkFullCalculationOnLoad()
    {
        var workbook = this.document.WorkbookPart?.Workbook;
        if (workbook is null)
            return;

        var properties = workbook.CalculationProperties;
        if (properties is null)
        {
            properties = new CalculationProperties();

            // calcPr sits near the end of the workbook element; appending keeps schema order valid for
            // the elements we actually touch.
            workbook.AppendChild(properties);
        }

        properties.FullCalculationOnLoad = true;
    }

    /// <summary>OpenXml exposes sheet state as a struct-like value, so it cannot be pattern-matched.</summary>
    static bool IsSheetVisible(Sheet sheet)
    {
        var state = sheet.State?.Value;
        return state is null || (state != SheetStateValues.Hidden && state != SheetStateValues.VeryHidden);
    }

    void ReportUnsupported(DocumentFormat.OpenXml.Spreadsheet.Workbook workbookElement)
    {
        if (this.document.VbaProjectPart() is not null)
        {
            this.Unsupported.Report(new UnsupportedFeature(
                "vbaProject", "Macros", UnsupportedSeverity.NotEditable,
                "Preserved on save. Editing macros is not supported."));
        }

        foreach (var sheet in this.sheets)
        {
            if (sheet.MergedRanges.Count > 0)
            {
                this.Unsupported.Report(new UnsupportedFeature(
                    sheet.Name, "Merged cells", UnsupportedSeverity.NotEditable,
                    "Shown, but merges cannot be added or removed yet."));
            }
        }

        if (workbookElement.DefinedNames is not null)
        {
            // Defined names are preserved, but nothing rewrites them yet, so a structural edit would
            // leave them pointing at the wrong cells.
            this.Unsupported.Report(new UnsupportedFeature(
                "workbook", "Defined names", UnsupportedSeverity.NotEditable));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.document.Dispose();

        base.Dispose(disposing);
    }
}

static class SpreadsheetDocumentExtensions
{
    public static VbaProjectPart? VbaProjectPart(this SpreadsheetDocument document)
        => document.WorkbookPart?.VbaProjectPart;
}
