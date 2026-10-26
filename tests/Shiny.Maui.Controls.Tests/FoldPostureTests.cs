using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Flyout;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>A posture source a test can move by hand.</summary>
sealed class FakeFoldSource : IFoldPostureSource
{
    FoldInfo current = FoldInfo.None;

    public FoldInfo Current
    {
        get => this.current;
        set
        {
            this.current = value;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Changed;

    public static FoldInfo Book(double x, double width = 0, double height = 2000, Thickness margin = default)
        => new(FoldPostureState.HalfOpen, FoldPostureOrientation.Vertical, new Rect(x, 0, width, height), margin);

    public static FoldInfo Tabletop(double y, double height = 0, double width = 2000)
        => new(FoldPostureState.HalfOpen, FoldPostureOrientation.Horizontal, new Rect(0, y, width, height));
}


/// <summary>
/// The controls driven by a fake posture source. The source is process-wide (FoldPosture is static),
/// so these run in the serial collection and always put the source back.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public sealed class FoldPostureTests : IDisposable
{
    readonly FakeFoldSource source = new();

    public FoldPostureTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
        FoldPosture.SetSource(this.source);
    }

    public void Dispose() => FoldPosture.SetSource(null);


    static (FlyoutView View, FlyoutPanel Panel) Flyout(
        FlyoutPresentation presentation = FlyoutPresentation.Auto,
        FlyoutPanelState state = FlyoutPanelState.Expanded,
        FlyoutSide side = FlyoutSide.Start,
        Action<FlyoutPanel>? configure = null)
    {
        var panel = new FlyoutPanel { Presentation = presentation, State = state, PanelContent = new BoxView(), ExpandedWidth = 280 };
        configure?.Invoke(panel);

        var view = new FlyoutView { Content = new BoxView(), IsAnimationEnabled = false };
        if (side == FlyoutSide.Start)
            view.Start = panel;
        else
            view.End = panel;

        return (view, panel);
    }


    /// <summary>
    /// Sets the view's own frame (which is what the fold tracker measures) and then runs the
    /// cross-platform layout pass, twice so a snap found during the first pass is arranged.
    /// </summary>
    static void Layout(FlyoutView view, double width = 840, double height = 600)
    {
        ((IView)view).Arrange(new Rect(0, 0, width, height));
        for (var i = 0; i < 2; i++)
        {
            var layout = (Microsoft.Maui.ILayout)view;
            layout.CrossPlatformMeasure(width, height);
            layout.CrossPlatformArrange(new Rect(0, 0, width, height));
        }
    }


    [Fact]
    public void NoSourceMeansNoFoldEvenWhenOneIsReported()
    {
        FoldPosture.SetSource(null);
        var (view, panel) = Flyout();
        Layout(view);

        FoldPosture.HasSource.ShouldBeFalse();
        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
        panel.Width.ShouldBe(280);
    }

    [Fact]
    public void ABookFoldSnapsTheExpandedPanelToTheFold()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, panel) = Flyout();
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeTrue();
        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(420);
        view.ContentBounds.X.ShouldBe(420);
        view.ContentBounds.Width.ShouldBe(420, "snapped content is narrowed into the far segment, not shifted");
        panel.ExpandedWidth.ShouldBe(280, "snapping never writes the user's width");
    }

    [Fact]
    public void TheHingeMarginBecomesTheGutter()
    {
        this.source.Current = FakeFoldSource.Book(420, margin: new Thickness(10, 0));
        var (view, _) = Flyout();
        Layout(view);

        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(410);
        view.ContentBounds.X.ShouldBe(430);
    }

    [Fact]
    public void SnappingIgnoresMinAndMaxWidth()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(configure: p => { p.MaxExpandedWidth = 300; p.MinExpandedWidth = 200; });
        Layout(view);

        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(420);
    }

    [Fact]
    public void SnappingForcesPushOverAnOverlayPresentation()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Overlay);
        Layout(view);

        view.GetEffectivePresentation(FlyoutSide.Start).ShouldBe(FlyoutPresentation.Push);
        view.ScrimProgress.ShouldBe(0);
    }

    [Fact]
    public void FoldBehaviorNoneLeavesThePanelAlone()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Push, configure: p => p.FoldBehavior = FoldBehavior.None);
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(280);
    }

    [Fact]
    public void ATabletopFoldLeavesTheFlyoutAlone()
    {
        this.source.Current = FakeFoldSource.Tabletop(300);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Push);
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(280);
    }

    [Fact]
    public void ACollapsedRailDoesNotSnap()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Push, state: FlyoutPanelState.Collapsed);
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
    }

    [Fact]
    public void TheEndPanelMirrors()
    {
        this.source.Current = FakeFoldSource.Book(500);
        var (view, _) = Flyout(side: FlyoutSide.End);
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.End).ShouldBeTrue();
        view.GetCurrentWidth(FlyoutSide.End).ShouldBe(340);
        view.ContentBounds.X.ShouldBe(0);
        view.ContentBounds.Width.ShouldBe(500);
    }

    [Fact]
    public void OpeningFlatRestoresTheUsersWidth()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Push);
        Layout(view);
        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(420);

        this.source.Current = new FoldInfo(FoldPostureState.Flat, FoldPostureOrientation.Vertical, null);
        Layout(view);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
        view.GetCurrentWidth(FlyoutSide.Start).ShouldBe(280);
    }

    [Fact]
    public void AFoldOutsideTheFlyoutIsIgnored()
    {
        // The flyout is 400 wide; the fold is at 420 in the window, beyond it.
        this.source.Current = FakeFoldSource.Book(420);
        var (view, _) = Flyout(presentation: FlyoutPresentation.Push);
        Layout(view, width: 400);

        view.IsSnappedToFold(FlyoutSide.Start).ShouldBeFalse();
    }


    [Fact]
    public void OverlayContentMovesIntoTheLargerSegment()
    {
        this.source.Current = FakeFoldSource.Book(300);
        var overlay = new Overlay();
        ((IView)overlay).Arrange(new Rect(0, 0, 800, 400));

        ((View)overlay.Content).Margin.ShouldBe(new Thickness(300, 0, 0, 0));

        this.source.Current = FoldInfo.None;
        ((View)overlay.Content).Margin.ShouldBe(new Thickness(0));
    }

    [Fact]
    public void OverlayContentGoesAboveATabletopFold()
    {
        this.source.Current = FakeFoldSource.Tabletop(250);
        var overlay = new Overlay();
        ((IView)overlay).Arrange(new Rect(0, 0, 800, 400));

        ((View)overlay.Content).Margin.ShouldBe(new Thickness(0, 0, 0, 150));
    }


    [Fact]
    public void GanttLinesItsSplitterUpWithTheFold()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var gantt = new Gantt.GanttView();
        ((IView)gantt).Arrange(new Rect(0, 0, 840, 600));

        gantt.IsSnappedToFold.ShouldBeTrue();
        gantt.EffectiveTaskPaneWidth.ShouldBe(417, "a 6-wide splitter centred on the fold");
        gantt.TaskPaneWidth.ShouldBe(260);

        this.source.Current = FoldInfo.None;
        gantt.IsSnappedToFold.ShouldBeFalse();
        gantt.EffectiveTaskPaneWidth.ShouldBe(260);
    }


    [Fact]
    public void FoldTrackerReportsTheFoldInTheReferenceViewsCoordinates()
    {
        this.source.Current = FakeFoldSource.Book(420);
        var view = new ContentView();
        FoldTracker? seen = null;
        var tracker = FoldTracker.Attach(view, t => seen = t);
        tracker.ShouldNotBeNull();

        ((IView)view).Arrange(new Rect(100, 0, 600, 400));

        seen.ShouldNotBeNull();
        tracker.Fold.ShouldBe(new Rect(320, 0, 0, 400));
        tracker.IsVertical.ShouldBeTrue();
    }

    [Fact]
    public void AnExplicitSourceWinsOverADefaultOne()
    {
        var other = new FakeFoldSource { Current = FakeFoldSource.Book(10) };
        FoldPosture.SetSource(other, isDefault: true);

        FoldPosture.Current.ShouldBe(this.source.Current, "the explicit source registered in the constructor stays");

        FoldPosture.SetSource(null);
        FoldPosture.SetSource(other, isDefault: true);
        FoldPosture.Current.Fold!.Value.X.ShouldBe(10, "with nothing explicit, the default fills the slot");
    }
}
