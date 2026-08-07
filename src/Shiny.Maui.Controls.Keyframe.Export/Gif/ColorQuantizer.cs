namespace Shiny.Maui.Controls.Keyframe.Export;

/// <summary>
/// Reduces a frame's colours to a palette using median cut.
/// </summary>
/// <remarks>
/// <para>Median cut repeatedly splits the colour cloud along whichever axis it is most spread over,
/// at the median rather than the midpoint. Splitting at the median is what makes it adaptive: a
/// frame that is mostly one blue and a little red spends most of its palette on distinguishing the
/// blues, which is where the eye will actually notice banding.</para>
/// <para>Palettes are built per frame rather than once for the whole animation. That costs a few
/// hundred bytes per frame in the file, and buys correct colour on animations whose palette shifts
/// over time — a fade from blue to yellow being the obvious case.</para>
/// </remarks>
public static class ColorQuantizer
{
    /// <summary>Below this alpha a pixel is written as transparent rather than matched to a colour.</summary>
    public const byte AlphaThreshold = 128;

    /// <summary>The result of quantising one frame.</summary>
    /// <param name="Palette">Palette entries as packed 0xRRGGBB values.</param>
    /// <param name="Indices">One palette index per pixel, row major.</param>
    /// <param name="TransparentIndex">Index reserved for transparent pixels, or -1 if the frame is opaque.</param>
    public readonly record struct Result(int[] Palette, byte[] Indices, int TransparentIndex);

