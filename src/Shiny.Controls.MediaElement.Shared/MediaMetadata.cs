namespace Shiny.Controls.Media;

/// <summary>
/// Now-playing information published to the OS while <c>EnableBackgroundPlayback</c> is on — the iOS/macOS
/// lock screen and Control Center (MPNowPlayingInfoCenter), the Android media notification (Media3
/// MediaSession), the Windows SMTC flyout, and the browser's media session UI.
/// </summary>
/// <remarks>
/// This is metadata only; it never affects decoding. Leaving it null still gives you background audio,
/// you just get an unlabelled entry in the OS transport UI.
/// </remarks>
public class MediaMetadata
{
    /// <summary>Track/episode title — the primary line in every OS transport UI.</summary>
    public string? Title { get; set; }

    /// <summary>Artist, channel, or speaker — the secondary line.</summary>
    public string? Artist { get; set; }

    /// <summary>Album/show/collection name. Shown by iOS and Windows; ignored by some Android skins.</summary>
    public string? Album { get; set; }

    /// <summary>
    /// Absolute URI of the artwork to show alongside the transport controls. Remote URIs are fetched by
    /// the backend on a background thread; a local <c>file://</c> URI avoids that round-trip.
    /// </summary>
    public string? ArtworkUri { get; set; }
}
