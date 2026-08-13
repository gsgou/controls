namespace Shiny.Maui.Controls.Images;

/// <summary>
/// Owns how images are fetched and cached for <see cref="ShinyImage"/>.
/// </summary>
/// <remarks>
/// Most apps never touch this directly - the control resolves it. Take a dependency on it when you
/// want to warm a cache ahead of a page, or to expose a "clear cached images" button in settings.
///
/// <para>If all you need is your own <c>HttpClient</c> (auth headers, cookies, pinning), replace
/// <see cref="IImageDownloader"/> instead and keep the caching and queueing here.</para>
/// </remarks>
public interface IImageService
{
    /// <summary>
    /// Resolves an image, going to memory, then disk, then the network.
    /// </summary>
    /// <param name="request">What to fetch and how to cache it.</param>
    /// <param name="progress">
    /// Receives state changes and byte counts. Construct it on the UI thread (a plain
    /// <c>Progress&lt;T&gt;</c> is enough) so callbacks marshal back there.
    /// </param>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>
    /// A result that is never null. Failures come back as <c>Success == false</c> with the exception
    /// attached rather than as a throw, because a broken image URL in a list is an ordinary event
    /// that should render error artwork, not unwind the caller.
    /// </returns>
    Task<ImageResult> GetAsync(
        ImageRequest request,
        IProgress<ImageLoadProgress>? progress = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes every cached image, on disk and in memory.</summary>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes the cached entry for one URI.</summary>
    Task ClearCacheAsync(string uri, CancellationToken cancellationToken = default);

    /// <summary>Total bytes currently held on disk.</summary>
    Task<long> GetCacheSizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches images into the cache without displaying them - for warming the next page of a list.
    /// Failures are swallowed; a prefetch that could not run is not an error worth surfacing.
    /// </summary>
    Task PrefetchAsync(IEnumerable<string> uris, CancellationToken cancellationToken = default);
}
