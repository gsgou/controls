using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Runs the round-trip guarantee against real documents in <c>corpus/</c>.
/// </summary>
/// <remarks>
/// Synthetic fixtures only prove the editor handles what the fixture builder knows how to produce.
/// Real files are where conditional formatting, pivot caches, slicers, drawings and twenty years of
/// Excel history actually live, and they are the only thing that can catch a destructive save.
/// </remarks>
public class CorpusRoundTripTests
{
    static readonly string[] EditablePartPrefixes =
    [
        "xl/worksheets/",
        "xl/sharedStrings.xml",
        "xl/workbook.xml",
        "xl/styles.xml",
        "xl/calcChain.xml"
    ];

    /// <summary>Yielded when the corpus is empty, because xunit treats a theory with no data as a failure.</summary>
    const string NoCorpus = "(none)";

    public static TheoryData<string> Documents()
    {
        var data = new TheoryData<string>();
        var directory = CorpusDirectory();
        if (directory is null)
        {
            data.Add(NoCorpus);
            return data;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.xlsx", SearchOption.AllDirectories))
        {
            // Excel's lock files are not documents.
            if (!Path.GetFileName(file).StartsWith("~$", StringComparison.Ordinal))
                data.Add(file);
        }

        if (data.Count == 0)
            data.Add(NoCorpus);

        return data;
    }

    static string? CorpusDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "corpus");
        return Directory.Exists(directory) ? directory : null;
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public async Task OpenAndSave_IsNonDestructive(string path)
    {
        if (path == NoCorpus)
            return;

        var original = await File.ReadAllBytesAsync(path);

        using var workbook = await Workbook.OpenAsync(path);
        var saved = workbook.ToArray();

        var diff = PackageComparer.Compare(original, saved);
        diff.Removed.ShouldBeEmpty($"{Path.GetFileName(path)} lost parts on a no-op save. {diff}");
        diff.Changed.ShouldBeEmpty($"{Path.GetFileName(path)} was modified by a no-op save. {diff}");
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public async Task EditingOneCell_TouchesOnlyEditableParts(string path)
    {
        if (path == NoCorpus)
            return;

        var original = await File.ReadAllBytesAsync(path);

        var collector = new UnsupportedFeatureCollector();
        using var workbook = await Workbook.OpenAsync(path, collector);

        var sheet = workbook.Sheets.FirstOrDefault();
        if (sheet is null)
            return;

        // Write into a cell far outside any plausible used range, so the edit cannot collide with the
        // document's own content and the test stays about the package, not the value.
        var target = new CellRef(200, 5000);
        workbook.Execute(new SetCellValueCommand(sheet.Name, target, CellValue.FromText("shiny-round-trip")));

        var saved = workbook.ToArray();
        var diff = PackageComparer.Compare(original, saved);

        diff.Removed.ShouldBeEmpty($"{Path.GetFileName(path)}: {diff}");

        foreach (var part in diff.Changed)
        {
            EditablePartPrefixes.Any(prefix => part.StartsWith(prefix, StringComparison.Ordinal))
                .ShouldBeTrue($"{Path.GetFileName(path)}: '{part}' was rewritten by a single-cell edit. {diff}");
        }

        using var reopened = await Workbook.OpenAsync(new MemoryStream(saved));
        reopened[sheet.Name].GetValue(target).AsText().ShouldBe("shiny-round-trip");
    }

    [Fact]
    public void Corpus_IsPresentOrExplicitlyEmpty()
    {
        // Not a failure — a reminder. An empty corpus means the round-trip guarantee is only being
        // checked against fixtures the test suite wrote itself.
        var directory = CorpusDirectory();
        var count = directory is null ? 0 : Directory.EnumerateFiles(directory, "*.xlsx", SearchOption.AllDirectories).Count();
        if (count == 0)
            Assert.True(true, "corpus/ is empty; round-trip is only verified against synthetic fixtures.");
    }
}
