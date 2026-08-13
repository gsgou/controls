namespace Shiny.Maui.Controls.Images;

/// <summary>
/// Tuning for <see cref="ImageService"/>. Configure with
/// <c>UseShinyControls(cfg =&gt; cfg.ConfigureImages(...))</c>.
/// </summary>
public class ImageOptions
{
    /// <summary>
    /// How many downloads may run at once. Everything past this waits, and a waiting request reports
    /// <see cref="ImageLoadState.Queued"/> so its ring spins rather than sitting at 0%.
    /// </summary>
    /// <remarks>
    /// Four is a deliberate compromise. Mobile radios do badly with a dozen parallel connections and
    /// a long list would otherwise open one per visible cell; four keeps the pipe busy without the
    /// head-of-line stalls that come from oversubscribing it.
    /// </remarks>
    public int MaxConcurrentDownloads { get; set; } = 4;

    /// <summary>
    /// How long a cached entry stays valid when the server did not say. A server-supplied
    /// <c>Cache-Control: max-age</c> or <c>Expires</c> always wins over this.
    /// </summary>
    public TimeSpan DiskCacheDuration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Where cache entries live. Null uses <c>&lt;cache&gt;/shinyimage</c>, where <c>&lt;cache&gt;</c>
    /// is the platform cache directory (falling back to local app data on heads that have none).
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Ceiling for the on-disk cache. Once exceeded, least-recently-used entries are deleted until
    /// the cache is back down to 80% of this. Zero or less disables trimming entirely.
    /// </summary>
    public long MaxDiskCacheBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>Whether decoded-image bytes are held in memory between loads.</summary>
    public bool MemoryCacheEnabled { get; set; } = true;

    /// <summary>Ceiling for the in-memory byte cache.</summary>
    public long MaxMemoryCacheBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>
    /// Images larger than this are never held in memory, only on disk.
    /// </summary>
    /// <remarks>
    /// Without a per-item ceiling one full-resolution photo would evict every thumbnail in the
    /// cache to hold the single thing least likely to be asked for again.
    /// </remarks>
    public long MaxMemoryItemBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>How long a single download may take before it is abandoned.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
