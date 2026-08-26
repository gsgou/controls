using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;
using CellValue = Shiny.Controls.Office.Spreadsheet.CellValue;
using Workbook = Shiny.Controls.Office.Spreadsheet.Workbook;

namespace Shiny.Controls.Office.Tests;

public class SheetEditingTests
{
    static async Task<Workbook> OpenAsync()
    {
        using var source = new MemoryStream(WorkbookFixture.BuildMultiSheet(), writable: false);
        return await Workbook.OpenAsync(source);
    }

    static SpreadsheetController ControllerFor(Workbook workbook)
        => new(workbook, workbook.Sheets.First(x => x.IsVisible));

    [Fact]
    public async Task ReadsEverySheetInBookOrder()
    {
        using var workbook = await OpenAsync();

        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales", "Summary", "Scratch"]);
        workbook.VisibleSheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales", "Summary"]);
        workbook["Scratch"].IsVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task ComputesAcrossSheets()
    {
        using var workbook = await OpenAsync();

        workbook.GetEffectiveValue("Summary", CellRef.Parse("A1")).AsNumber().ShouldBe(84);
        workbook.GetEffectiveValue("Summary", CellRef.Parse("B1")).AsNumber().ShouldBe(10);
    }

    [Fact]
    public async Task AddSheet_LandsAtTheGivenPositionAndUndoesAway()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new AddSheetCommand("Notes", 1));
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Notes", "Q1 Sales", "Summary", "Scratch"]);

        workbook.Undo.Undo();
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales", "Summary", "Scratch"]);

