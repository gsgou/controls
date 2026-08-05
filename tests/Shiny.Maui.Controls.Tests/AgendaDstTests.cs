using Shiny.Maui.Controls.Scheduler;
using Shiny.Maui.Controls.Scheduler.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The agenda lays out local wall-clock time - 24 rows, always - but a DST-transition day is not 24
/// hours. Implementing a move as <c>evt.Start + TimeSpan.FromMinutes(delta)</c> lands the event an
/// hour off across the transition, because DateTimeOffset arithmetic is absolute-time arithmetic.
///
/// A fixed zone is used rather than TimeZoneInfo.Local so these are machine-independent.
/// </summary>
public class AgendaDstTests
{
    static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    static readonly TimeSpan Est = TimeSpan.FromHours(-5);
    static readonly TimeSpan Edt = TimeSpan.FromHours(-4);
    static readonly TimeSpan Min15 = TimeSpan.FromMinutes(15);

    // 2026: spring forward Mar 8 (02:00 -> 03:00), fall back Nov 1 (02:00 -> 01:00).
    static DateTime SpringLocal(int hour, int minute = 0) => new(2026, 3, 8, hour, minute, 0);
    static DateTime FallLocal(int hour, int minute = 0) => new(2026, 11, 1, hour, minute, 0);


    [Fact]
    public void SpringForwardGapIsPushedToTheFirstValidTime()
    {
        var result = AgendaGeometry.FromLocal(SpringLocal(2, 30), NewYork);

        result.DateTime.ShouldBe(SpringLocal(3));
        result.Offset.ShouldBe(Edt);
    }


    /// <summary>Falling back, the hour happens twice; a user dragging downward means the first one.</summary>
    [Fact]
    public void FallBackAmbiguityResolvesToThePreTransitionOffset()
    {
        var result = AgendaGeometry.FromLocal(FallLocal(1, 30), NewYork);

        result.DateTime.ShouldBe(FallLocal(1, 30));
        result.Offset.ShouldBe(Edt);
    }


    [Fact]
    public void ValidTimesKeepTheOffsetAtThatInstant()
    {
        AgendaGeometry.FromLocal(SpringLocal(1), NewYork).Offset.ShouldBe(Est);
        AgendaGeometry.FromLocal(SpringLocal(4), NewYork).Offset.ShouldBe(Edt);
    }


    /// <summary>
    /// Dragging an event down four rows means four rows on the clock. Naive absolute arithmetic
    /// would land it at 05:00 - one row past where the finger was.
    /// </summary>
    [Fact]
    public void DragAcrossTheSpringForwardPreservesWallClockIntent()
    {
        var start = AgendaGeometry.FromLocal(SpringLocal(0), NewYork);
        var end = AgendaGeometry.FromLocal(SpringLocal(1), NewYork);

        var (newStart, newEnd) = AgendaGeometry.Apply(
            start, end, SchedulerEventChangeKind.Move, 240, 0, Min15, NewYork);

        newStart.DateTime.ShouldBe(SpringLocal(4));
        newEnd.DateTime.ShouldBe(SpringLocal(5));
        newStart.Offset.ShouldBe(Edt);

        // ...and that is genuinely three absolute hours, not four.
        (newStart - start).ShouldBe(TimeSpan.FromHours(3));
    }


    [Fact]
    public void MoveThatLandsInTheGapPushesTheWholeEventForward()
    {
        var start = AgendaGeometry.FromLocal(SpringLocal(1), NewYork);
        var end = AgendaGeometry.FromLocal(SpringLocal(1, 30), NewYork);

        var (newStart, newEnd) = AgendaGeometry.Apply(
            start, end, SchedulerEventChangeKind.Move, 60, 0, Min15, NewYork);

        // 02:00 does not exist, so it lands on the first valid minute...
        newStart.DateTime.ShouldBe(SpringLocal(3));
        // ...and the end follows it rather than collapsing onto it.
        newEnd.DateTime.ShouldBe(SpringLocal(3, 30));
        (newEnd - newStart).ShouldBe(TimeSpan.FromMinutes(30));
    }


    [Fact]
    public void MoveIntoTheAmbiguousHourTakesThePreTransitionOffset()
    {
        var start = AgendaGeometry.FromLocal(FallLocal(0, 30), NewYork);
        var end = AgendaGeometry.FromLocal(FallLocal(1), NewYork);

        var (newStart, newEnd) = AgendaGeometry.Apply(
            start, end, SchedulerEventChangeKind.Move, 60, 0, Min15, NewYork);

        newStart.DateTime.ShouldBe(FallLocal(1, 30));
        newStart.Offset.ShouldBe(Edt);
        newEnd.DateTime.ShouldBe(FallLocal(2));
    }


    /// <summary>
    /// The timeline draws wall-clock minutes; the spring-forward day only has 23 absolute hours but
    /// still renders 24 rows.
    /// </summary>
    [Fact]
    public void DurationIsMeasuredOnTheClockNotTheCalendar()
    {
        var start = AgendaGeometry.FromLocal(SpringLocal(1), NewYork);
        var end = AgendaGeometry.FromLocal(SpringLocal(4), NewYork);

        AgendaGeometry.DurationMinutes(start, end, NewYork).ShouldBe(180);
        (end - start).ShouldBe(TimeSpan.FromHours(2));
    }


    [Fact]
    public void StartMinuteOfDayIsTheWallClockRow()
    {
        var start = AgendaGeometry.FromLocal(SpringLocal(9, 30), NewYork);
        AgendaGeometry.StartMinuteOfDay(start, NewYork).ShouldBe(570);
    }
}
