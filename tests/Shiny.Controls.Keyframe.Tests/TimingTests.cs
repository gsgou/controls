using Shiny.Controls.Keyframe;

namespace Shiny.Controls.Keyframe.Tests;

public class TimingTests
{
    static Timing OneSecond(Action<Timing>? configure = null)
    {
        var timing = new Timing { Duration = TimeSpan.FromSeconds(1) };
        configure?.Invoke(timing);
        return timing;
    }

    [Fact]
    public void ProgressRunsZeroToOneAcrossTheDuration()
    {
        var timing = OneSecond();

        Assert.Equal(0d, timing.Sample(TimeSpan.Zero).Progress, 6);
        Assert.Equal(0.25d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(750)).Progress, 6);
    }

    [Fact]
    public void EndOfFinalIterationReadsAsProgressOneNotZero()
    {
        // The boundary case that quietly breaks naive implementations: `elapsed % duration` is 0
        // at the exact end, so a Forwards fill snaps back to the opening pose instead of holding
        // the closing one.
        var timing = OneSecond(t => t.Fill = FillMode.Forwards);

        var sample = timing.Sample(TimeSpan.FromSeconds(1));

        Assert.True(sample.ShouldApply);
        Assert.Equal(1d, sample.Progress, 6);
        Assert.Equal(0, sample.Iteration);
    }

    [Fact]
    public void IterationBoundaryBelongsToTheNextIterationWhenMoreRemain()
    {
        var timing = OneSecond(t => t.Iterations = 3);

        var sample = timing.Sample(TimeSpan.FromSeconds(1));

        Assert.Equal(1, sample.Iteration);
        Assert.Equal(0d, sample.Progress, 6);
    }

    [Fact]
    public void AlternateReversesOddIterations()
    {
        var timing = OneSecond(t =>
        {
            t.Iterations = 4;
            t.Direction = PlaybackDirection.Alternate;
        });

        // Iteration 0 runs forwards...
        Assert.Equal(0.25d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
        // ...iteration 1 runs backwards.
        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(1250)).Progress, 6);
        // ...and iteration 2 forwards again.
        Assert.Equal(0.25d, timing.Sample(TimeSpan.FromMilliseconds(2250)).Progress, 6);
    }

    [Fact]
    public void AlternateReverseFlipsThePhase()
    {
        var timing = OneSecond(t =>
        {
            t.Iterations = 2;
            t.Direction = PlaybackDirection.AlternateReverse;
        });

        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
        Assert.Equal(0.25d, timing.Sample(TimeSpan.FromMilliseconds(1250)).Progress, 6);
    }

    [Fact]
    public void ReverseRunsEveryIterationBackwards()
    {
        var timing = OneSecond(t =>
        {
            t.Iterations = 2;
            t.Direction = PlaybackDirection.Reverse;
        });

        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(1250)).Progress, 6);
    }

    [Fact]
    public void FillNoneLeavesTargetsAloneOutsideTheActiveWindow()
    {
        var timing = OneSecond(t => t.Delay = TimeSpan.FromMilliseconds(500));

        Assert.False(timing.Sample(TimeSpan.Zero).ShouldApply);
        Assert.False(timing.Sample(TimeSpan.FromSeconds(2)).ShouldApply);
    }

    [Fact]
    public void FillBackwardsHoldsTheOpeningPoseDuringTheDelay()
    {
        var timing = OneSecond(t =>
        {
            t.Delay = TimeSpan.FromMilliseconds(500);
            t.Fill = FillMode.Backwards;
        });

        var sample = timing.Sample(TimeSpan.FromMilliseconds(100));

        Assert.True(sample.ShouldApply);
        Assert.Equal(0d, sample.Progress, 6);
    }

    [Fact]
    public void FillBackwardsUnderReverseHoldsTheEndValue()
    {
        // Under Reverse the first instant of playback is progress 1, so a Backwards fill must hold
        // the *end* of the keyframe list — not offset zero.
        var timing = OneSecond(t =>
        {
            t.Delay = TimeSpan.FromMilliseconds(500);
            t.Fill = FillMode.Backwards;
            t.Direction = PlaybackDirection.Reverse;
        });

        Assert.Equal(1d, timing.Sample(TimeSpan.Zero).Progress, 6);
    }

    [Fact]
    public void FillForwardsHoldsAfterTheEnd()
    {
        var timing = OneSecond(t => t.Fill = FillMode.Forwards);

        var sample = timing.Sample(TimeSpan.FromSeconds(10));

        Assert.True(sample.ShouldApply);
        Assert.Equal(1d, sample.Progress, 6);
        Assert.True(sample.IsFinished);
    }

    [Fact]
    public void FillForwardsAfterAnOddAlternateIterationHoldsTheStartValue()
    {
        // Two alternating passes end where they began, so holding forwards must land on 0.
        var timing = OneSecond(t =>
        {
            t.Iterations = 2;
            t.Direction = PlaybackDirection.Alternate;
            t.Fill = FillMode.Forwards;
        });

        Assert.Equal(0d, timing.Sample(TimeSpan.FromSeconds(5)).Progress, 6);
    }

    [Fact]
    public void NegativeDelayStartsPartwayThrough()
    {
        var timing = OneSecond(t => t.Delay = TimeSpan.FromMilliseconds(-250));

        Assert.Equal(0.25d, timing.Sample(TimeSpan.Zero).Progress, 6);
    }

    [Fact]
    public void FractionalIterationsTruncateTheFinalPass()
    {
        var timing = OneSecond(t =>
        {
            t.Iterations = 2.5;
            t.Fill = FillMode.Forwards;
        });

        Assert.Equal(TimeSpan.FromMilliseconds(2500), timing.ActiveDuration);

        var atEnd = timing.Sample(TimeSpan.FromMilliseconds(2500));
        Assert.Equal(0.5d, atEnd.Progress, 6);
        Assert.Equal(2, atEnd.Iteration);
    }

    [Fact]
    public void IterationStartOffsetsIntoTheFirstPass()
    {
        var timing = OneSecond(t => t.IterationStart = 0.5);

        Assert.Equal(0.5d, timing.Sample(TimeSpan.Zero).Progress, 6);
        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
    }

    [Fact]
    public void IterationStartShiftsWhichPassesAlternate()
    {
        var timing = OneSecond(t =>
        {
            t.Iterations = 4;
            t.IterationStart = 1;
            t.Direction = PlaybackDirection.Alternate;
        });

        // Starting on iteration 1 means the very first visible pass runs backwards.
        Assert.Equal(0.75d, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
    }

    [Fact]
    public void InfiniteIterationsNeverFinish()
    {
        var timing = OneSecond(t => t.Iterations = double.PositiveInfinity);

        var sample = timing.Sample(TimeSpan.FromHours(1));

        Assert.True(sample.ShouldApply);
        Assert.False(sample.IsFinished);
        Assert.Equal(TimeSpan.MaxValue, timing.TotalDuration);
    }

    [Fact]
    public void EndDelayPostponesFinishWithoutChangingProgress()
    {
        var timing = OneSecond(t =>
        {
            t.EndDelay = TimeSpan.FromSeconds(1);
            t.Fill = FillMode.Forwards;
        });

        Assert.False(timing.Sample(TimeSpan.FromMilliseconds(1500)).IsFinished);
        Assert.True(timing.Sample(TimeSpan.FromMilliseconds(2000)).IsFinished);
        Assert.Equal(TimeSpan.FromSeconds(2), timing.TotalDuration);
    }

    [Fact]
    public void IterationLevelEasingIsAppliedAfterDirection()
    {
        // With Reverse plus an ease-out curve, the animation should decelerate into the *start*,
        // which means feeding the reversed progress through the curve, not the other way round.
        var timing = OneSecond(t =>
        {
            t.Direction = PlaybackDirection.Reverse;
            t.Easing = Easings.CubicOut;
        });

        var expected = Easings.CubicOut(0.75d);
        Assert.Equal(expected, timing.Sample(TimeSpan.FromMilliseconds(250)).Progress, 6);
    }

    [Fact]
    public void ZeroDurationIsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Timing { Duration = TimeSpan.Zero });

    [Fact]
    public void NegativeIterationsAreRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Timing { Iterations = -1 });

    [Fact]
    public void CloneCopiesEveryField()
    {
        var timing = OneSecond(t =>
        {
            t.Delay = TimeSpan.FromSeconds(2);
            t.EndDelay = TimeSpan.FromSeconds(3);
            t.Iterations = 5;
            t.IterationStart = 0.25;
            t.Direction = PlaybackDirection.AlternateReverse;
            t.Fill = FillMode.Both;
            t.Easing = Easings.BounceOut;
        });

        var clone = timing.Clone();

        Assert.Equal(timing.Duration, clone.Duration);
        Assert.Equal(timing.Delay, clone.Delay);
        Assert.Equal(timing.EndDelay, clone.EndDelay);
        Assert.Equal(timing.Iterations, clone.Iterations);
        Assert.Equal(timing.IterationStart, clone.IterationStart);
        Assert.Equal(timing.Direction, clone.Direction);
        Assert.Equal(timing.Fill, clone.Fill);
        Assert.Same(timing.Easing, clone.Easing);
    }
}
