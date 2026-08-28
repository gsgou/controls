using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Cell, column and row formatting: interning styles, the delta semantics of a toolbar command, and
/// what actually reaches the file.
/// </summary>
public class CellFormattingTests
{
    static Workbook New() => Workbook.Create("Sheet1");

    static SpreadsheetController Controller(Workbook workbook) => new(workbook, workbook.Sheets[0]);

    static ResolvedFormat FormatOf(Workbook workbook, string reference)
    {
        var sheet = workbook.Sheets[0];
        return workbook.Styles.Resolve(sheet.GetEffectiveStyleIndex(CellRef.Parse(reference)));
    }

    static void Set(Workbook workbook, string reference, CellValue value)
        => workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse(reference), value));

    // ---- the delta ----

    [Fact]
    public void FormatChange_LeavesEverythingItDoesNotName()
    {
        var start = ResolvedFormat.Default with { Italic = true, Foreground = new ArgbColor(255, 200, 0, 0) };
        var result = new CellFormatChange { Bold = true }.ApplyTo(start);

        result.Bold.ShouldBeTrue();
        result.Italic.ShouldBeTrue("a change that does not mention italic must not clear it");
        result.Foreground.ShouldBe(start.Foreground);
    }

    [Fact]
    public void ClearChange_StartsFromTheDefault()
    {
        var start = ResolvedFormat.Default with { Bold = true, Background = new ArgbColor(255, 1, 2, 3) };
        CellFormatChange.Clear.ApplyTo(start).ShouldBe(ResolvedFormat.Default);
    }

    // ---- interning ----

    [Fact]
    public void IdenticalFormats_InternToTheSameIndex()
    {
        using var workbook = New();
        var format = ResolvedFormat.Default with { Bold = true };

        var first = workbook.StyleWriter.Intern(format);
        var second = workbook.StyleWriter.Intern(format with { });

        second.ShouldBe(first);
        first.ShouldNotBe(0u);
    }

    [Fact]
    public void DefaultFormat_InternsToIndexZero()
    {
        using var workbook = New();
        workbook.StyleWriter.Intern(ResolvedFormat.Default).ShouldBe(0u);
    }

    [Fact]
    public void BoldingManyCells_AddsOneCellFormat()
    {
        // The whole point of interning: without it a wide selection appends one font and one cell
        // format per cell, and the styles part grows without bound over a session of editing.
        using var workbook = New();
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:J20"));
        controller.ToggleBold();

        var xml = SheetXml(workbook, "xl/styles.xml");
        CountOf(xml, "<x:xf ").ShouldBe(2, "the default cell format plus exactly one for bold");
    }

    // ---- toggles ----

    [Fact]
    public void Bold_TogglesOffAgain()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.ToggleBold();
        FormatOf(workbook, "A1").Bold.ShouldBeTrue();

        controller.ToggleBold();
        FormatOf(workbook, "A1").Bold.ShouldBeFalse();
    }

    [Fact]
    public void Formatting_AccumulatesRatherThanReplacing()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.ToggleBold();
        controller.ToggleItalic();
        controller.SetTextColor(new ArgbColor(255, 0x21, 0x7A, 0x3C));

        var format = FormatOf(workbook, "A1");
        format.Bold.ShouldBeTrue();
        format.Italic.ShouldBeTrue();
        format.Foreground.ShouldBe(new ArgbColor(255, 0x21, 0x7A, 0x3C));
    }

    [Fact]
    public void Highlight_SetsAndClearsTheFill()
    {
        using var workbook = New();
        var controller = Controller(workbook);
        var yellow = new ArgbColor(255, 0xFF, 0xEB, 0x3B);

        controller.SetFillColor(yellow);
        FormatOf(workbook, "A1").Background.ShouldBe(yellow);

        controller.SetFillColor(null);
        FormatOf(workbook, "A1").Background.IsTransparent.ShouldBeTrue();
    }

    [Fact]
    public void Alignment_PressedTwice_ReturnsToGeneral()
    {
        // General is not a fourth option nobody picks: it is what keeps numbers right-aligned, and a
        // toolbar with no way back to it can only ever make a sheet worse.
        using var workbook = New();
        var controller = Controller(workbook);

        controller.SetAlignment(CellHorizontalAlignment.Center);
        FormatOf(workbook, "A1").HorizontalAlignment.ShouldBe(CellHorizontalAlignment.Center);

        controller.SetAlignment(CellHorizontalAlignment.Center);
        FormatOf(workbook, "A1").HorizontalAlignment.ShouldBe(CellHorizontalAlignment.General);
    }

    [Fact]
    public void MixedSelection_KeepsEachCellsOwnColour()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        controller.SetTextColor(new ArgbColor(255, 200, 0, 0));

        controller.Selection.MoveTo(CellRef.Parse("A2"));
        controller.SetTextColor(new ArgbColor(255, 0, 0, 200));

        controller.Selection.SelectRange(CellRange.Parse("A1:A2"));
        controller.ToggleBold();

        FormatOf(workbook, "A1").ShouldSatisfyAllConditions(
            x => x.Bold.ShouldBeTrue(),
            x => x.Foreground.ShouldBe(new ArgbColor(255, 200, 0, 0)));

        FormatOf(workbook, "A2").ShouldSatisfyAllConditions(
            x => x.Bold.ShouldBeTrue(),
            x => x.Foreground.ShouldBe(new ArgbColor(255, 0, 0, 200)));
    }

    // ---- undo ----

    [Fact]
    public void Formatting_UndoesAsOneStep()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.Selection.SelectRange(CellRange.Parse("A1:C3"));
        controller.ToggleBold();

        FormatOf(workbook, "B2").Bold.ShouldBeTrue();

        controller.Undo();

        FormatOf(workbook, "B2").Bold.ShouldBeFalse();
        FormatOf(workbook, "C3").Bold.ShouldBeFalse();
        workbook.Undo.CanUndo.ShouldBeFalse("nine cells is still one action");
    }

    [Fact]
    public void ClearFormatting_LeavesTheContentsAlone()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        Set(workbook, "A1", CellValue.FromNumber(42));
        controller.ToggleBold();
        controller.SetNumberFormat(NumberFormatPreset.Percent);

        controller.ClearFormatting();

        FormatOf(workbook, "A1").ShouldBe(ResolvedFormat.Default);
        workbook.Sheets[0].GetValue(CellRef.Parse("A1")).AsNumber().ShouldBe(42);
    }

    // ---- number formats ----

    [Fact]
    public void NumberFormat_ChangesWhatTheCellDisplays()
    {
        using var workbook = New();
        var controller = Controller(workbook);
        var sheet = workbook.Sheets[0];

        Set(workbook, "A1", CellValue.FromNumber(0.256));
        controller.SetNumberFormat(NumberFormatPreset.Percent);

        var format = FormatOf(workbook, "A1");
        workbook.Styles.Format(sheet.GetDisplayValue(CellRef.Parse("A1")), format).ShouldBe("25.60%");
    }

    [Fact]
    public void CustomNumberFormat_ResolvesWithoutReopeningTheFile()
    {
        // The resolver reads numFmts once, at open. A format added afterwards has to be handed to it,
        // or the cell silently renders as General until the workbook is closed and opened again.
        using var workbook = New();
        var controller = Controller(workbook);

        Set(workbook, "A1", CellValue.FromNumber(1234.5));
        controller.SetNumberFormatCode("#,##0.000");

        FormatOf(workbook, "A1").NumberFormatCode.ShouldBe("#,##0.000");
    }

    [Theory]
    [InlineData("", 1, "0.0")]
    [InlineData("0.00", 1, "0.000")]
    [InlineData("0.00", -1, "0.0")]
    [InlineData("0.0", -1, "0")]
    [InlineData("0", -1, "0")]
    [InlineData("#,##0.00", 1, "#,##0.000")]
    [InlineData("#,##0.00;[Red]-#,##0.00", 1, "#,##0.000;[Red]-#,##0.00")]
    public void AdjustDecimals(string code, int delta, string expected)
        => NumberFormats.AdjustDecimals(code, delta).ShouldBe(expected);

    [Fact]
    public void AdjustDecimals_IgnoresAFullStopInsideALiteral()
    {
        // 0" pt." is a valid code, and the stop in it prints. Treating it as the decimal point puts
        // the new zeros inside the quotes, where they show up as the text ".00".
        NumberFormats.AdjustDecimals("0\" pt.\"", 2).ShouldBe("0.00\" pt.\"");
    }

    // ---- columns ----

    [Fact]
    public void FormattingAColumnHeaderSelection_WritesAColumnStyle()
    {
        using var workbook = New();
        var controller = Controller(workbook);
        var sheet = workbook.Sheets[0];

        controller.Selection.SelectColumn(2);
        controller.SetNumberFormat(NumberFormatPreset.Currency);

        sheet.GetColumnStyleIndex(2).ShouldNotBeNull();
        sheet.GetStyleIndex(CellRef.Parse("C1")).ShouldBeNull("the cells are not touched; they inherit");

        // Including a row nobody has been near, which is the point of a column format.
        FormatOf(workbook, "C900").NumberFormatCode.ShouldNotBeEmpty();
    }

    [Fact]
    public void ColumnFormat_ReachesCellsThroughTheEffectiveStyle()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        Set(workbook, "B4", CellValue.FromNumber(7));
        controller.Selection.SelectColumn(1);
        controller.ToggleBold();

        FormatOf(workbook, "B4").Bold.ShouldBeTrue();
        FormatOf(workbook, "A4").Bold.ShouldBeFalse();
    }

    [Fact]
    public void ClearingOneCellInAFormattedColumn_OverridesRatherThanInherits()
    {
        // Index 0 normally means "no style attribute at all". Under a formatted column it has to stay
        // on the cell, as the override that stops the column's formatting coming back.
        using var workbook = New();
        var controller = Controller(workbook);

        Set(workbook, "B2", CellValue.FromNumber(1));
        controller.Selection.SelectColumn(1);
        controller.ToggleBold();

        controller.Selection.MoveTo(CellRef.Parse("B2"));
        controller.ClearFormatting();

        FormatOf(workbook, "B2").Bold.ShouldBeFalse();
        FormatOf(workbook, "B3").Bold.ShouldBeTrue("the rest of the column is untouched");
    }

    [Fact]
    public void ColumnFormat_Undoes()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.Selection.SelectColumn(4);
        controller.ToggleItalic();
        FormatOf(workbook, "E10").Italic.ShouldBeTrue();

        controller.Undo();
        FormatOf(workbook, "E10").Italic.ShouldBeFalse();
        workbook.Sheets[0].GetColumnStyleIndex(4).ShouldBeNull();
    }

    [Fact]
    public void ColumnWidth_SurvivesASave()
    {
        using var workbook = New();
        var sheet = workbook.Sheets[0];

        workbook.Execute(new SetColumnWidthCommand("Sheet1", 1, 3, 24.5));

        sheet.GetColumnWidth(1).ShouldBe(24.5);
        sheet.GetColumnWidth(3).ShouldBe(24.5);
        sheet.GetColumnWidth(4).ShouldBeNull();

        var xml = SheetXml(workbook, "xl/worksheets/sheet1.xml");
        xml.ShouldContain("min=\"2\"");
        xml.ShouldContain("max=\"4\"", customMessage: "one element, not three: identical neighbours merge");
    }

    [Fact]
    public void ColumnSpans_SplitAroundAColumnInTheMiddle()
    {
        using var workbook = New();
        var sheet = workbook.Sheets[0];

        workbook.Execute(new SetColumnWidthCommand("Sheet1", 0, 9, 12));
        workbook.Execute(new SetColumnWidthCommand("Sheet1", 4, 4, 30));

        sheet.GetColumnWidth(3).ShouldBe(12);
        sheet.GetColumnWidth(4).ShouldBe(30);
        sheet.GetColumnWidth(5).ShouldBe(12);
    }

    [Fact]
    public void ColumnWidth_Undoes()
    {
        using var workbook = New();
        var sheet = workbook.Sheets[0];

        workbook.Execute(new SetColumnWidthCommand("Sheet1", 0, 4, 12));
        workbook.Execute(new SetColumnWidthCommand("Sheet1", 2, 2, 40));

        workbook.Undo.Undo();

        sheet.GetColumnWidth(2).ShouldBe(12);
        sheet.GetColumnWidth(1).ShouldBe(12);
    }

    [Fact]
    public void AutoFit_SizesToTheWidestValue()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        Set(workbook, "A1", CellValue.FromText("Hi"));
        Set(workbook, "A2", CellValue.FromText("A considerably longer label"));

        controller.Selection.SelectColumn(0);
        controller.AutoFitColumns();

        workbook.Sheets[0].GetColumnWidth(0).ShouldBe(28, "27 characters of label plus one of padding");
    }

    [Fact]
    public void FormattingARowHeaderSelection_WritesARowStyle()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.Selection.SelectRow(3);
        controller.ToggleBold();

        workbook.Sheets[0].GetRowStyleIndex(3).ShouldNotBeNull();
        FormatOf(workbook, "ZZ4").Bold.ShouldBeTrue();
        FormatOf(workbook, "A3").Bold.ShouldBeFalse();
    }

    // ---- the file ----

    [Fact]
    public void HighlightingACell_WritesASolidFill()
    {
        using var workbook = New();
        var controller = Controller(workbook);

        controller.SetFillColor(new ArgbColor(255, 0xFF, 0xEB, 0x3B));

        var xml = SheetXml(workbook, "xl/styles.xml");
        xml.ShouldContain("patternType=\"solid\"");
        xml.ShouldContain("FFFFEB3B");
    }

    [Fact]
    public void AWorkbookNobodyFormatted_IsStillNotRewritten()
    {
        // The style writer creates parts on demand. Merely constructing it must not count as an edit,
        // or opening a file and closing it would change the bytes on disk.
        using var workbook = New();
        _ = Controller(workbook).ActiveFormat;

        workbook.IsDirty.ShouldBeFalse();
    }

    static string SheetXml(Workbook workbook, string entry)
        => System.Text.Encoding.UTF8.GetString(PackageComparer.ReadEntry(workbook.ToArray(), entry));

    static int CountOf(string haystack, string needle)
    {
        var count = 0;
        for (var at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = haystack.IndexOf(needle, at + 1, StringComparison.Ordinal))
            count++;

        return count;
    }
}
