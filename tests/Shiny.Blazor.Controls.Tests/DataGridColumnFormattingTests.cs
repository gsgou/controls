using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Column formatting exists so the ordinary cases - money, percentages, dates, a null placeholder,
/// a red negative - stop requiring a full cell template. These tests pin the two things that make
/// that trustworthy: the preset/format/prefix pipeline produces what it claims, and the cell, the
/// search index and the group header all agree because they share one code path.
/// </summary>
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

    static PropertyColumn<Row, TProp> Column<TProp>(
        Expression<Func<Row, TProp>> property,
        Action<PropertyColumn<Row, TProp>>? configure = null)
    {
        var col = new PropertyColumn<Row, TProp> { Property = property, Culture = Invariant };
        configure?.Invoke(col);

        // OnParametersSet is what compiles the expression into a getter; the grid does this for real
        // columns, so a hand-built one has to be told the same way.
        typeof(PropertyColumn<Row, TProp>)
            .GetMethod("OnParametersSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(col, null);
        return col;
    }

    [Fact]
    public void Currency_preset_formats_without_a_format_string()
    {
        var col = Column<decimal>(x => x.Salary, c => { c.DisplayAs = DataGridColumnFormat.Currency; c.Culture = UsEnglish; });
        col.GetText(new Row { Salary = 45000m }).ShouldBe("$45,000.00");
    }

    [Fact]
    public void Decimals_narrows_the_preset()
    {
        var col = Column<decimal>(x => x.Salary, c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.Decimals = 0;
            c.Culture = UsEnglish;
        });
        col.GetText(new Row { Salary = 45000m }).ShouldBe("$45,000");
    }

    [Fact]
    public void Explicit_string_format_beats_the_preset()
    {
        var col = Column<decimal>(x => x.Salary, c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.StringFormat = "N2";
        });
        col.GetText(new Row { Salary = 1234.5m }).ShouldBe("1,234.50");
    }

    [Fact]
    public void Obsolete_format_alias_still_works_when_string_format_is_unset()
    {
#pragma warning disable CS0618
        var col = Column<decimal>(x => x.Salary, c => c.Format = "N1");
#pragma warning restore CS0618
        col.GetText(new Row { Salary = 1234.5m }).ShouldBe("1,234.5");
    }

    [Fact]
    public void Percent_preset_multiplies_by_one_hundred_like_dotnet_does()
    {
        var col = Column<double>(x => x.Rate, c => { c.DisplayAs = DataGridColumnFormat.Percent; c.Decimals = 0; });
        col.GetText(new Row { Rate = 0.15 }).ShouldBe("15 %");
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5.0 MB")]
    public void FileSize_preset_scales_to_a_unit(long bytes, string expected)
    {
        var col = Column<long>(x => x.Size, c => c.DisplayAs = DataGridColumnFormat.FileSize);
        col.GetText(new Row { Size = bytes }).ShouldBe(expected);
    }

    [Fact]
    public void Boolean_preset_uses_glyphs_by_default_and_custom_text_when_given()
    {
        var glyphs = Column<bool>(x => x.Active, c => c.DisplayAs = DataGridColumnFormat.Boolean);
        glyphs.GetText(new Row { Active = true }).ShouldBe("✓");
        glyphs.GetText(new Row { Active = false }).ShouldBe("✗");

        var words = Column<bool>(x => x.Active, c =>
        {
            c.DisplayAs = DataGridColumnFormat.Boolean;
            c.TrueText = "Active";
            c.FalseText = "Inactive";
        });
        words.GetText(new Row { Active = false }).ShouldBe("Inactive");
    }

    [Fact]
    public void Enum_preset_prefers_description_then_falls_back_to_a_humanized_name()
    {
        var col = Column<Status>(x => x.State, c => c.DisplayAs = DataGridColumnFormat.Enum);
        col.GetText(new Row { State = Status.Approved }).ShouldBe("Signed off");
        col.GetText(new Row { State = Status.InProgress }).ShouldBe("In Progress");
    }

    [Fact]
    public void Null_shows_the_placeholder_and_never_wears_the_prefix()
    {
        var col = Column<int?>(x => x.Score, c =>
        {
            c.NullText = "—";
            c.Prefix = "#";
        });
        col.GetText(new Row { Score = null }).ShouldBe("—");
        col.GetText(new Row { Score = 7 }).ShouldBe("#7");
    }

    [Fact]
    public void Empty_string_counts_as_missing()
    {
        var col = Column<string?>(x => x.Name, c => c.NullText = "(none)");
        col.GetText(new Row { Name = "" }).ShouldBe("(none)");
        col.GetText(new Row { Name = null }).ShouldBe("(none)");
    }

    [Fact]
    public void Suffix_follows_the_formatted_value()
    {
        var col = Column<double>(x => x.Rate, c => { c.StringFormat = "N1"; c.Suffix = " kg"; });
        col.GetText(new Row { Rate = 2.26 }).ShouldBe("2.3 kg");
    }

    [Fact]
    public void TextFormatter_replaces_the_format_but_keeps_prefix_and_placeholder()
    {
        var col = Column<int?>(x => x.Score, c =>
        {
            c.NullText = "n/a";
            c.Suffix = " pts";
            c.TextFormatter = v => v >= 90 ? "A" : "B";
        });
        col.GetText(new Row { Score = 95 }).ShouldBe("A pts");
        col.GetText(new Row { Score = null }).ShouldBe("n/a");
    }

    [Fact]
    public void Group_header_text_matches_the_cells_under_it()
    {
        var col = Column<decimal>(x => x.Salary, c =>
        {
            c.DisplayAs = DataGridColumnFormat.Currency;
            c.Decimals = 0;
            c.Culture = UsEnglish;
        });

        // The group header formats the bare key; the cell formats the item. They have to agree, which
        // they only do because both go through FormatValue.
        col.FormatValue(45000m).ShouldBe(col.GetText(new Row { Salary = 45000m }));
    }

    [Fact]
    public void Auto_alignment_puts_quantities_right_and_text_left()
    {
        Column<decimal>(x => x.Salary).EffectiveAlignment.ShouldBe(DataGridCellAlignment.End);
        Column<int?>(x => x.Score).EffectiveAlignment.ShouldBe(DataGridCellAlignment.End);
        Column<string?>(x => x.Name).EffectiveAlignment.ShouldBe(DataGridCellAlignment.Start);
        Column<DateTime>(x => x.Started).EffectiveAlignment.ShouldBe(DataGridCellAlignment.Start);
        Column<bool>(x => x.Active, c => c.DisplayAs = DataGridColumnFormat.Boolean)
            .EffectiveAlignment.ShouldBe(DataGridCellAlignment.Start);
    }

    [Fact]
    public void A_numeric_column_shown_as_text_is_not_right_aligned()
    {
        // An account "number" is an identifier, not a quantity - the preset is what settles it.
        Column<int?>(x => x.Score, c => c.DisplayAs = DataGridColumnFormat.Text)
            .EffectiveAlignment.ShouldBe(DataGridCellAlignment.Start);
    }

    [Fact]
    public void Explicit_alignment_wins_and_the_header_follows_the_cells()
    {
        var col = Column<string?>(x => x.Name, c => c.Alignment = DataGridCellAlignment.Center);
        col.EffectiveAlignment.ShouldBe(DataGridCellAlignment.Center);
        col.EffectiveHeaderAlignment.ShouldBe(DataGridCellAlignment.Center);

        col.HeaderAlignment = DataGridCellAlignment.Start;
        col.EffectiveHeaderAlignment.ShouldBe(DataGridCellAlignment.Start);
    }

    [Fact]
    public void Template_columns_align_left_unless_told_otherwise()
    {
        new TemplateColumn<Row> { Title = "actions" }.EffectiveAlignment.ShouldBe(DataGridCellAlignment.Start);
        new TemplateColumn<Row> { Title = "actions", Alignment = DataGridCellAlignment.End }
            .EffectiveAlignment.ShouldBe(DataGridCellAlignment.End);
    }

    [Fact]
    public void Cell_style_reaches_the_class_and_style_attributes()
    {
        var grid = new DataGrid<Row>();
        var col = Column<decimal>(x => x.Salary, c => c.CellStyle = r => r.Salary < 0
            ? new DataGridCellStyle { TextColor = "#c62828", Bold = true, CssClass = "negative" }
            : null);

        var negative = grid.ResolveCellStyle(col, new Row { Salary = -1 }, true, true, true, true);
        grid.CellCssClass(col, negative).ShouldContain("negative");
        grid.CellInlineStyle(col, negative)!.ShouldContain("color:#c62828");
        grid.CellInlineStyle(col, negative)!.ShouldContain("font-weight:600");

        var positive = grid.ResolveCellStyle(col, new Row { Salary = 1 }, true, true, true, true);
        grid.CellCssClass(col, positive).ShouldNotContain("negative");
        grid.CellInlineStyle(col, positive).ShouldBeNullOrEmpty();
    }

    [Fact]
    public void Alignment_and_wrapping_reach_the_cell_classes()
    {
        var grid = new DataGrid<Row>();

        var money = Column<decimal>(x => x.Salary, c => c.DisplayAs = DataGridColumnFormat.Currency);
        grid.CellCssClass(money, null).ShouldContain("shiny-dg-align-end");

        var notes = Column<string?>(x => x.Name, c => { c.Wrap = true; c.MaxLines = 2; });
        grid.CellCssClass(notes, null).ShouldContain("shiny-dg-wrap");
        grid.CellInlineStyle(notes, null)!.ShouldContain("-webkit-line-clamp:2");

        // Clamping without wrapping would cap a line the cell already had - it is not emitted.
        var oneLine = Column<string?>(x => x.Name, c => c.MaxLines = 2);
        (grid.CellInlineStyle(oneLine, null) ?? "").ShouldNotContain("line-clamp");
    }
}
