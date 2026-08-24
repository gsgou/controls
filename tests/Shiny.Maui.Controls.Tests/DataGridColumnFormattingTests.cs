using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Column formatting exists so the ordinary cases - money, percentages, dates, a null placeholder,
/// a red negative - stop requiring a full cell template. The load-bearing test here is
/// <see cref="TheOldBindingDialectStillFormats"/>: the cell used to go through a binding's
/// <c>StringFormat</c> ("{0:C0}") while search and aggregates went through
/// <c>IFormattable.ToString</c> ("C0"), so quick-search over a formatted column matched nothing.
/// Both dialects now land in the same place.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class DataGridColumnFormattingTests
{
    enum Status
    {
        InProgress,

        [Description("Signed off")]
        Approved
    }

    class Row
    {
        public string? Name { get; set; }
        public decimal Salary { get; set; }
        public double Rate { get; set; }
        public long Size { get; set; }
        public bool Active { get; set; }
        public DateTime Started { get; set; }
        public Status State { get; set; }
        public int? Score { get; set; }
    }

    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    static readonly CultureInfo UsEnglish = new("en-US");

    static DataGridColumn Column(string property, Action<DataGridColumn>? configure = null)
    {
        var col = new DataGridColumn { Title = property, PropertyName = property, Culture = Invariant };
        configure?.Invoke(col);
        return col;
    }

    static DataGrid.DataGrid GridWith(DataGridColumn column, params Row[] rows)
    {
        var grid = new DataGrid.DataGrid();
        grid.Columns.Add(column);
        grid.ItemsSource = rows;
        return grid;
    }

    [Fact]
    public void CurrencyPresetFormatsWithoutAFormatString()
    {
        var col = Column(nameof(Row.Salary), c => { c.DisplayAs = DataGridColumnFormat.Currency; c.Culture = UsEnglish; });
        col.GetText(new Row { Salary = 45000m }).ShouldBe("$45,000.00");
    }

    [Fact]
    public void DecimalsNarrowsThePreset()
    {
        var col = Column(nameof(Row.Salary), c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.Decimals = 0;
            c.Culture = UsEnglish;
        });
        col.GetText(new Row { Salary = 45000m }).ShouldBe("$45,000");
    }

    [Fact]
    public void ExplicitStringFormatBeatsThePreset()
    {
        var col = Column(nameof(Row.Salary), c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.StringFormat = "N2";
        });
        col.GetText(new Row { Salary = 1234.5m }).ShouldBe("1,234.50");
    }

    /// <summary>
    /// Existing XAML says <c>StringFormat="{}{0:C0}"</c> because that is what a MAUI binding needs.
    /// That form has to keep working, and has to produce the same text as the bare "C0" form.
    /// </summary>
    [Fact]
    public void TheOldBindingDialectStillFormats()
    {
        var braces = Column(nameof(Row.Salary), c => { c.StringFormat = "{0:C0}"; c.Culture = UsEnglish; });
        var bare = Column(nameof(Row.Salary), c => { c.StringFormat = "C0"; c.Culture = UsEnglish; });

        var row = new Row { Salary = 45000m };
        braces.GetText(row).ShouldBe("$45,000");
        bare.GetText(row).ShouldBe(braces.GetText(row));
    }

    [Fact]
    public void PercentPresetMultipliesByOneHundredLikeDotnetDoes()
    {
        var col = Column(nameof(Row.Rate), c => { c.DisplayAs = DataGridColumnFormat.Percent; c.Decimals = 0; });
        col.GetText(new Row { Rate = 0.15 }).ShouldBe("15 %");
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5.0 MB")]
    public void FileSizePresetScalesToAUnit(long bytes, string expected)
    {
        var col = Column(nameof(Row.Size), c => c.DisplayAs = DataGridColumnFormat.FileSize);
        col.GetText(new Row { Size = bytes }).ShouldBe(expected);
    }

    [Fact]
    public void BooleanPresetUsesGlyphsByDefaultAndCustomTextWhenGiven()
    {
        var glyphs = Column(nameof(Row.Active), c => c.DisplayAs = DataGridColumnFormat.Boolean);
        glyphs.GetText(new Row { Active = true }).ShouldBe("✓");
        glyphs.GetText(new Row { Active = false }).ShouldBe("✗");

        var words = Column(nameof(Row.Active), c =>
        {
            c.DisplayAs = DataGridColumnFormat.Boolean;
            c.TrueText = "Active";
            c.FalseText = "Inactive";
        });
        words.GetText(new Row { Active = false }).ShouldBe("Inactive");
    }

    [Fact]
    public void EnumPresetPrefersDescriptionThenFallsBackToAHumanizedName()
    {
        var col = Column(nameof(Row.State), c => c.DisplayAs = DataGridColumnFormat.Enum);
        col.GetText(new Row { State = Status.Approved }).ShouldBe("Signed off");
        col.GetText(new Row { State = Status.InProgress }).ShouldBe("In Progress");
    }

    [Fact]
    public void NullShowsThePlaceholderAndNeverWearsThePrefix()
    {
        var col = Column(nameof(Row.Score), c =>
        {
            c.NullText = "—";
            c.Prefix = "#";
        });
        col.GetText(new Row { Score = null }).ShouldBe("—");
        col.GetText(new Row { Score = 7 }).ShouldBe("#7");
    }

    [Fact]
    public void EmptyStringCountsAsMissing()
    {
        var col = Column(nameof(Row.Name), c => c.NullText = "(none)");
        col.GetText(new Row { Name = "" }).ShouldBe("(none)");
        col.GetText(new Row { Name = null }).ShouldBe("(none)");
    }

    [Fact]
    public void SuffixFollowsTheFormattedValue()
    {
        var col = Column(nameof(Row.Rate), c => { c.StringFormat = "N1"; c.Suffix = " kg"; });
        col.GetText(new Row { Rate = 2.26 }).ShouldBe("2.3 kg");
    }

    [Fact]
    public void TextFormatterReplacesTheFormatButKeepsPrefixAndPlaceholder()
    {
        var col = Column(nameof(Row.Score), c =>
        {
            c.NullText = "n/a";
            c.Suffix = " pts";
            c.TextFormatter = v => (int)v! >= 90 ? "A" : "B";
        });
        col.GetText(new Row { Score = 95 }).ShouldBe("A pts");
        col.GetText(new Row { Score = null }).ShouldBe("n/a");
    }

    [Fact]
    public void GroupHeaderTextMatchesTheCellsUnderIt()
    {
        var col = Column(nameof(Row.Salary), c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.Decimals = 0;
            c.Culture = UsEnglish;
        });
        col.FormatValue(45000m).ShouldBe(col.GetText(new Row { Salary = 45000m }));
    }

    [Fact]
    public void AutoAlignmentPutsQuantitiesRightAndTextLeft()
    {
        Align(Column(nameof(Row.Salary)), new Row { Salary = 1 }).ShouldBe(TextAlignment.End);
        Align(Column(nameof(Row.Score)), new Row { Score = 1 }).ShouldBe(TextAlignment.End);
        Align(Column(nameof(Row.Name)), new Row { Name = "x" }).ShouldBe(TextAlignment.Start);
        Align(Column(nameof(Row.Started)), new Row()).ShouldBe(TextAlignment.Start);

        var flag = Column(nameof(Row.Active), c => c.DisplayAs = DataGridColumnFormat.Boolean);
        Align(flag, new Row()).ShouldBe(TextAlignment.Start);
    }

    /// <summary>An account "number" is an identifier, not a quantity - the preset is what settles it.</summary>
    [Fact]
    public void ANumericColumnShownAsTextIsNotRightAligned()
    {
        var col = Column(nameof(Row.Score), c => c.DisplayAs = DataGridColumnFormat.Text);
        Align(col, new Row { Score = 1 }).ShouldBe(TextAlignment.Start);
    }

    [Fact]
    public void ExplicitAlignmentWins()
    {
        var col = Column(nameof(Row.Salary), c => c.Alignment = DataGridCellAlignment.Center);
        Align(col, new Row { Salary = 1 }).ShouldBe(TextAlignment.Center);
    }

    [Fact]
    public void TemplateColumnsAlignLeftUnlessToldOtherwise()
    {
        var grid = new DataGrid.DataGrid();
        var col = new DataGridTemplateColumn { Title = "actions" };
        grid.Columns.Add(col);

        grid.ResolveAlignment(col, col.Alignment).ShouldBe(TextAlignment.Start);
        grid.ResolveAlignment(col, DataGridCellAlignment.End).ShouldBe(TextAlignment.End);
    }

    static TextAlignment Align(DataGridColumn col, Row row)
    {
        var grid = GridWith(col, row);
        return grid.ResolveAlignment(col, col.Alignment);
    }
}
