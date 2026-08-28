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
    readonly WorkbookPart workbookPart;
    readonly DocumentFormat.OpenXml.Spreadsheet.Workbook workbookElement;
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

        this.workbookPart = workbookPart;
        this.workbookElement = workbookElement;

        this.SharedStrings = new SharedStrings(workbookPart);
        this.Styles = new StyleResolver(workbookPart, unsupported);
        this.StyleWriter = new StyleWriter(workbookPart, this.Styles, this.OnContentChanged);

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
                sheet,
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

    /// <summary>Reads a cell's style index into a flattened <see cref="ResolvedFormat"/>.</summary>
    public StyleResolver Styles { get; }

    /// <summary>
    /// The other direction: turns a <see cref="ResolvedFormat"/> into a style index cells can carry.
    /// </summary>
    /// <remarks>
    /// Public because a caller building a workbook from scratch has a real use for it, but note that
    /// interning a style is not itself an edit — pair it with a command so the change can be undone.
    /// </remarks>
    public StyleWriter StyleWriter { get; }

    /// <summary>The calculation engine. Formulas are indexed lazily on first use — see <see cref="EnsureFormulasLoaded"/>.</summary>
    public CalcEngine Calc { get; } = new();

    public Worksheet this[string name]
        => this.sheets.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"No sheet named '{name}'.");

    /// <summary>The sheet with that name, or null. Names match the way Excel matches them: case-insensitively.</summary>
    public Worksheet? Find(string? name)
        => name is null
            ? null
            : this.sheets.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The sheets Excel would show tabs for, in book order.</summary>
    public IEnumerable<Worksheet> VisibleSheets => this.sheets.Where(x => x.IsVisible);

    /// <summary>
    /// Raised after a sheet is added, removed, renamed, reordered, or shown or hidden.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="OfficeDocument.MarkDirty"/> because it means something different to a
    /// view: a cell edit repaints the grid, but a structural change can invalidate the sheet a view is
    /// pointing at entirely, and every host needs to hear about that whether or not it tracks dirt.
    /// </remarks>
    public event EventHandler? SheetsChanged;

    // ---- structural sheet edits ----
    //
    // All of these are internal: they are the working end of the commands in Spreadsheet.Commands, and
    // going around the command is what would leave the undo stack describing a workbook that no longer
    // exists. Each one leaves the package in a state Excel will open, which is why they all end at
    // AfterStructuralChange rather than just mutating and returning.

    /// <summary>
    /// Adds a sheet at <paramref name="index"/> in the tab order, either empty or from the XML of a
    /// sheet that was deleted or copied.
    /// </summary>
    internal Worksheet InsertSheet(string name, int index, string? worksheetXml, bool visible)
    {
        if (!SheetNames.IsAvailable(name, this.sheets.Select(x => x.Name), except: null, out var error))
            throw new ArgumentException(error, nameof(name));

        var part = this.workbookPart.AddNewPart<WorksheetPart>();
        part.Worksheet = worksheetXml is null
            ? new DocumentFormat.OpenXml.Spreadsheet.Worksheet(new SheetData())
            : new DocumentFormat.OpenXml.Spreadsheet.Worksheet(worksheetXml);

        var entry = new Sheet
        {
            Id = this.workbookPart.GetIdOfPart(part),
            SheetId = this.NextSheetId(),
            Name = name
        };

        if (!visible)
            entry.State = SheetStateValues.Hidden;

        index = Math.Clamp(index, 0, this.sheets.Count);
        var sheetsElement = this.SheetsElement();

        // The XML list can hold chart and macro sheets this does not model, so the position in
        // this.sheets is not the position in the element - it has to be resolved through the entry of
        // whichever modelled sheet is currently there.
        if (index < this.sheets.Count)
            sheetsElement.InsertBefore(entry, this.sheets[index].Entry);
        else
            sheetsElement.AppendChild(entry);

        var sheet = new Worksheet(this, part, entry, name, entry.SheetId!.Value, visible);
        this.sheets.Insert(index, sheet);

        this.AfterStructuralChange();
        return sheet;
    }

    /// <summary>
    /// Removes a sheet, returning everything needed to put it back exactly as it was.
    /// </summary>
    /// <remarks>
    /// The snapshot carries the sheet's whole XML as text rather than the live part, because the part
    /// is gone the moment this returns — and an undo that restored an empty sheet with the right name
    /// would look like it worked while having thrown the contents away.
    /// </remarks>
    internal SheetSnapshot RemoveSheet(string name)
    {
        var sheet = this[name];

        if (sheet.IsVisible && this.sheets.Count(x => x.IsVisible) == 1)
            throw new InvalidOperationException("A workbook must keep at least one visible sheet.");

        var snapshot = new SheetSnapshot(
            sheet.Name,
            this.sheets.IndexOf(sheet),
            sheet.IsVisible,
            sheet.Part.Worksheet?.OuterXml ?? new DocumentFormat.OpenXml.Spreadsheet.Worksheet(new SheetData()).OuterXml);

        var xmlIndex = this.XmlIndexOf(sheet);

        sheet.Entry.Remove();
        this.workbookPart.DeletePart(sheet.Part);
        this.sheets.Remove(sheet);

        // A defined name scoped to the sheet has nothing left to be scoped to; the ones above it have
        // shifted down by one, because LocalSheetId is a position in the list, not an identity.
        this.RemapDefinedNameScopes(scope => scope == xmlIndex ? null : scope > xmlIndex ? scope - 1 : scope);

        this.AfterStructuralChange();
        return snapshot;
    }

    /// <summary>Renames a sheet and repoints every formula and defined name that referred to it.</summary>
    internal void RenameSheet(string name, string newName)
    {
        var sheet = this[name];
        if (string.Equals(sheet.Name, newName, StringComparison.Ordinal))
            return;

        if (!SheetNames.IsAvailable(newName, this.sheets.Select(x => x.Name), except: sheet.Name, out var error))
            throw new ArgumentException(error, nameof(newName));

        var previous = sheet.Name;

        foreach (var other in this.sheets)
            other.RewriteFormulas(text => FormulaSheetRenamer.Rename(text, previous, newName));

        this.RewriteDefinedNames(previous, newName);

        sheet.Entry.Name = newName;
        sheet.Name = newName;

        this.AfterStructuralChange();
    }

    /// <summary>Moves a sheet to a different position in the tab order.</summary>
    internal void MoveSheet(string name, int index)
    {
        var sheet = this[name];
        var from = this.sheets.IndexOf(sheet);
        index = Math.Clamp(index, 0, this.sheets.Count - 1);

        if (from == index)
            return;

        var scopeBefore = this.XmlSheetOrder();

        this.sheets.RemoveAt(from);
        this.sheets.Insert(index, sheet);

        sheet.Entry.Remove();
        var sheetsElement = this.SheetsElement();

        // this.sheets is already in its new order, so the neighbour to insert before is the next
        // modelled sheet after the new position - if there is one.
        if (index + 1 < this.sheets.Count)
            sheetsElement.InsertBefore(sheet.Entry, this.sheets[index + 1].Entry);
        else
            sheetsElement.AppendChild(sheet.Entry);

        var scopeAfter = this.XmlSheetOrder();
        this.RemapDefinedNameScopes(scope =>
        {
            // Map through identity: where did the sheet that used to be at this position end up?
            if (scope >= scopeBefore.Count)
                return scope;

            var moved = scopeAfter.IndexOf(scopeBefore[(int)scope]);
            return moved < 0 ? null : (uint)moved;
        });

        this.AfterStructuralChange();
    }

    /// <summary>Hides or shows a sheet. Hidden sheets still calculate and are still saved.</summary>
    internal void SetSheetVisibility(string name, bool visible)
    {
        var sheet = this[name];
        if (sheet.IsVisible == visible)
            return;

        if (!visible && this.sheets.Count(x => x.IsVisible) == 1)
            throw new InvalidOperationException("A workbook must keep at least one visible sheet.");

        // Absent is the schema default and what Excel writes for a visible sheet, so clearing the
        // attribute is the correct way to unhide rather than writing state="visible".
        sheet.Entry.State = visible ? null : SheetStateValues.Hidden;
        sheet.IsVisible = visible;

        this.AfterStructuralChange();
    }

    Sheets SheetsElement()
        => this.workbookElement.Sheets ?? this.workbookElement.AppendChild(new Sheets());

    /// <summary>Where a sheet sits in the raw <c>&lt;sheets&gt;</c> list, which is what LocalSheetId counts.</summary>
    uint XmlIndexOf(Worksheet sheet)
    {
        var index = 0u;
        foreach (var entry in this.SheetsElement().Elements<Sheet>())
        {
            if (ReferenceEquals(entry, sheet.Entry))
                return index;

            index++;
        }

        return index;
    }

    List<Sheet> XmlSheetOrder() => this.SheetsElement().Elements<Sheet>().ToList();

    /// <summary>
    /// A sheet id no sheet is using. Ids are not positions and Excel does not require them to be dense,
    /// so counting past the highest is enough and avoids ever reusing the id of a deleted sheet.
    /// </summary>
    uint NextSheetId()
    {
        var highest = 0u;
        foreach (var entry in this.SheetsElement().Elements<Sheet>())
            highest = Math.Max(highest, entry.SheetId?.Value ?? 0u);

        return highest + 1;
    }

    void RewriteDefinedNames(string oldName, string newName)
    {
        foreach (var defined in this.workbookElement.DefinedNames?.Elements<DefinedName>() ?? Enumerable.Empty<DefinedName>())
        {
            if (defined.Text is not { Length: > 0 } text)
                continue;

            var rewritten = FormulaSheetRenamer.Rename(text, oldName, newName);
            if (!string.Equals(rewritten, text, StringComparison.Ordinal))
                defined.Text = rewritten;
        }
    }

    /// <summary>
    /// Moves sheet-scoped defined names onto their sheet's new position, dropping the ones whose sheet
    /// has gone. <paramref name="map"/> returns null for a scope that no longer exists.
    /// </summary>
    void RemapDefinedNameScopes(Func<uint, uint?> map)
    {
        var names = this.workbookElement.DefinedNames;
        if (names is null)
            return;

        foreach (var defined in names.Elements<DefinedName>().ToList())
        {
            if (defined.LocalSheetId?.Value is not { } scope)
                continue;

            var moved = map(scope);
            if (moved is null)
                defined.Remove();
            else if (moved.Value != scope)
                defined.LocalSheetId = moved.Value;
        }

        if (!names.Elements<DefinedName>().Any())
            names.Remove();
    }

    /// <summary>
    /// Puts the workbook back in a consistent state after the sheet list changes, and tells the views.
    /// </summary>
    void AfterStructuralChange()
    {
        this.DropCalculationChain();
        this.RebuildCalc();
        this.OnContentChanged();
        this.SheetsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes the calculation chain part.
    /// </summary>
    /// <remarks>
    /// calcChain.xml is a cache of the order Excel last computed cells in, and it names sheets by their
    /// position. After a sheet is added, removed or moved, that cache points at the wrong cells — and
    /// Excel does not recover from it gracefully, it declares the file corrupt and offers to repair it.
    /// The part is optional and is rebuilt on the next calculation, so the fix is simply to drop it.
    /// </remarks>
    void DropCalculationChain()
    {
        if (this.workbookPart.CalculationChainPart is { } chain)
            this.workbookPart.DeletePart(chain);
    }

    /// <summary>
    /// Reindexes the calc engine against the sheets as they are now.
    /// </summary>
    /// <remarks>
    /// Formulas are keyed by sheet name, so a rename orphans every entry for the old name and a delete
    /// leaves entries for a sheet that is gone; both would go on feeding stale results to the grid.
    /// Rebuilding wholesale is coarse, but structural edits are rare and a partial reindex here would be
    /// a second, subtler copy of the dependency graph's rules.
    /// </remarks>
    /// <summary>
    /// Reindexes and recomputes every formula. Needed after formula <em>text</em> changes in bulk —
    /// a sheet copy repoints the copy's self-references, and the engine is still holding the originals.
    /// </summary>
    internal void Recalculate()
    {
        this.RebuildCalc();
        this.OnContentChanged();
    }

    void RebuildCalc()
    {
        if (!this.formulasLoaded)
            return;

        this.Calc.Clear();
        this.formulasLoaded = false;
        this.EnsureFormulasLoaded();
    }

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

/// <summary>
/// A deleted sheet, complete enough to be put back byte for byte.
/// </summary>
/// <param name="Name">The name it had, which is also the name every formula still expects.</param>
/// <param name="Index">Its position in the tab order.</param>
/// <param name="IsVisible">Whether it was showing.</param>
/// <param name="Xml">The whole worksheet element, values, formulas, styles, merges and all.</param>
public sealed record SheetSnapshot(string Name, int Index, bool IsVisible, string Xml);

static class SpreadsheetDocumentExtensions
{
    public static VbaProjectPart? VbaProjectPart(this SpreadsheetDocument document)
        => document.WorkbookPart?.VbaProjectPart;
}
