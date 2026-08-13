namespace Shiny.Blazor.Controls.Images;

/// <summary>
/// A straightforward <see cref="IImageDownloader"/> over an injected <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// Registered by <c>AddShinyImages()</c> so that configuring the client - a base address, a delegating
/// handler that attaches a token - is enough to make authenticated images work without writing a
/// downloader at all.
/// </remarks>
public class HttpImageDownloader(HttpClient client) : IImageDownloader
{
    /// <inheritdoc />
    public virtual async Task<ImageDownloadResult> DownloadAsync(
        string uri,
        IProgress<ImageLoadProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        var response = await client
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        progress?.Report(new ImageLoadProgress(ImageLoadState.Downloading, 0, total));

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var memory = new MemoryStream();
            var buffer = new byte[65536];
            long read = 0;
            int count;

            while ((count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                memory.Write(buffer, 0, count);
                read += count;
                progress?.Report(new ImageLoadProgress(ImageLoadState.Downloading, read, total));
            }

            return new ImageDownloadResult(memory.ToArray(), response.Content.Headers.ContentType?.MediaType);
        }
    }
}
