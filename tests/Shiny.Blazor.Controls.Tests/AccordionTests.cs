using Shiny.Blazor.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// The accordion's rules run entirely in the component, not the renderer, so they can be driven
/// directly: register a few expanders against the host interface and toggle them.
/// </summary>
public class AccordionTests
{
    static (Accordion Accordion, IAccordionHost Host, List<Expander> Items) Build(
        AccordionSelectionMode mode = AccordionSelectionMode.Single,
        bool allowCollapseAll = true,
        int count = 3
    )
    {
        var accordion = new Accordion { SelectionMode = mode, AllowCollapseAll = allowCollapseAll };
        var host = (IAccordionHost)accordion;
        var items = new List<Expander>();

        for (var i = 0; i < count; i++)
        {
            var item = new Expander { AccordionHost = accordion, HeaderText = "Item " + i };
            host.Register(item);
            items.Add(item);
        }

        return (accordion, host, items);
    }


    [Fact]
    public async Task SingleModeClosesWhateverElseWasOpen()
    {
        var (_, _, items) = Build();

        await items[0].ExpandAsync();
        await items[2].ExpandAsync();

        items[0].IsExpanded.ShouldBeFalse();
        items[1].IsExpanded.ShouldBeFalse();
        items[2].IsExpanded.ShouldBeTrue();
    }


    [Fact]
    public async Task MultipleModeLeavesTheOthersAlone()
    {
        var (accordion, _, items) = Build(AccordionSelectionMode.Multiple);

        await items[0].ExpandAsync();
        await items[2].ExpandAsync();

        accordion.ExpandedIndexes.ShouldBe([0, 2]);
    }


    [Fact]
    public async Task ExpandedIndexFollowsWhicheverIsOpen()
    {
        var (accordion, _, items) = Build();

        await items[1].ExpandAsync();
        accordion.ExpandedIndex.ShouldBe(1);

        await items[1].CollapseAsync();
        accordion.ExpandedIndex.ShouldBe(-1);
    }


    [Fact]
    public async Task RefusingToCollapseAllKeepsTheLastOneOpen()
    {
        var (_, _, items) = Build(allowCollapseAll: false);

        await items[1].ExpandAsync();
        await items[1].CollapseAsync();

        items[1].IsExpanded.ShouldBeTrue();
    }


    [Fact]
    public async Task TheLastOpenItemLosesItsCollapseAffordance()
    {
        var (_, _, items) = Build(allowCollapseAll: false);

        await items[1].ExpandAsync();

        items[1].CanCollapse.ShouldBeFalse();
        items[0].CanCollapse.ShouldBeTrue();
    }


    [Fact]
    public async Task CollapsingIsFineWhenSomethingElseIsStillOpen()
    {
        var (_, _, items) = Build(AccordionSelectionMode.Multiple, allowCollapseAll: false);

        await items[0].ExpandAsync();
        await items[1].ExpandAsync();

        items[0].CanCollapse.ShouldBeTrue();

        await items[0].CollapseAsync();
        items[0].IsExpanded.ShouldBeFalse();
    }


    [Fact]
    public void ExpandItemOpensByIndex()
    {
        var (accordion, _, items) = Build();

        accordion.ExpandItem(1).ShouldBeTrue();
        items[1].IsExpanded.ShouldBeTrue();

        accordion.ExpandItem(99).ShouldBeFalse();
    }


    [Fact]
    public void ExpandAllIsIgnoredInSingleMode()
    {
        var (accordion, _, _) = Build();

        accordion.ExpandAll();

        accordion.ExpandedIndexes.ShouldBeEmpty();
    }


    [Fact]
    public void ExpandAllOpensEverythingInMultipleMode()
    {
        var (accordion, _, _) = Build(AccordionSelectionMode.Multiple);

        accordion.ExpandAll();

        accordion.ExpandedIndexes.ShouldBe([0, 1, 2]);
    }


    [Fact]
    public async Task ItemEventsCarryTheIndexAndTheModel()
    {
        AccordionItemChangedEventArgs? seen = null;
        var accordion = new Accordion
        {
            OnItemExpanded = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<AccordionItemChangedEventArgs>(
                new object(), a => seen = a
            )
        };
        var host = (IAccordionHost)accordion;

        var first = new Expander { AccordionHost = accordion, Item = "Alpha" };
        var second = new Expander { AccordionHost = accordion, Item = "Beta" };
        host.Register(first);
        host.Register(second);

        await second.ExpandAsync();

        seen.ShouldNotBeNull();
        seen.Index.ShouldBe(1);
        seen.Data.ShouldBe("Beta");
        seen.IsExpanded.ShouldBeTrue();
    }


    [Fact]
    public async Task ClosingTheOthersDoesNotEchoAsItemEvents()
    {
        var collapses = 0;
        var accordion = new Accordion
        {
            OnItemCollapsed = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<AccordionItemChangedEventArgs>(
                new object(), _ => collapses++
            )
        };
        var host = (IAccordionHost)accordion;

        var first = new Expander { AccordionHost = accordion };
        var second = new Expander { AccordionHost = accordion };
        host.Register(first);
        host.Register(second);

        await first.ExpandAsync();
        await second.ExpandAsync();

        // First closing is a consequence of second opening, not an event of its own.
        collapses.ShouldBe(0);
    }


    [Fact]
    public async Task UnregisteringDropsTheItem()
    {
        var (accordion, host, items) = Build();

        await items[1].ExpandAsync();
        host.Unregister(items[1]);

        accordion.ItemViews.Count.ShouldBe(2);
        accordion.ItemViews.ShouldNotContain(items[1]);
    }
}