    /// <summary>Quantises premultiplied RGBA pixels to at most <paramref name="maxColors"/> entries.</summary>
    /// <param name="pixels">RGBA pixels, four bytes each, row major.</param>
    /// <param name="maxColors">Palette size limit, 2 to 256.</param>
    public static Result Quantize(byte[] pixels, int maxColors = 256)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxColors, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxColors, 256);

        var pixelCount = pixels.Length / 4;
        var hasTransparency = false;

        // Histogram of the opaque colours. Counting first means the median split can weight by how
        // common a colour is, not just how many distinct values sit on each side.
        var histogram = new Dictionary<int, int>();

        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * 4;

            if (pixels[offset + 3] < AlphaThreshold)
            {
                hasTransparency = true;
                continue;
            }

            var rgb = (pixels[offset] << 16) | (pixels[offset + 1] << 8) | pixels[offset + 2];
            histogram[rgb] = histogram.GetValueOrDefault(rgb) + 1;
        }

        // Transparency costs one palette slot, which we put at index 0.
        var transparentIndex = hasTransparency ? 0 : -1;
        var colorSlots = hasTransparency ? maxColors - 1 : maxColors;

        var colors = BuildPalette(histogram, colorSlots);

        var palette = new int[colors.Length + (hasTransparency ? 1 : 0)];
        if (hasTransparency)
            palette[0] = 0;

        colors.CopyTo(palette, hasTransparency ? 1 : 0);

        var indices = MapPixels(pixels, pixelCount, palette, hasTransparency, transparentIndex);
        return new Result(palette, indices, transparentIndex);
    }

    static int[] BuildPalette(Dictionary<int, int> histogram, int maxColors)
    {
        if (histogram.Count == 0)
            return [0];

        if (histogram.Count <= maxColors)
            return [.. histogram.Keys];

        var entries = new Entry[histogram.Count];
        var index = 0;

        foreach (var (rgb, count) in histogram)
            entries[index++] = new Entry((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, count);

        var buckets = new List<Bucket> { Bucket.Create(entries, 0, entries.Length) };

        while (buckets.Count < maxColors)
        {
            // Always split whichever bucket currently spans the most colour, so palette entries go
            // where the error is largest.
            var target = -1;
            var widest = 0;

            for (var i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Length < 2)
                    continue;

                var range = buckets[i].WidestRange;
                if (range > widest)
                {
                    widest = range;
                    target = i;
                }
            }

            if (target < 0)
                break; // Every remaining bucket holds a single colour; nothing left to split.

            var bucket = buckets[target];
            var axis = bucket.WidestAxis;

            Array.Sort(entries, bucket.Start, bucket.Length, EntryComparer.For(axis));

            // Split at the weighted median so both halves carry a similar number of pixels.
            var half = bucket.Weight / 2;
            var running = 0L;
            var split = bucket.Start;

            while (split < bucket.Start + bucket.Length - 1 && running + entries[split].Count <= half)
            {
                running += entries[split].Count;
                split++;
            }

            buckets[target] = Bucket.Create(entries, bucket.Start, split - bucket.Start);
            buckets.Add(Bucket.Create(entries, split, bucket.Start + bucket.Length - split));
        }

        var palette = new int[buckets.Count];

        for (var i = 0; i < buckets.Count; i++)
            palette[i] = buckets[i].AverageColor(entries);

        return palette;
    }

    static byte[] MapPixels(byte[] pixels, int pixelCount, int[] palette, bool hasTransparency, int transparentIndex)
    {
        var indices = new byte[pixelCount];

        // Nearest-colour search is the expensive part, so memoise it. Real frames repeat colours
        // heavily — flat fills, antialiasing runs — and the cache turns a per-pixel palette scan
        // into a dictionary hit for all but the first occurrence of each distinct colour.
        var cache = new Dictionary<int, byte>();
        var searchStart = hasTransparency ? 1 : 0;

        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * 4;

            if (hasTransparency && pixels[offset + 3] < AlphaThreshold)
            {
                indices[i] = (byte)transparentIndex;
                continue;
            }

            var rgb = (pixels[offset] << 16) | (pixels[offset + 1] << 8) | pixels[offset + 2];

            if (!cache.TryGetValue(rgb, out var mapped))
            {
                mapped = FindNearest(palette, searchStart, pixels[offset], pixels[offset + 1], pixels[offset + 2]);
                cache[rgb] = mapped;
            }

            indices[i] = mapped;
        }

        return indices;
    }

    static byte FindNearest(int[] palette, int start, byte r, byte g, byte b)
    {
        var best = start;
        var bestDistance = int.MaxValue;

        for (var i = start; i < palette.Length; i++)
        {
            var dr = r - ((palette[i] >> 16) & 0xFF);
            var dg = g - ((palette[i] >> 8) & 0xFF);
            var db = b - (palette[i] & 0xFF);

            // Weighted to approximate perceived difference: the eye is far more sensitive to green
            // than to blue, so an unweighted distance picks visibly wrong greens.
            var distance = 2 * dr * dr + 4 * dg * dg + 3 * db * db;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = i;

            if (distance == 0)
                break;
        }

        return (byte)best;
    }

    readonly record struct Entry(byte R, byte G, byte B, int Count);

    readonly record struct Bucket(int Start, int Length, int WidestAxis, int WidestRange, long Weight)
    {
        public static Bucket Create(Entry[] entries, int start, int length)
        {
            if (length <= 0)
                return new Bucket(start, 0, 0, 0, 0);

            byte minR = 255, minG = 255, minB = 255;
            byte maxR = 0, maxG = 0, maxB = 0;
            var weight = 0L;

            for (var i = start; i < start + length; i++)
            {
                var entry = entries[i];

                minR = Math.Min(minR, entry.R);
                minG = Math.Min(minG, entry.G);
                minB = Math.Min(minB, entry.B);
                maxR = Math.Max(maxR, entry.R);
                maxG = Math.Max(maxG, entry.G);
                maxB = Math.Max(maxB, entry.B);

                weight += entry.Count;
            }

            int rangeR = maxR - minR, rangeG = maxG - minG, rangeB = maxB - minB;

            var axis = 0;
            var range = rangeR;

            if (rangeG > range)
            {
                axis = 1;
                range = rangeG;
            }

            if (rangeB > range)
            {
                axis = 2;
                range = rangeB;
            }

            return new Bucket(start, length, axis, range, weight);
        }

        public int AverageColor(Entry[] entries)
        {
            if (Length == 0 || Weight == 0)
                return 0;

            long r = 0, g = 0, b = 0;

            for (var i = Start; i < Start + Length; i++)
            {
                var entry = entries[i];
                r += (long)entry.R * entry.Count;
                g += (long)entry.G * entry.Count;
                b += (long)entry.B * entry.Count;
            }

            return (int)((r / Weight) << 16 | (g / Weight) << 8 | b / Weight);
        }
    }

    sealed class EntryComparer : IComparer<Entry>
    {
        static readonly EntryComparer Red = new(0);
        static readonly EntryComparer Green = new(1);
        static readonly EntryComparer Blue = new(2);

        readonly int axis;

        EntryComparer(int axis) => this.axis = axis;

        public static IComparer<Entry> For(int axis) => axis switch
        {
            0 => Red,
            1 => Green,
            _ => Blue
        };

        public int Compare(Entry x, Entry y) => axis switch
        {
            0 => x.R.CompareTo(y.R),
            1 => x.G.CompareTo(y.G),
            _ => x.B.CompareTo(y.B)
        };
    }
}
