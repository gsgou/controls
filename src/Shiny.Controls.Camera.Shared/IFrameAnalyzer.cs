namespace Shiny.Controls.Camera;

/// <summary>
/// A pluggable frame analyzer. Implementations inspect a <see cref="CameraFrame"/> and return the styled
/// <see cref="OverlayBox"/>es they want drawn over the preview (in normalized upright image space), while
/// surfacing their semantic result through their own strongly-typed event (e.g. a barcode analyzer's
/// decoded value). The pipeline runs each analyzer with a max-in-flight of one and drops frames while it
/// is busy, so an analyzer may take as long as a frame interval without backing up the camera.
/// Implementations must be allocation-light and must not retain the frame past the returned task.
/// </summary>
/// <remarks>
/// Prefer deriving from <c>FrameAnalyzer</c> (in <c>Shiny.Maui.Controls.Camera</c>), which implements this
/// interface, fires typed events/commands on the UI thread, and adds a <c>ShowBoundingBox</c> toggle.
/// </remarks>
public interface IFrameAnalyzer
{
    /// <summary>Stable identifier used to key/replace this analyzer's boxes in the overlay.</summary>
    string Id { get; }

    /// <summary>
    /// Analyze a single frame and return the boxes to draw for this analyzer. The returned set
    /// <b>replaces</b> this analyzer's previous boxes and persists across subsequent frames until it is
    /// next replaced; return <c>null</c> to <b>clear</b> them (nothing is currently seen). Raise any
    /// semantic result through the analyzer's own typed event before returning. Honor <paramref name="ct"/>
    /// for cooperative cancellation when the camera stops.
    /// </summary>
    ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct);
}
