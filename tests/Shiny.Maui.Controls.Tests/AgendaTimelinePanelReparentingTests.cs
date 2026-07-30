using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Scheduler.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// <see cref="Shiny.Maui.Controls.Scheduler.SchedulerAgendaView"/> owns a single
/// <see cref="CurrentTimeIndicator"/> but rebuilds its panels (BuildColumns discards them and creates
/// new ones whenever the day count, timezones or columns change). The new panel then adopts that same
/// indicator while the previous panel's layer still has it parented, and on Android adding an
/// already-parented view throws <c>IllegalStateException: The specified child already has a parent</c>
/// - which, escaping the <c>async void</c> loader, killed the whole app.
///
/// These assert the managed invariant behind that crash: an indicator only ever has one parent.
/// </summary>
public class AgendaTimelinePanelReparentingTests
{
    static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void AdoptingTheIndicatorDetachesItFromThePreviousPanel()
    {
        _ = new Application();

        var indicator = new CurrentTimeIndicator();
        var first = new AgendaTimelinePanel();
        var second = new AgendaTimelinePanel();

        first.Build(Today, [], indicator, showTimeMarker: true);
        var firstParent = indicator.Parent;
        firstParent.ShouldNotBeNull("the first panel should have taken the indicator");

        // The rebuild case: a brand-new panel adopts the same indicator instance.
        second.Build(Today, [], indicator, showTimeMarker: true);

        indicator.Parent.ShouldNotBe(firstParent, "the indicator must move to the new panel");
        ParentChainContains(indicator, second).ShouldBeTrue();
        ParentChainContains(indicator, first).ShouldBeFalse("the old panel must have released it");
    }

    [Fact]
    public void RebuildingTheSamePanelRepeatedlyKeepsOneParent()
    {
        _ = new Application();

        var indicator = new CurrentTimeIndicator();
        var panel = new AgendaTimelinePanel();

        for (var i = 0; i < 5; i++)
            panel.Build(Today, [], indicator, showTimeMarker: true);

        indicator.Parent.ShouldNotBeNull();
        ParentChainContains(indicator, panel).ShouldBeTrue();
    }

    [Fact]
    public void RebuildDoesNotStackBackgroundTapRecognizers()
    {
        _ = new Application();

        var panel = new AgendaTimelinePanel();
        for (var i = 0; i < 4; i++)
            panel.Build(Today, [], null, showTimeMarker: false);

        // The events layer is the only descendant carrying the background tap; it is attached once,
        // not once per Build (which previously fired TimeSlotTapped repeatedly).
        var layers = Descendants(panel).OfType<Layout>().ToList();
        foreach (var layer in layers)
            layer.GestureRecognizers.OfType<TapGestureRecognizer>().Count()
                .ShouldBeLessThanOrEqualTo(1, $"{layer.GetType().Name} accumulated tap recognizers");
    }

    static bool ParentChainContains(Element element, Element ancestor)
    {
        for (var p = element.Parent; p is not null; p = p.Parent)
        {
            if (ReferenceEquals(p, ancestor))
                return true;
        }
        return false;
    }

    static IEnumerable<Element> Descendants(Element root)
    {
        foreach (var child in root.LogicalChildrenInternal())
        {
            yield return child;
            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }
}

static class ElementChildrenExtensions
{
    /// <summary>Walks whichever child collection the element exposes publicly.</summary>
    public static IEnumerable<Element> LogicalChildrenInternal(this Element element) => element switch
    {
        Layout layout => layout.Children.OfType<Element>(),
        ContentView content => content.Content is null ? [] : [content.Content],
        _ => []
    };
}
