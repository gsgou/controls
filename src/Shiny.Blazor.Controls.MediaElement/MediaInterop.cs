using Shiny.Controls.Media;

namespace Shiny.Blazor.Controls.Media;

/// <summary>
/// The player snapshot pushed up from JavaScript on every media event.
/// </summary>
/// <remarks>
/// A named DTO with plain properties, never an anonymous type and never an array of DTOs: the IL trimmer
/// follows the annotation only as far as the declared type, so both of those deserialize fine in a debug
/// run and then throw in a trimmed/published WebAssembly build.
/// </remarks>
public class MediaStatus
{
    /// <summary>The player state, as one of the <see cref="MediaElementState"/> names.</summary>
    public string State { get; set; } = nameof(MediaElementState.None);

    /// <summary>Playhead position in seconds.</summary>
    public double Position { get; set; }

    /// <summary>Total length in seconds; 0 while unknown or for a live stream.</summary>
    public double Duration { get; set; }

    /// <summary>Buffered-ahead fraction, 0..1.</summary>
    public double Buffered { get; set; }

    /// <summary>Whether output is currently muted.</summary>
    public bool Muted { get; set; }

    /// <summary>Current volume, 0..1.</summary>
    public double Volume { get; set; }

    /// <summary>Video track width in pixels; 0 for audio-only media.</summary>
    public int Width { get; set; }

    /// <summary>Video track height in pixels; 0 for audio-only media.</summary>
    public int Height { get; set; }
}


/// <summary>What the current browser will actually honour, probed once when the component starts.</summary>
public class MediaBrowserCapabilities
{
    /// <summary>
    /// Whether <c>video.volume</c> is settable. iOS Safari refuses it outright — the volume slider is
    /// hidden there rather than left as a control that does nothing.
    /// </summary>
    public bool Volume { get; set; }

    /// <summary>Whether the Picture-in-Picture API is available.</summary>
    public bool PictureInPicture { get; set; }

    /// <summary>Whether the Fullscreen API is available.</summary>
    public bool Fullscreen { get; set; }

    /// <summary>Whether <c>navigator.mediaSession</c> is available for OS transport metadata.</summary>
    public bool MediaSession { get; set; }
}
