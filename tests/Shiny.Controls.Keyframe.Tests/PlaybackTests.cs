using Shiny.Controls.Keyframe;

namespace Shiny.Controls.Keyframe.Tests;

public class PlaybackTests
{
    sealed class Probe
    {
        public double Value { get; set; }
    }

    static Timeline TimelineFor(Probe probe, double seconds = 1d, Action<TimelineBuilder>? configure = null)
    {
        var builder = TimelineBuilder
            .Create(TimeSpan.FromSeconds(seconds))
            .Animate(probe, static (p, v) => p.Value = v, k => k.From(0).To(100), static p => p.Value);

        configure?.Invoke(builder);
        return builder.Build();
    }

    // --- Storyboard ---------------------------------------------------------------------

    [Fact]
    public void StoryboardTotalDurationIsTheLatestEnding()
    {
        var a = new Probe();
        var b = new Probe();

        var storyboard = new Storyboard()
            .Add(TimelineFor(a, 1d))
            .Add(TimelineFor(b, 1d), TimeSpan.FromSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(3), storyboard.TotalDuration);
    }

    [Fact]
    public void ThenAppendsAfterEverythingAlreadyAdded()
    {
        var a = new Probe();
        var b = new Probe();

        var storyboard = new Storyboard()
            .Add(TimelineFor(a, 1d))
            .Then(TimelineFor(b, 1d, t => t.HoldEnd()));

        storyboard.CaptureBaselines();

        // Halfway through the second segment the first has long finished and the second is at 50%.
        storyboard.Evaluate(TimeSpan.FromMilliseconds(1500));
        Assert.Equal(50d, b.Value, 6);
    }

    [Fact]
    public void ThenWithAGapInsertsAPause()
    {
        var a = new Probe();
        var b = new Probe();

        var storyboard = new Storyboard()
            .Add(TimelineFor(a, 1d))
            .Then(TimelineFor(b, 1d), TimeSpan.FromMilliseconds(500));

        Assert.Equal(TimeSpan.FromMilliseconds(2500), storyboard.TotalDuration);
    }

    [Fact]
    public void StaggerSpacesStartsEvenly()
    {
        var probes = Enumerable.Range(0, 3).Select(_ => new Probe()).ToArray();
        var timelines = probes.Select(p => (IAnimationNode)TimelineFor(p, 1d, t => t.Fill(FillMode.Both))).ToArray();

        var storyboard = new Storyboard().Stagger(timelines, TimeSpan.FromMilliseconds(200));
        storyboard.CaptureBaselines();

        storyboard.Evaluate(TimeSpan.FromMilliseconds(400));

        // First started at 0ms (400ms in), second at 200ms (200ms in), third right now (0ms in).
        Assert.Equal(40d, probes[0].Value, 6);
        Assert.Equal(20d, probes[1].Value, 6);
        Assert.Equal(0d, probes[2].Value, 6);
    }

    [Fact]
    public void StoryboardReportsFinishedOnlyWhenEveryChildHas()
    {
        var a = new Probe();
        var b = new Probe();

        var storyboard = new Storyboard()
            .Add(TimelineFor(a, 1d))
            .Add(TimelineFor(b, 3d));

        storyboard.CaptureBaselines();

        Assert.False(storyboard.Evaluate(TimeSpan.FromSeconds(2)));
        Assert.True(storyboard.Evaluate(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void StoryboardsNest()
    {
        var probe = new Probe();

        var inner = new Storyboard().Add(TimelineFor(probe, 1d, t => t.HoldEnd()));
        var outer = new Storyboard().Add(inner, TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(2), outer.TotalDuration);

        outer.CaptureBaselines();
        outer.Evaluate(TimeSpan.FromMilliseconds(1500));
        Assert.Equal(50d, probe.Value, 6);
    }

    [Fact]
    public void StoryboardRejectsContainingItself()
    {
        var storyboard = new Storyboard();
        Assert.Throws<ArgumentException>(() => storyboard.Add(storyboard));
    }

    [Fact]
    public void ThenAfterAnInfiniteNodeIsRejected()
    {
        var probe = new Probe();
        var storyboard = new Storyboard().Add(TimelineFor(probe, 1d, t => t.RepeatForever()));

        Assert.Throws<InvalidOperationException>(() => storyboard.Then(TimelineFor(new Probe())));
    }

    // --- Player -------------------------------------------------------------------------

    [Fact]
    public void PlayerAdvancesWithTheClock()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock);

        player.Play();
        clock.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(25d, probe.Value, 6);
        Assert.Equal(PlaybackState.Running, player.State);
    }

    [Fact]
    public void PauseIgnoresTheClockAndResumeContinues()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock);

        player.Play();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        player.Pause();
        clock.Advance(TimeSpan.FromMilliseconds(500));

        Assert.Equal(25d, probe.Value, 6);

