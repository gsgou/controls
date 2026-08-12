using Shiny.Controls.Media;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Media.Tests;

public class MediaTimeFormatterTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(7, "0:07")]
    [InlineData(59, "0:59")]
    [InlineData(60, "1:00")]
    [InlineData(271, "4:31")]
    [InlineData(3599, "59:59")]
    public void Renders_minutes_and_seconds_under_an_hour(int seconds, string expected)
        => MediaTimeFormatter.Format(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);

    [Theory]
    [InlineData(3600, "1:00:00")]
    [InlineData(3729, "1:02:09")]
    [InlineData(45296, "12:34:56")]
    public void Widens_to_hours_once_there_is_an_hour(int seconds, string expected)
        => MediaTimeFormatter.Format(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);

    [Fact]
    public void ForceHours_keeps_the_label_width_stable_across_the_hour_boundary()
    {
        // A 90-minute video's position label must not shrink from 1:00:00 back to 59:59 as it rewinds.
        MediaTimeFormatter.Format(TimeSpan.FromSeconds(3599), forceHours: true).ShouldBe("0:59:59");
        MediaTimeFormatter.Format(TimeSpan.FromSeconds(5), forceHours: true).ShouldBe("0:00:05");
    }

    [Fact]
    public void Clamps_negative_values_to_zero()
        => MediaTimeFormatter.Format(TimeSpan.FromSeconds(-30)).ShouldBe("0:00");

    [Fact]
    public void Truncates_rather_than_rounds_sub_second_values()
        => MediaTimeFormatter.Format(TimeSpan.FromMilliseconds(1999)).ShouldBe("0:01");
}
