using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The solver is where a tooltip either works or does not: everything else is chrome. It is pure
/// geometry by design — rects in, rects out — so the rules that are hard to see on a device (the flip,
/// the clamp, and the tail sliding to keep pointing at a target the bubble was moved away from) are
/// cheap to pin down here.
/// </summary>
public class TooltipPlacementSolverTests
{
    static readonly Size Screen = new(400, 800);
    static readonly Size Bubble = new(200, 100);


    [Fact]
    public void AutoPrefersBelowWhenThereIsRoom()
    {
        var target = new Rect(150, 300, 100, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Auto);

        layout.Placement.ShouldBe(TooltipPlacement.Bottom);
        layout.Fits.ShouldBeTrue();
        layout.Bubble.Top.ShouldBe(target.Bottom + 8);
    }


    [Fact]
    public void AutoFlipsAboveForATargetNearTheBottom()
    {
        var target = new Rect(150, 740, 100, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Auto);

        layout.Placement.ShouldBe(TooltipPlacement.Top);
        layout.Bubble.Bottom.ShouldBe(target.Top - 8);
    }


    [Fact]
    public void AnExplicitSideFlipsToItsOppositeRatherThanBeingClipped()
    {
        // Hard against the left edge: Left cannot fit, Right can.
        var target = new Rect(4, 400, 60, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Left);

        layout.Placement.ShouldBe(TooltipPlacement.Right);
    }


    [Fact]
    public void AnExplicitSideIsKeptWhenItFits()
    {
        var target = new Rect(250, 400, 60, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Left);

        layout.Placement.ShouldBe(TooltipPlacement.Left);
        layout.Bubble.Right.ShouldBe(target.Left - 8);
    }


    [Fact]
    public void TheBubbleIsClampedInsideTheScreenMargin()
    {
        // Centring a 200-wide bubble on a target 20px from the left would put it at -80.
        var target = new Rect(10, 300, 40, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Bottom, margin: 12);

        layout.Bubble.Left.ShouldBe(12);
        layout.Bubble.Right.ShouldBeLessThanOrEqualTo(Screen.Width - 12);
    }


    [Fact]
    public void TheTailFollowsTheTargetAfterTheBubbleIsClamped()
    {
        var target = new Rect(10, 300, 40, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Bottom, margin: 12, tailInset: 16);

        // Target centre is x=30, bubble starts at x=12, so the tail belongs 18 in from its left edge.
        (layout.Bubble.Left + layout.TailOffset).ShouldBe(target.Center.X, 0.01);
    }


    [Fact]
    public void TheTailNeverReachesTheBubblesRoundedCorners()
    {
        // Target far left of a bubble that has been clamped right: the tail wants to go off the end.
        var target = new Rect(0, 300, 8, 40);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Bottom, margin: 12, tailInset: 16);

        layout.TailOffset.ShouldBeGreaterThanOrEqualTo(16);
        layout.TailOffset.ShouldBeLessThanOrEqualTo(layout.Bubble.Width - 16);
    }


    [Fact]
    public void CenterIgnoresTheTargetEntirely()
    {
        var target = new Rect(0, 0, 10, 10);

        var layout = TooltipPlacementSolver.Solve(target, Bubble, Screen, TooltipPlacement.Center);

        layout.Placement.ShouldBe(TooltipPlacement.Center);
        layout.TailOffset.ShouldBe(0);
        layout.Bubble.Center.X.ShouldBe(Screen.Width / 2, 0.01);
        layout.Bubble.Center.Y.ShouldBe(Screen.Height / 2, 0.01);
    }


    [Fact]
    public void ABubbleTooBigForTheScreenIsPinnedRatherThanThrowing()
    {
        // The clamp range inverts here — the low bound lands above the high one, which is exactly what
        // Math.Clamp throws on. The solver has to pick a side instead.
        var oversized = new Size(600, 900);
        var target = new Rect(150, 300, 100, 40);

        var layout = TooltipPlacementSolver.Solve(target, oversized, Screen, TooltipPlacement.Auto, margin: 12);

        layout.Fits.ShouldBeFalse();
        layout.Bubble.Left.ShouldBe(12);
        layout.Bubble.Top.ShouldBe(12);
    }


    [Fact]
    public void WhenNothingFitsItTakesTheRoomiestSide()
    {
        // Centred near the top of a small container, with a bubble that fits on no side at all:
        // above has nothing, left and right have 100 each, below has 130.
        var target = new Rect(120, 20, 60, 30);
        var small = new Size(300, 200);
        var big = new Size(280, 190);

        var layout = TooltipPlacementSolver.Solve(target, big, small, TooltipPlacement.Auto);

        layout.Fits.ShouldBeFalse();
        layout.Placement.ShouldBe(TooltipPlacement.Bottom);
    }


    [Theory]
    [InlineData(TooltipPlacement.Top, TooltipPlacement.Bottom)]
    [InlineData(TooltipPlacement.Bottom, TooltipPlacement.Top)]
    [InlineData(TooltipPlacement.Left, TooltipPlacement.Right)]
    [InlineData(TooltipPlacement.Right, TooltipPlacement.Left)]
    [InlineData(TooltipPlacement.Center, TooltipPlacement.Center)]
    public void OppositeIsSymmetric(TooltipPlacement input, TooltipPlacement expected)
        => TooltipPlacementSolver.Opposite(input).ShouldBe(expected);
}
