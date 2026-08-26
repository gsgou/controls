using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Grouping and summary rows meet in the flattened display list: a group header, its rows (or the next
/// level's headers), and the summary rows that total exactly the items under it. These pin what lands
/// in that list, since it is the only thing the CollectionView can render.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class DataGridGroupingTests
{
    class Employee
    {
        public string Department { get; set; } = "";
        public string Region { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
    }

    static readonly Employee[] Staff =
    {
        new() { Department = "Sales", Region = "West", Name = "ann", Salary = 100 },
        new() { Department = "Sales", Region = "East", Name = "bob", Salary = 200 },
        new() { Department = "Ops", Region = "West", Name = "cal", Salary = 300 },
        new() { Department = "Ops", Region = "West", Name = "dee", Salary = 400 }
    };

    static DataGrid.DataGrid BuildGrid(Action<DataGrid.DataGrid>? configure = null)
    {
        new Application();

        var grid = new DataGrid.DataGrid();
        grid.Columns.Add(new DataGridColumn { Title = "Department", PropertyName = nameof(Employee.Department) });
        grid.Columns.Add(new DataGridColumn { Title = "Region", PropertyName = nameof(Employee.Region) });
        grid.Columns.Add(new DataGridColumn { Title = "Name", PropertyName = nameof(Employee.Name) });
        grid.Columns.Add(new DataGridColumn { Title = "Salary", PropertyName = nameof(Employee.Salary), DisplayAs = DataGridColumnFormat.Currency, Decimals = 0 });
        configure?.Invoke(grid);
        grid.ItemsSource = Staff.ToList();
        return grid;
    }

    /// <summary>A total row: "Total" against the Name column, the sum against Salary.</summary>
    static DataGridSummaryRow TotalRow(DataGridSummaryScope scope = DataGridSummaryScope.Both)
    {
        var row = new DataGridSummaryRow { Scope = scope };
        row.Cells.Add(new DataGridSummaryCell { Column = nameof(Employee.Name), Text = "Total", Alignment = DataGridCellAlignment.End });
        row.Cells.Add(new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Sum });
        return row;
    }

    static IReadOnlyList<string> Shape(DataGrid.DataGrid grid)
        => grid.DisplayItems
            .Select(i => i switch
            {
                DataGridGroupHeader h => $"group{h.Level}:{h.KeyText}:{h.Count}",
                DataGridSummaryRowItem s => $"summary:{s.Items.Count}",
                DataGridRow r => $"row:{((Employee)r.Data).Name}",
                _ => "other"
            })
            .ToList();

    static IReadOnlyList<string> FooterTexts(DataGrid.DataGrid grid)
        => grid.FooterViews
            .SelectMany(v => ((Grid)v).Children.OfType<Label>())
            .Select(l => l.Text)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList()!;

    [Fact]
    public void UngroupedGridIsAFlatRunOfRows()
    {
        var grid = BuildGrid();

        Shape(grid).ShouldBe(new[] { "row:ann", "row:bob", "row:cal", "row:dee" });
    }

    [Fact]
    public void EachGroupHeaderCarriesItsOwnRows()
    {
        var grid = BuildGrid(g => g.GroupBy.Add(nameof(Employee.Department)));

        // Ops sorts before Sales - groups are ordered by key, not by the order rows arrive in.
        Shape(grid).ShouldBe(new[]
        {
            "group0:Ops:2", "row:cal", "row:dee",
            "group0:Sales:2", "row:ann", "row:bob"
        });
    }

    [Fact]
    public void GroupsNestInDeclarationOrder()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.GroupBy.Add(nameof(Employee.Region));
        });

        Shape(grid).ShouldBe(new[]
        {
            "group0:Ops:2", "group1:West:2", "row:cal", "row:dee",
            "group0:Sales:2", "group1:East:1", "row:bob", "group1:West:1", "row:ann"
        });
    }

    [Fact]
    public void GroupingDoesNotNeedTheInteractiveSwitch()
    {
        // Groupable gates the header's ⊞ button, not grouping itself - a declared GroupBy stands on
        // its own, which is what makes a view-model-driven grouping work.
        var grid = BuildGrid(g =>
        {
            g.Groupable = false;
            g.GroupBy.Add(nameof(Employee.Department));
        });

        Shape(grid).ShouldContain("group0:Ops:2");
    }

    [Fact]
    public void SummaryRowFollowsEveryGroupAndTotalsOnlyThatGroup()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.SummaryRows.Add(TotalRow());
        });

        Shape(grid).ShouldBe(new[]
        {
            "group0:Ops:2", "row:cal", "row:dee", "summary:2",
            "group0:Sales:2", "row:ann", "row:bob", "summary:2"
        });

        var summaries = grid.DisplayItems.OfType<DataGridSummaryRowItem>().ToList();
        var salary = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Salary));
        var name = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Name));

        // Ops = 300 + 400, Sales = 100 + 200 - and the label sits in the column beside the number.
        summaries[0].TextFor(salary).ShouldBe("$700");
        summaries[1].TextFor(salary).ShouldBe("$300");
        summaries[0].TextFor(name).ShouldBe("Total");
    }

    [Fact]
    public void ASummaryLeavesEveryColumnItDeclaresNoCellForBlank()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.SummaryRows.Add(TotalRow());
        });

        var summary = grid.DisplayItems.OfType<DataGridSummaryRowItem>().First();
        var department = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Department));

        summary.TextFor(department).ShouldBeNull();
    }

    [Fact]
    public void HeaderPlacementKeepsAGroupsTotalsVisibleWhileItIsCollapsed()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.SummaryRows.Add(TotalRow());
            g.GroupSummaryPlacement = DataGridGroupSummaryPlacement.Header;
            g.GroupsInitiallyExpanded = false;
        });

        Shape(grid).ShouldBe(new[]
        {
            "group0:Ops:2", "summary:2",
            "group0:Sales:2", "summary:2"
        });
    }

    [Fact]
    public void FooterPlacementCollapsesWithTheRowsItTotals()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.SummaryRows.Add(TotalRow());
            g.GroupsInitiallyExpanded = false;
        });

        Shape(grid).ShouldBe(new[] { "group0:Ops:2", "group0:Sales:2" });
    }

    [Fact]
    public void CollapseStateIsPerGroupPathSoRepeatedKeysStayIndependent()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.GroupBy.Add(nameof(Employee.Region));
        });

        // "West" appears under both departments; collapsing one must not collapse the other.
        var west = grid.DisplayItems
            .OfType<DataGridGroupHeader>()
            .First(h => h.Level == 1 && h.KeyText == "West");

        grid.ToggleGroup(west);

        Shape(grid).ShouldBe(new[]
        {
            "group0:Ops:2", "group1:West:2",
            "group0:Sales:2", "group1:East:1", "row:bob", "group1:West:1", "row:ann"
        });
    }

    [Fact]
    public void CollapseAllReachesGroupsThatWereNeverOnScreen()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.GroupBy.Add(nameof(Employee.Region));
        });

        grid.CollapseAllGroups();
        Shape(grid).ShouldBe(new[] { "group0:Ops:2", "group0:Sales:2" });

        // The inner groups were not in the item list to be collapsed one by one, so reopening a
        // parent must not spring its children open with it.
        grid.ToggleGroup(grid.Groups[0]);
        Shape(grid).ShouldBe(new[] { "group0:Ops:2", "group1:West:2", "group0:Sales:2" });

        grid.ExpandAllGroups();
        Shape(grid).ShouldContain("row:cal");
    }

    [Fact]
    public void ScopeKeepsAGridTotalOutOfTheGroups()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupBy.Add(nameof(Employee.Department));
            g.SummaryRows.Add(TotalRow(DataGridSummaryScope.Grid));
        });

        Shape(grid).ShouldNotContain("summary:2");
        FooterTexts(grid).ShouldContain("$1,000");
    }

    [Fact]
    public void TheGridsOwnSummaryTotalsEveryProcessedRow()
    {
        var grid = BuildGrid(g => g.SummaryRows.Add(TotalRow()));

        FooterTexts(grid).ShouldBe(new[] { "Total", "$1,000" });
    }

    [Fact]
    public void SummaryRowsStackInDeclarationOrder()
    {
        var grid = BuildGrid(g =>
        {
            var count = new DataGridSummaryRow();
            count.Cells.Add(new DataGridSummaryCell { Column = nameof(Employee.Name), Text = "Headcount" });
            count.Cells.Add(new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Count });

            g.SummaryRows.Add(TotalRow());
            g.SummaryRows.Add(count);
        });

        grid.FooterViews.Count.ShouldBe(2);
        FooterTexts(grid).ShouldBe(new[] { "Total", "$1,000", "Headcount", "4" });
    }

    [Fact]
    public void ColumnLevelAggregatesStillProduceTheirFooterRow()
    {
        // The pre-summary-row API: an Aggregate hung off the column itself.
        var grid = BuildGrid(g => g.Columns
            .Single(c => c.PropertyName == nameof(Employee.Salary))
            .Aggregate = new DataGridAggregateDefinition { Type = DataGridAggregateType.Sum, Format = "C0" });

        FooterTexts(grid).ShouldBe(new[] { "$1,000" });
    }

    [Fact]
    public void TheHeaderButtonAppendsAndRemovesAGroupingLevel()
    {
        var grid = BuildGrid(g => g.Groupable = true);
        var department = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Department));
        var region = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Region));

        grid.ToggleGroupBy(department);
        grid.ToggleGroupBy(region);
        grid.GroupBy.ShouldBe(new[] { nameof(Employee.Department), nameof(Employee.Region) });

        grid.ToggleGroupBy(department);
        grid.GroupBy.ShouldBe(new[] { nameof(Employee.Region) });
        Shape(grid).ShouldBe(new[] { "group0:East:1", "row:bob", "group0:West:3", "row:ann", "row:cal", "row:dee" });
    }

    [Fact]
    public void DescendingReversesTheGroupOrderWithoutTouchingTheRows()
    {
        var grid = BuildGrid(g =>
        {
            g.GroupSortDirection = DataGridSortDirection.Descending;
            g.GroupBy.Add(nameof(Employee.Department));
        });

        Shape(grid).ShouldBe(new[]
        {
            "group0:Sales:2", "row:ann", "row:bob",
            "group0:Ops:2", "row:cal", "row:dee"
        });
    }

    [Fact]
    public void AnAggregateWithNoFormatWearsItsColumnsOwn()
    {
        var grid = BuildGrid();
        var salary = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Salary));

        var sum = new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Sum };
        var average = new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Average };
        var count = new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Count };
        var explicitFormat = new DataGridSummaryCell { Column = nameof(Employee.Salary), Aggregate = DataGridAggregateType.Sum, StringFormat = "N1" };

        sum.ComputeText(salary, Staff).ShouldBe("$1,000");
        average.ComputeText(salary, Staff).ShouldBe("$250");
        // A count is a count - it never wears the currency the column's values wear.
        count.ComputeText(salary, Staff).ShouldBe("4");
        explicitFormat.ComputeText(salary, Staff).ShouldBe("1,000.0");
    }

    [Fact]
    public void ACustomAggregateOwnsItsOwnText()
    {
        var grid = BuildGrid();
        var salary = grid.Columns.Single(c => c.PropertyName == nameof(Employee.Salary));

        var cell = new DataGridSummaryCell
        {
            Column = nameof(Employee.Salary),
            Aggregate = DataGridAggregateType.Custom,
            CustomAggregate = items => $"{items.Count()} people"
        };

        cell.ComputeText(salary, Staff).ShouldBe("4 people");
    }
}