        workbook.Undo.Redo();
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Notes", "Q1 Sales", "Summary", "Scratch"]);
    }

    [Fact]
    public async Task AddSheet_RejectsANameAlreadyInUse()
    {
        using var workbook = await OpenAsync();

        // Excel matches sheet names case-insensitively, so 'data' is not a free name here.
        Should.Throw<ArgumentException>(() => workbook.Execute(new AddSheetCommand("data", 0)));
    }

    [Fact]
    public async Task DeleteSheet_ComesBackWithItsContents()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new DeleteSheetCommand("Q1 Sales"));
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Summary", "Scratch"]);

        workbook.Undo.Undo();

        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales", "Summary", "Scratch"]);
        workbook["Q1 Sales"].GetValue(CellRef.Parse("A1")).AsNumber().ShouldBe(10);
    }

    [Fact]
    public async Task DeleteSheet_RefusesToRemoveTheLastVisibleOne()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new DeleteSheetCommand("Q1 Sales"));
        workbook.Execute(new DeleteSheetCommand("Summary"));

        // Scratch is hidden, so Data is the only sheet Excel would still have a tab for.
        Should.Throw<InvalidOperationException>(() => workbook.Execute(new DeleteSheetCommand("Data")));
    }

    [Fact]
    public async Task DeleteSheet_DropsDefinedNamesScopedToItAndShiftsTheRest()
    {
        using var workbook = await OpenAsync();

        // Region is scoped to Summary, which sits at index 2; removing Q1 Sales shifts it to 1.
        workbook.Execute(new DeleteSheetCommand("Q1 Sales"));

        var saved = workbook.ToArray();
        Scope(saved, "Region").ShouldBe(1u);

        workbook.Execute(new DeleteSheetCommand("Summary"));
        Scope(workbook.ToArray(), "Region").ShouldBeNull();
    }

    [Fact]
    public async Task RenameSheet_RewritesEveryFormulaThatPointedAtIt()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new RenameSheetCommand("Data", "Raw"));

        var summary = workbook["Summary"];
        summary.GetFormula(CellRef.Parse("A1")).ShouldBe("Raw!B1*2");

        // The bare "Data!" inside the string literal is text, not a reference, and must survive intact.
        summary.GetFormula(CellRef.Parse("C1")).ShouldBe("\"Data!\"&Raw!B1");

        workbook.GetEffectiveValue("Summary", CellRef.Parse("A1")).AsNumber().ShouldBe(84);
    }

    [Fact]
    public async Task RenameSheet_QuotesANameThatNeedsIt()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new RenameSheetCommand("Q1 Sales", "Quarter"));
        workbook["Summary"].GetFormula(CellRef.Parse("B1")).ShouldBe("SUM(Quarter!A1:A3)");

        workbook.Execute(new RenameSheetCommand("Quarter", "Q1 & Q2"));
        workbook["Summary"].GetFormula(CellRef.Parse("B1")).ShouldBe("SUM('Q1 & Q2'!A1:A3)");

        // Quoted going in, and still quoted coming back out under a name that reads as a cell.
        workbook.Execute(new RenameSheetCommand("Q1 & Q2", "Q1"));
        workbook["Summary"].GetFormula(CellRef.Parse("B1")).ShouldBe("SUM('Q1'!A1:A3)");
    }

    [Fact]
    public async Task RenameSheet_UndoesBackToTheOriginalFormulas()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new RenameSheetCommand("Data", "Raw"));
        workbook.Undo.Undo();

        workbook["Data"].Name.ShouldBe("Data");
        workbook["Summary"].GetFormula(CellRef.Parse("A1")).ShouldBe("Data!B1*2");
    }

    [Fact]
    public async Task RenameSheet_RejectsIllegalNames()
    {
        using var workbook = await OpenAsync();

        Should.Throw<ArgumentException>(() => workbook.Execute(new RenameSheetCommand("Data", "A/B")));
        Should.Throw<ArgumentException>(() => workbook.Execute(new RenameSheetCommand("Data", new string('x', 32))));
        Should.Throw<ArgumentException>(() => workbook.Execute(new RenameSheetCommand("Data", "Summary")));
    }

    [Fact]
    public async Task RenameSheet_ToADifferentCasingOfItsOwnNameIsAllowed()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new RenameSheetCommand("Data", "DATA"));
        workbook["Data"].Name.ShouldBe("DATA");
    }

    [Fact]
    public async Task MoveSheet_ReordersTabsAndUndoesBack()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new MoveSheetCommand("Summary", 0));
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Summary", "Data", "Q1 Sales", "Scratch"]);

        workbook.Undo.Undo();
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales", "Summary", "Scratch"]);
    }

    [Fact]
    public async Task MoveSheet_KeepsDefinedNameScopesOnTheirOwnSheet()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new MoveSheetCommand("Summary", 0));
        Scope(workbook.ToArray(), "Region").ShouldBe(0u);
    }

    [Fact]
    public async Task SetSheetVisibility_RoundTripsThroughTheStateAttribute()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new SetSheetVisibilityCommand("Scratch", true));
        workbook["Scratch"].IsVisible.ShouldBeTrue();
        State(workbook.ToArray(), "Scratch").ShouldBeNull();

        workbook.Undo.Undo();
        workbook["Scratch"].IsVisible.ShouldBeFalse();
        State(workbook.ToArray(), "Scratch").ShouldBe(SheetStateValues.Hidden);
    }

    [Fact]
    public async Task DuplicateSheet_CopiesContentAndRepointsSelfReferences()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new DuplicateSheetCommand("Summary", "Summary (2)", 3));

        var copy = workbook["Summary (2)"];
        copy.GetFormula(CellRef.Parse("A1")).ShouldBe("Data!B1*2");
        workbook.GetEffectiveValue("Summary (2)", CellRef.Parse("A1")).AsNumber().ShouldBe(84);

        // Data is another sheet, so the copy goes on reading it - only references back to the sheet
        // being copied move onto the copy.
        workbook.Execute(new DuplicateSheetCommand("Data", "Data (2)", 1));
        workbook["Data (2)"].GetValue(CellRef.Parse("B1")).AsNumber().ShouldBe(42);
    }

    [Fact]
    public async Task StructuralEdits_DropTheCalculationChain()
    {
        // calcChain.xml names cells by sheet position; left behind after a delete it points at the
        // wrong sheet, and Excel reports the file as corrupt rather than ignoring it.
        using var workbook = await OpenAsync();

        workbook.Execute(new DeleteSheetCommand("Q1 Sales"));

        using var package = SpreadsheetDocument.Open(new MemoryStream(workbook.ToArray()), isEditable: false);
        package.WorkbookPart!.CalculationChainPart.ShouldBeNull();
        package.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().Select(x => x.Name?.Value)
            .ShouldBe(["Data", "Summary", "Scratch"]);
    }

    [Fact]
    public async Task SavedPackage_KeepsRelationshipsAndSheetContentAligned()
    {
        using var workbook = await OpenAsync();

        workbook.Execute(new AddSheetCommand("Notes", 0));
        workbook.Execute(new SetCellValueCommand("Notes", CellRef.Parse("A1"), CellValue.FromText("hello")));
        workbook.Execute(new DeleteSheetCommand("Q1 Sales"));

        using var reopened = await Workbook.OpenAsync(new MemoryStream(workbook.ToArray(), writable: false));

        reopened.Sheets.Select(x => x.Name).ShouldBe(["Notes", "Data", "Summary", "Scratch"]);
        reopened["Notes"].GetValue(CellRef.Parse("A1")).AsText().ShouldBe("hello");
        reopened["Data"].GetValue(CellRef.Parse("A1")).AsText().ShouldBe("Widget");
    }

    // ---- controller ----

    [Fact]
    public async Task Controller_RemembersWhereEachSheetWasLeft()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);
        controller.Resize(800, 600);

        controller.Selection.MoveTo(CellRef.Parse("D4"));
        controller.Metrics.Columns.SetSize(0, 210);

        controller.SwitchSheet("Summary");
        controller.Selection.Active.ShouldBe(CellRef.Parse("A1"));

        controller.SwitchSheet("Data");
        controller.Selection.Active.ShouldBe(CellRef.Parse("D4"));
        controller.Metrics.Columns.SizeOf(0).ShouldBe(210);
    }

    [Fact]
    public async Task Controller_AddSheet_SwitchesToTheNewSheet()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);

        var added = controller.AddSheet();

        added.Name.ShouldBe("Sheet5");
        controller.Sheet.ShouldBe(added);

        // Straight after Data, not at the end: a new sheet goes in beside the one you were on.
        workbook.Sheets.Select(x => x.Name).ShouldBe(["Data", "Sheet5", "Q1 Sales", "Summary", "Scratch"]);
    }

    [Fact]
    public async Task Controller_DeletingTheSheetOnScreen_MovesToTheNextTab()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);
        controller.SwitchSheet("Q1 Sales");

        controller.DeleteSheet(controller.Sheet);

        controller.Sheet.Name.ShouldBe("Summary");
    }

    [Fact]
    public async Task Controller_UndoingADelete_ComesBackToTheRestoredSheet()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);
        controller.SwitchSheet("Summary");
        controller.Selection.MoveTo(CellRef.Parse("C3"));

        controller.DeleteSheet(controller.Sheet);
        controller.Sheet.Name.ShouldBe("Q1 Sales");

        controller.Undo();

        // A restored sheet is a different Worksheet instance with the same name; the remembered
        // position has to be found by name or the grid jumps back to A1.
        controller.Sheet.Name.ShouldBe("Summary");
        controller.Selection.Active.ShouldBe(CellRef.Parse("C3"));
    }

    [Fact]
    public async Task Controller_HidingTheSheetOnScreen_MovesToAVisibleOne()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);
        controller.SwitchSheet("Summary");

        controller.SetSheetVisible(controller.Sheet, false);

        controller.Sheet.IsVisible.ShouldBeTrue();
        controller.Sheet.Name.ShouldBe("Data");
        controller.VisibleSheets.Select(x => x.Name).ShouldBe(["Data", "Q1 Sales"]);
    }

    [Fact]
    public async Task Controller_RenameKeepsTheSheetOnScreenAndItsPosition()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);
        controller.Selection.MoveTo(CellRef.Parse("B3"));

        controller.RenameSheet(controller.Sheet, "Raw");

        controller.Sheet.Name.ShouldBe("Raw");
        controller.Selection.Active.ShouldBe(CellRef.Parse("B3"));

        controller.SwitchSheet("Summary");
        controller.SwitchSheet("Raw");
        controller.Selection.Active.ShouldBe(CellRef.Parse("B3"));
    }

    [Fact]
    public async Task Controller_ReportsWhenTheLastVisibleSheetCannotGo()
    {
        using var workbook = await OpenAsync();
        var controller = ControllerFor(workbook);

        controller.CanRemoveFromView(workbook["Data"]).ShouldBeTrue();

        controller.DeleteSheet(workbook["Q1 Sales"]);
        controller.DeleteSheet(workbook["Summary"]);

        controller.CanRemoveFromView(workbook["Data"]).ShouldBeFalse();
        controller.CanRemoveFromView(workbook["Scratch"]).ShouldBeTrue();
    }

    static uint? Scope(byte[] package, string name)
        => DefinedNameOf(package, name)?.LocalSheetId?.Value;

    static DefinedName? DefinedNameOf(byte[] package, string name)
    {
        using var document = SpreadsheetDocument.Open(new MemoryStream(package), isEditable: false);
        return document.WorkbookPart?.Workbook.DefinedNames?
            .Elements<DefinedName>()
            .FirstOrDefault(x => x.Name?.Value == name);
    }

    static SheetStateValues? State(byte[] package, string sheetName)
    {
        using var document = SpreadsheetDocument.Open(new MemoryStream(package), isEditable: false);
        return document.WorkbookPart?.Workbook.Sheets?
            .Elements<Sheet>()
            .FirstOrDefault(x => x.Name?.Value == sheetName)?
            .State?.Value;
    }
}
