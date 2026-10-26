using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The fold maths every fold-aware control shares. Pure geometry: no views, no posture source, so
/// these run in parallel with everything else.
/// </summary>
public class FoldLayoutTests
{
    static readonly Rect Host = new(0, 0, 800, 400);

    // ---- FoldIn ----------------------------------------------------------------------------------

    [Fact]
    public void FoldInTranslatesAWindowFoldIntoTheView()
    {
        // A view starting 100 into the window, a zero-width fold at window x 420.
        var fold = FoldLayout.FoldIn(new Rect(100, 50, 600, 300), new Rect(420, 0, 0, 1000));

        fold.ShouldNotBeNull();
        fold.Value.X.ShouldBe(320);
        fold.Value.Width.ShouldBe(0);
        fold.Value.Y.ShouldBe(0);
        fold.Value.Height.ShouldBe(300, "clipped to the view's own height");
    }

    [Fact]
    public void FoldInIsNullWhenTheFoldMissesTheView()
        => FoldLayout.FoldIn(new Rect(0, 0, 400, 800), new Rect(420, 0, 0, 800)).ShouldBeNull();

    [Fact]
    public void FoldInIsNullWhenTheFoldOnlyGrazesAnEdge()
        // centre line on the view's right edge: one segment, no division
        => FoldLayout.FoldIn(new Rect(0, 0, 400, 800), new Rect(390, 0, 20, 800)).ShouldBeNull();

    [Fact]
    public void FoldInIsNullWithNoFold()
        => FoldLayout.FoldIn(Host, null).ShouldBeNull();

    [Fact]
    public void FoldInHandlesAHorizontalFold()
    {
        var fold = FoldLayout.FoldIn(new Rect(0, 100, 800, 600), new Rect(0, 400, 800, 0));

        fold.ShouldNotBeNull();
        FoldLayout.IsVertical(fold.Value).ShouldBeFalse();
        fold.Value.Y.ShouldBe(300);
    }

    // ---- SegmentFor ------------------------------------------------------------------------------

    [Fact]
    public void NoFoldMeansTheWholeHost()
        => FoldLayout.SegmentFor(Host, null, FoldPreference.Top).ShouldBe(Host);

    [Fact]
    public void VerticalFoldTakesTheLargerSegment()
    {
        var segment = FoldLayout.SegmentFor(Host, new Rect(300, 0, 0, 400), FoldPreference.Top);
        segment.ShouldBe(new Rect(300, 0, 500, 400));
    }

    [Fact]
    public void EqualSegmentsGoLeadingInLeftToRight()
        => FoldLayout.SegmentFor(Host, new Rect(400, 0, 0, 400), FoldPreference.Larger)
            .ShouldBe(new Rect(0, 0, 400, 400));

    [Fact]
    public void EqualSegmentsGoToTheRightInRightToLeft()
        => FoldLayout.SegmentFor(Host, new Rect(400, 0, 0, 400), FoldPreference.Larger, rightToLeft: true)
            .ShouldBe(new Rect(400, 0, 400, 400));

