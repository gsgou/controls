namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// Target capture resolution for video recording, expressed as an intent rather than exact pixels.
/// </summary>
/// <remarks>
/// <para>
/// Each platform maps these onto its own native ladder — CameraX <c>Quality</c>, AVFoundation session
/// presets, Windows <c>VideoEncodingQuality</c> — so the same value means the same thing everywhere it can.
/// A device that cannot deliver the requested rung falls back to the nearest one it supports rather than
/// failing: capture hardware varies enormously and a camera that refuses to start is never the better
/// outcome. Read <see cref="CameraView.VideoQuality"/> back to see what was asked for, not what was granted —
/// the negotiated size is a property of the recorded file.
/// </para>
/// <para>
/// This is the <i>capture</i> resolution, so on Apple it sizes the whole session: the preview and any frame
/// analysis see it too. That is deliberate — a preview that does not match the recording is its own class of
/// bug — but it means dropping to <see cref="Low"/> for storage reasons also reduces what an analyzer has to
/// work with.
/// </para>
/// </remarks>
public enum VideoQuality
{
    /// <summary>The lowest resolution the device offers. Smallest files and least heat; typically QVGA/CIF.</summary>
    Lowest,

    /// <summary>Standard definition, around 480p.</summary>
    Low,

    /// <summary>720p.</summary>
    Medium,

    /// <summary>1080p. The default — see <see cref="CameraView.VideoQuality"/>.</summary>
    High,

    /// <summary>2160p (4K), where the device supports it.</summary>
    UltraHigh,

    /// <summary>The highest resolution the device offers. Resolves to 4K or beyond on modern hardware.</summary>
    Highest
}
