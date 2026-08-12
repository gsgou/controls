using Shiny.Maui.Controls.Media;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Media.Tests;

public class MediaSeekBarTests
{
    public MediaSeekBarTests() => TestDispatcherProvider.Install();

    [Fact]
    public void The_played_fraction_tracks_position_over_duration()
    {
        var bar = new MediaSeekBar
        {
            Duration = TimeSpan.FromSeconds(200),
            Position = TimeSpan.FromSeconds(50)
        };

        DrawableOf(bar).Fraction.ShouldBe(0.25d, 0.0001d);
    }

    [Fact]
    public void A_zero_duration_leaves_the_thumb_at_the_start()
    {
        // Before metadata loads (and for a live stream) there is nothing to be a fraction of; dividing
        // anyway would push NaN into the draw path.
        var bar = new MediaSeekBar { Position = TimeSpan.FromSeconds(10) };

        DrawableOf(bar).Fraction.ShouldBe(0d);
    }

    [Fact]
    public void A_position_past_the_end_clamps_to_full()
    {
        var bar = new MediaSeekBar
        {
            Duration = TimeSpan.FromSeconds(10),
            Position = TimeSpan.FromSeconds(30)
        };

        DrawableOf(bar).Fraction.ShouldBe(1d);
    }

    [Theory]
    [InlineData(-0.5, 0d)]
    [InlineData(0.42, 0.42)]
    [InlineData(3d, 1d)]
    public void Buffered_progress_is_clamped(double reported, double expected)
    {
        var bar = new MediaSeekBar { BufferedProgress = reported };

        DrawableOf(bar).Buffered.ShouldBe(expected, 0.0001d);
    }

    [Fact]
    public void Styling_properties_reach_the_drawable()
    {
        var bar = new MediaSeekBar { TrackHeight = 6, ThumbSize = 18 };
        var drawable = DrawableOf(bar);

        drawable.TrackHeight.ShouldBe(6f);
        drawable.ThumbSize.ShouldBe(18f);
    }

    // The bindable properties feed the drawable through Refresh(); reading it back is how the maths is
    // observable without a rendering surface.
    static SeekBarDrawable DrawableOf(MediaSeekBar bar) => (SeekBarDrawable)bar.Drawable!;
}