    [Fact]
    public void LeadingAndTrailingAreHonouredOnABook()
    {
        var fold = new Rect(300, 0, 0, 400);
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Leading).ShouldBe(new Rect(0, 0, 300, 400));
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Trailing).ShouldBe(new Rect(300, 0, 500, 400));
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Leading, rightToLeft: true).ShouldBe(new Rect(300, 0, 500, 400));
    }

    [Fact]
    public void TabletopPutsTopContentAboveAndBottomContentBelow()
    {
        var fold = new Rect(0, 250, 800, 0);
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Top).ShouldBe(new Rect(0, 0, 800, 250));
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Bottom).ShouldBe(new Rect(0, 250, 800, 150));
        FoldLayout.SegmentFor(Host, fold, FoldPreference.Larger).ShouldBe(new Rect(0, 0, 800, 250));
    }

    [Fact]
    public void AHingeWithWidthIsLeftOutOfBothSegments()
    {
        var segments = FoldLayout.Segments(Host, new Rect(390, 0, 20, 400));

        segments.ShouldNotBeNull();
        segments.Value.First.ShouldBe(new Rect(0, 0, 390, 400));
        segments.Value.Second.ShouldBe(new Rect(410, 0, 390, 400));
    }

    [Fact]
    public void SegmentMarginIsZeroWithNoFold()
        => FoldLayout.SegmentMargin(new Size(800, 400), null, FoldPreference.Top).ShouldBe(default(Thickness));

    [Fact]
    public void SegmentMarginConfinesAChildToTheSegment()
        => FoldLayout.SegmentMargin(new Size(800, 400), new Rect(300, 0, 0, 400), FoldPreference.Top)
            .ShouldBe(new Thickness(300, 0, 0, 0));

    [Fact]
    public void SegmentContainingFollowsThePoint()
    {
        var fold = new Rect(390, 0, 20, 400);
        FoldLayout.SegmentContaining(Host, fold, new Point(100, 100)).ShouldBe(new Rect(0, 0, 390, 400));
        FoldLayout.SegmentContaining(Host, fold, new Point(700, 100)).ShouldBe(new Rect(410, 0, 390, 400));
    }

    // ---- two-pane helpers ------------------------------------------------------------------------

    [Fact]
    public void FlyoutSnapPutsTheLeftPanelEdgeOnTheFold()
    {
        var snap = FoldLayout.FlyoutSnap(840, new Rect(400, 0, 40, 600), panelOnLeft: true);

        snap.ShouldNotBeNull();
        snap.Value.PanelWidth.ShouldBe(400);
        snap.Value.Inset.ShouldBe(440, "the content starts on the far side of the hinge");
    }

    [Fact]
    public void FlyoutSnapMirrorsForTheRightPanel()
    {
        var snap = FoldLayout.FlyoutSnap(840, new Rect(400, 0, 40, 600), panelOnLeft: false);

        snap.ShouldNotBeNull();
        snap.Value.PanelWidth.ShouldBe(400);
        snap.Value.Inset.ShouldBe(440);
    }

    [Fact]
    public void FlyoutSnapWithAZeroWidthFoldHasNoGutter()
    {
        var snap = FoldLayout.FlyoutSnap(840, new Rect(420, 0, 0, 600), panelOnLeft: true);
        snap!.Value.PanelWidth.ShouldBe(420);
        snap.Value.Inset.ShouldBe(420);
    }

    [Fact]
    public void FlyoutSnapIgnoresATabletopFold()
        => FoldLayout.FlyoutSnap(840, new Rect(0, 300, 840, 0), panelOnLeft: true).ShouldBeNull();

    [Fact]
    public void TabletopDetentPutsTheSheetEdgeOnTheFold()
    {
        FoldLayout.TabletopDetent(800, new Rect(0, 380, 600, 40), fromBottom: true).ShouldBe(0.475);
        FoldLayout.TabletopDetent(800, new Rect(0, 380, 600, 40), fromBottom: false).ShouldBe(0.475);
        FoldLayout.TabletopDetent(800, new Rect(300, 0, 0, 800), fromBottom: true).ShouldBeNull();
    }

    [Fact]
    public void SplitRatioFollowsAFoldRunningTheSameWay()
    {
        FoldLayout.SplitRatio(new Size(800, 400), new Rect(200, 0, 0, 400), horizontalSplit: true).ShouldBe(0.25);
        FoldLayout.SplitRatio(new Size(800, 400), new Rect(200, 0, 0, 400), horizontalSplit: false).ShouldBeNull();
    }

    [Fact]
    public void ItemsBeforeFoldSplitsInProportion()
    {
        FoldLayout.ItemsBeforeFold(800, new Rect(400, 0, 0, 80), 4).ShouldBe(2);
        FoldLayout.ItemsBeforeFold(800, new Rect(400, 0, 0, 80), 5).ShouldBe(3);
        FoldLayout.ItemsBeforeFold(800, new Rect(0, 40, 800, 0), 4).ShouldBeNull();
    }

    // ---- FoldInfo ----------------------------------------------------------------------------------

    [Fact]
    public void AvoidGrowsTheFoldByTheHingeMargin()
    {
        var info = new FoldInfo(FoldPostureState.HalfOpen, FoldPostureOrientation.Vertical, new Rect(400, 0, 0, 800), new Thickness(12, 0));

        info.Avoid.ShouldBe(new Rect(388, 0, 24, 800));
        info.IsBook.ShouldBeTrue();
        info.IsTabletop.ShouldBeFalse();
    }

    [Fact]
    public void NoneIsNotSeparating()
    {
        FoldInfo.None.IsSeparating.ShouldBeFalse();
        FoldInfo.None.Avoid.ShouldBeNull();
    }

    [Fact]
    public void FlatWithoutARectDoesNotSeparate()
        => new FoldInfo(FoldPostureState.Flat, FoldPostureOrientation.Vertical, null).IsSeparating.ShouldBeFalse();

    // ---- ShinyTabBar --------------------------------------------------------------------------------

    [Fact]
    public void TabBarDealsTabsToBothSegmentsWithAGapOnTheFold()
    {
        var layout = ShinyTabBar.FoldColumns(800, new Rect(390, 0, 20, 60), cellCount: 4, centerWidth: null);

        layout.ShouldNotBeNull();
        layout.Value.ColumnWidths.ShouldBe([195d, 195d, 20d, 195d, 195d]);
        layout.Value.CellColumns.ShouldBe([0, 1, 3, 4]);
        layout.Value.CenterColumn.ShouldBe(-1);
    }

    [Fact]
    public void TabBarKeepsTheCentreButtonOffTheHinge()
    {
        // 4 tabs + centre = 5 slots; on a centred fold 2.5 round away to 3 before it, so the centre
        // button (slot 2) ends on the left of the hinge instead of straddling it.
        var layout = ShinyTabBar.FoldColumns(800, new Rect(400, 0, 0, 60), cellCount: 4, centerWidth: 72);

        layout.ShouldNotBeNull();
        var l = layout.Value;
        l.CenterColumn.ShouldBe(2);
        (l.CenterStart + l.CenterWidth).ShouldBeLessThanOrEqualTo(400);
        l.ColumnWidths.Sum().ShouldBe(800, 0.001);
        l.CellColumns.ShouldBe([0, 1, 4, 5]);
    }

    [Fact]
    public void TabBarIgnoresATabletopFold()
        => ShinyTabBar.FoldColumns(800, new Rect(0, 30, 800, 0), 4, null).ShouldBeNull();
}
