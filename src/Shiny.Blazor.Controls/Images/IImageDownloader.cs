namespace Shiny.Blazor.Controls.Images;

/// <summary>What a download produced.</summary>
/// <param name="Bytes">The encoded image.</param>
/// <param name="ContentType">The media type, used to type the blob handed to the <c>&lt;img&gt;</c>.</param>
public record ImageDownloadResult(byte[] Bytes, string? ContentType = null);


/// <summary>
/// How image bytes are fetched when the browser cannot do it for you.
/// </summary>
/// <remarks>
/// <para>This is the one thing a plain <c>&lt;img&gt;</c> genuinely cannot do: an image behind a
/// bearer token. The element sends whatever ambient cookies apply and nothing else - there is no way
/// to attach an <c>Authorization</c> header to it. Registering a downloader routes the fetch through
/// your own <c>HttpClient</c> instead, and <see cref="ShinyImage"/> wraps the resulting bytes in a
/// blob URL for display.</para>
///
/// <para>Everything else about image loading is left to the browser on purpose. It already has a
/// well-tuned HTTP cache with correct revalidation, shared across tabs and persisted between
/// sessions; a cache layer on top of that would duplicate it worse.</para>
/// </remarks>
/// <example>
/// <code>
/// class AuthenticatedDownloader(HttpClient client, ITokenStore tokens) : IImageDownloader
/// {
///     public async Task&lt;ImageDownloadResult&gt; DownloadAsync(
///         string uri, IProgress&lt;ImageLoadProgress&gt;? progress, CancellationToken ct)
///     {
///         var msg = new HttpRequestMessage(HttpMethod.Get, uri);
///         msg.Headers.Authorization = new("Bearer", await tokens.GetAsync(ct));
///
///         var response = await client.SendAsync(msg, ct);
///         response.EnsureSuccessStatusCode();
///
///         return new ImageDownloadResult(
///             await response.Content.ReadAsByteArrayAsync(ct),
///             response.Content.Headers.ContentType?.MediaType
///         );
///     }
/// }
///
/// // builder.Services.AddShinyImages&lt;AuthenticatedDownloader&gt;();
/// </code>
/// </example>
public interface IImageDownloader
{
    /// <summary>
    /// Fetch the image. Report progress if you can measure it; leaving <paramref name="progress"/>
    /// alone simply keeps the ring indeterminate. Throw on failure - the control turns that into its
    /// error artwork.
    /// </summary>
    Task<ImageDownloadResult> DownloadAsync(
        string uri,
        IProgress<ImageLoadProgress>? progress,
        CancellationToken cancellationToken
    );
}
