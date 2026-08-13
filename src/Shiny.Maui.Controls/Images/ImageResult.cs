namespace Shiny.Maui.Controls.Images;

/// <summary>Which tier answered a request.</summary>
public enum ImageOrigin
{
    /// <summary>Served from the in-process byte cache.</summary>
    Memory,

    /// <summary>Read from the on-disk cache.</summary>
    Disk,

    /// <summary>Downloaded.</summary>
    Network
}


/// <summary>
/// The outcome of <see cref="IImageService.GetAsync"/>.
/// </summary>
/// <remarks>
/// Two payload shapes rather than one, deliberately. <see cref="Bytes"/> is set when the image is
/// small enough to hold in memory, and a caller should prefer it - building an
/// <c>ImageSource.FromStream</c> over a byte array avoids a file read per cell in a scrolling list.
/// Large images carry only <see cref="FilePath"/> so a 20MB photo is never resident.
///
/// <para>Note what is deliberately <b>not</b> here: an <c>ImageSource</c>. A stream-backed
/// ImageSource is consumed once and platform image handles are bound to the view that realized
/// them, so caching and sharing one across cells produces blank images. The caller builds a fresh
/// ImageSource from these bytes every time, which costs nothing.</para>
/// </remarks>
public record ImageResult
{
    ImageResult() { }

    /// <summary>Whether the image was obtained.</summary>
    public bool Success { get; private init; }

    /// <summary>The encoded image bytes, when small enough to have been held. May be null on success.</summary>
    public byte[]? Bytes { get; private init; }

    /// <summary>The on-disk cache entry, when caching is on. May be null on success.</summary>
    public string? FilePath { get; private init; }

    /// <summary>Size of the image in bytes, when known.</summary>
    public long? ContentLength { get; private init; }

    /// <summary>Which tier answered.</summary>
    public ImageOrigin Origin { get; private init; }

    /// <summary>Why the load failed. Null on success.</summary>
    public Exception? Error { get; private init; }

    internal static ImageResult Ok(ImageOrigin origin, byte[]? bytes, string? filePath, long? contentLength) => new()
    {
        Success = true,
        Origin = origin,
        Bytes = bytes,
        FilePath = filePath,
        ContentLength = contentLength ?? bytes?.LongLength
    };

    internal static ImageResult Fail(Exception error) => new() { Success = false, Error = error };
}
