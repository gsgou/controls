namespace Shiny.Blazor.Controls.Scheduler;

/// <summary>
/// The pure minutes/pixels/snap arithmetic behind the agenda timeline. Layout already needed the
/// forward direction (minutes -> Y); drag needs the inverse, and the two must not drift apart - so
/// both live here and are unit tested (in the MAUI test project - the two files are the same math).
/// </summary>
/// <remarks>
/// This file is mirrored, near line for line, by <c>Shiny.Maui.Controls/Scheduler/Internal/AgendaGeometry.cs</c>.
/// Keep the two in sync; the test corpus in <c>AgendaGeometryTests</c> is the shared contract.
/// </remarks>
static class AgendaGeometry
{
    public const double MinutesPerDay = 24 * 60;

    public static double MinutesToY(double minutes, double timeSlotHeight)
        => minutes * timeSlotHeight / 60.0;

    public static double YToMinutes(double y, double timeSlotHeight)
        => timeSlotHeight <= 0 ? 0 : y * 60.0 / timeSlotHeight;

    /// <summary>Rounds to the nearest snap boundary, halves away from zero.</summary>
    public static double SnapMinutes(double minutes, int snapMinutes)
    {
        var snap = Math.Clamp(snapMinutes, 1, 60);
        return Math.Round(minutes / snap, MidpointRounding.AwayFromZero) * snap;
    }

    /// <summary>
    /// Converts a snapped local wall-clock minute offset within <paramref name="date"/> into a
    /// DateTimeOffset carrying the zone's offset *at that instant*.
    /// </summary>
    public static DateTimeOffset ToLocal(DateOnly date, double snappedMinutes, TimeZoneInfo? zone = null)
        => FromLocal(date.ToDateTime(TimeOnly.MinValue).AddMinutes(snappedMinutes), zone);

    /// <summary>
    /// Rebuilds a DateTimeOffset from a wall-clock local time, resolving the two transition cases
    /// DateTimeOffset arithmetic would otherwise get wrong:
    /// spring-forward (the local time does not exist - push to the first valid time) and fall-back
    /// (it happens twice - take the pre-transition, larger offset, which is what a user dragging
    /// downward through the repeated hour means).
    /// </summary>
    public static DateTimeOffset FromLocal(DateTime local, TimeZoneInfo? zone = null)
    {
        var tz = zone ?? TimeZoneInfo.Local;
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        // DST gaps are 30/60/120 minutes in practice; the bound just stops a pathological zone
        // from spinning here forever.
        for (var i = 0; i < 240 && tz.IsInvalidTime(local); i++)
            local = local.AddMinutes(1);

        var offset = tz.IsAmbiguousTime(local)
            ? tz.GetAmbiguousTimeOffsets(local).Max()
            : tz.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }

    /// <summary>The event's wall-clock minute offset within the local day it starts on.</summary>
    public static double StartMinuteOfDay(DateTimeOffset start, TimeZoneInfo? zone = null)
        => ToLocalWallClock(start, zone).TimeOfDay.TotalMinutes;

    /// <summary>Wall-clock duration, which is what the timeline draws (an hour of DST is not an hour of pixels).</summary>
    public static double DurationMinutes(DateTimeOffset start, DateTimeOffset end, TimeZoneInfo? zone = null)
        => (ToLocalWallClock(end, zone) - ToLocalWallClock(start, zone)).TotalMinutes;

    static DateTime ToLocalWallClock(DateTimeOffset value, TimeZoneInfo? zone)
        => TimeZoneInfo.ConvertTime(value, zone ?? TimeZoneInfo.Local).DateTime;

    /// <summary>
    /// Applies a move/resize delta in wall-clock space, enforcing <paramref name="minDuration"/> and
    /// day-boundary clamping.
    /// </summary>
    /// <param name="deltaMinutes">
    /// Already-snapped wall-clock delta. For <see cref="SchedulerEventChangeKind.Move"/> it shifts
    /// both edges; for a resize it moves only the named edge.
    /// </param>
    /// <param name="dayDelta">Whole days to shift by (cross-day drag). Only honoured for a move.</param>
    /// <remarks>
    /// A move clamps the start so the event stays inside the day it started on. An event that
    /// already spilled past midnight (an overnight event, or one longer than a day) is never yanked
    /// backwards to satisfy that - it simply cannot be pushed any later than it already is.
    /// </remarks>
    public static (DateTimeOffset Start, DateTimeOffset End) Apply(
        DateTimeOffset start,
        DateTimeOffset end,
        SchedulerEventChangeKind kind,
        double deltaMinutes,
        int dayDelta,
        TimeSpan minDuration,
        TimeZoneInfo? zone = null)
    {
        var tz = zone ?? TimeZoneInfo.Local;
        var localStart = ToLocalWallClock(start, tz);
        var localEnd = ToLocalWallClock(end, tz);
        var duration = (localEnd - localStart).TotalMinutes;
        var minMinutes = Math.Max(1, minDuration.TotalMinutes);

        DateTimeOffset newStart;
        DateTimeOffset newEnd;

        switch (kind)
        {
            case SchedulerEventChangeKind.Move:
            {
                var startMinute = localStart.TimeOfDay.TotalMinutes;
                var maxStart = duration <= MinutesPerDay ? MinutesPerDay - duration : 0;

                // Never drag an already-spilling event backwards just to satisfy the clamp.
                maxStart = Math.Max(maxStart, startMinute);
                var newStartMinute = Math.Clamp(startMinute + deltaMinutes, 0, maxStart);

                newStart = FromLocal(localStart.Date.AddDays(dayDelta).AddMinutes(newStartMinute), tz);

                // The duration is measured on the clock, not the calendar, and the start may have
                // been nudged out of a DST gap - so the end is rebuilt from where the start landed.
                newEnd = FromLocal(ToLocalWallClock(newStart, tz).AddMinutes(duration), tz);
                break;
            }

            case SchedulerEventChangeKind.ResizeStart:
            {
                var dayFloor = localStart.Date;
                var ceiling = localEnd.AddMinutes(-minMinutes);
                var candidate = localStart.AddMinutes(deltaMinutes);

                if (candidate < dayFloor) candidate = dayFloor;
                if (candidate > ceiling) candidate = ceiling;

                newStart = FromLocal(candidate, tz);
                newEnd = FromLocal(localEnd, tz);
                break;
            }

            default:
            {
                var dayCeiling = localEnd.Date.AddDays(1);
                var floor = localStart.AddMinutes(minMinutes);
                var candidate = localEnd.AddMinutes(deltaMinutes);

                if (candidate > dayCeiling) candidate = dayCeiling;
                if (candidate < floor) candidate = floor;

                newStart = FromLocal(localStart, tz);
                newEnd = FromLocal(candidate, tz);
                break;
            }
        }

        // Backstop: a resize whose edge got pushed out of a DST gap can land past its own clamp.
        // Never hand back an inside-out event.
        if (newEnd <= newStart)
            newEnd = newStart.AddMinutes(minMinutes);

        return (newStart, newEnd);
    }
}
