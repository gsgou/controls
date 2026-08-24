using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
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
/// The formatting unit tests prove the helpers return the right strings; these prove the grid
/// actually puts those strings - and the alignment/wrap/style classes - into the rendered
/// <c>&lt;td&gt;</c>. That wiring is where a Razor attribute silently stops being applied, so it is
/// worth rendering for real rather than trusting the helper in isolation.
/// </summary>
public class DataGridFormattingRenderTests
{
    enum ReviewState
    {
        [Description("Signed off")]
        SignedOff,

        AwaitingReview
    }

    class Person
    {
        public string Name { get; set; } = "";
        public decimal Salary { get; set; }
        public double Rate { get; set; }
        public bool Active { get; set; }
        public DateTime? Reviewed { get; set; }
        public ReviewState State { get; set; }
        public long Size { get; set; }
    }

    static readonly Person[] People =
    [
        new() { Name = "Ada", Salary = 142000, Rate = 0.61, Active = true, Reviewed = new DateTime(2024, 3, 4), State = ReviewState.SignedOff, Size = 1536 },
        new() { Name = "Alan", Salary = 98000, Rate = 0.42, Active = false, Reviewed = null, State = ReviewState.AwaitingReview, Size = 512 },
    ];

    /// <summary>Static rendering never reaches OnAfterRender, so the grid's JS is never called.</summary>
    sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }

    static async Task<string> RenderAsync(Action<RenderTreeBuilder> columns)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IJSRuntime>(new NoJs());
        var provider = services.BuildServiceProvider();

        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Items"] = People,
                ["Columns"] = (RenderFragment)(b => columns(b))
            });
            var output = await renderer.RenderComponentAsync<DataGrid<Person>>(parameters);
            // HtmlRenderer escapes every non-ASCII character, so "\u2713" arrives as "&#x2713;".
            // Decode once here and the assertions can read like the text a user actually sees.
            return System.Net.WebUtility.HtmlDecode(output.ToHtmlString());
        });
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

    [Fact]
    public async Task PresetsReachTheRenderedCells()
    {
        var html = await RenderAsync(b =>
        {
            Column<decimal>(b, 0, x => x.Salary, ("DisplayAs", DataGridColumnFormat.Currency), ("Decimals", 0));
            Column<double>(b, 10, x => x.Rate, ("DisplayAs", DataGridColumnFormat.Percent), ("Decimals", 0));
            Column<long>(b, 20, x => x.Size, ("DisplayAs", DataGridColumnFormat.FileSize));
            Column<bool>(b, 30, x => x.Active, ("DisplayAs", DataGridColumnFormat.Boolean));
            Column<ReviewState>(b, 40, x => x.State, ("DisplayAs", DataGridColumnFormat.Enum));
            Column<DateTime?>(b, 50, x => x.Reviewed, ("DisplayAs", DataGridColumnFormat.Date), ("NullText", "—"));
        });

        html.ShouldContain("$142,000");
        html.ShouldContain("61%");
        html.ShouldContain("1.5 KB");
        html.ShouldContain("✓");
        html.ShouldContain("✗");
        html.ShouldContain("Signed off");
        html.ShouldContain("Awaiting Review");
        html.ShouldContain("3/4/2024");
        html.ShouldContain("—");
    }

    [Fact]
    public async Task NumericColumnsRightAlignThemselvesHeaderIncluded()
    {
        var html = await RenderAsync(b =>
        {
            Column<string>(b, 0, x => x.Name);
            Column<decimal>(b, 10, x => x.Salary, ("DisplayAs", DataGridColumnFormat.Currency));
        });

        // Two data rows plus the header, all right-aligned; the name column stays at the default.
        System.Text.RegularExpressions.Regex.Matches(html, "shiny-dg-align-end").Count.ShouldBe(3);
    }

    [Fact]
    public async Task CellStyleAndWrappingReachTheCellAttributes()
    {
        var html = await RenderAsync(b =>
        {
            Column<string>(b, 0, x => x.Name, ("Wrap", true), ("MaxLines", 2));
            Column<decimal>(b, 10, x => x.Salary,
                ("DisplayAs", DataGridColumnFormat.Currency),
                ("CellStyle", (Func<Person, DataGridCellStyle?>)(p => p.Salary < 100000
                    ? new DataGridCellStyle { TextColor = "#c62828", Bold = true, CssClass = "underpaid" }
                    : null)));
        });

        html.ShouldContain("shiny-dg-wrap");
        html.ShouldContain("-webkit-line-clamp:2");
        html.ShouldContain("underpaid");
        html.ShouldContain("color:#c62828");
        html.ShouldContain("font-weight:600");

        // Only the one row under the threshold is styled.
        System.Text.RegularExpressions.Regex.Matches(html, "underpaid").Count.ShouldBe(1);
    }

    [Fact]
    public async Task PrefixSuffixAndTextFormatterReachTheCells()
    {
        var html = await RenderAsync(b =>
        {
            Column<decimal>(b, 0, x => x.Salary, ("StringFormat", "N0"), ("Prefix", "≈ "), ("Suffix", " USD"));
            Column<double>(b, 10, x => x.Rate,
                ("TextFormatter", (Func<double, string?>)(r => r > 0.5 ? "High" : "Low")));
        });

        html.ShouldContain("≈ 142,000 USD");
        html.ShouldContain("High");
        html.ShouldContain("Low");
    }
}
