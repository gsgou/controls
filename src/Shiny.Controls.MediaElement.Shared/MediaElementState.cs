namespace Shiny.Controls.Media;

/// <summary>
/// The lifecycle state of a media player, reported by every backend (AVPlayer, ExoPlayer,
/// Windows MediaPlayer, GtkMediaFile, HTML5 media).
/// </summary>
public enum MediaElementState
{
    /// <summary>No source has been set, or the source was cleared.</summary>
    None,

    /// <summary>A source was set and the backend is resolving/loading it. Duration is not known yet.</summary>
    Opening,

    /// <summary>Playback is stalled while the backend refills its buffer. It resumes on its own.</summary>
    Buffering,

    /// <summary>Media is advancing.</summary>
    Playing,

    /// <summary>Playback is suspended at the current position and can be resumed in place.</summary>
    Paused,

    /// <summary>Playback is halted and the position has been reset to the start.</summary>
    Stopped,

    /// <summary>The source could not be opened or playback aborted. See the failure message.</summary>
    Failed
}
