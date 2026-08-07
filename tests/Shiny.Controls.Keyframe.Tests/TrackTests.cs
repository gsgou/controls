using System.Runtime.CompilerServices;
using Shiny.Controls.Keyframe;

namespace Shiny.Controls.Keyframe.Tests;

public class TrackTests
{
    sealed class Probe
    {
        public double Value { get; set; }
    }

    static Track<Probe, double> TrackOf(Probe probe, params Key<double>[] keys)
        => new(probe, static (p, v) => p.Value = v, keys, DoubleInterpolator.Instance, static p => p.Value);

    [Fact]
    public void InterpolatesLinearlyBetweenTwoKeys()
    {
        var track = TrackOf(new Probe(), new Key<double>(0, 0), new Key<double>(1, 100));

        Assert.Equal(0d, track.Evaluate(0d), 6);
        Assert.Equal(50d, track.Evaluate(0.5d), 6);
        Assert.Equal(100d, track.Evaluate(1d), 6);
    }

    [Fact]
    public void SegmentEasingGovernsTheSegmentThatBeginsAtTheKey()
    {
        // CSS semantics: the curve declared on a keyframe shapes the run *out* of it. Getting this
        // backwards produces motion that looks subtly wrong and is maddening to debug.
        var track = TrackOf(
            new Probe(),
            new Key<double>(0, 0, Easings.QuadIn),
            new Key<double>(1, 100));

        // Halfway through the segment, QuadIn(0.5) == 0.25.
        Assert.Equal(25d, track.Evaluate(0.5d), 6);
    }

    [Fact]
    public void EasingOnTheFinalKeyIsIgnored()
    {
        var track = TrackOf(
            new Probe(),
            new Key<double>(0, 0),
            new Key<double>(1, 100, Easings.QuadIn));

        Assert.Equal(50d, track.Evaluate(0.5d), 6);
    }

    [Fact]
    public void MultipleSegmentsEachUseTheirOwnCurve()
    {
        var track = TrackOf(
            new Probe(),
            new Key<double>(0.0, 0),
            new Key<double>(0.5, 10, Easings.QuadIn),
            new Key<double>(1.0, 20));

        Assert.Equal(5d, track.Evaluate(0.25d), 6);              // linear first half
        Assert.Equal(10d + 10d * 0.25d, track.Evaluate(0.75d), 6); // QuadIn(0.5) == 0.25
    }

    [Fact]
    public void DuplicateOffsetsProduceAHardCut()
    {
        var track = TrackOf(
            new Probe(),
            new Key<double>(0.0, 0),
            new Key<double>(0.5, 0),
            new Key<double>(0.5, 100),
            new Key<double>(1.0, 100));

        Assert.Equal(0d, track.Evaluate(0.499d), 6);
        Assert.Equal(100d, track.Evaluate(0.5d), 6);
    }

    [Fact]
    public void ValuesOutsideTheKeyRangeAreHeldNotExtrapolated()
    {
        var track = TrackOf(
            new Probe(),
            new Key<double>(0.25, 10),
            new Key<double>(0.75, 20));

        Assert.Equal(10d, track.Evaluate(0d), 6);
        Assert.Equal(20d, track.Evaluate(1d), 6);
    }

    [Fact]
    public void OvershootFromTimelineEasingExtrapolatesPastTheEndKeys()
    {
        // A back/elastic curve at timeline level hands progress above 1 to the track. Clamping
        // there would silently flatten the overshoot the author explicitly asked for.
        var track = TrackOf(new Probe(), new Key<double>(0, 0), new Key<double>(1, 100));

        Assert.Equal(110d, track.Evaluate(1.1d), 6);
        Assert.Equal(-10d, track.Evaluate(-0.1d), 6);
    }

    [Fact]
    public void SingleKeyTrackAlwaysReportsThatValue()
    {
        var track = TrackOf(new Probe(), new Key<double>(0.5, 42));

        Assert.Equal(42d, track.Evaluate(0d), 6);
        Assert.Equal(42d, track.Evaluate(1d), 6);
    }

    [Fact]
    public void SequentialAndRandomAccessAgree()
    {
        // Exercises the cached-segment fast path against the binary-search fallback.
        var keys = Enumerable.Range(0, 11)
            .Select(i => new Key<double>(i / 10d, i * 10d))
            .ToArray();

        var sequential = TrackOf(new Probe(), keys);
        var random = TrackOf(new Probe(), keys);

        var samples = Enumerable.Range(0, 101).Select(i => i / 100d).ToArray();

        var forward = samples.Select(sequential.Evaluate).ToArray();
        var shuffled = samples.Reverse().Select(random.Evaluate).Reverse().ToArray();

        Assert.Equal(forward, shuffled);
    }

    [Fact]
    public void ImplicitKeyResolvesToTheValueCapturedAtStart()
    {
        var probe = new Probe { Value = 7 };
        var track = TrackOf(probe, Key<double>.Current(), new Key<double>(1, 100));

        track.CaptureBaseline();

        // Changing the target afterwards must not move the animation's starting point — otherwise
        // the animation would chase its own output every frame.
        probe.Value = 999;

        Assert.Equal(7d, track.Evaluate(0d), 6);
        Assert.Equal(53.5d, track.Evaluate(0.5d), 6);
    }

    [Fact]
    public void ImplicitKeyWithoutAGetterIsRejected()
    {
        var probe = new Probe();

        var error = Assert.Throws<ArgumentException>(() => new Track<Probe, double>(
            probe,
            static (p, v) => p.Value = v,
            [Key<double>.Current(), new Key<double>(1, 1)],
            DoubleInterpolator.Instance));

        Assert.Contains("getter", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestoreBaselinePutsTheTargetBack()
    {
        var probe = new Probe { Value = 5 };
        var track = TrackOf(probe, new Key<double>(0, 0), new Key<double>(1, 100));

        track.CaptureBaseline();
        track.Apply(1d);
        Assert.Equal(100d, probe.Value, 6);

        track.RestoreBaseline();
        Assert.Equal(5d, probe.Value, 6);
    }

    [Fact]
    public void KeysAreSortedRegardlessOfAuthoringOrder()
    {
        var track = TrackOf(
            new Probe(),
            new Key<double>(1.0, 100),
            new Key<double>(0.0, 0),
            new Key<double>(0.5, 50));

        Assert.Equal([0d, 0.5d, 1d], track.Keys.Select(k => k.Offset));
        Assert.Equal(50d, track.Evaluate(0.5d), 6);
    }

    [Fact]
    public void OffsetsOutsideTheUnitIntervalAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Key<double>(-0.1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Key<double>(1.1, 0));
    }

    [Fact]
    public void TargetIsHeldWeaklySoLoopingAnimationsCannotLeak()
    {
        var track = CreateOrphanedTrack();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(track.IsAlive);

        // A dead track must go quietly inert rather than throwing on the animation thread.
        track.Apply(0.5d);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Track<Probe, double> CreateOrphanedTrack()
        => TrackOf(new Probe(), new Key<double>(0, 0), new Key<double>(1, 1));
}
