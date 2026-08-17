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
    /// Whether this analyzer wants the frame that is about to be delivered. Called on the capture thread,
    /// once per frame, <b>before the platform materializes anything</b> — return <c>false</c> and the frame
    /// is skipped without being wrapped at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>This is where a rate limit belongs, and returning early from
    /// <see cref="AnalyzeAsync"/> is not the same thing.</b> By the time <c>AnalyzeAsync</c> runs, the
    /// platform has already built a <see cref="CameraFrame"/> for the buffer — which on Apple means a
    /// full-frame pixel copy (8.3 MB at 1080p). An analyzer that runs five passes a second but returns
    /// early from the other twenty-five was still paying for thirty frames a second; declaring the
    /// cadence here means it pays for five.
    /// </para>
    /// <para>
    /// Must be cheap and must not block: it runs on the capture callback, ahead of the encoder. Read a
    /// cached deadline, not a setting.
    /// </para>
    /// <para>
    /// The default returns <c>true</c> — every frame — which is the behaviour that existed before this
    /// member, so an analyzer written against an earlier version is unaffected.
    /// </para>
    /// </remarks>
    bool WantsFrame() => true;

    /// <summary>
    /// Analyze a single frame and return the boxes to draw for this analyzer. The returned set
    /// <b>replaces</b> this analyzer's previous boxes and persists across subsequent frames until it is
    /// next replaced; return <c>null</c> to <b>clear</b> them (nothing is currently seen). Raise any
    /// semantic result through the analyzer's own typed event before returning. Honor <paramref name="ct"/>
    /// for cooperative cancellation when the camera stops.
    /// </summary>
    ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct);
}
