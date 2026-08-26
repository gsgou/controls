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
/// Highlighting is three features wearing one coat - a row, a column and a cell all resolve through
/// the same pipeline - so the tests that matter are the ones about what happens where they meet: the
/// precedence between overlapping rules, and the edges each one strokes. A row highlight that boxed
/// every cell it touched would be the obvious wrong answer, and
/// <see cref="RowHighlightStrokesThePerimeterNotEachCell"/> is what pins that down.
/// </summary>
public class DataGridHighlightTests
{
    class Person
    {
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
        public bool Overdue { get; set; }
    }

    static readonly Person[] People =
    [
        new() { Name = "Ada", Salary = 142000 },
        new() { Name = "Alan", Salary = 98000, Overdue = true },
        new() { Name = "Grace", Salary = 120000 }
    ];

    sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }

    static async Task<string> RenderAsync(
        Action<RenderTreeBuilder> columns,
        params (string Name, object? Value)[] gridParameters
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IJSRuntime>(new NoJs());
        var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var values = new Dictionary<string, object?>
            {
                ["Items"] = People,
                ["Columns"] = (RenderFragment)(b => columns(b))
            };
            foreach (var (name, value) in gridParameters)
                values[name] = value;

            var output = await renderer.RenderComponentAsync<DataGrid<Person>>(ParameterView.FromDictionary(values));
            return System.Net.WebUtility.HtmlDecode(output.ToHtmlString());
        });
    }

    static void Column<TProp>(
        RenderTreeBuilder b,
        int seq,
        Expression<Func<Person, TProp>> property,
        params (string Name, object? Value)[] extra
    )
    {
        b.OpenComponent<PropertyColumn<Person, TProp>>(seq);
        b.AddComponentParameter(seq + 1, nameof(PropertyColumn<Person, TProp>.Property), property);
        b.AddComponentParameter(seq + 2, nameof(PropertyColumn<Person, TProp>.Culture), new CultureInfo("en-US"));
        var i = seq + 3;
        foreach (var (name, value) in extra)
            b.AddComponentParameter(i++, name, value);
        b.CloseComponent();
    }

    static void ThreeColumns(RenderTreeBuilder b)
    {
        Column<string>(b, 0, x => x.Name);
        Column<decimal>(b, 10, x => x.Salary);
        Column<bool>(b, 20, x => x.Overdue);
    }

    /// <summary>The cells of one rendered row, in column order.</summary>
    static IReadOnlyList<string> RowCells(string html, string rowText)
    {
        var rows = Regex.Matches(html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Where(r => r.Contains(rowText, StringComparison.Ordinal))
            .ToList();

        rows.Count.ShouldBe(1);
        return Regex.Matches(rows[0], "<td[^>]*>", RegexOptions.Singleline).Select(m => m.Value).ToList();
    }

    [Fact]
    public async Task FillIsAGradientLayerSoTheRowUnderneathStillReads()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("RowHighlight", (Func<Person, DataGridCellStyle?>)(p => p.Overdue
                ? new DataGridCellStyle { Fill = "var(--shiny-color-error)" }
                : null))
        );

        // background-image, not background-color: the wash has to layer over the stripe/selection tint
        // rather than replace it, and it has to stay behind the cell's text.
        html.ShouldContain("background-image:linear-gradient(color-mix(in srgb, var(--shiny-color-error) 25%, transparent)");
        html.ShouldNotContain("background-color:var(--shiny-color-error)");

        // One row, every one of its three cells.
        Regex.Matches(html, "background-image:linear-gradient").Count.ShouldBe(3);
    }

    [Fact]
    public async Task AFullyOpaqueFillSkipsTheColourMix()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("RowHighlight", (Func<Person, DataGridCellStyle?>)(p => p.Overdue
                ? new DataGridCellStyle { Fill = "#ffd54f", FillOpacity = 1 }
                : null))
        );

        html.ShouldContain("background-image:linear-gradient(#ffd54f,#ffd54f)");
        html.ShouldNotContain("color-mix(in srgb, #ffd54f");
    }

    [Fact]
    public async Task RowHighlightStrokesThePerimeterNotEachCell()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("RowHighlight", (Func<Person, DataGridCellStyle?>)(p => p.Overdue
                ? new DataGridCellStyle { BorderColor = "#d32f2f", BorderStyle = DataGridBorderStyle.Dashed }
                : null))
        );

        var cells = RowCells(html, "Alan");
        cells.Count.ShouldBe(3);

        // Every cell in the row is capped top and bottom...
        foreach (var cell in cells)
        {
            cell.ShouldContain("border-top:2px dashed #d32f2f");
            cell.ShouldContain("border-bottom:2px dashed #d32f2f");
        }

        // ...but the vertical strokes only close the two ends of the run.
        cells[0].ShouldContain("border-left:2px dashed #d32f2f");
        cells[0].ShouldNotContain("border-right:");
        cells[1].ShouldNotContain("border-left:");
        cells[1].ShouldNotContain("border-right:");
        cells[2].ShouldContain("border-right:2px dashed #d32f2f");
        cells[2].ShouldNotContain("border-left:");
    }

    [Fact]
    public async Task ColumnHighlightStrokesTheRunTopAndBottomOnly()
    {
        var html = await RenderAsync(
            b =>
            {
                Column<string>(b, 0, x => x.Name);
                Column<decimal>(b, 10, x => x.Salary,
                    ("Highlight", new DataGridCellStyle
                    {
                        BorderColor = "#1565c0",
                        BorderStyle = DataGridBorderStyle.Solid,
                        BorderWidth = "3px"
                    }));
                Column<bool>(b, 20, x => x.Overdue);
            }
        );

        var first = RowCells(html, "Ada")[1];
        var middle = RowCells(html, "Alan")[1];
        var last = RowCells(html, "Grace")[1];

        // Both flanks on every cell - the region is one column wide.
        foreach (var cell in new[] { first, middle, last })
        {
            cell.ShouldContain("border-left:3px solid #1565c0");
            cell.ShouldContain("border-right:3px solid #1565c0");
        }

        first.ShouldContain("border-top:3px solid #1565c0");
        first.ShouldNotContain("border-bottom:");
        middle.ShouldNotContain("border-top:");
        middle.ShouldNotContain("border-bottom:");
        last.ShouldContain("border-bottom:3px solid #1565c0");
        last.ShouldNotContain("border-top:");
    }

    [Fact]
    public async Task ACellRuleBoxesTheOneCellItNames()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("Highlights", new[]
            {
                new DataGridHighlight<Person>
                {
                    RowPredicate = p => p.Overdue,
                    Column = "Salary",
                    Fill = "#ff5252",
                    BorderColor = "#b71c1c",
                    BorderStyle = DataGridBorderStyle.Dotted
                }
            })
        );

        var cell = RowCells(html, "Alan")[1];
        cell.ShouldContain("border-top:2px dotted #b71c1c");
        cell.ShouldContain("border-right:2px dotted #b71c1c");
        cell.ShouldContain("border-bottom:2px dotted #b71c1c");
        cell.ShouldContain("border-left:2px dotted #b71c1c");

        // Exactly one cell in the whole grid was painted.
        Regex.Matches(html, "border-top:2px dotted").Count.ShouldBe(1);
        Regex.Matches(html, "background-image:linear-gradient").Count.ShouldBe(1);
    }

    [Fact]
    public async Task ScopeIsDerivedFromWhichTargetsAreSet()
    {
        new DataGridHighlight<Person>().Scope.ShouldBe(DataGridHighlightScope.Grid);
        new DataGridHighlight<Person> { Column = "Salary" }.Scope.ShouldBe(DataGridHighlightScope.Column);
        new DataGridHighlight<Person> { RowPredicate = _ => true }.Scope.ShouldBe(DataGridHighlightScope.Row);
        new DataGridHighlight<Person> { Item = People[0] }.Scope.ShouldBe(DataGridHighlightScope.Row);
        new DataGridHighlight<Person> { Item = People[0], Column = "Salary" }.Scope
            .ShouldBe(DataGridHighlightScope.Cell);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ANarrowerScopeWinsAndTheRestOfTheStyleSurvivesIt()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("Highlights", new[]
            {
                // Column-wide wash...
                new DataGridHighlight<Person>
                {
                    Column = "Salary",
                    Fill = "#90caf9",
                    FillOpacity = 1
                },
                // ...and one cell in it that only speaks to the stroke. It must keep the column's fill:
                // the merge is per member group, not per member.
                new DataGridHighlight<Person>
                {
                    RowPredicate = p => p.Overdue,
                    Column = "Salary",
                    BorderColor = "#b71c1c",
                    BorderStyle = DataGridBorderStyle.Solid
                }
            })
        );

        var cell = RowCells(html, "Alan")[1];
        cell.ShouldContain("background-image:linear-gradient(#90caf9,#90caf9)");
        cell.ShouldContain("border-top:2px solid #b71c1c");

        // Every row of the column keeps the wash.
        Regex.Matches(html, Regex.Escape("linear-gradient(#90caf9,#90caf9)")).Count.ShouldBe(3);
    }

    [Fact]
    public async Task ADisabledRuleStaysInTheCollectionAndPaintsNothing()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("Highlights", new[]
            {
                new DataGridHighlight<Person> { Column = "Salary", Fill = "#90caf9", IsEnabled = false }
            })
        );

        html.ShouldNotContain("background-image:linear-gradient");
    }

    [Fact]
    public async Task ExplicitEdgesOverrideTheDerivedPerimeter()
    {
        var html = await RenderAsync(
            ThreeColumns,
            ("RowHighlight", (Func<Person, DataGridCellStyle?>)(p => p.Overdue
                ? new DataGridCellStyle
                {
                    BorderColor = "#d32f2f",
                    BorderStyle = DataGridBorderStyle.Solid,
                    BorderEdges = DataGridBorderEdges.Bottom
                }
                : null))
        );

        var cells = RowCells(html, "Alan");
        foreach (var cell in cells)
        {
            cell.ShouldContain("border-bottom:2px solid #d32f2f");
            cell.ShouldNotContain("border-top:");
            cell.ShouldNotContain("border-left:");
            cell.ShouldNotContain("border-right:");
        }
    }

    [Fact]
    public async Task AColumnCellStyleStillWinsOverEveryHighlight()
    {
        var html = await RenderAsync(
            b =>
            {
                Column<string>(b, 0, x => x.Name);
                Column<decimal>(b, 10, x => x.Salary,
                    ("Highlight", new DataGridCellStyle { TextColor = "#1565c0" }),
                    ("CellStyle", (Func<Person, DataGridCellStyle?>)(p => p.Overdue
                        ? new DataGridCellStyle { TextColor = "#b71c1c", Bold = true }
                        : null)));
                Column<bool>(b, 20, x => x.Overdue);
            },
            ("RowHighlight", (Func<Person, DataGridCellStyle?>)(_ => new DataGridCellStyle { TextColor = "#2e7d32" }))
        );

        RowCells(html, "Alan")[1].ShouldContain("color:#b71c1c");
        // The row rule beats the column's static Highlight for every cell it did not overrule.
        RowCells(html, "Ada")[1].ShouldContain("color:#2e7d32");
    }

    [Theory]
    [InlineData(DataGridHighlightScope.Cell, true, true, true, true, DataGridBorderEdges.All)]
    [InlineData(DataGridHighlightScope.Cell, false, false, false, false, DataGridBorderEdges.All)]
    [InlineData(DataGridHighlightScope.Row, false, false, true, true, DataGridBorderEdges.Top | DataGridBorderEdges.Bottom)]
    [InlineData(DataGridHighlightScope.Column, true, true, false, false, DataGridBorderEdges.Left | DataGridBorderEdges.Right)]
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
        => DataGrid<Person>.EdgesFor(scope, firstColumn, lastColumn, firstRow, lastRow).ShouldBe(expected);
}
