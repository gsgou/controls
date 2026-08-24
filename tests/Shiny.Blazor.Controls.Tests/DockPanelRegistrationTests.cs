using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Blazor.Controls.Docking;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// How a panel is registered decides whether the user can close it, and closing the wrong one
/// strands a layout that nothing in the app can rebuild. The default has to stay permissive - every
/// panel registered before this existed must remain closable - and the opt-out has to actually
/// reach the host rather than only the tab strip.
/// </summary>
public class DockPanelRegistrationTests
{
    sealed class TestPanel : ComponentBase;

    static IDockableContentFactory Resolve(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        services.AddShinyDocking();
        register(services);

        return services.BuildServiceProvider().GetRequiredService<IDockableContentFactory>();
    }

    [Fact]
    public void PanelsAreClosableByDefault()
    {
        var factory = Resolve(x => x.AddDockPanel<TestPanel>("output"));

        factory.CanClose.ShouldBeTrue();
    }

    [Fact]
    public void PanelCanBeRegisteredAsUnclosable()
    {
        var factory = Resolve(x => x.AddDockPanel<TestPanel>("explorer-tree", canClose: false));

        factory.CanClose.ShouldBeFalse();
    }

    [Fact]
    public void ClosabilityDoesNotDisturbTheOtherMetadata()
    {
        var factory = Resolve(x => x.AddDockPanel<TestPanel>("explorer-tree", "Folders", "📁", canClose: false));

        factory.PanelTypeId.ShouldBe("explorer-tree");
        factory.DisplayName.ShouldBe("Folders");
        factory.Icon.ShouldBe("📁");
        factory.CanClose.ShouldBeFalse();
    }

    [Fact]
    public void TheRegistryReportsWhatWasRegistered()
    {
        var services = new ServiceCollection();
        services.AddShinyDocking();
        services.AddDockPanel<TestPanel>("pinned", canClose: false);
        services.AddDockPanel<TestPanel>("output");

        var registry = services.BuildServiceProvider().GetRequiredService<DockableContentRegistry>();

        // the host asks the registry, not the service collection - so this is the lookup that has
        // to carry the flag through
        registry.Resolve("pinned")!.CanClose.ShouldBeFalse();
        registry.Resolve("output")!.CanClose.ShouldBeTrue();
    }

    /// <summary>
    /// A layout naming a panel this app no longer registers has to stay closable, or the user is
    /// left with a tab they cannot read and cannot get rid of.
    /// </summary>
    [Fact]
    public void AnUnknownPanelTypeIsNotResolved()
    {
        var services = new ServiceCollection();
        services.AddShinyDocking();

        var registry = services.BuildServiceProvider().GetRequiredService<DockableContentRegistry>();

        registry.Resolve("never-registered").ShouldBeNull();
    }
}
