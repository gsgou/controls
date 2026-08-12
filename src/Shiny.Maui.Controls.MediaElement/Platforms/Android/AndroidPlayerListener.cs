using AndroidX.Media3.Common;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// Bridges the Media3 <c>Player.Listener</c> callbacks we care about onto plain delegates.
/// </summary>
/// <remarks>
/// Every member of <see cref="IPlayerListener"/> is a Java default method, so only the four that matter
/// are implemented here — the rest fall through to Media3's own no-op defaults.
/// </remarks>
class AndroidPlayerListener : Java.Lang.Object, IPlayerListener
{
    readonly Action<int> onPlaybackStateChanged;
    readonly Action<bool> onIsPlayingChanged;
    readonly Action<PlaybackException?> onPlayerError;
    readonly Action onTracksReady;

    public AndroidPlayerListener(
        Action<int> onPlaybackStateChanged,
        Action<bool> onIsPlayingChanged,
        Action<PlaybackException?> onPlayerError,
        Action onTracksReady)
    {
        this.onPlaybackStateChanged = onPlaybackStateChanged;
        this.onIsPlayingChanged = onIsPlayingChanged;
        this.onPlayerError = onPlayerError;
        this.onTracksReady = onTracksReady;
    }

    public void OnPlaybackStateChanged(int playbackState) => this.onPlaybackStateChanged(playbackState);

    public void OnIsPlayingChanged(bool isPlaying) => this.onIsPlayingChanged(isPlaying);

    public void OnPlayerError(PlaybackException? error) => this.onPlayerError(error);

    public void OnVideoSizeChanged(VideoSize? videoSize) => this.onTracksReady();
}
