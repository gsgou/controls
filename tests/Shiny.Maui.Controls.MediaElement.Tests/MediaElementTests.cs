using Microsoft.Maui.Graphics;
using Shiny.Controls.Media;
using Shiny.Maui.Controls.Media;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Media.Tests;

public class MediaElementTests : MediaTestBase
{
    [Fact]
    public void Transport_calls_reach_the_backend()
    {
        var element = new MediaElement();

        element.Play();
        element.Pause();
        element.Stop();

        this.Backend.Calls.ShouldContain("Play");
        this.Backend.Calls.ShouldContain("Pause");
        this.Backend.Calls.ShouldContain("Stop");
    }

    [Fact]
    public async Task Seek_clamps_to_the_media_length()
    {
        var element = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(2));

        await element.SeekAsync(TimeSpan.FromMinutes(10));

        this.Backend.Calls.ShouldContain($"Seek({TimeSpan.FromMinutes(2)})");
    }

    [Fact]
    public async Task Seek_clamps_negative_positions_to_zero()
    {
        var element = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(2));

        await element.SeekAsync(TimeSpan.FromSeconds(-5));

        this.Backend.Calls.ShouldContain($"Seek({TimeSpan.Zero})");
    }

    [Fact]
    public void Assigning_Position_from_outside_seeks()
    {
        var element = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(5));

        element.Position = TimeSpan.FromSeconds(42);

        this.Backend.Calls.ShouldContain($"Seek({TimeSpan.FromSeconds(42)})");
    }

    [Fact]
    public void The_position_tick_does_not_seek_back_to_where_the_player_already_is()
    {
        // The timer writes the player's own position into the Position property. If that round-tripped
        // into a seek, remote streams would re-buffer four times a second.
        var element = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(5));
        this.Backend.RaiseState(MediaElementState.Playing);

        this.Backend.Position = TimeSpan.FromSeconds(3);
        var timer = TimerWithInterval(element.PositionUpdateInterval);
        timer.ShouldNotBeNull();
        timer.Fire();

        element.Position.ShouldBe(TimeSpan.FromSeconds(3));
        this.Backend.Calls.ShouldNotContain($"Seek({TimeSpan.FromSeconds(3)})");
    }

    [Fact]
    public void The_position_timer_only_runs_while_playing()
    {
        // The timer is created lazily on the first Playing transition — an element that never plays
        // shouldn't leave a dispatcher timer behind.
        var element = new MediaElement();
        TimerWithInterval(element.PositionUpdateInterval).ShouldBeNull();

        this.Backend.RaiseState(MediaElementState.Playing);
        var timer = TimerWithInterval(element.PositionUpdateInterval);
        timer.ShouldNotBeNull();
        timer.IsRunning.ShouldBeTrue();

        this.Backend.RaiseState(MediaElementState.Paused);
        timer.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Backend_state_flows_into_CurrentState_and_the_event()
    {
        var element = new MediaElement();
        var observed = new List<MediaElementState>();
        element.StateChanged += (_, state) => observed.Add(state);

        this.Backend.RaiseState(MediaElementState.Buffering);
        this.Backend.RaiseState(MediaElementState.Playing);

        element.CurrentState.ShouldBe(MediaElementState.Playing);
        observed.ShouldBe([MediaElementState.Buffering, MediaElementState.Playing]);
    }

    [Fact]
    public void Opening_publishes_the_duration_and_capabilities()
    {
        var element = new MediaElement();
        var opened = false;
        element.MediaOpened += (_, _) => opened = true;

        this.Backend.RaiseOpened(TimeSpan.FromSeconds(90));

        opened.ShouldBeTrue();
        element.Duration.ShouldBe(TimeSpan.FromSeconds(90));
        element.Capabilities.ShouldBe(this.Backend.Capabilities);
    }

    [Fact]
    public void A_backend_failure_surfaces_as_Failed_state_and_the_event()
    {
        var element = new MediaElement();
        MediaFailure? failure = null;
        element.MediaFailed += (_, f) => failure = f;

        this.Backend.RaiseFailed("boom");

        failure!.Message.ShouldBe("boom");
        element.CurrentState.ShouldBe(MediaElementState.Failed);
    }

    [Fact]
    public void Setting_a_source_opens_it_on_the_backend()
    {
        var element = new MediaElement
        {
            Source = MediaSource.FromUri("https://example.com/clip.mp4")
        };

        this.Backend.OpenCount.ShouldBe(1);
        this.Backend.OpenedSource.ShouldBeOfType<UriMediaSource>();
    }

    [Fact]
    public void AutoPlay_starts_playback_once_the_source_opens()
    {
        var element = new MediaElement { AutoPlay = true };
        element.Source = MediaSource.FromUri("https://example.com/clip.mp4");

        this.Backend.Calls.ShouldContain("Play");
    }

    [Fact]
    public void Without_AutoPlay_a_new_source_stays_paused()
    {
        var element = new MediaElement();
        element.Source = MediaSource.FromUri("https://example.com/clip.mp4");

        this.Backend.Calls.ShouldNotContain("Play");
    }

    [Fact]
    public void Playback_settings_are_forwarded_as_they_change()
    {
        var element = new MediaElement
        {
            Volume = 0.4,
            IsMuted = true,
            PlaybackRate = 1.5,
            IsLooping = true,
            Aspect = MediaAspect.AspectFill,
            KeepScreenOn = true
        };

        this.Backend.Volume.ShouldBe(0.4);
        this.Backend.Muted.ShouldBeTrue();
        this.Backend.Rate.ShouldBe(1.5);
        this.Backend.Looping.ShouldBeTrue();
        this.Backend.Aspect.ShouldBe(MediaAspect.AspectFill);
        this.Backend.KeepScreenOn.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.5, 0d)]
    [InlineData(1.7, 1d)]
    public void Volume_is_clamped_to_zero_through_one(double assigned, double expected)
    {
        var element = new MediaElement { Volume = assigned };
        element.Volume.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.1, 0.25)]
    [InlineData(9d, 4d)]
    public void PlaybackRate_is_clamped_to_a_playable_range(double assigned, double expected)
    {
        var element = new MediaElement { PlaybackRate = assigned };
        element.PlaybackRate.ShouldBe(expected);
    }

    [Fact]
    public void ToggleMute_flips_the_property_and_the_backend()
    {
        var element = new MediaElement();

        element.ToggleMute();
        element.IsMuted.ShouldBeTrue();
        this.Backend.Muted.ShouldBeTrue();

        element.ToggleMute();
        element.IsMuted.ShouldBeFalse();
        this.Backend.Muted.ShouldBeFalse();
    }

    [Fact]
    public void Background_playback_publishes_the_metadata()
    {
        var metadata = new MediaMetadata { Title = "Episode 1" };
        var element = new MediaElement
        {
            EnableBackgroundPlayback = true,
            Metadata = metadata
        };

        this.Backend.BackgroundEnabled.ShouldBeTrue();
        this.Backend.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public async Task Picture_in_picture_reports_the_platform_verdict()
    {
        var element = new MediaElement();

        (await element.TryEnterPictureInPictureAsync()).ShouldBeFalse();

        this.Backend.PictureInPictureSucceeds = true;
        (await element.TryEnterPictureInPictureAsync()).ShouldBeTrue();
        element.IsPictureInPictureActive.ShouldBeTrue();
    }

    [Fact]
    public void MediaEnded_is_forwarded()
    {
        var element = new MediaElement();
        var ended = false;
        element.MediaEnded += (_, _) => ended = true;

        this.Backend.RaiseEnded();

        ended.ShouldBeTrue();
    }

    [Fact]
    public void A_remote_transport_command_refreshes_the_position()
    {
        // The OS transport UI drives the player directly; the control only has to re-read where it landed.
        var element = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(3));
        this.Backend.Position = TimeSpan.FromSeconds(75);

        this.Backend.RaiseRemoteCommand(MediaRemoteCommand.Seek);

        element.Position.ShouldBe(TimeSpan.FromSeconds(75));
    }

    [Fact]
    public void A_fullscreen_mirror_starts_from_the_running_player_not_from_defaults()
    {
        // Regression: the fullscreen page used to open reading 0:00 / 0:00 with an empty scrubber over a
        // video 30 seconds in, because the mirror started from property defaults and the next position
        // tick never comes while playback is paused.
        var owner = new MediaElement();
        this.Backend.RaiseOpened(TimeSpan.FromMinutes(2));
        this.Backend.RaiseState(MediaElementState.Paused);
        this.Backend.Position = TimeSpan.FromSeconds(30);
        this.Backend.BufferedProgress = 0.5;

        var mirror = new MediaElement(owner);

        mirror.Duration.ShouldBe(TimeSpan.FromMinutes(2));
        mirror.Position.ShouldBe(TimeSpan.FromSeconds(30));
        mirror.BufferedProgress.ShouldBe(0.5);
        mirror.CurrentState.ShouldBe(MediaElementState.Paused);
        mirror.IsFullScreen.ShouldBeTrue();
    }

    [Fact]
    public void A_fullscreen_mirror_adopts_the_owners_transport_configuration()
    {
        var owner = new MediaElement
        {
            ShowVolumeControl = false,
            ShowTimeLabels = false,
            AutoHideTransportBar = false,
            SeekBarColor = Colors.Red
        };

        var mirror = new MediaElement(owner);

        mirror.ShowVolumeControl.ShouldBeFalse();
        mirror.ShowTimeLabels.ShouldBeFalse();
        mirror.AutoHideTransportBar.ShouldBeFalse();
        mirror.SeekBarColor.ShouldBe(Colors.Red);
    }

    [Fact]
    public void The_control_has_no_backend_when_none_is_registered()
    {
        MediaPlayerBackends.Factory = null;
        MediaPlayerBackends.IsSupported.ShouldBeFalse();

        // An unsupported host must still lay the page out rather than throw from the constructor.
        var element = new MediaElement { Source = MediaSource.FromUri("https://example.com/clip.mp4") };

        Should.NotThrow(() => element.Play());
        Should.NotThrow(() => element.Stop());
        element.CurrentState.ShouldBe(MediaElementState.None);
    }
}
