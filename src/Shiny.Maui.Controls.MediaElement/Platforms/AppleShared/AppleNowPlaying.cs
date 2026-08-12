using AVFoundation;
using Foundation;
using MediaPlayer;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// Publishes now-playing metadata to the lock screen / Control Center / macOS media widget, and routes
/// the OS transport buttons back into the player.
/// </summary>
/// <remarks>
/// The remote command centre is process-global: registering a handler twice stacks them and every press
/// fires both. This type therefore owns exactly one registration per instance and tears it down in
/// <see cref="Disable"/>, which the backend calls on dispose as well as when background playback is
/// turned off.
/// </remarks>
class AppleNowPlaying : IDisposable
{
    readonly Action<MediaRemoteCommand> onCommand;
    readonly Action<TimeSpan> onSeek;
    bool enabled;

    // MPRemoteCommand.AddTarget returns a token that must be handed back to RemoveTarget — holding the
    // lambda isn't enough.
    NSObject? playToken;
    NSObject? pauseToken;
    NSObject? toggleToken;
    NSObject? stopToken;
    NSObject? seekToken;

    public AppleNowPlaying(Action<MediaRemoteCommand> onCommand, Action<TimeSpan> onSeek)
    {
        this.onCommand = onCommand;
        this.onSeek = onSeek;
    }

    public void Enable(MediaMetadata? metadata, TimeSpan duration, TimeSpan position, double rate)
    {
        if (!this.enabled)
        {
            this.ConfigureAudioSession();
            this.RegisterCommands();
            this.enabled = true;
        }

        this.Update(metadata, duration, position, rate);
    }

    public void Disable()
    {
        if (!this.enabled)
            return;

        this.UnregisterCommands();
        // Assigning null is how MPNowPlayingInfoCenter is cleared; the binding just isn't annotated for it.
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = null!;
        this.enabled = false;
    }

    public void Update(MediaMetadata? metadata, TimeSpan duration, TimeSpan position, double rate)
    {
        if (!this.enabled)
            return;

        var info = new MPNowPlayingInfo
        {
            Title = metadata?.Title ?? String.Empty,
            Artist = metadata?.Artist,
            AlbumTitle = metadata?.Album,
            PlaybackDuration = duration.TotalSeconds,
            ElapsedPlaybackTime = position.TotalSeconds,
            PlaybackRate = rate
        };

        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = info;
    }

    void ConfigureAudioSession()
    {
#if IOS || MACCATALYST
        // Without the Playback category the session is Ambient and iOS silences it the moment the screen
        // locks — the single most common reason "background audio" appears not to work.
        var session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.Playback);
        session.SetActive(true, out _);
#endif
    }

    void RegisterCommands()
    {
        var center = MPRemoteCommandCenter.Shared;

        center.PlayCommand.Enabled = true;
        this.playToken = center.PlayCommand.AddTarget(_ =>
        {
            this.onCommand(MediaRemoteCommand.Play);
            return MPRemoteCommandHandlerStatus.Success;
        });

        center.PauseCommand.Enabled = true;
        this.pauseToken = center.PauseCommand.AddTarget(_ =>
        {
            this.onCommand(MediaRemoteCommand.Pause);
            return MPRemoteCommandHandlerStatus.Success;
        });

        center.TogglePlayPauseCommand.Enabled = true;
        this.toggleToken = center.TogglePlayPauseCommand.AddTarget(_ =>
        {
            this.onCommand(MediaRemoteCommand.TogglePlayPause);
            return MPRemoteCommandHandlerStatus.Success;
        });

        center.StopCommand.Enabled = true;
        this.stopToken = center.StopCommand.AddTarget(_ =>
        {
            this.onCommand(MediaRemoteCommand.Stop);
            return MPRemoteCommandHandlerStatus.Success;
        });

        center.ChangePlaybackPositionCommand.Enabled = true;
        this.seekToken = center.ChangePlaybackPositionCommand.AddTarget(evt =>
        {
            if (evt is not MPChangePlaybackPositionCommandEvent position)
                return MPRemoteCommandHandlerStatus.CommandFailed;

            this.onSeek(TimeSpan.FromSeconds(position.PositionTime));
            this.onCommand(MediaRemoteCommand.Seek);
            return MPRemoteCommandHandlerStatus.Success;
        });
    }

    void UnregisterCommands()
    {
        var center = MPRemoteCommandCenter.Shared;

        Remove(center.PlayCommand, ref this.playToken);
        Remove(center.PauseCommand, ref this.pauseToken);
        Remove(center.TogglePlayPauseCommand, ref this.toggleToken);
        Remove(center.StopCommand, ref this.stopToken);
        Remove(center.ChangePlaybackPositionCommand, ref this.seekToken);

        static void Remove(MPRemoteCommand command, ref NSObject? token)
        {
            if (token is null)
                return;

            command.RemoveTarget(token);
            token = null;
        }
    }

    public void Dispose() => this.Disable();
}
