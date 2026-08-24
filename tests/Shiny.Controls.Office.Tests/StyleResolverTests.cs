using Shiny.Controls.Office.Spreadsheet;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class StyleResolverTests
{
    static async Task<Workbook> OpenAsync()
    {
        using var source = new MemoryStream(WorkbookFixture.Build(), writable: false);
        return await Workbook.OpenAsync(source);
    }

    [Fact]
    public async Task ResolvesTheFullStyleChain()
    {
        using var workbook = await OpenAsync();
        var sheet = workbook["Data"];

        var format = workbook.Styles.Resolve(sheet.GetStyleIndex(CellRef.Parse("A2")));

        format.NumberFormatCode.ShouldBe("#,##0.00");
        format.Bold.ShouldBeTrue();
        format.Foreground.ShouldBe(new ArgbColor(255, 0xCC, 0, 0));
        format.Background.ShouldBe(new ArgbColor(255, 0xFF, 0xF2, 0xCC));
        format.HorizontalAlignment.ShouldBe(CellHorizontalAlignment.Center);
        format.WrapText.ShouldBeTrue();
    }

    [Fact]
    public async Task UnstyledCells_ResolveToTheDefault()
    {
        using var workbook = await OpenAsync();
        workbook.Styles.Resolve(null).ShouldBeSameAs(ResolvedFormat.Default);
    }

    [Fact]
    public async Task AppliesTheCustomNumberFormat()
    {
        using var workbook = await OpenAsync();
        var format = workbook.Styles.Resolve(1u);

        workbook.Styles.Format(CellValue.FromNumber(1234.5), format).ShouldBe("1,234.50");
    }

    [Fact]
    public async Task BuiltInNumberFormats_AreKnownWithoutBeingInTheFile()
    {
        // Formats 0-49 are defined by the spec and never written into styles.xml, so a resolver that
        // only reads numFmts silently renders percentages and dates as raw numbers.
        using var workbook = await OpenAsync();

        var percent = ResolvedFormat.Default with { NumberFormatCode = "0%" };
        workbook.Styles.Format(CellValue.FromNumber(0.75), percent).ShouldBe("75%");
    }

    [Theory]
    [InlineData(CellValueKind.Number, CellHorizontalAlignment.Right)]
    [InlineData(CellValueKind.Text, CellHorizontalAlignment.Left)]
    [InlineData(CellValueKind.Boolean, CellHorizontalAlignment.Center)]
    [InlineData(CellValueKind.Error, CellHorizontalAlignment.Center)]
    public void GeneralAlignment_DependsOnTheValueType(CellValueKind kind, CellHorizontalAlignment expected)
        => ResolvedFormat.Default.EffectiveAlignment(kind).ShouldBe(expected);

    [Fact]
    public void ExplicitAlignment_OverridesTheTypeDefault()
    {
        var format = ResolvedFormat.Default with { HorizontalAlignment = CellHorizontalAlignment.Left };
        format.EffectiveAlignment(CellValueKind.Number).ShouldBe(CellHorizontalAlignment.Left);
    }

    [Fact]
    public async Task ErrorsAndBooleans_FormatAsTheirExcelText()
    {
        using var workbook = await OpenAsync();
        var format = ResolvedFormat.Default;

        workbook.Styles.Format(CellValue.FromError(CellError.Div0), format).ShouldBe("#DIV/0!");
        workbook.Styles.Format(CellValue.FromBoolean(true), format).ShouldBe("TRUE");
        workbook.Styles.Format(CellValue.Blank, format).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GeneralFormat_SwitchesToScientificForExtremeValues()
    {
        using var workbook = await OpenAsync();
        var general = ResolvedFormat.Default;

        workbook.Styles.Format(CellValue.FromNumber(0), general).ShouldBe("0");
        workbook.Styles.Format(CellValue.FromNumber(1.5), general).ShouldBe("1.5");
        workbook.Styles.Format(CellValue.FromNumber(1e12), general).ShouldContain("E+");
    }

    [Theory]
    [InlineData(0.0, 100, 100, 100)]
    [InlineData(1.0, 255, 255, 255)]
    [InlineData(-1.0, 0, 0, 0)]
    public void Tint_LightensTowardWhiteAndDarkensTowardBlack(double tint, int r, int g, int b)
    {
        // "Accent1, Lighter 40%" is a tint in the file, not a separate colour. Ignoring it renders every
        // themed fill at full saturation.
        var method = typeof(StyleResolver).GetMethod("ApplyTint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        var result = (ArgbColor)method!.Invoke(null, [new ArgbColor(255, 100, 100, 100), tint])!;
        result.R.ShouldBe((byte)r);
        result.G.ShouldBe((byte)g);
        result.B.ShouldBe((byte)b);
    }

    [Fact]
    public void ArgbColor_RoundTripsThroughUInt32()
    {
        var color = new ArgbColor(0x80, 0x12, 0x34, 0x56);
        ArgbColor.FromUInt32(color.ToUInt32()).ShouldBe(color);
        color.ToString().ShouldBe("#80123456");
    }
}
