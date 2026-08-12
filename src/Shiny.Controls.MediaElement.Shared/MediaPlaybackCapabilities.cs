namespace Shiny.Controls.Media;

/// <summary>
/// What the current platform backend can actually do. Read it before offering an affordance in a UI —
/// the transport bar uses it to hide buttons that would be dead on this platform.
/// </summary>
[Flags]
public enum MediaPlaybackCapabilities
{
    /// <summary>Nothing beyond play/pause/seek.</summary>
    None = 0,

    /// <summary>
    /// Audio keeps playing when the app is backgrounded or the device is locked, with OS transport
    /// controls (lock screen, notification, or media keys) driven by <c>MediaElement.Metadata</c>.
    /// </summary>
    BackgroundAudio = 1 << 0,

    /// <summary>The video image can be detached into a floating always-on-top window.</summary>
    PictureInPicture = 1 << 1,

    /// <summary><c>PlaybackRate</c> is honoured (all backends except some legacy GTK builds).</summary>
    PlaybackRate = 1 << 2,

    /// <summary>
    /// <c>Volume</c> is honoured per-player. Where this is absent the volume slider is hidden and
    /// only <c>IsMuted</c> works (iOS Safari, for example, refuses programmatic volume).
    /// </summary>
    Volume = 1 << 3,

    /// <summary>The backend reports how much of the media it has buffered ahead of the playhead.</summary>
    BufferProgress = 1 << 4
}
