using Shiny.Maui.Controls.Scheduler.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The forward direction (minutes -> Y) has always driven the agenda layout; drag adds the inverse,
/// and the two silently disagreeing is the kind of bug that reads as "the event lands one slot off".
/// </summary>
public class AgendaGeometryTests
{
    [Theory]
    [InlineData(20.0)]
    [InlineData(60.0)]
    [InlineData(200.0)]
    public void MinutesAndYRoundTrip(double timeSlotHeight)
    {
        foreach (var minutes in new[] { 0.0, 7.5, 15.0, 90.0, 720.0, 1439.0, 1440.0 })
        {
            var y = AgendaGeometry.MinutesToY(minutes, timeSlotHeight);
            AgendaGeometry.YToMinutes(y, timeSlotHeight).ShouldBe(minutes, 0.0001);
        }
    }


    [Fact]
    public void MinutesToYUsesTheSlotHeightPerHour()
    {
        AgendaGeometry.MinutesToY(60, 60).ShouldBe(60);
        AgendaGeometry.MinutesToY(30, 60).ShouldBe(30);
        AgendaGeometry.MinutesToY(60, 200).ShouldBe(200);
        AgendaGeometry.MinutesToY(1440, 20).ShouldBe(480);
    }


    /// <summary>A zero/negative slot height would otherwise divide by zero on the inverse.</summary>
    [Fact]
    public void YToMinutesIsSafeAtZeroHeight()
    {
        AgendaGeometry.YToMinutes(100, 0).ShouldBe(0);
        AgendaGeometry.YToMinutes(100, -5).ShouldBe(0);
    }


    [Theory]
    [InlineData(0, 15, 0)]
    [InlineData(7, 15, 0)]
    [InlineData(8, 15, 15)]
    [InlineData(15, 15, 15)]
    [InlineData(22, 15, 15)]
    [InlineData(23, 15, 30)]
    [InlineData(14, 5, 15)]
    [InlineData(12, 5, 10)]
    [InlineData(44, 30, 30)]
    [InlineData(46, 30, 60)]
    [InlineData(31, 60, 60)]
    [InlineData(29, 60, 0)]
    public void SnapsToTheNearestBoundary(double minutes, int snap, double expected)
        => AgendaGeometry.SnapMinutes(minutes, snap).ShouldBe(expected);


    /// <summary>Halves go away from zero in both directions, so up and down feel symmetric.</summary>
    [Fact]
    public void SnapsExactHalvesAwayFromZero()
    {
        AgendaGeometry.SnapMinutes(7.5, 15).ShouldBe(15);
        AgendaGeometry.SnapMinutes(-7.5, 15).ShouldBe(-15);
    }


    [Fact]
    public void SnapsNegativeDeltas()
    {
        AgendaGeometry.SnapMinutes(-8, 15).ShouldBe(-15);
        AgendaGeometry.SnapMinutes(-7, 15).ShouldBe(0);
        AgendaGeometry.SnapMinutes(-40, 30).ShouldBe(-30);
    }


    [Fact]
    public void SnapGranularityIsClampedToOneThroughSixty()
    {
        AgendaGeometry.SnapMinutes(37, 0).ShouldBe(37);      // clamps to 1
        AgendaGeometry.SnapMinutes(37, -10).ShouldBe(37);
        AgendaGeometry.SnapMinutes(100, 999).ShouldBe(120);  // clamps to 60
    }


    [Fact]
    public void ToLocalPlacesTheMinuteOffsetInsideTheDate()
    {
        var date = new DateOnly(2026, 6, 15);
        var result = AgendaGeometry.ToLocal(date, 9 * 60 + 30, TimeZoneInfo.Utc);

        result.DateTime.ShouldBe(new DateTime(2026, 6, 15, 9, 30, 0));
        result.Offset.ShouldBe(TimeSpan.Zero);
    }
}
