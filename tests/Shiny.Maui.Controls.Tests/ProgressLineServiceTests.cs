using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The service's arithmetic: reference counting across overlapping runs, the trickle curve, and the
/// rules that stop the line reporting something untrue.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ProgressLineServiceTests
{
    public ProgressLineServiceTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    static ProgressLineService Service() => new();


    [Fact]
    public void StartingARunPutsTheLineUp()
    {
        var service = Service();

        using var run = service.Start();

        service.IsRunning.ShouldBeTrue();
        run.IsComplete.ShouldBeFalse();
    }


    /// <summary>
    /// Two parallel requests are one line. The alternative — the line vanishing when the first of
    /// them lands — is the bug this exists to prevent.
    /// </summary>
    [Fact]
    public void TheLineStaysUpUntilTheLastRunFinishes()
    {
        var service = Service();
        var first = service.Start();
        var second = service.Start();

        first.Complete();

        service.IsRunning.ShouldBeTrue();

        second.Complete();

        service.IsRunning.ShouldBeFalse();
    }


    [Fact]
    public void CompleteAllEndsEveryRun()
    {
        var service = Service();
        var first = service.Start();
        var second = service.Start();

        service.CompleteAll();

        service.IsRunning.ShouldBeFalse();
        first.IsComplete.ShouldBeTrue();
        second.IsComplete.ShouldBeTrue();
    }


    [Fact]
    public void DisposingTheHandleCompletesTheRun()
    {
        var service = Service();
        var run = service.Start();

        run.Dispose();

        run.IsComplete.ShouldBeTrue();
        service.IsRunning.ShouldBeFalse();
    }


    [Fact]
    public void ARunStartsPartWayAcrossSoItIsNeverAZeroWidthNothing()
    {
        var service = Service();

        using var run = service.Start(c => c.StartProgress = 0.08);

        run.Progress.ShouldBe(0.08);
    }


    [Fact]
    public void ProgressNeverGoesBackwards()
    {
        var service = Service();
        using var run = service.Start();
        run.SetProgress(0.6);

        run.SetProgress(0.2);

        run.Progress.ShouldBe(0.6);
    }


    [Fact]
    public void ProgressPastOneIsClamped()
    {
        var service = Service();
        using var run = service.Start();

        run.SetProgress(5);

        run.Progress.ShouldBe(1);
    }


    [Fact]
    public void AFinishedRunIgnoresLateReports()
    {
        var service = Service();
        var run = service.Start();
        run.Complete();

        run.SetProgress(0.3);

        run.Progress.ShouldBe(1);
    }


    /// <summary>
    /// The trickle decelerates toward the ceiling and never arrives: a line that reaches 100% on its
    /// own has told the user the work finished when it has not.
    /// </summary>
    [Fact]
    public void TheTrickleApproachesTheCeilingWithoutReachingIt()
    {
        var service = Service();
        using var run = service.Start(c =>
        {
            c.StartProgress = 0.08;
            c.TrickleCeiling = 0.9;
            c.TrickleRate = 0.12;
        });

        var timer = TestDispatcherProvider.Instance.Timers[^1];
        var previous = run.Progress;

        for (var i = 0; i < 200; i++)
        {
            timer.Fire();
            run.Progress.ShouldBeGreaterThan(previous);
            run.Progress.ShouldBeLessThan(0.9);
            previous = run.Progress;
        }
    }


    [Fact]
    public void AnIndeterminateRunDoesNotTrickle()
    {
        var service = Service();
        using var run = service.Start(c => c.Indeterminate = true);

        TestDispatcherProvider.Instance.Timers[^1].Fire();

        run.Progress.ShouldBe(0.08);
    }


    [Fact]
    public void TrickleCanBeTurnedOff()
    {
        var service = Service();
        using var run = service.Start(c => c.Trickle = false);

        TestDispatcherProvider.Instance.Timers[^1].Fire();

        run.Progress.ShouldBe(0.08);
    }


    /// <summary>
    /// The slowest run wins, not the average — otherwise a quick call drags the bar most of the way
    /// across while the slow one it is actually waiting on has barely started.
    /// </summary>
    [Fact]
    public void TheSlowestRunIsTheOneShown()
    {
        var service = Service();
        using var slow = service.Start(c => c.Trickle = false);
        using var quick = service.Start(c => c.Trickle = false);

        quick.SetProgress(0.95);
        slow.SetProgress(0.2);

        Math.Min(slow.Progress, quick.Progress).ShouldBe(0.2);
    }
}
