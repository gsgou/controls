using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class RoundTripTests
{
    /// <summary>
    /// Parts the editor legitimately rewrites when a cell changes. Anything outside this set changing
    /// is a bug — it means an edit reached a part it had no business touching.
    /// </summary>
    static readonly string[] EditablePartPrefixes =
    [
        "xl/worksheets/",
        "xl/sharedStrings.xml",
        "xl/workbook.xml",
        "xl/styles.xml"
    ];

    /// <summary>
    /// Reads calcPr/@fullCalcOnLoad, or null when the attribute is absent. Read as XML rather than by
    /// string match because OpenXml is free to serialise the boolean as either "1" or "true".
    /// </summary>
    static bool? FullCalcOnLoad(byte[] package)
    {
        var xml = System.Xml.Linq.XDocument.Parse(
            System.Text.Encoding.UTF8.GetString(PackageComparer.ReadEntry(package, "xl/workbook.xml")));

        var attribute = xml.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "calcPr")?
            .Attribute("fullCalcOnLoad");

        return attribute is null ? null : attribute.Value is "1" or "true";
    }

    static async Task<Workbook> OpenFixtureAsync(byte[] bytes, IUnsupportedFeatureSink? sink = null)
    {
        using var source = new MemoryStream(bytes, writable: false);
        return await Workbook.OpenAsync(source, sink);
    }

    [Fact]
    public async Task OpenAndSaveWithNoEdits_LeavesEveryPartIdentical()
    {
        var original = WorkbookFixture.Build();

        using var workbook = await OpenFixtureAsync(original);
        var saved = workbook.ToArray();

        var diff = PackageComparer.Compare(original, saved);
        diff.Added.ShouldBeEmpty();
        diff.Removed.ShouldBeEmpty();

        // An untouched document must survive a save completely unchanged. If this fails, opening a file
        // is itself destructive and nothing further can be trusted.
        diff.Changed.ShouldBeEmpty(diff.ToString());
    }

    [Fact]
    public async Task EditingOneCell_LeavesUnrelatedPartsByteIdentical()
    {
        var original = WorkbookFixture.Build();

        using var workbook = await OpenFixtureAsync(original);
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(99)));
        var saved = workbook.ToArray();

        var diff = PackageComparer.Compare(original, saved);
        diff.Removed.ShouldBeEmpty();

        foreach (var part in diff.Changed)
        {
            EditablePartPrefixes.Any(prefix => part.StartsWith(prefix, StringComparison.Ordinal))
                .ShouldBeTrue($"'{part}' was rewritten by a single-cell edit, but it is not a part the editor should touch. {diff}");
        }
    }

    [Fact]
    public async Task EditingOneCell_PreservesPartsTheEditorDoesNotModel()
    {
        var original = WorkbookFixture.Build();
        var foreignParts = PackageComparer.EntryNames(original)
            .Where(x => x.StartsWith("customXml/", StringComparison.Ordinal) || x.Contains("custom.xml", StringComparison.Ordinal))
            .ToList();

        foreignParts.ShouldNotBeEmpty("the fixture must contain parts the editor has no model for, or this test proves nothing");

        using var workbook = await OpenFixtureAsync(original);
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("A1"), CellValue.FromText("Replaced")));
        var saved = workbook.ToArray();

        foreach (var part in foreignParts)
        {
            PackageComparer.ReadEntry(saved, part)
                .ShouldBe(PackageComparer.ReadEntry(original, part), $"'{part}' must survive an unrelated edit untouched");
        }
    }

    [Fact]
    public async Task EditingAValue_MarksTheWorkbookForRecalculation()
    {
        // Editing an input invalidates every formula that depends on it. With no calc engine yet, the
        // only honest outcome is to have Excel recompute on open rather than leave stale cached results.
        var original = WorkbookFixture.Build();

        using var workbook = await OpenFixtureAsync(original);
        workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(7)));
        var saved = workbook.ToArray();

        FullCalcOnLoad(saved).ShouldBe(true);
    }

    [Fact]
    public async Task SavingWithoutEdits_DoesNotForceRecalculation()
    {
        var original = WorkbookFixture.Build();

        using var workbook = await OpenFixtureAsync(original);
        var saved = workbook.ToArray();

        FullCalcOnLoad(saved).ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsAsync_WritesAtomicallyAndRetargets()
    {
        var directory = Directory.CreateTempSubdirectory("shiny-office-tests");
        try
        {
            var path = Path.Combine(directory.FullName, "book.xlsx");
            using (var workbook = await OpenFixtureAsync(WorkbookFixture.Build()))
            {
                workbook.Execute(new SetCellValueCommand("Data", CellRef.Parse("B1"), CellValue.FromNumber(5)));
                workbook.IsDirty.ShouldBeTrue();

                await workbook.SaveAsAsync(path);

                workbook.Path.ShouldBe(path);
                workbook.IsDirty.ShouldBeFalse();
            }

            // No temporary files left behind, and the result reopens.
            Directory.GetFiles(directory.FullName).ShouldHaveSingleItem();

            using var reopened = await Workbook.OpenAsync(path);
            reopened["Data"].GetValue(CellRef.Parse("B1")).AsNumber().ShouldBe(5);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
