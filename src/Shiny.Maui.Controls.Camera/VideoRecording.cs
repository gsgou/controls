namespace Shiny.Maui.Controls.Camera;

/// <summary>Options for a single <see cref="CameraView.StartVideoRecordingAsync"/> call.</summary>
public class VideoRecordingOptions
{
    /// <summary>Capture audio alongside video. Default <c>true</c> (requires microphone permission).</summary>
    public bool IncludeAudio { get; set; } = true;

    /// <summary>
    /// Destination file path. When null, a unique temp path is used (extension chosen per platform —
    /// <c>.mov</c> on Apple, <c>.mp4</c> on Android/Windows).
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Optional overlay composited into <b>every recorded frame</b> (watermark, timestamp, telemetry, reticles).
    /// When null, the raw sensor feed is written via the platform's native recorder (fast path — no perf or
    /// behavior change). When set, an owned/composited encode path is used and <see cref="IVideoOverlayRenderer.DrawOverlay"/>
    /// is invoked per frame off the UI thread — see the interface docs for threading and coordinate rules.
    /// </summary>
    public IVideoOverlayRenderer? Overlay { get; set; }
}


/// <summary>The result of <see cref="CameraView.StopVideoRecordingAsync"/>.</summary>
/// <param name="FilePath">Absolute path to the recorded video file.</param>
/// <param name="Duration">Length of the recording, if known.</param>
public record CameraVideo(string FilePath, TimeSpan? Duration = null)
{
    /// <summary>Open a read stream over the recorded file.</summary>
    public Stream OpenRead() => File.OpenRead(this.FilePath);
}
