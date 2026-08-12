namespace Shiny.Controls.Media;

/// <summary>
/// Formats playback positions the way media players do — <c>0:07</c>, <c>4:31</c>, <c>1:02:09</c> — so the
/// MAUI transport bar and the Blazor one label their timelines identically.
/// </summary>
public static class MediaTimeFormatter
{
    /// <summary>
    /// Render <paramref name="value"/> as <c>m:ss</c>, widening to <c>h:mm:ss</c> only once there is an hour
    /// to show. Negative and unknown (<see cref="TimeSpan.Zero"/>-or-less) values render as <c>0:00</c>.
    /// </summary>
    /// <param name="value">The position or duration to format.</param>
    /// <param name="forceHours">
    /// Force the <c>h:mm:ss</c> form even under an hour. Pass the media's total duration through here so a
    /// 90-minute video doesn't shuffle its position label from <c>59:59</c> to <c>1:00:00</c> mid-playback.
    /// </param>
    public static string Format(TimeSpan value, bool forceHours = false)
    {
        if (value < TimeSpan.Zero)
            value = TimeSpan.Zero;

        var total = (long)value.TotalSeconds;
        var hours = total / 3600;
        var minutes = total % 3600 / 60;
        var seconds = total % 60;

        return hours > 0 || forceHours
            ? $"{hours}:{minutes:D2}:{seconds:D2}"
            : $"{minutes}:{seconds:D2}";
    }
}
