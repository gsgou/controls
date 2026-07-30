using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Cover for the guard failing on <b>subclassed</b> controls.
///
/// <para>
/// <see cref="ImplicitStyleConstructionTests"/> only ever constructs controls directly, so it
/// could not see this: <see cref="StyleGuard.MarkReady"/> used to refuse to mark anything
/// unless the object's runtime type matched the type whose constructor was calling. Every base
/// constructor in the chain therefore no-op'd, and a control designed to be subclassed - which
/// is the only way <see cref="ShinyContentPage"/> is ever used from XAML - never had its queue
/// flushed. The page's own <c>PageContent</c> assignment stayed parked in the queue forever and
/// the page rendered blank, with no exception to point at it.
/// </para>
///
/// <para>
/// Readiness is now tracked per level of the hierarchy, so a base control's callbacks replay
/// when the base constructor ends regardless of what the runtime type turns out to be.
/// </para>
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class SubclassedControlConstructionTests
{
    /// <summary>Stands in for an app's <c>MyPage : ShinyContentPage</c> XAML page.</summary>
    sealed class SubclassedPage : ShinyContentPage
    {
        public SubclassedPage(View content)
            // InitializeComponent() assigns PageContent for a real XAML page; this is the
            // same assignment, arriving after the base constructor has run.
            => this.PageContent = content;
    }

    [Fact]
    public void SubclassedPage_PutsPageContentInTheVisualTree()
    {
        new Application();

        var content = new Label { Text = "hello" };
        var page = new SubclassedPage(content);

        var root = (Grid)((ContentPage)page).Content!;
        root.Children.ShouldContain(content);
    }

    [Fact]
    public void SubclassedPage_UnderAnImplicitStyle_StillPutsPageContentInTheVisualTree()
    {
        // The implicit style is what forces the callbacks to fire mid-construction, before the
        // base constructor has built rootGrid.
        var app = new Application();
        app.Resources.Add(new Style(typeof(SubclassedPage))
        {
            Setters =
            {
                new Setter { Property = ShinyContentPage.BackdropColorProperty, Value = Colors.Blue },
                new Setter { Property = ShinyContentPage.LoadingMessageProperty, Value = "working" }
            }
        });

        var content = new Label { Text = "hello" };
        var page = new SubclassedPage(content);

        var root = (Grid)((ContentPage)page).Content!;
        root.Children.ShouldContain(content);
        page.OverlayHost.BackdropColor.ShouldBe(Colors.Blue);
        page.LoadingOverlay.Message.ShouldBe("working");
    }

    [Fact]
    public void DirectPage_StillPutsPageContentInTheVisualTree()
    {
        new Application();

        var content = new Label { Text = "hello" };
        var page = new ShinyContentPage { PageContent = content };

        var root = (Grid)((ContentPage)page).Content!;
        root.Children.ShouldContain(content);
    }

    [Fact]
    public void BaseLevelIsReady_EvenWhenTheRuntimeTypeIsASubclass()
    {
        new Application();

        var page = new SubclassedPage(new Label());

        StyleGuard.IsReady(page, typeof(ShinyContentPage)).ShouldBeTrue();
        // The subclass declares no guarded properties, so it never marks a level of its own -
        // and must not need to.
        StyleGuard.IsReady(page, typeof(SubclassedPage)).ShouldBeFalse();
    }

    /// <summary>
    /// A base level is flushed by the base constructor and is unaffected by whether any
    /// more-derived constructor ever marks. This is the whole point: the base's own callbacks
    /// touch only the base's own fields.
    /// </summary>
    [Fact]
    public void BaseLevelFlushIsIndependentOfTheDerivedLevel()
    {
        var control = new Label();
        var ran = new List<string>();

        StyleGuard.WhenReady(control, typeof(View), () => ran.Add("base"));

        StyleGuard.MarkReady(control, typeof(View));
        ran.ShouldBe(["base"]);

        // The derived level never being marked does not retroactively affect the base.
        StyleGuard.IsReady(control, typeof(Label)).ShouldBeFalse();
        ran.ShouldBe(["base"]);
    }

    /// <summary>
    /// The generic overload takes its level from <c>T</c>, so it is subclass-safe too.
    /// </summary>
    [Fact]
    public void GenericGuard_ScopesToItsTypeArgument()
    {
        var page = new SubclassedPage(new Label());
        var ran = false;

        // ShinyContentPage's level was marked by its own constructor, so this runs now.
        StyleGuard.WhenReady<ShinyContentPage>(page, _ => ran = true);

        ran.ShouldBeTrue();
    }

    [Fact]
    public void ScopedGuard_ReplaysInArrivalOrder()
    {
        var control = new Label();
        var ran = new List<int>();

        StyleGuard.WhenReady(control, typeof(ShinyContentPage), () => ran.Add(1));
        StyleGuard.WhenReady(control, typeof(ShinyContentPage), () => ran.Add(2));
        StyleGuard.WhenReady(control, typeof(ShinyContentPage), () => ran.Add(3));

        StyleGuard.MarkReady(control, typeof(ShinyContentPage));

        ran.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ScopedGuard_RunsImmediatelyOnceItsLevelIsReady()
    {
        var control = new Label();
        var ran = false;

        StyleGuard.MarkReady(control, typeof(ShinyContentPage));
        StyleGuard.WhenReady(control, typeof(ShinyContentPage), () => ran = true);

        ran.ShouldBeTrue();
    }

    [Fact]
    public void ScopedGuard_LevelsAreIndependent()
    {
        var control = new Label();
        var ran = new List<string>();

        StyleGuard.WhenReady(control, typeof(ShinyContentPage), () => ran.Add("base"));
        StyleGuard.WhenReady(control, typeof(SubclassedPage), () => ran.Add("derived"));

        StyleGuard.MarkReady(control, typeof(ShinyContentPage));
        ran.ShouldBe(["base"]);

        StyleGuard.MarkReady(control, typeof(SubclassedPage));
        ran.ShouldBe(["base", "derived"]);
    }
}
