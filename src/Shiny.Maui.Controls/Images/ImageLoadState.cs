namespace Shiny.Maui.Controls.Images;

/// <summary>
/// Where a <see cref="ShinyImage"/> is in its load.
/// </summary>
public enum ImageLoadState
{
    /// <summary>Nothing has been asked for yet - no <c>Uri</c> and no <c>Source</c>.</summary>
    None,

    /// <summary>
    /// The request is waiting for a download slot. There is nothing to measure yet, so the ring
    /// spins without a percentage no matter how big the image turns out to be.
    /// </summary>
    Queued,

    /// <summary>Bytes are arriving. Determinate only when the response carried a content length.</summary>
    Downloading,

    /// <summary>The image is on screen.</summary>
    Loaded,

    /// <summary>The load failed. The error artwork is showing.</summary>
    Failed
}
