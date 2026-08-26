using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Highlighting is three features wearing one coat - a row, a column and a cell all resolve through
/// the same pipeline - so the tests that matter are the ones about what happens where they meet: the
/// precedence between overlapping rules, and the edges each one strokes. A row highlight that boxed
/// every cell it touched would be the obvious wrong answer, and
/// <see cref="EdgesTraceTheRegionNotTheCell"/> is what pins that down.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class DataGridHighlightTests
{
    class Row
    {
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
        public bool Overdue { get; set; }
    }

    static readonly Row Ada = new() { Name = "Ada", Salary = 142000 };
    static readonly Row Alan = new() { Name = "Alan", Salary = 98000, Overdue = true };
    static readonly Row Grace = new() { Name = "Grace", Salary = 120000 };

    static DataGrid.DataGrid BuildGrid(Action<DataGrid.DataGrid>? configure = null)
    {
        new Application();

        var grid = new DataGrid.DataGrid();
        grid.Columns.Add(new DataGridColumn { Title = "Name", PropertyName = nameof(Row.Name) });
        grid.Columns.Add(new DataGridColumn { Title = "Salary", PropertyName = nameof(Row.Salary) });
        grid.Columns.Add(new DataGridColumn { Title = "Overdue", PropertyName = nameof(Row.Overdue) });
        configure?.Invoke(grid);
        grid.ItemsSource = new List<Row> { Ada, Alan, Grace };
        return grid;
    }

    static DataGridColumn Column(DataGrid.DataGrid grid, string title)
        => grid.Columns.First(c => c.Title == title);

    static IReadOnlyList<DataGridRow> Rows(DataGrid.DataGrid grid)
        => grid.DisplayItems.OfType<DataGridRow>().ToList();

    // ---------- the wash ----------

    [Fact]
    public void AFillIsAWashSoWhatIsUnderTheCellStillReads()
    {
        var style = new DataGridCellStyle { Fill = Colors.Red };

        // Translucent by default and returned as such, so the row's own stripe/selection composites
        // through it rather than being replaced by it.
        var wash = style.WashColor()!;
        wash.Alpha.ShouldBe((float)DataGridCellStyle.DefaultFillOpacity, 0.001f);
        style.EffectiveBackground().Alpha.ShouldBe(wash.Alpha, 0.001f);
    }

    [Fact]
    public void AnOpaqueFillIsLeftAlone()
    {
        var style = new DataGridCellStyle { Fill = Colors.Red, FillOpacity = 1 };
        style.WashColor().ShouldBe(Colors.Red);
    }

    [Fact]
    public void FillOpacityScalesAnAlphaTheColourAlreadyHad()
    {
        var style = new DataGridCellStyle { Fill = Colors.Red.WithAlpha(0.5f), FillOpacity = 0.5 };
        style.WashColor()!.Alpha.ShouldBe(0.25f, 0.001f);
    }

    [Fact]
    public void AFillOverAnOpaqueBackgroundIsFlattenedOntoIt()
    {
        // Nothing shows through an opaque BackgroundColor, so the wash has to be composited rather
        // than left translucent - otherwise the cell would read as half its own background.
        var style = new DataGridCellStyle
        {
            BackgroundColor = Colors.White,
            Fill = Colors.Black,
            FillOpacity = 0.5
        };

        var background = style.EffectiveBackground();
        background.Alpha.ShouldBe(1f);
        background.Red.ShouldBe(0.5f, 0.01f);
    }

    // ---------- merging ----------

    [Fact]
    public void MergeTakesMemberGroupsWholesaleNotMemberByMember()
    {
        var under = new DataGridCellStyle { Fill = Colors.Blue, FillOpacity = 1, TextColor = Colors.Green };
        var over = new DataGridCellStyle { BorderColor = Colors.Red, BorderStyle = DataGridBorderStyle.Solid };

        var merged = DataGridCellStyle.Merge(under, over)!;

        // The narrower style only spoke to the stroke, so the wash and the text survive it intact.
        merged.Fill.ShouldBe(Colors.Blue);
        merged.FillOpacity.ShouldBe(1);
        merged.TextColor.ShouldBe(Colors.Green);
        merged.BorderColor.ShouldBe(Colors.Red);
    }

    [Fact]
    public void MergeLetsTheNarrowerStyleReplaceAWholeGroup()
    {
        var under = new DataGridCellStyle { Fill = Colors.Blue, FillOpacity = 1 };
        var over = new DataGridCellStyle { Fill = Colors.Red };

        var merged = DataGridCellStyle.Merge(under, over)!;
        merged.Fill.ShouldBe(Colors.Red);
        // FillOpacity travels with Fill rather than being inherited from the wider rule.
        merged.FillOpacity.ShouldBeNull();
    }

    // ---------- edges ----------

    [Theory]
    [InlineData(DataGridHighlightScope.Cell, true, true, true, true, DataGridBorderEdges.All)]
    [InlineData(DataGridHighlightScope.Cell, false, false, false, false, DataGridBorderEdges.All)]
    [InlineData(DataGridHighlightScope.Row, false, false, true, true, DataGridBorderEdges.Top | DataGridBorderEdges.Bottom)]
    [InlineData(DataGridHighlightScope.Row, true, false, true, true, DataGridBorderEdges.Top | DataGridBorderEdges.Bottom | DataGridBorderEdges.Left)]
    [InlineData(DataGridHighlightScope.Column, false, false, false, false, DataGridBorderEdges.Left | DataGridBorderEdges.Right)]
    [InlineData(DataGridHighlightScope.Column, false, false, true, false, DataGridBorderEdges.Left | DataGridBorderEdges.Right | DataGridBorderEdges.Top)]
    [InlineData(DataGridHighlightScope.Grid, false, false, false, false, DataGridBorderEdges.None)]
    [InlineData(DataGridHighlightScope.Grid, true, true, true, true, DataGridBorderEdges.All)]
    public void EdgesTraceTheRegionNotTheCell(
        DataGridHighlightScope scope,
        bool firstColumn,
        bool lastColumn,
        bool firstRow,
        bool lastRow,
        DataGridBorderEdges expected
    )
        => DataGrid.DataGrid.EdgesFor(scope, firstColumn, lastColumn, firstRow, lastRow).ShouldBe(expected);

    // ---------- scope derivation ----------

    [Fact]
    public void ScopeIsDerivedFromWhichTargetsAreSet()
    {
        new DataGridHighlight().Scope.ShouldBe(DataGridHighlightScope.Grid);
        new DataGridHighlight { Column = "Salary" }.Scope.ShouldBe(DataGridHighlightScope.Column);
        new DataGridHighlight { RowPredicate = _ => true }.Scope.ShouldBe(DataGridHighlightScope.Row);
        new DataGridHighlight { Item = Ada }.Scope.ShouldBe(DataGridHighlightScope.Row);
        new DataGridHighlight { Item = Ada, Column = "Salary" }.Scope.ShouldBe(DataGridHighlightScope.Cell);

        // A bound selection that came back null is a real state - it must stop the rule matching
        // rather than silently widen it to the whole grid.
        new DataGridHighlight { Item = null }.Scope.ShouldBe(DataGridHighlightScope.Row);
    }

    [Fact]
    public void AColumnIsNamedByEitherPropertyNameOrTitle()
    {
        var grid = BuildGrid();
        var salary = Column(grid, "Salary");

        new DataGridHighlight { Column = "salary" }.MatchesColumn(salary).ShouldBeTrue();
        new DataGridHighlight { Column = "Salary" }.MatchesColumn(salary).ShouldBeTrue();
        new DataGridHighlight { Column = "Name" }.MatchesColumn(salary).ShouldBeFalse();
        new DataGridHighlight().MatchesColumn(salary).ShouldBeTrue();
    }

    // ---------- resolution through the grid ----------

    [Fact]
    public void ARowRuleCoversEveryColumnAndOnlyTheMatchedRows()
    {
        var grid = BuildGrid(g => g.Highlights.Add(new DataGridHighlight
        {
            RowPredicate = r => ((Row)r).Overdue,
            Fill = Colors.Red
        }));

        foreach (var title in new[] { "Name", "Salary", "Overdue" })
        {
            grid.ResolveCellStyle(Column(grid, title), Alan, true, false, false, false)!.Fill.ShouldBe(Colors.Red);
            grid.ResolveCellStyle(Column(grid, title), Ada, true, false, false, false).ShouldBeNull();
        }
    }

    [Fact]
    public void ANarrowerScopeWinsAndTheRestOfTheStyleSurvivesIt()
    {
        var grid = BuildGrid(g =>
        {
            g.Highlights.Add(new DataGridHighlight { Column = "Salary", Fill = Colors.LightBlue, FillOpacity = 1 });
            g.Highlights.Add(new DataGridHighlight
            {
                RowPredicate = r => ((Row)r).Overdue,
                Column = "Salary",
                BorderColor = Colors.DarkRed,
                BorderStyle = DataGridBorderStyle.Solid
            });
        });

        var style = grid.ResolveCellStyle(Column(grid, "Salary"), Alan, false, false, false, false)!;
        style.Fill.ShouldBe(Colors.LightBlue);
        style.BorderColor.ShouldBe(Colors.DarkRed);
        // Cell scope, so the stroke boxes the one cell.
        style.BorderEdges.ShouldBe(DataGridBorderEdges.All);
    }

    [Fact]
    public void AColumnCellStyleStillWinsOverEveryHighlight()
    {
        var grid = BuildGrid(g =>
        {
            g.RowHighlight = _ => new DataGridCellStyle { TextColor = Colors.Green };
            Column(g, "Salary").Highlight = new DataGridCellStyle { TextColor = Colors.Blue };
            Column(g, "Salary").CellStyle = r => ((Row)r).Overdue
                ? new DataGridCellStyle { TextColor = Colors.DarkRed }
                : null;
        });

        grid.ResolveCellStyle(Column(grid, "Salary"), Alan, false, false, false, false)!
            .TextColor.ShouldBe(Colors.DarkRed);

        // Nothing overruled Ada's cell, so the row rule beats the column's static Highlight.
        grid.ResolveCellStyle(Column(grid, "Salary"), Ada, false, false, false, false)!
            .TextColor.ShouldBe(Colors.Green);
    }

    [Fact]
    public void ADisabledRuleStaysInTheCollectionAndPaintsNothing()
    {
        var grid = BuildGrid(g => g.Highlights.Add(new DataGridHighlight
        {
            Column = "Salary",
            Fill = Colors.LightBlue,
            IsEnabled = false
        }));

        grid.ResolveCellStyle(Column(grid, "Salary"), Ada, false, false, false, false).ShouldBeNull();
    }

    [Fact]
    public void ARowRuleStrokesThePerimeterOfTheRun()
    {
        var grid = BuildGrid(g => g.Highlights.Add(new DataGridHighlight
        {
            Item = Alan,
            BorderColor = Colors.Red,
            BorderStyle = DataGridBorderStyle.Dashed
        }));

        var first = grid.ResolveCellStyle(Column(grid, "Name"), Alan, true, false, false, false)!;
        var middle = grid.ResolveCellStyle(Column(grid, "Salary"), Alan, false, false, false, false)!;
        var last = grid.ResolveCellStyle(Column(grid, "Overdue"), Alan, false, true, false, false)!;

        var caps = DataGridBorderEdges.Top | DataGridBorderEdges.Bottom;
        first.BorderEdges.ShouldBe(caps | DataGridBorderEdges.Left);
        middle.BorderEdges.ShouldBe(caps);
        last.BorderEdges.ShouldBe(caps | DataGridBorderEdges.Right);
    }

    [Fact]
    public void ExplicitEdgesOverrideTheDerivedPerimeter()
    {
        var grid = BuildGrid(g => g.Highlights.Add(new DataGridHighlight
        {
            RowPredicate = _ => true,
            BorderColor = Colors.Red,
            BorderStyle = DataGridBorderStyle.Solid,
            BorderEdges = DataGridBorderEdges.Bottom
        }));

        grid.ResolveCellStyle(Column(grid, "Name"), Ada, true, false, false, false)!
            .BorderEdges.ShouldBe(DataGridBorderEdges.Bottom);
    }

    // ---------- block position ----------

    [Fact]
    public void TheEndsOfTheRenderedBlockAreStampedOntoTheRows()
    {
        var rows = Rows(BuildGrid());

        rows.Count.ShouldBe(3);
        rows[0].IsFirstRow.ShouldBeTrue();
        rows[0].IsLastRow.ShouldBeFalse();
        rows[1].IsFirstRow.ShouldBeFalse();
        rows[1].IsLastRow.ShouldBeFalse();
        rows[2].IsLastRow.ShouldBeTrue();
    }

    [Fact]
    public void ASingleRowIsBothEndsOfItselfSoItIsBoxedIn()
    {
        new Application();
        var grid = new DataGrid.DataGrid();
        grid.Columns.Add(new DataGridColumn { Title = "Name", PropertyName = nameof(Row.Name) });
        grid.ItemsSource = new List<Row> { Ada };

        var only = Rows(grid).Single();
        only.IsFirstRow.ShouldBeTrue();
        only.IsLastRow.ShouldBeTrue();
    }

    [Fact]
    public void EachGroupIsItsOwnBlock()
    {
        var grid = BuildGrid(g =>
        {
            g.Groupable = true;
            Column(g, "Overdue").Groupable = true;
        });
        grid.ToggleGroupBy(Column(grid, "Overdue"));

        var rows = Rows(grid);
        rows.Count.ShouldBe(3);

        // Two groups (false: Ada + Grace, true: Alan) rather than one run of three, so a column
        // highlight is capped at the group header sitting between them.
        rows.Count(r => r.IsFirstRow).ShouldBe(2);
        rows.Count(r => r.IsLastRow).ShouldBe(2);
    }

    // ---------- the stroke drawable ----------

    [Fact]
    public void TheDrawableOnlyArmsWhenThereIsAStrokeToPaint()
    {
        var drawable = new DataGridHighlightDrawable();

        drawable.Apply(null, DataGridBorderEdges.All).ShouldBeFalse();
        drawable.Apply(new DataGridCellStyle { Fill = Colors.Red }, DataGridBorderEdges.All).ShouldBeFalse();
        drawable.Apply(
            new DataGridCellStyle { BorderColor = Colors.Red, BorderStyle = DataGridBorderStyle.Solid },
            DataGridBorderEdges.None
        ).ShouldBeFalse();

        drawable.Apply(
            new DataGridCellStyle { BorderColor = Colors.Red, BorderStyle = DataGridBorderStyle.Dashed, BorderWidth = 4 },
            DataGridBorderEdges.Top
        ).ShouldBeTrue();

        drawable.Stroke.ShouldBe(Colors.Red);
        drawable.Thickness.ShouldBe(4f);
        drawable.Edges.ShouldBe(DataGridBorderEdges.Top);
    }

    [Fact]
    public void TheDrawableFallsBackToTheDefaultThickness()
    {
        var drawable = new DataGridHighlightDrawable();
        drawable.Apply(
            new DataGridCellStyle { BorderColor = Colors.Red, BorderStyle = DataGridBorderStyle.Solid },
            DataGridBorderEdges.All
        );

        drawable.Thickness.ShouldBe((float)DataGridCellStyle.DefaultBorderWidth);
    }
}
