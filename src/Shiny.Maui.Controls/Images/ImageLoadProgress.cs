namespace Shiny.Maui.Controls.Images;

/// <summary>
/// A snapshot of an in-flight image load. This is the binding context handed to a custom
/// <c>LoadingTemplate</c>, so everything a template could reasonably want to show is on it.
/// </summary>
/// <param name="State">Where the load is.</param>
/// <param name="BytesRead">Bytes received so far. Zero while queued.</param>
/// <param name="TotalBytes">
/// The expected size, or null when the server did not send a content length (chunked transfer, or
/// a proxy that stripped it).
/// </param>
public record ImageLoadProgress(ImageLoadState State, long BytesRead = 0, long? TotalBytes = null)
{
    /// <summary>The idle snapshot - nothing requested.</summary>
    public static readonly ImageLoadProgress None = new(ImageLoadState.None);

    /// <summary>Waiting for a download slot.</summary>
    public static readonly ImageLoadProgress Queued = new(ImageLoadState.Queued);

    /// <summary>
    /// Completion from 0-1, or <c>null</c> when it cannot be known.
    ///
    /// <para>Null is the signal the ring uses to pick indeterminate over determinate, and it is null
    /// in three distinct situations that all look the same to a user: the request is still queued,
    /// the server sent no content length, or the content length was zero/nonsense. Callers should
    /// test for null rather than comparing against 0, because 0 is a legitimate determinate value at
    /// the instant a measured download starts.</para>
    /// </summary>
    public double? Percent => this.State == ImageLoadState.Downloading && this.TotalBytes is > 0
        ? Math.Clamp((double)this.BytesRead / this.TotalBytes.Value, 0, 1)
        : null;

    /// <summary>True when the ring should spin rather than fill.</summary>
    public bool IsIndeterminate => this.Percent is null;

    /// <summary>Convenience for templates: <c>Percent</c> as 0-100, or 0 when indeterminate.</summary>
    public double PercentDisplay => (this.Percent ?? 0) * 100;
}
