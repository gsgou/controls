namespace Shiny.Controls.Camera;

/// <summary>
/// The output of one <see cref="IFrameAnalyzer"/> pass over a single frame. Carries the analyzer
/// identity so the camera view can replace (not accumulate) the previous result from the same
/// analyzer when drawing the overlay.
/// </summary>
/// <param name="AnalyzerId">Stable identifier of the producing analyzer (see <see cref="IFrameAnalyzer.Id"/>).</param>
/// <param name="Detections">The detections found in the frame (may be empty).</param>
public record DetectionResult(string AnalyzerId, IReadOnlyList<Detection> Detections)
{
    /// <summary>An empty result for the given analyzer.</summary>
    public static DetectionResult Empty(string analyzerId) => new(analyzerId, []);

    /// <summary><c>true</c> when no detections were produced.</summary>
    public bool IsEmpty => this.Detections.Count == 0;
}
