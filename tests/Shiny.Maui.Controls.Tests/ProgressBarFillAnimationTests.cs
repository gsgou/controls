using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The fill slide. Only the paths that resolve synchronously are asserted here — an in-flight
/// <c>Animation.Commit</c> advances on MAUI's ticker, which headless tests do not run, so timing the
/// slide itself would be asserting the absence of a ticker rather than the behaviour. What is
/// covered is every route that must <i>not</i> animate, which is where the bugs are: a bar that
/// re-animates on every measure pass, or runs backwards on a mode change.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ProgressBarFillAnimationTests
{
    public ProgressBarFillAnimationTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    sealed class TestProgressBar : ProgressBar
    {
        /// <summary>Stands in for the layout pass that gives the bar its width.</summary>
        public void LayoutAt(double width) => this.OnSizeAllocated(width, 8);
    }

    static TestProgressBar Bar(double width = 200)
    {
        var bar = new TestProgressBar { AnimateProgress = false };
        bar.LayoutAt(width);
        return bar;
    }


    [Fact]
    public void TheFillIsAnimatedByDefault()
        => new ProgressBar().AnimateProgress.ShouldBeTrue();


    [Fact]
    public void WithAnimationOffTheFillSnapsToTheNewValue()
    {
        var bar = Bar();

        bar.Value = 25;

        bar.CurrentFillWidth.ShouldBe(50);
    }


    [Fact]
    public void AZeroDurationSnaps()
    {
        var bar = Bar();
        bar.AnimateProgress = true;
        bar.ProgressAnimationDuration = 0;

        bar.Value = 75;

        bar.CurrentFillWidth.ShouldBe(150);
    }


    [Fact]
    public void TheFillDrainsAsWellAsFills()
    {
        var bar = Bar();
        bar.Value = 80;

        bar.Value = 20;

        bar.CurrentFillWidth.ShouldBe(40);
    }


    /// <summary>
    /// The bar being measured is not the value changing. Animating here would make the fill visibly
    /// re-run every time its container resized.
    /// </summary>
    [Fact]
    public void ALayoutPassSnapsRatherThanAnimating()
    {
        var bar = new TestProgressBar { AnimateProgress = true, Value = 50 };

        bar.LayoutAt(400);

        bar.CurrentFillWidth.ShouldBe(200);
    }


}
