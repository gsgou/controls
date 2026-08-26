using Shiny.Maui.Controls.Images.Svg;

namespace Shiny.Maui.Controls.Images;

/// <summary>
/// The paths that do not go through <see cref="IImageService"/>: embedded resources, <c>data:</c>
/// URIs, files, bundled assets - and, wherever the payload turns out to be SVG, the vector
/// presenter rather than the raster one.
/// </summary>
public partial class ShinyImage
{
    // Enough to see past a BOM, an XML declaration and a DOCTYPE before deciding what a payload is.
    const int SniffLength = 1024;


    async Task LoadLocalAsync(string uri, bool bypassCache, CancellationToken token)
    {
        // A bundled or on-disk raster goes straight to MAUI. Reading its bytes here to sniff them
        // would mean holding a full-resolution photo in memory to learn something the extension
        // already said.
        if (!MayBeVector(uri))
        {
            this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded));
            await this.ShowImageAsync(ImageSource.FromFile(uri)).ConfigureAwait(true);
            this.RaiseLoaded(uri, ImageOrigin.Disk, null);
            return;
        }

        var origin = ImageContent.IsResource(uri) || ImageContent.IsData(uri) ? ImageOrigin.Memory : ImageOrigin.Disk;

        // Reading a resource or a bundled file is fast but not free - a cold app-package read on
        // Android goes through the asset manager - so the ring spins rather than the frame sitting
        // empty. There is no percentage to report: none of these sources streams a length.
        this.SetProgress(new ImageLoadProgress(ImageLoadState.Downloading, 0, null));

        byte[] bytes;
        string cacheKey;
        try
        {
            (bytes, cacheKey) = await ReadContentAsync(uri, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            this.ShowError(uri, ex);
            return;
        }

        if (!this.StillWanted(uri, token))
            return;

        if (SvgDocument.LooksLikeSvg(bytes))
        {
            await this.ShowVectorAsync(uri, bytes, cacheKey, bypassCache, origin).ConfigureAwait(true);
            return;
        }

        // A resource:// or data: URI can perfectly well hold a PNG; only the bytes could have said.
        this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded, bytes.LongLength, bytes.LongLength));
        await this.ShowImageAsync(ImageSource.FromStream(() => new MemoryStream(bytes))).ConfigureAwait(true);
        this.RaiseLoaded(uri, origin, bytes.LongLength);
    }


    async Task ShowVectorAsync(string uri, byte[] bytes, string cacheKey, bool bypassCache, ImageOrigin origin)
    {
        SvgDocument document;
        try
        {
            // A forced reload has to drop the parse too, or ReloadAsync would re-fetch bytes and
            // then show the document built from the ones it just replaced.
            if (bypassCache)
                this.SvgCache.Remove(cacheKey);

            document = this.SvgCache.Get(cacheKey, () => SvgDocument.Parse(bytes));
        }
        catch (Exception ex)
        {
            this.ShowError(uri, ex);
            return;
        }

        this.targetImage.Source = null;
        this.targetImage.Opacity = 0;
        this.showingVector = true;

        this.vectorDrawable.Document = document;
        this.ApplyVectorAppearance();

        this.SetValue(LoadErrorPropertyKey, null);
        this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded, bytes.LongLength, bytes.LongLength));

        await this.FadeInAsync(this.vectorImage).ConfigureAwait(true);
        this.RaiseLoaded(uri, origin, bytes.LongLength);
    }


    /// <summary>Pushes the appearance properties a vector cares about onto the drawable.</summary>
    void ApplyVectorAppearance()
    {
        this.vectorDrawable.Aspect = this.Aspect;
        this.vectorDrawable.TintColor = this.SvgTintColor ?? Colors.Black;

        // GraphicsView caches its last frame, so changing what the drawable would paint is invisible
        // until it is told the answer changed.
        this.vectorImage.Invalidate();
    }


    static async Task<(byte[] Bytes, string CacheKey)> ReadContentAsync(string uri, CancellationToken cancellationToken)
    {
        if (ImageContent.IsResource(uri))
            return (ImageContent.ReadResource(uri), uri);

        if (ImageContent.IsData(uri))
        {
            var payload = ImageContent.ReadData(uri);

            // A data URI is its own content, and using the whole string as a key would put a
            // megabyte of base64 into a dictionary. Length plus hash tells two of them apart well
            // enough for a cache that lives and dies with the process.
            return (payload, $"data:{payload.LongLength}:{uri.GetHashCode(StringComparison.Ordinal)}");
        }

        var bytes = await ImageContent.ReadFileAsync(uri, cancellationToken).ConfigureAwait(false);
        return (bytes, FileCacheKey(uri));
    }


    /// <summary>
    /// Reads the payload of a service result when - and only when - it turns out to be a vector.
    /// </summary>
    /// <remarks>
    /// The large-image path of <see cref="IImageService"/> deliberately hands back a file path
    /// rather than bytes, so this sniffs the head of that file instead of loading it. A photo the
    /// service kept out of memory must not be pulled into memory just to learn it is a photo.
    /// </remarks>
    static async Task<byte[]?> ReadVectorAsync(ImageResult result, CancellationToken cancellationToken)
    {
        if (result.Bytes is { Length: > 0 } bytes)
            return SvgDocument.LooksLikeSvg(bytes) ? bytes : null;

        var path = result.FilePath;
        if (String.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var head = new byte[SniffLength];
        int read;

        await using (var stream = File.OpenRead(path))
            read = await stream.ReadAtLeastAsync(head, head.Length, false, cancellationToken).ConfigureAwait(false);

        return SvgDocument.LooksLikeSvg(head.AsSpan(0, read))
            ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
            : null;
    }


    static string FileCacheKey(string path)
    {
        // A bundled asset cannot change without a new build, so its path alone is a stable key. A
        // file on disk can be rewritten under the same name, and serving the previous parse would
        // be a bug the user can see - so its stamp goes into the key.
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? $"{path}|{info.LastWriteTimeUtc.Ticks}|{info.Length}" : path;
        }
        catch (Exception)
        {
            return path;
        }
    }


    /// <summary>Whether a non-remote URI is worth reading into memory to check for markup.</summary>
    static bool MayBeVector(string uri)
    {
        if (ImageContent.IsResource(uri) || ImageContent.IsData(uri))
            return true;

        // For a plain path the extension is the only cheap signal, and being wrong about it costs
        // the raster fast path rather than correctness - the bytes still get the final say.
        var marker = uri.IndexOfAny(['?', '#']);
        var span = marker < 0 ? uri.AsSpan() : uri.AsSpan(0, marker);

        return span.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
               || span.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase);
    }
}
