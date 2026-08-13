namespace Shiny.Maui.Controls.Images.Caching;

/// <summary>
/// A size-bounded LRU of <b>encoded image bytes</b>, keyed by URI.
/// </summary>
/// <remarks>
/// <para><b>Bytes, not <c>ImageSource</c>.</b> Caching ImageSource objects is the obvious design and
/// the wrong one: a stream-backed ImageSource is consumed the first time a handler reads it, and the
/// platform image handle a realized source holds belongs to the view that realized it. Hand the same
/// instance to two cells of a CollectionView and one of them renders blank. Bytes have neither
/// problem - every control builds its own <c>ImageSource.FromStream(() =&gt; new MemoryStream(bytes))</c>,
/// which is cheap, and the expensive part (the network) is what actually got skipped.</para>
///
/// <para>This tier exists for scrolling. Without it, every scroll-back re-reads and re-decodes from
/// disk; with it, a list that fits inside the budget only ever touches disk once per image.</para>
/// </remarks>
public class MemoryImageCache
{
    sealed class Entry
    {
        public required byte[] Bytes { get; init; }
        public long Ticks { get; set; }
    }

    readonly ImageOptions options;
    readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    readonly Lock gate = new();
    long clock;
    long totalBytes;

    /// <summary>Creates the cache.</summary>
    public MemoryImageCache(ImageOptions options) => this.options = options;


    /// <summary>Current resident bytes.</summary>
    public long SizeInBytes
    {
        get
        {
            lock (this.gate)
                return this.totalBytes;
        }
    }

    /// <summary>Number of resident entries.</summary>
    public int Count
    {
        get
        {
            lock (this.gate)
                return this.entries.Count;
        }
    }


    /// <summary>Returns the cached bytes for a URI, or null. A hit refreshes the entry's recency.</summary>
    public byte[]? Get(string uri)
    {
        if (!this.options.MemoryCacheEnabled)
            return null;

        lock (this.gate)
        {
            if (!this.entries.TryGetValue(uri, out var entry))
                return null;

            entry.Ticks = ++this.clock;
            return entry.Bytes;
        }
    }


    /// <summary>
    /// Adds bytes to the cache, evicting least-recently-used entries to stay inside the budget.
    /// Items over <see cref="ImageOptions.MaxMemoryItemBytes"/> are refused outright.
    /// </summary>
    public void Set(string uri, byte[] bytes)
    {
        if (!this.options.MemoryCacheEnabled || bytes.LongLength == 0)
            return;

        // Refusing the oversized item is the point of the per-item ceiling: admitting one
        // full-resolution photo would evict every thumbnail in the cache to hold the single thing
        // least likely to be asked for again.
        if (bytes.LongLength > this.options.MaxMemoryItemBytes || bytes.LongLength > this.options.MaxMemoryCacheBytes)
            return;

        lock (this.gate)
        {
            if (this.entries.Remove(uri, out var existing))
                this.totalBytes -= existing.Bytes.LongLength;

            this.entries[uri] = new Entry { Bytes = bytes, Ticks = ++this.clock };
            this.totalBytes += bytes.LongLength;

            this.EvictWhileOverBudget();
        }
    }


    /// <summary>Drops one entry.</summary>
    public void Remove(string uri)
    {
        lock (this.gate)
        {
            if (this.entries.Remove(uri, out var entry))
                this.totalBytes -= entry.Bytes.LongLength;
        }
    }


    /// <summary>Drops everything.</summary>
    public void Clear()
    {
        lock (this.gate)
        {
            this.entries.Clear();
            this.totalBytes = 0;
        }
    }


    // Called under the lock. A linear scan per eviction is O(n) but n is tiny (a 32MB budget of
    // thumbnails is a few hundred entries) and evictions only happen on a miss that overflows -
    // an intrusive LRU list would be more machinery than the problem deserves.
    void EvictWhileOverBudget()
    {
        while (this.totalBytes > this.options.MaxMemoryCacheBytes && this.entries.Count > 0)
        {
            var oldestKey = String.Empty;
            var oldestTicks = long.MaxValue;

            foreach (var pair in this.entries)
            {
                if (pair.Value.Ticks >= oldestTicks)
                    continue;

                oldestTicks = pair.Value.Ticks;
                oldestKey = pair.Key;
            }

            if (!this.entries.Remove(oldestKey, out var evicted))
                break;

            this.totalBytes -= evicted.Bytes.LongLength;
        }
    }
}
