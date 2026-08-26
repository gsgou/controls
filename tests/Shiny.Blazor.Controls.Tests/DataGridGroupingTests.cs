using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Grouping and summary rows are only real once they reach the DOM: a group header row, its rows (or
/// the next level's headers), and a summary row totalling exactly the items under it. These render for
/// real rather than asserting on the helpers, because the wiring between them is what breaks.
/// </summary>
public class DataGridGroupingTests
{
    class Person
    {
        public string Department { get; set; } = "";
        public string Region { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
    }

    static readonly Person[] Staff =
    [
        new() { Department = "Sales", Region = "West", Name = "Ann", Salary = 100 },
        new() { Department = "Sales", Region = "East", Name = "Bob", Salary = 200 },
        new() { Department = "Ops", Region = "West", Name = "Cal", Salary = 300 },
        new() { Department = "Ops", Region = "West", Name = "Dee", Salary = 400 },
    ];

    /// <summary>Static rendering never reaches OnAfterRender, so the grid's JS is never called.</summary>
    sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }

    static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IJSRuntime>(new NoJs());
        var provider = services.BuildServiceProvider();

        parameters["Items"] = Staff;
        parameters.TryAdd("Columns", (RenderFragment)DefaultColumns);

        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<DataGrid<Person>>(ParameterView.FromDictionary(parameters));
            return System.Net.WebUtility.HtmlDecode(output.ToHtmlString());
        });
    }

    static void DefaultColumns(RenderTreeBuilder b)
    {
        Column<string>(b, 0, x => x.Department);
        Column<string>(b, 10, x => x.Region);
        Column<string>(b, 20, x => x.Name);
        Column<decimal>(b, 30, x => x.Salary, ("DisplayAs", DataGridColumnFormat.Currency), ("Decimals", 0));
    }

    static void Column<TProp>(
        RenderTreeBuilder b,
        int seq,
        Expression<Func<Person, TProp>> property,
        params (string Name, object? Value)[] extra)
    {
        b.OpenComponent<PropertyColumn<Person, TProp>>(seq);
        b.AddComponentParameter(seq + 1, nameof(PropertyColumn<Person, TProp>.Property), property);
        b.AddComponentParameter(seq + 2, nameof(PropertyColumn<Person, TProp>.Culture), new CultureInfo("en-US"));
        var i = seq + 3;
        foreach (var (name, value) in extra)
            b.AddComponentParameter(i++, name, value);
        b.CloseComponent();
    }

    /// <summary>A total row: "Total" against the Name column, the sum against Salary.</summary>
    static RenderFragment TotalRow(DataGridSummaryScope scope = DataGridSummaryScope.Both)
        => b =>
        {
            b.OpenComponent<SummaryRow<Person>>(0);
            b.AddComponentParameter(1, nameof(SummaryRow<Person>.Scope), scope);
            b.AddComponentParameter(2, nameof(SummaryRow<Person>.ChildContent), (RenderFragment)(cb =>
            {
                Cell(cb, 0, "Name", ("Text", "Total"), ("Alignment", DataGridCellAlignment.End));
                Cell(cb, 10, "Salary", ("Aggregate", DataGridAggregateType.Sum));
            }));
            b.CloseComponent();
        };

    static void Cell(RenderTreeBuilder b, int seq, string column, params (string Name, object? Value)[] extra)
    {
        b.OpenComponent<SummaryCell<Person>>(seq);
        b.AddComponentParameter(seq + 1, nameof(SummaryCell<Person>.Column), column);
        var i = seq + 2;
        foreach (var (name, value) in extra)
            b.AddComponentParameter(i++, name, value);
        b.CloseComponent();
    }

    static int Count(string html, string needle) => Regex.Matches(html, Regex.Escape(needle)).Count;

    [Fact]
    public async Task AGroupHeaderNamesItsColumnKeyAndSize()
    {
        var html = await RenderAsync(new() { ["GroupBy"] = new[] { "Department" } });

        html.ShouldContain("shiny-dg-group-header");
        html.ShouldContain("Department:</strong>");
        html.ShouldContain(">Ops<");
        html.ShouldContain(">Sales<");
        html.ShouldContain("(2)");
    }

    [Fact]
    public async Task NestedLevelsIndentInsteadOfNesting()
    {
        var html = await RenderAsync(new() { ["GroupBy"] = new[] { "Department", "Region" } });

        // Two outer groups plus three inner ones - West appears under both departments.
        Count(html, "shiny-dg-group-header").ShouldBe(5);
        html.ShouldContain("shiny-dg-group-indent\" style=\"width:0px");
        html.ShouldContain("shiny-dg-group-indent\" style=\"width:18px");
    }

    [Fact]
    public async Task EveryGroupGetsASummaryRowTotallingOnlyItsOwnRows()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department" },
            ["SummaryRows"] = TotalRow()
        });

        Count(html, "shiny-dg-group-footer").ShouldBe(2);
        html.ShouldContain("$700");   // Ops: 300 + 400
        html.ShouldContain("$300");   // Sales: 100 + 200
        html.ShouldContain("$1,000"); // and the grid's own total, in the tfoot
        Count(html, ">Total<").ShouldBe(3);
    }

    [Fact]
    public async Task HeaderPlacementKeepsTotalsVisibleWhileGroupsAreCollapsed()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department" },
            ["SummaryRows"] = TotalRow(),
            ["GroupSummaryPlacement"] = DataGridGroupSummaryPlacement.Header,
            ["GroupsInitiallyExpanded"] = false
        });

        // No data rows at all, but both group totals are still on screen.
        html.ShouldNotContain(">Ann<");
        html.ShouldContain("$700");
        html.ShouldContain("$300");
    }

    [Fact]
    public async Task FooterPlacementCollapsesWithTheRowsItTotals()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department" },
            ["SummaryRows"] = TotalRow(),
            ["GroupsInitiallyExpanded"] = false
        });

        html.ShouldNotContain("shiny-dg-group-footer");
        html.ShouldContain("$1,000");
    }

    [Fact]
    public async Task ACollapsedGroupContributesNoNestedHeadersEither()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department", "Region" },
            ["GroupsInitiallyExpanded"] = false
        });

        // Only the two outer groups are on screen; nothing nested, and no rows.
        Count(html, "shiny-dg-group-header").ShouldBe(2);
        html.ShouldNotContain("width:18px");
        html.ShouldNotContain(">Ann<");
    }

    [Fact]
    public async Task ScopeKeepsAGridTotalOutOfTheGroups()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department" },
            ["SummaryRows"] = TotalRow(DataGridSummaryScope.Grid)
        });

        html.ShouldNotContain("shiny-dg-group-footer");
        html.ShouldContain("$1,000");
    }

    [Fact]
    public async Task SummaryRowsStackInDeclarationOrder()
    {
        RenderFragment rows = b =>
        {
            TotalRow()(b);
            b.OpenComponent<SummaryRow<Person>>(100);
            b.AddComponentParameter(101, nameof(SummaryRow<Person>.ChildContent), (RenderFragment)(cb =>
            {
                Cell(cb, 0, "Name", ("Text", "Headcount"));
                Cell(cb, 10, "Salary", ("Aggregate", DataGridAggregateType.Count));
            }));
            b.CloseComponent();
        };

        var html = await RenderAsync(new() { ["SummaryRows"] = rows });

        var foot = html[html.IndexOf("<tfoot", StringComparison.Ordinal)..];
        Count(foot, "<tr").ShouldBe(2);
        foot.IndexOf("Total", StringComparison.Ordinal).ShouldBeLessThan(foot.IndexOf("Headcount", StringComparison.Ordinal));
        foot.ShouldContain("$1,000");
        foot.ShouldContain(">4<");
    }

    [Fact]
    public async Task ALabelCellLeavesEveryOtherColumnBlank()
    {
        var html = await RenderAsync(new() { ["SummaryRows"] = TotalRow() });

        var foot = html[html.IndexOf("<tfoot", StringComparison.Ordinal)..];

        // Four columns, two of which the row declares nothing for.
        Count(foot, "<td").ShouldBe(4);
        Count(foot, "></td>").ShouldBe(2);
    }

    [Fact]
    public async Task ColumnLevelAggregatesStillProduceTheirFooterRow()
    {
        // The pre-summary-row API: an Aggregate hung off the column itself.
        var html = await RenderAsync(new()
        {
            ["Columns"] = (RenderFragment)(b =>
            {
                Column<string>(b, 0, x => x.Name);
                Column<decimal>(b, 10, x => x.Salary,
                    ("DisplayAs", DataGridColumnFormat.Currency),
                    ("Decimals", 0),
                    ("Aggregate", new AggregateDefinition<Person> { Type = DataGridAggregateType.Sum, Format = "C0" }));
            })
        });

        html.ShouldContain("<tfoot");
        html.ShouldContain("$1,000");
    }

    [Fact]
    public async Task GroupingDoesNotNeedTheInteractiveSwitch()
    {
        // Groupable gates the header's ⊞ button, not grouping itself - a bound GroupBy stands alone.
        var html = await RenderAsync(new() { ["GroupBy"] = new[] { "Department" }, ["Groupable"] = false });

        html.ShouldContain("shiny-dg-group-header");
        html.ShouldNotContain("shiny-dg-group-icon");
    }

    [Fact]
    public async Task DescendingReversesTheGroupOrder()
    {
        var html = await RenderAsync(new()
        {
            ["GroupBy"] = new[] { "Department" },
            ["GroupSortDirection"] = DataGridSortDirection.Descending
        });

        html.IndexOf(">Sales<", StringComparison.Ordinal)
            .ShouldBeLessThan(html.IndexOf(">Ops<", StringComparison.Ordinal));
    }
}
