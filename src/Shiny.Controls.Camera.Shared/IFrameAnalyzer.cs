namespace Shiny.Controls.Camera;

/// <summary>
/// A pluggable frame analyzer. Implementations inspect a <see cref="CameraFrame"/> and return
/// <see cref="Detection"/>s in normalized upright image space. The camera pipeline runs each analyzer
/// with a max-in-flight of one and drops frames while it is busy, so an analyzer may take as long as a
/// frame interval without backing up the camera. Implementations must be allocation-light and must not
/// retain the frame past the returned task.
/// </summary>
public interface IFrameAnalyzer
{
    /// <summary>Stable identifier used to key/replace this analyzer's results in the overlay.</summary>
    string Id { get; }

    /// <summary>
    /// Analyze a single frame. Return <c>null</c> (or an empty <see cref="DetectionResult"/>) when there
    /// is nothing to report. Honor <paramref name="ct"/> for cooperative cancellation when the camera stops.
    /// </summary>
    ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct);
}
