using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

/// <summary>
/// The video capture settings are session-level bindable properties. The native ladders they map onto live in
/// the platform handlers and cannot be reached from the base TFM, so what is pinned here is the contract every
/// platform reads from: the defaults, and that null means "platform decides".
/// </summary>
public class CameraViewVideoSettingsTests
{
    [Fact]
    public void Quality_defaults_to_1080p()
    {
        // Deliberately explicit rather than "whatever the platform likes". Before these properties existed
        // every platform picked its own — CameraX 720p, AVFoundation PresetHigh, Windows 720p — so the same
        // app recorded visibly different footage depending on where it ran. The default is the fix for that,
        // which makes it worth a test that fails loudly if someone changes it.
        new CameraView().VideoQuality.ShouldBe(VideoQuality.High);
    }


    [Fact]
    public void Bitrate_and_frame_rate_default_to_the_platform_choice()
    {
        var view = new CameraView();

        // null is not "zero" — it is "do not pass a target at all", which is what lets each platform apply
        // the tuned default for the negotiated resolution
        view.VideoBitrate.ShouldBeNull();
        view.VideoFrameRate.ShouldBeNull();
    }


    [Fact]
    public void Audio_mixes_with_other_apps_by_default()
    {
        // The default is what fixes the bug, so it is the part worth pinning. Exclusive is what iOS applies on
        // its own — and it costs both directions at once: starting a recording stops whatever was playing
        // (music over CarPlay, a navigation app), and anything starting playback afterwards interrupts the
        // capture session, which stops video as well as audio. Flipping this default back would reintroduce
        // both, silently, in the platform layer where no test can see it.
        new CameraView().MixWithOtherAudio.ShouldBeTrue();
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Audio_mixing_round_trips(bool mix)
    {
        var view = new CameraView { MixWithOtherAudio = mix };
        view.MixWithOtherAudio.ShouldBe(mix);
    }


    [Theory]
    [InlineData(VideoQuality.Lowest)]
    [InlineData(VideoQuality.Low)]
    [InlineData(VideoQuality.Medium)]
    [InlineData(VideoQuality.High)]
    [InlineData(VideoQuality.UltraHigh)]
    [InlineData(VideoQuality.Highest)]
    public void Quality_round_trips(VideoQuality quality)
    {
        var view = new CameraView { VideoQuality = quality };
        view.VideoQuality.ShouldBe(quality);
    }


    [Fact]
    public void Bitrate_and_frame_rate_round_trip()
    {
        var view = new CameraView { VideoBitrate = 8_000_000, VideoFrameRate = 24 };

        view.VideoBitrate.ShouldBe(8_000_000);
        view.VideoFrameRate.ShouldBe(24);
    }


    [Fact]
    public void The_quality_ladder_is_ordered_smallest_to_largest()
    {
        // Every platform mapping is written as a switch over these in ascending order, and Android's
        // FallbackStrategy.LowerQualityOrHigherThan degrades *downwards* on unsupported hardware. Both read
        // correctly only while the enum is ordered — reordering it would silently invert the fallback.
        ((int)VideoQuality.Lowest).ShouldBeLessThan((int)VideoQuality.Low);
        ((int)VideoQuality.Low).ShouldBeLessThan((int)VideoQuality.Medium);
        ((int)VideoQuality.Medium).ShouldBeLessThan((int)VideoQuality.High);
        ((int)VideoQuality.High).ShouldBeLessThan((int)VideoQuality.UltraHigh);
        ((int)VideoQuality.UltraHigh).ShouldBeLessThan((int)VideoQuality.Highest);
    }
}
