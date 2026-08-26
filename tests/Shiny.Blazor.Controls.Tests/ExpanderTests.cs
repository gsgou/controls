using Shiny.Blazor.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

public class ExpanderTests
{
    [Fact]
    public async Task TogglesBetweenOpenAndClosed()
    {
        var expander = new Expander { HeaderText = "Shipping" };

        expander.IsExpanded.ShouldBeFalse();

        await expander.ToggleAsync();
        expander.IsExpanded.ShouldBeTrue();

        await expander.ToggleAsync();
        expander.IsExpanded.ShouldBeFalse();
    }


    [Fact]
    public async Task RaisesOpenAndCloseSeparately()
    {
        var log = new List<string>();
        var expander = new Expander
        {
            OnExpanded = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => log.Add("open")),
            OnCollapsed = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => log.Add("close"))
        };

        await expander.ExpandAsync();
        await expander.CollapseAsync();

        log.ShouldBe(["open", "close"]);
    }


    [Fact]
    public async Task ANoOpChangeIsSilent()
    {
        var opens = 0;
        var expander = new Expander
        {
            IsExpanded = true,
            OnExpanded = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => opens++)
        };

        await expander.ExpandAsync();

        opens.ShouldBe(0);
    }


    [Fact]
    public async Task AnItemInNoAccordionAnswersToItself()
    {
        var expander = new Expander();

        await expander.ExpandAsync();

        expander.IsExpanded.ShouldBeTrue();
        expander.AccordionHost.ShouldBeNull();
    }
}