        player.Resume();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal(50d, probe.Value, 6);
    }

    [Fact]
    public void RateScalesPlaybackSpeed()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock) { Rate = 2d };

        player.Play();
        clock.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(50d, probe.Value, 6);
    }

    [Fact]
    public void NegativeRateRunsBackwardsAndFinishesAtTheStart()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.Fill(FillMode.Both)), clock);

        player.Play();
        player.Seek(TimeSpan.FromMilliseconds(800));
        Assert.Equal(80d, probe.Value, 6);

        player.Rate = -1d;
        clock.Advance(TimeSpan.FromMilliseconds(300));
        Assert.Equal(50d, probe.Value, 6);

        clock.Advance(TimeSpan.FromMilliseconds(600));
        Assert.Equal(0d, probe.Value, 6);
        Assert.Equal(PlaybackState.Finished, player.State);
    }

    [Fact]
    public void SeekingIsExactAndOrderIndependent()
    {
        // The whole point of stateless evaluation: jumping around must give the same answers as
        // arriving at each point by ticking.
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.Fill(FillMode.Both)), clock);

        player.Play();

        player.Seek(TimeSpan.FromMilliseconds(900));
        Assert.Equal(90d, probe.Value, 6);

        player.Seek(TimeSpan.FromMilliseconds(100));
        Assert.Equal(10d, probe.Value, 6);

        player.Seek(TimeSpan.FromMilliseconds(500));
        Assert.Equal(50d, probe.Value, 6);
    }

    [Fact]
    public void SeekProgressScrubsAcrossTheWholeDuration()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 2d, t => t.Fill(FillMode.Both)), clock);

        player.Play();
        player.SeekProgress(0.25d);

        Assert.Equal(25d, probe.Value, 6);
    }

    [Fact]
    public void SeekProgressIsRejectedForInfiniteAnimations()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.RepeatForever()), clock);

        Assert.Throws<InvalidOperationException>(() => player.SeekProgress(0.5d));
    }

    [Fact]
    public void StopRestoresBaselinesWhenAsked()
    {
        var probe = new Probe { Value = 7 };
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock) { RestoreOnStop = true };

        player.Play();
        clock.Advance(TimeSpan.FromMilliseconds(500));
        Assert.Equal(50d, probe.Value, 6);

        player.Stop();
        Assert.Equal(7d, probe.Value, 6);
        Assert.Equal(PlaybackState.Idle, player.State);
    }

    [Fact]
    public async Task PlayAsyncCompletesWhenTheAnimationFinishes()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.HoldEnd()), clock);

        var run = player.PlayAsync();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(16));

        Assert.True(await run);
        Assert.Equal(100d, probe.Value, 6);
    }

    [Fact]
    public async Task PlayAsyncReportsFalseWhenStopped()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock);

        var run = player.PlayAsync();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        player.Stop();

        Assert.False(await run);
    }

    [Fact]
    public async Task PlayAsyncIsCancellable()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe), clock);
        using var cts = new CancellationTokenSource();

        var run = player.PlayAsync(cts.Token);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        await cts.CancelAsync();

        Assert.False(await run);
        Assert.Equal(PlaybackState.Idle, player.State);
    }

    [Fact]
    public void FinishJumpsStraightToTheEnd()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.HoldEnd()), clock);

        player.Play();
        player.Finish();

        Assert.Equal(100d, probe.Value, 6);
        Assert.Equal(PlaybackState.Finished, player.State);
    }

    [Fact]
    public void FinishIsRejectedForInfiniteAnimations()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        using var player = new Player(TimelineFor(probe, 1d, t => t.RepeatForever()), clock);

        player.Play();
        Assert.Throws<InvalidOperationException>(player.Finish);
    }

    [Fact]
    public void PlayerStopsListeningToTheClockOnceDisposed()
    {
        var probe = new Probe();
        var clock = new ManualClock();
        var player = new Player(TimelineFor(probe), clock);

        player.Play();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        player.Dispose();
        clock.Advance(TimeSpan.FromMilliseconds(500));

        Assert.Equal(25d, probe.Value, 6);
    }

    // --- Timeline events ----------------------------------------------------------------

    [Fact]
    public void FinishedIsRaisedOnceOnly()
    {
        var probe = new Probe();
        var timeline = TimelineFor(probe, 1d, t => t.HoldEnd());

        var count = 0;
        timeline.Finished += (_, _) => count++;

        timeline.CaptureBaselines();
        timeline.Evaluate(TimeSpan.FromSeconds(1));
        timeline.Evaluate(TimeSpan.FromSeconds(2));
        timeline.Evaluate(TimeSpan.FromSeconds(3));

        Assert.Equal(1, count);
    }

    [Fact]
    public void IterationChangedFiresOnEachBoundaryButNotAtTheStart()
    {
        var probe = new Probe();
        var timeline = TimelineFor(probe, 1d, t => t.Repeat(3));

        var observed = new List<int>();
        timeline.IterationChanged += (_, i) => observed.Add(i);

        timeline.CaptureBaselines();
        timeline.Evaluate(TimeSpan.Zero);
        timeline.Evaluate(TimeSpan.FromMilliseconds(500));
        timeline.Evaluate(TimeSpan.FromMilliseconds(1500));
        timeline.Evaluate(TimeSpan.FromMilliseconds(2500));

        Assert.Equal([1, 2], observed);
    }

    [Fact]
    public void ReplayingRaisesFinishedAgain()
    {
        var probe = new Probe();
        var timeline = TimelineFor(probe, 1d, t => t.HoldEnd());

        var count = 0;
        timeline.Finished += (_, _) => count++;

        timeline.CaptureBaselines();
        timeline.Evaluate(TimeSpan.FromSeconds(2));

        timeline.CaptureBaselines();
        timeline.Evaluate(TimeSpan.FromSeconds(2));

        Assert.Equal(2, count);
    }

    [Fact]
    public void ScrubIsRejectedForInfiniteTimelines()
    {
        var timeline = TimelineFor(new Probe(), 1d, t => t.RepeatForever());
        Assert.Throws<InvalidOperationException>(() => timeline.Scrub(0.5d));
    }

    [Fact]
    public void PruneDeadTracksRemovesCollectedTargets()
    {
        var timeline = new Timeline();
        AddOrphanedTrack(timeline);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Equal(1, timeline.PruneDeadTracks());
        Assert.Empty(timeline.Tracks);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AddOrphanedTrack(Timeline timeline)
        => timeline.Add(new Track<Probe, double>(
            new Probe(),
            static (p, v) => p.Value = v,
            [new Key<double>(0, 0), new Key<double>(1, 1)],
            DoubleInterpolator.Instance));
}
