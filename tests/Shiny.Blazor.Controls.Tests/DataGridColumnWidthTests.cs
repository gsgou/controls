using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Under <c>table-layout: auto</c> a browser treats <c>width</c> on a cell as a suggestion and
/// compresses every column to fit its container. A grid asking for 1320px of columns inside an 810px
/// scroller therefore rendered at 810px, never overflowed, and its frozen columns had nothing to stay
/// put against - the pinning looked broken when the real fault was that nothing scrolled.
/// </summary>
public class DataGridColumnWidthTests
{
    class Row
    {
        public string Name { get; set; } = "";
    }

    static string? StyleFor(string? width)
    {
        var grid = new DataGrid<Row>();
        var column = new TemplateColumn<Row> { Title = "col", Width = width };
        return grid.ColumnWidthStyle(column);
    }

    [Fact]
    public void ADeclaredPixelWidthCarriesAMinWidthSoTheTableCannotCompressIt()
        => StyleFor("160px").ShouldBe("width:160px;min-width:160px;");

    [Fact]
    public void OtherAbsoluteUnitsAreHeldTheSameWay()
        => StyleFor("12rem").ShouldBe("width:12rem;min-width:12rem;");

    /// <summary>
    /// A percentage is asking to be relative to the container, which is exactly what shrinking is -
    /// pinning a min-width onto it would fight the thing it asked for.
    /// </summary>
    [Fact]
    public void APercentageWidthIsLeftFreeToShrink()
        => StyleFor("20%").ShouldBe("width:20%;");

    [Fact]
    public void AColumnWithNoWidthDeclaresNothing()
        => StyleFor(null).ShouldBeNull();
}
