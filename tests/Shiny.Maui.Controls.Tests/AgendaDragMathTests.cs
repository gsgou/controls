using Shiny.Maui.Controls.Scheduler;
using Shiny.Maui.Controls.Scheduler.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The move/resize rules a user notices immediately when they are wrong: a drag that changes an
/// event's length, a resize that turns it inside out, or an overnight event silently truncated to
/// start at midnight.
/// </summary>
public class AgendaDragMathTests
{
    // A fixed zone with no DST at all, so these cases test the geometry and nothing else.
    static readonly TimeZoneInfo Fixed = TimeZoneInfo.CreateCustomTimeZone("Shiny/Test", TimeSpan.FromHours(-3), "Shiny Test", "Shiny Test");
    static readonly TimeSpan Min15 = TimeSpan.FromMinutes(15);

    static DateTimeOffset At(int day, int hour, int minute = 0)
        => AgendaGeometry.FromLocal(new DateTime(2026, 6, day, hour, minute, 0), Fixed);

    static (DateTimeOffset Start, DateTimeOffset End) Apply(
        DateTimeOffset start, DateTimeOffset end, SchedulerEventChangeKind kind, double delta, int dayDelta = 0)
        => AgendaGeometry.Apply(start, end, kind, delta, dayDelta, Min15, Fixed);


    [Fact]
    public void MovePreservesDuration()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10, 30), SchedulerEventChangeKind.Move, 45);

        start.ShouldBe(At(15, 9, 45));
        end.ShouldBe(At(15, 11, 15));
        (end - start).ShouldBe(TimeSpan.FromMinutes(90));
    }


    [Fact]
    public void MoveAcceptsNegativeDeltas()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.Move, -90);

        start.ShouldBe(At(15, 7, 30));
        end.ShouldBe(At(15, 8, 30));
    }


    [Fact]
    public void MoveClampsAtTheStartOfTheDay()
    {
        var (start, end) = Apply(At(15, 0, 30), At(15, 1, 30), SchedulerEventChangeKind.Move, -300);

        start.ShouldBe(At(15, 0));
        end.ShouldBe(At(15, 1));
    }


    [Fact]
    public void MoveClampsAtTheEndOfTheDayRatherThanSpilling()
    {
        var (start, end) = Apply(At(15, 22), At(15, 23), SchedulerEventChangeKind.Move, 300);

        start.ShouldBe(At(15, 23));
        end.ShouldBe(At(16, 0));
    }


    /// <summary>
    /// An overnight event already crosses midnight. Clamping it "into its day" would yank it
    /// backwards the moment the user grabbed it.
    /// </summary>
    [Fact]
    public void MoveNeverYanksAnAlreadySpillingEventBackwards()
    {
        var start0 = At(15, 20);
        var end0 = At(16, 1);

        var (start, end) = Apply(start0, end0, SchedulerEventChangeKind.Move, 0);
        start.ShouldBe(start0);
        end.ShouldBe(end0);

        // ...and it can still be dragged earlier
        var (earlier, earlierEnd) = Apply(start0, end0, SchedulerEventChangeKind.Move, -120);
        earlier.ShouldBe(At(15, 18));
        earlierEnd.ShouldBe(At(15, 23));
    }


    [Fact]
    public void CrossDayMoveShiftsWholeDaysAndKeepsTheTime()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.Move, 0, dayDelta: 2);

        start.ShouldBe(At(17, 9));
        end.ShouldBe(At(17, 10));
    }


    [Fact]
    public void CrossDayMoveCombinesWithTheTimeDelta()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.Move, -60, dayDelta: -1);

        start.ShouldBe(At(14, 8));
        end.ShouldBe(At(14, 9));
    }


    /// <summary>
    /// The panel clamps a spillover event's *visual* top to midnight. Applying the delta to that
    /// clamped position instead of the real Start would silently truncate the event.
    /// </summary>
    [Fact]
    public void ClippedMultiDayEventsKeepTheirTrueStart()
    {
        var start0 = At(14, 22);   // began the previous day; the panel for the 15th draws it from 00:00
        var end0 = At(15, 3);

        var (start, end) = Apply(start0, end0, SchedulerEventChangeKind.Move, -30);

        // Applied to the real 22:00, not to the clamped 00:00 the panel drew.
        start.ShouldBe(At(14, 21, 30));
        end.ShouldBe(At(15, 2, 30));
    }


    [Fact]
    public void ResizeEndMovesOnlyTheEnd()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeEnd, 30);

        start.ShouldBe(At(15, 9));
        end.ShouldBe(At(15, 10, 30));
    }


    [Fact]
    public void ResizeStartMovesOnlyTheStart()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeStart, -30);

        start.ShouldBe(At(15, 8, 30));
        end.ShouldBe(At(15, 10));
    }


    [Fact]
    public void ResizeEndClampsAtMinDurationInsteadOfFlippingTheEvent()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeEnd, -240);

        start.ShouldBe(At(15, 9));
        end.ShouldBe(At(15, 9, 15));
    }


    [Fact]
    public void ResizeStartClampsAtMinDurationInsteadOfFlippingTheEvent()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeStart, 240);

        start.ShouldBe(At(15, 9, 45));
        end.ShouldBe(At(15, 10));
    }


    [Fact]
    public void ResizeStartClampsAtTheDayBoundary()
    {
        var (start, end) = Apply(At(15, 1), At(15, 3), SchedulerEventChangeKind.ResizeStart, -300);

        start.ShouldBe(At(15, 0));
        end.ShouldBe(At(15, 3));
    }


    [Fact]
    public void ResizeEndClampsAtMidnight()
    {
        var (start, end) = Apply(At(15, 22), At(15, 23), SchedulerEventChangeKind.ResizeEnd, 300);

        start.ShouldBe(At(15, 22));
        end.ShouldBe(At(16, 0));
    }


    [Fact]
    public void ResizeIgnoresTheDayDelta()
    {
        var (start, end) = Apply(At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeEnd, 30, dayDelta: 3);

        start.ShouldBe(At(15, 9));
        end.ShouldBe(At(15, 10, 30));
    }


    [Fact]
    public void MinDurationIsHonoured()
    {
        var (_, end) = AgendaGeometry.Apply(
            At(15, 9), At(15, 10), SchedulerEventChangeKind.ResizeEnd, -240, 0, TimeSpan.FromMinutes(45), Fixed);

        end.ShouldBe(At(15, 9, 45));
    }
}
