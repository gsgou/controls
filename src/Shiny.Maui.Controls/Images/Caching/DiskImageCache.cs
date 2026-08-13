using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shiny.Maui.Controls.Images.Caching;

/// <summary>
/// The on-disk half of the image cache: one file per image plus a small JSON sidecar holding its
/// expiry and last-access time.
/// </summary>
public class DiskImageCache
{
    const string DirectoryName = "shinyimage";
    const string ImageExtension = ".bin";
    const string MetaExtension = ".json";

    // Trimming down to exactly the ceiling would leave the cache re-trimming on every subsequent
    // write. Going to 80% buys a margin so the (synchronous, directory-enumerating) trim is rare.
    const double TrimTargetRatio = 0.8;

    readonly ImageOptions options;
    readonly SemaphoreSlim trimGate = new(1, 1);
    string? resolvedRoot;

    /// <summary>Creates the cache.</summary>
    public DiskImageCache(ImageOptions options) => this.options = options;


    /// <summary>The directory cache entries are written to. Created on first use.</summary>
    public string Root
    {
        get
        {
            if (this.resolvedRoot is not null)
                return this.resolvedRoot;

            var root = this.options.CacheDirectory ?? Path.Combine(ResolveCacheBase(), DirectoryName);
            Directory.CreateDirectory(root);
            this.resolvedRoot = root;
            return root;
        }
    }


