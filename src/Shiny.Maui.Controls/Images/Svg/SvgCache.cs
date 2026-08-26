namespace Shiny.Maui.Controls.Images.Svg;

/// <summary>
/// A bounded LRU of parsed SVG documents, keyed by where the artwork came from.
/// </summary>
/// <remarks>
/// <para>This is the tier that makes SVG cheap. <see cref="IImageService"/> already stops the same
/// URL being downloaded twice, but the bytes are only half the cost: turning them into geometry
/// means an XML parse, a path-data parse per shape, and a bounds measurement per shape - all of it
/// pure CPU, all of it on the UI thread's critical path, and all of it repeated for every cell in a
/// list that shows the same icon. Parsed documents are immutable, so one parse can serve every
/// control on screen and every scroll-back to it.</para>
///
/// <para>Bounded by entry count rather than bytes. A parsed document's real footprint is a graph of
/// small objects that no cheap measurement describes honestly, and the count is what actually
/// matters here - a screen shows tens of distinct drawings, not thousands.</para>
/// </remarks>
public sealed class SvgCache
{
    sealed class Entry
    {
        public required SvgDocument Document { get; init; }
        public long Ticks { get; set; }
    }

    readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    readonly Lock gate = new();
    long clock;

    /// <summary>Creates a cache.</summary>
    /// <param name="entryLimit">How many documents to keep. Zero or less disables caching entirely.</param>
    public SvgCache(int entryLimit = 32) => this.EntryLimit = entryLimit;


    /// <summary>How many documents the cache will hold before evicting.</summary>
    public int EntryLimit { get; }

    /// <summary>How many documents are resident.</summary>
    public int Count
    {
        get
        {
            lock (this.gate)
                return this.entries.Count;
        }
    }


    /// <summary>
    /// Returns the cached document for a key, parsing it through <paramref name="factory"/> on a miss.
    /// </summary>
    /// <param name="key">
    /// What identifies this artwork. A URI is usually right; for a local file, include the write time
    /// so an edited file is not served from a stale parse.
    /// </param>
    /// <param name="factory">Builds the document. Only called on a miss.</param>
    public SvgDocument Get(string key, Func<SvgDocument> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (this.TryGet(key, out var hit))
            return hit;

        // Deliberately outside the lock. Parsing is the slow part, and holding the gate across it
        // would make every other control on the page wait behind one document. The cost of losing
        // the race is one duplicate parse; the cost of not losing it is a stalled UI thread.
        var document = factory();

        this.Set(key, document);
        return document;
    }


    /// <summary>Looks up a document without parsing. A hit refreshes the entry's recency.</summary>
    public bool TryGet(string key, out SvgDocument document)
    {
        lock (this.gate)
        {
            if (this.entries.TryGetValue(key, out var entry))
            {
                entry.Ticks = ++this.clock;
                document = entry.Document;
                return true;
            }
        }

        document = null!;
        return false;
    }


    /// <summary>Adds a document, evicting the least recently used to stay inside the limit.</summary>
    public void Set(string key, SvgDocument document)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(document);

        if (this.EntryLimit <= 0)
            return;

        lock (this.gate)
        {
            this.entries[key] = new Entry { Document = document, Ticks = ++this.clock };

            while (this.entries.Count > this.EntryLimit)
            {
                var oldestKey = String.Empty;
                var oldestTicks = Int64.MaxValue;

                foreach (var pair in this.entries)
                {
                    if (pair.Value.Ticks >= oldestTicks)
                        continue;

                    oldestTicks = pair.Value.Ticks;
                    oldestKey = pair.Key;
                }

                if (!this.entries.Remove(oldestKey))
                    break;
            }
        }
    }


    /// <summary>Drops one document - what a forced reload does before re-fetching.</summary>
    public void Remove(string key)
    {
        lock (this.gate)
            this.entries.Remove(key);
    }


    /// <summary>Drops everything.</summary>
    public void Clear()
    {
        lock (this.gate)
            this.entries.Clear();
    }
}
