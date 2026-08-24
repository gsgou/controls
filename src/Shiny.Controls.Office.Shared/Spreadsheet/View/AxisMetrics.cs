namespace Shiny.Controls.Office.Spreadsheet.View;

/// <summary>
/// Positions along one axis of the grid, where most entries share a default size and a few differ.
/// </summary>
/// <remarks>
/// <para>
/// A sheet has 1,048,576 rows. Storing a cumulative offset per row would allocate megabytes for a sheet
/// that is almost entirely default height, so overrides are held sparsely and the offset of an index is
/// derived: <c>index * default + (sum of overrides below it) - (count of overrides below it) * default</c>.
/// </para>
/// <para>
/// The prefix sums that make that a binary search rather than a scan are rebuilt lazily, so setting a
/// hundred column widths in a row costs one rebuild rather than a hundred.
/// </para>
/// </remarks>
public sealed class AxisMetrics
{
    readonly Dictionary<int, double> overrides = new();
    readonly SortedSet<int> hidden = new();

    int[] overrideIndexes = [];
    double[] prefixSums = [];
    bool dirty = true;

    public AxisMetrics(double defaultSize, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultSize);
        this.DefaultSize = defaultSize;
        this.Count = count;
    }

    public double DefaultSize { get; private set; }

    public int Count { get; }

    public int OverrideCount => this.overrides.Count;

    public void SetDefaultSize(double size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        this.DefaultSize = size;
        this.dirty = true;
    }

    public double SizeOf(int index)
    {
        if (this.hidden.Contains(index))
            return 0;

        return this.overrides.TryGetValue(index, out var size) ? size : this.DefaultSize;
    }

    public void SetSize(int index, double size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (Math.Abs(size - this.DefaultSize) < 0.001)
            this.overrides.Remove(index);
        else
            this.overrides[index] = size;

        this.dirty = true;
    }

    public void ResetSize(int index)
    {
        this.overrides.Remove(index);
        this.dirty = true;
    }

    public bool IsHidden(int index) => this.hidden.Contains(index);

    public void SetHidden(int index, bool value)
    {
        if (value)
            this.hidden.Add(index);
        else
            this.hidden.Remove(index);

        this.dirty = true;
    }

    /// <summary>The distance from the origin to the leading edge of <paramref name="index"/>.</summary>
    public double OffsetOf(int index)
    {
        if (index <= 0)
            return 0;

        this.Rebuild();

        // Everything below `index` at default size, corrected by the overrides that fall below it.
        var overridesBelow = this.CountBelow(index);
        var overrideTotal = overridesBelow == 0 ? 0 : this.prefixSums[overridesBelow - 1];
        return (index - overridesBelow) * this.DefaultSize + overrideTotal;
    }

    /// <summary>The index whose band contains <paramref name="offset"/>, clamped to the axis.</summary>
    public int IndexAt(double offset)
    {
        if (offset <= 0)
            return 0;

        this.Rebuild();

        // Binary search over offsets rather than walking, so scrolling to row 900,000 is not a scan.
        var low = 0;
        var high = this.Count - 1;

        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (this.OffsetOf(middle + 1) <= offset)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    /// <summary>Total extent of the whole axis.</summary>
    public double TotalSize => this.OffsetOf(this.Count);

    /// <summary>Total extent of a half-open index span.</summary>
    public double SizeOfRange(int start, int endExclusive)
        => this.OffsetOf(endExclusive) - this.OffsetOf(start);

    /// <summary>The last index whose leading edge is still before <paramref name="limit"/>.</summary>
    public int LastIndexWithin(int start, double limit)
    {
        var origin = this.OffsetOf(start);
        var index = this.IndexAt(origin + limit);
        return Math.Min(index, this.Count - 1);
    }

    void Rebuild()
    {
        if (!this.dirty)
            return;

        // Hidden entries participate as zero-size overrides.
        var combined = new Dictionary<int, double>(this.overrides);
        foreach (var index in this.hidden)
            combined[index] = 0;

        this.overrideIndexes = combined.Keys.OrderBy(x => x).ToArray();
        this.prefixSums = new double[this.overrideIndexes.Length];

        var running = 0d;
        for (var i = 0; i < this.overrideIndexes.Length; i++)
        {
            running += combined[this.overrideIndexes[i]];
            this.prefixSums[i] = running;
        }

        this.dirty = false;
    }

    /// <summary>How many overrides sit strictly below <paramref name="index"/>.</summary>
    int CountBelow(int index)
    {
        var low = 0;
        var high = this.overrideIndexes.Length;

        while (low < high)
        {
            var middle = (low + high) / 2;
            if (this.overrideIndexes[middle] < index)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