    /// <summary>
    /// The stable file-name stem for a URI.
    /// </summary>
    /// <remarks>
    /// SHA-256 rather than a cheaper hash or an escaped URL. The escaped URL blows past path length
    /// limits on long query strings and leaks signed-URL credentials into a filename; a short hash
    /// invites collisions that would serve one user's image for another's URL.
    /// </remarks>
    public static string GetKey(string uri)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(uri)));
    }


    /// <summary>Full path of the image file for a URI, whether or not it exists.</summary>
    public string GetImagePath(string uri) => Path.Combine(this.Root, GetKey(uri) + ImageExtension);

    string GetMetaPath(string key) => Path.Combine(this.Root, key + MetaExtension);


    /// <summary>
    /// Reads an entry when it exists and has not expired, refreshing its last-access stamp.
    /// Returns null on a miss, an expired entry, or anything unreadable.
    /// </summary>
    public async Task<CacheEntryMeta?> GetAsync(string uri, CancellationToken cancellationToken = default)
    {
        var key = GetKey(uri);
        var imagePath = Path.Combine(this.Root, key + ImageExtension);
        var metaPath = this.GetMetaPath(key);

        if (!File.Exists(imagePath) || !File.Exists(metaPath))
            return null;

        var meta = await this.ReadMetaAsync(metaPath, cancellationToken).ConfigureAwait(false);
        if (meta is null)
            return null;

        if (meta.ExpiresUtc <= DateTimeOffset.UtcNow)
            return null;

        // Best-effort: a failed touch costs LRU accuracy, not correctness, and is not worth
        // failing an otherwise good cache hit over.
        meta.LastAccessUtc = DateTimeOffset.UtcNow;
        try
        {
            await this.WriteMetaAsync(metaPath, meta, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }

        return meta;
    }


    /// <summary>
    /// Copies a download into the cache, reporting progress as the bytes go by, and returns the
    /// final entry.
    /// </summary>
    /// <remarks>
    /// Writes to a temp file and moves it into place. A download cancelled or dropped mid-flight
    /// would otherwise leave a truncated file that the next read happily treats as a valid cached
    /// image - a corrupt thumbnail that survives until the entry expires days later.
    /// </remarks>
    public async Task<CacheEntryMeta> WriteAsync(
        string uri,
        Stream source,
        ImageDownloadResult download,
        TimeSpan fallbackDuration,
        Action<long>? onBytesWritten = null,
        CancellationToken cancellationToken = default
    )
    {
        var key = GetKey(uri);
        var imagePath = Path.Combine(this.Root, key + ImageExtension);
        var tempPath = imagePath + ".tmp";

        long written = 0;

        try
        {
            await using (var file = File.Create(tempPath))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    written += read;
                    onBytesWritten?.Invoke(written);
                }
            }

            File.Move(tempPath, imagePath, true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        var now = DateTimeOffset.UtcNow;
        var meta = new CacheEntryMeta
        {
            Url = uri,
            DownloadedUtc = now,
            ExpiresUtc = download.ExpiresUtc ?? now.Add(fallbackDuration),
            ETag = download.ETag,
            ContentLength = written,
            ContentType = download.ContentType,
            LastAccessUtc = now
        };

        await this.WriteMetaAsync(this.GetMetaPath(key), meta, cancellationToken).ConfigureAwait(false);
        return meta;
    }


    /// <summary>Deletes everything in the cache directory.</summary>
    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        if (!Directory.Exists(this.Root))
            return;

        foreach (var file in Directory.EnumerateFiles(this.Root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDelete(file);
        }
    }, cancellationToken);


    /// <summary>Deletes the entry for one URI, if present.</summary>
    public Task RemoveAsync(string uri, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        var key = GetKey(uri);
        TryDelete(Path.Combine(this.Root, key + ImageExtension));
        TryDelete(this.GetMetaPath(key));
    }, cancellationToken);


    /// <summary>Total bytes of cached images (sidecars excluded - they are noise at this scale).</summary>
    public Task<long> GetSizeAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        if (!Directory.Exists(this.Root))
            return 0L;

        var total = 0L;
        foreach (var file in Directory.EnumerateFiles(this.Root, "*" + ImageExtension))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                total += new FileInfo(file).Length;
            }
            catch
            {
                // a file deleted between enumerate and stat is a miss, not an error
            }
        }
        return total;
    }, cancellationToken);


    /// <summary>
    /// Deletes least-recently-used entries until the cache is back under
    /// <see cref="ImageOptions.MaxDiskCacheBytes"/>. Safe to call concurrently - only one trim runs.
    /// </summary>
    public async Task TrimAsync(CancellationToken cancellationToken = default)
    {
        if (this.options.MaxDiskCacheBytes <= 0)
            return;

        // A second trim while one is running would enumerate a directory the first is deleting from
        // and double-count. Skipping is right: the running trim already covers the same work.
        if (!await this.trimGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            await Task.Run(() => this.TrimCore(cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.trimGate.Release();
        }
    }


    void TrimCore(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(this.Root))
            return;

        var entries = new List<(string Key, long Size, DateTimeOffset LastAccess)>();
        var total = 0L;

        foreach (var imagePath in Directory.EnumerateFiles(this.Root, "*" + ImageExtension))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var info = new FileInfo(imagePath);
                var key = Path.GetFileNameWithoutExtension(imagePath);

                // The sidecar is the source of truth for last-access; the filesystem's own atime is
                // unreliable (relatime, noatime, and iOS simply lies about it).
                var lastAccess = this.ReadMetaSync(this.GetMetaPath(key))?.LastAccessUtc
                                 ?? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);

                entries.Add((key, info.Length, lastAccess));
                total += info.Length;
            }
            catch
            {
                // ignored - unreadable entries get skipped, not fatal
            }
        }

        if (total <= this.options.MaxDiskCacheBytes)
            return;

        var target = (long)(this.options.MaxDiskCacheBytes * TrimTargetRatio);
        entries.Sort((a, b) => a.LastAccess.CompareTo(b.LastAccess));

        foreach (var entry in entries)
        {
            if (total <= target)
                break;

            cancellationToken.ThrowIfCancellationRequested();
            TryDelete(Path.Combine(this.Root, entry.Key + ImageExtension));
            TryDelete(this.GetMetaPath(entry.Key));
            total -= entry.Size;
        }
    }


    async Task<CacheEntryMeta?> ReadMetaAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer
                .DeserializeAsync(stream, ImageCacheJsonContext.Default.CacheEntryMeta, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }


    CacheEntryMeta? ReadMetaSync(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(stream, ImageCacheJsonContext.Default.CacheEntryMeta);
        }
        catch
        {
            return null;
        }
    }


    async Task WriteMetaAsync(string path, CacheEntryMeta meta, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer
            .SerializeAsync(stream, meta, ImageCacheJsonContext.Default.CacheEntryMeta, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// The platform cache directory, or local app data when there is none.
    /// </summary>
    /// <remarks>
    /// <c>FileSystem.CacheDirectory</c> throws <c>NotImplementedInReferenceAssembly</c> on the bare
    /// net10.0 head - which is the one the macOS AppKit and Linux GTK4 app heads build against - so
    /// it cannot simply be called. The fallback keeps image caching working on those heads instead
    /// of taking the control down at first use.
    /// </remarks>
    internal static string ResolveCacheBase()
    {
        try
        {
            var dir = FileSystem.CacheDirectory;
            if (!String.IsNullOrWhiteSpace(dir))
                return dir;
        }
        catch
        {
            // no platform filesystem on this head
        }

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return String.IsNullOrWhiteSpace(local) ? Path.GetTempPath() : local;
    }


    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored - a locked or already-deleted file is not worth failing a clear over
        }
    }
}
