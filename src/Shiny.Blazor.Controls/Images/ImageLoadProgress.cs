namespace Shiny.Blazor.Controls.Images;

/// <summary>
/// Where a <see cref="ShinyImage"/> is in its load. Mirrors the MAUI enum of the same name so a
/// shared view model reads identically on both hosts.
/// </summary>
public enum ImageLoadState
{
    /// <summary>Nothing has been asked for yet.</summary>
    None,

    /// <summary>The request has been made but no bytes have arrived.</summary>
    Queued,

    /// <summary>Bytes are arriving. Determinate only when the response carried a content length.</summary>
    Downloading,

    /// <summary>The image is on screen.</summary>
    Loaded,

    /// <summary>The load failed. The error artwork is showing.</summary>
    Failed
}


/// <summary>
/// A snapshot of an in-flight image load - the context handed to a custom <c>LoadingContent</c>
/// fragment.
/// </summary>
/// <param name="State">Where the load is.</param>
/// <param name="BytesRead">Bytes received so far.</param>
/// <param name="TotalBytes">
/// The expected size, or null when the server sent no <c>Content-Length</c> - or when the browser is
/// loading the image itself, which it does without telling anyone how far along it is.
/// </param>
public record ImageLoadProgress(ImageLoadState State, long BytesRead = 0, long? TotalBytes = null)
{
    /// <summary>The idle snapshot.</summary>
    public static readonly ImageLoadProgress None = new(ImageLoadState.None);

    /// <summary>Requested, nothing measured yet.</summary>
    public static readonly ImageLoadProgress Queued = new(ImageLoadState.Queued);

    /// <summary>Completion from 0-1, or null when it cannot be known.</summary>
    public double? Percent => this.State == ImageLoadState.Downloading && this.TotalBytes is > 0
        ? Math.Clamp((double)this.BytesRead / this.TotalBytes.Value, 0, 1)
        : null;

    /// <summary>True when the ring should spin rather than fill.</summary>
    public bool IsIndeterminate => this.Percent is null;

    /// <summary>Convenience for templates: <c>Percent</c> as 0-100, or 0 when indeterminate.</summary>
    public double PercentDisplay => (this.Percent ?? 0) * 100;
}
