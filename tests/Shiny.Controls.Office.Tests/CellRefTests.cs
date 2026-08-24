using Shiny.Controls.Office.Spreadsheet;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class CellRefTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    [InlineData(16383, "XFD")]
    public void ColumnName_RoundTrips(int index, string name)
    {
        // The Z -> AA boundary is where naive base-26 goes wrong, so it is worth pinning explicitly.
        CellRef.ColumnName(index).ShouldBe(name);
        CellRef.ParseColumnName(name).ShouldBe(index);
    }

    [Theory]
    [InlineData("A1", 0, 0, false, false)]
    [InlineData("B2", 1, 1, false, false)]
    [InlineData("XFD1048576", 16383, 1048575, false, false)]
    [InlineData("$A$1", 0, 0, true, true)]
    [InlineData("$C7", 2, 6, true, false)]
    [InlineData("C$7", 2, 6, false, true)]
    [InlineData("aa10", 26, 9, false, false)]
    public void Parse_ReadsAddressesAndAbsoluteMarkers(string text, int column, int row, bool columnAbsolute, bool rowAbsolute)
    {
        var reference = CellRef.Parse(text);
        reference.Column.ShouldBe(column);
        reference.Row.ShouldBe(row);
        reference.ColumnAbsolute.ShouldBe(columnAbsolute);
        reference.RowAbsolute.ShouldBe(rowAbsolute);
    }

    [Theory]
    [InlineData("$A$1")]
    [InlineData("$C7")]
    [InlineData("C$7")]
    [InlineData("XFD1048576")]
    public void ToString_PreservesAbsoluteMarkers(string text)
    {
        // A formula written $A$1 has to be written back as $A$1; normalising it away silently rewrites
        // the user's formulas.
        CellRef.Parse(text).ToString().ShouldBe(text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("A0")]
    [InlineData("$")]
    [InlineData("$A")]
    [InlineData("A1B")]
    [InlineData("XFE1")]
    [InlineData("A1048577")]
    [InlineData("AAAA1")]
    public void TryParse_RejectsInvalidAddresses(string text)
        => CellRef.TryParse(text, out _).ShouldBeFalse();

    [Fact]
    public void Relative_StripsAbsoluteMarkersForUseAsAKey()
    {
        var absolute = CellRef.Parse("$B$3");
        var relative = CellRef.Parse("B3");

        absolute.ShouldNotBe(relative);
        absolute.Relative().ShouldBe(relative);
    }
}

public class CellRangeTests
{
    [Fact]
    public void Constructor_NormalisesCorners()
    {
        var ascending = CellRange.Parse("A1:C3");
        var descending = new CellRange(CellRef.Parse("C3"), CellRef.Parse("A1"));

        descending.ShouldBe(ascending);
        ascending.ToString().ShouldBe("A1:C3");
    }

    [Fact]
    public void SingleCell_FormatsWithoutAColon()
    {
        var range = CellRange.Parse("D4");
        range.IsSingleCell.ShouldBeTrue();
        range.ToString().ShouldBe("D4");
    }

    [Fact]
    public void Cells_EnumeratesRowByRow()
    {
        var cells = CellRange.Parse("A1:B2").Cells().Select(x => x.ToString()).ToArray();
        cells.ShouldBe(["A1", "B1", "A2", "B2"]);
    }

    [Fact]
    public void CountsAndContainment()
    {
        var range = CellRange.Parse("B2:D5");
        range.ColumnCount.ShouldBe(3);
        range.RowCount.ShouldBe(4);
        range.CellCount.ShouldBe(12);

        range.Contains(CellRef.Parse("C3")).ShouldBeTrue();
        range.Contains(CellRef.Parse("A1")).ShouldBeFalse();
        range.Intersects(CellRange.Parse("D5:F9")).ShouldBeTrue();
        range.Intersects(CellRange.Parse("E1:F9")).ShouldBeFalse();
    }
}
