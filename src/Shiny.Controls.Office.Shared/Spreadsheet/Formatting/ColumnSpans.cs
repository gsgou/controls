using DocumentFormat.OpenXml.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.View;
using SheetElement = DocumentFormat.OpenXml.Spreadsheet.Worksheet;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// Edits a worksheet's <c>&lt;cols&gt;</c> list.
/// </summary>
/// <remarks>
/// <para>
/// A worksheet does not store a property per column. It stores a list of <em>spans</em> — one
/// <c>&lt;col min="3" max="7"&gt;</c> element says the same thing about five columns at once — so
/// setting a width or a style on column D is never a matter of finding column D's element. The span
/// covering it has to be split around it first, and the leftovers put back.
/// </para>
/// <para>
/// Excel is strict about the result: the spans must be in ascending order and must not overlap. A file
/// that breaks either rule opens as corrupt rather than being repaired, which is why everything here
/// goes through one normalise-and-rewrite pass rather than patching elements in place.
/// </para>
/// </remarks>
static class ColumnSpans
{
    /// <summary>One span of columns and what the file says about them. Zero-based and inclusive.</summary>
    internal sealed record Span(int First, int Last)
    {
        public double? Width { get; init; }
        public bool CustomWidth { get; init; }
        public uint? Style { get; init; }
        public bool Hidden { get; init; }
        public bool BestFit { get; init; }
        public uint? OutlineLevel { get; init; }
        public bool Collapsed { get; init; }

        /// <summary>Everything except the extent, so two neighbours can be tested for merging.</summary>
        public Span Properties => this with { First = 0, Last = 0 };
    }

    /// <summary>Applies <paramref name="mutate"/> to every column in a range, splitting spans as needed.</summary>
    /// <returns>True when anything actually changed.</returns>
    public static bool Apply(SheetElement sheetElement, int first, int last, Func<Span, Span> mutate)
    {
        ArgumentNullException.ThrowIfNull(sheetElement);
        ArgumentNullException.ThrowIfNull(mutate);

        first = Math.Clamp(first, 0, CellRef.MaxColumn);
        last = Math.Clamp(last, first, CellRef.MaxColumn);

        var spans = Read(sheetElement);
        var before = spans;

        spans = Split(spans, first, last);

        var result = new List<Span>(spans.Count + 1);
        var covered = first - 1;

        foreach (var span in spans)
        {
            if (span.Last < first || span.First > last)
            {
                result.Add(span);
                continue;
            }

            // Gaps inside the range have no element at all; they need one before they can carry a style.
            if (span.First > covered + 1)
                result.Add(mutate(new Span(covered + 1, span.First - 1)));

            result.Add(mutate(span) with { First = span.First, Last = span.Last });
            covered = span.Last;
        }

        if (covered < last)
            result.Add(mutate(new Span(covered + 1, last)));

        result.Sort((a, b) => a.First.CompareTo(b.First));

        var merged = Merge(result);
        if (Same(before, merged))
            return false;

        Write(sheetElement, merged);
        return true;
    }

    /// <summary>
    /// Moves the spans along with an inserted or deleted band of columns.
    /// </summary>
    /// <remarks>
    /// The insert deliberately leaves the new columns with no span of their own rather than copying
    /// the one they were pushed out of: Excel gives an inserted column the width of its left-hand
    /// neighbour but no style, and a span silently extended over the gap would carry the style too.
    /// </remarks>
    /// <param name="delta">Positive to insert that many columns at <paramref name="at"/>, negative to delete.</param>
    public static void Shift(SheetElement sheetElement, int at, int delta)
    {
        ArgumentNullException.ThrowIfNull(sheetElement);

        if (delta == 0)
            return;

        var spans = Read(sheetElement);
        if (spans.Count == 0)
            return;

        var result = new List<Span>(spans.Count + 1);

        foreach (var span in spans)
        {
            if (span.Last < at)
            {
                result.Add(span);
                continue;
            }

            // A span straddling the edit keeps its left half where it is; only the part at or past the
            // insertion point moves, so the columns before it do not silently change width.
            if (span.First < at)
            {
                result.Add(span with { Last = at - 1 });

                var tail = Move(at, span.Last);
                if (tail is { } moved)
                    result.Add(span with { First = moved.First, Last = moved.Last });

                continue;
            }

            if (Move(span.First, span.Last) is { } shifted)
                result.Add(span with { First = shifted.First, Last = shifted.Last });
        }

        result.Sort((a, b) => a.First.CompareTo(b.First));

        var merged = Merge(result);
        if (!Same(spans, merged))
            Write(sheetElement, merged);

        (int First, int Last)? Move(int first, int last)
        {
            if (delta < 0)
            {
                // A span that falls entirely inside the deleted band goes with it; one that only
                // starts inside it loses its left end and resumes where the band did.
                var bandEnd = at - delta;
                if (last < bandEnd)
                    return null;

                first = Math.Max(first, bandEnd);
            }

            var from = first + delta;
            if (from > CellRef.MaxColumn)
                return null;

            return (from, Math.Min(last + delta, CellRef.MaxColumn));
        }
    }

    /// <summary>The properties recorded for one column, or null when the file says nothing about it.</summary>
    public static Span? Find(SheetElement sheetElement, int column)
    {
        foreach (var span in Read(sheetElement))
        {
            if (column >= span.First && column <= span.Last)
                return span;
        }

        return null;
    }

    public static IReadOnlyList<Span> Read(SheetElement sheetElement)
    {
        var columns = sheetElement.GetFirstChild<Columns>();
        if (columns is null)
            return Array.Empty<Span>();

        var spans = new List<Span>();
        foreach (var column in columns.Elements<Column>())
        {
            var min = (int)(column.Min?.Value ?? 1) - 1;
            var max = (int)(column.Max?.Value ?? 1) - 1;
            if (min < 0 || max < min)
                continue;

            spans.Add(new Span(min, Math.Min(max, CellRef.MaxColumn))
            {
                Width = column.Width?.Value,
                CustomWidth = column.CustomWidth?.Value ?? false,
                Style = column.Style?.Value,
                Hidden = column.Hidden?.Value ?? false,
                BestFit = column.BestFit?.Value ?? false,
                OutlineLevel = column.OutlineLevel?.Value,
                Collapsed = column.Collapsed?.Value ?? false
            });
        }

        spans.Sort((a, b) => a.First.CompareTo(b.First));
        return spans;
    }

    /// <summary>Cuts every span at the range's edges, so no span straddles the boundary.</summary>
    static List<Span> Split(IReadOnlyList<Span> spans, int first, int last)
    {
        var result = new List<Span>(spans.Count + 2);

        foreach (var span in spans)
        {
            if (span.Last < first || span.First > last)
            {
                result.Add(span);
                continue;
            }

            if (span.First < first)
                result.Add(span with { Last = first - 1 });

            result.Add(span with
            {
                First = Math.Max(span.First, first),
                Last = Math.Min(span.Last, last)
            });

            if (span.Last > last)
                result.Add(span with { First = last + 1 });
        }

        result.Sort((a, b) => a.First.CompareTo(b.First));
        return result;
    }

    /// <summary>
    /// Folds neighbouring spans that now say the same thing back into one.
    /// </summary>
    /// <remarks>
    /// Without it, formatting columns one at a time across a sheet leaves one element per column, and
    /// the list grows every time a range is split and never shrinks when the pieces match again.
    /// </remarks>
    static List<Span> Merge(List<Span> spans)
    {
        var result = new List<Span>(spans.Count);

        foreach (var span in spans)
        {
            if (IsDefault(span))
                continue;

            if (result.Count > 0 &&
                result[^1].Last + 1 == span.First &&
                result[^1].Properties == span.Properties)
            {
                result[^1] = result[^1] with { Last = span.Last };
                continue;
            }

            result.Add(span);
        }

        return result;
    }

    /// <summary>True when a span says nothing the file would not say by leaving the column out.</summary>
    static bool IsDefault(Span span)
        => span.Width is null && !span.CustomWidth && span.Style is null && !span.Hidden &&
           !span.BestFit && span.OutlineLevel is null && !span.Collapsed;

    static bool Same(IReadOnlyList<Span> a, IReadOnlyList<Span> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static void Write(SheetElement sheetElement, IReadOnlyList<Span> spans)
    {
        var columns = sheetElement.GetFirstChild<Columns>();

        if (spans.Count == 0)
        {
            columns?.Remove();
            return;
        }

        if (columns is null)
        {
            columns = new Columns();

            // cols must precede sheetData in the schema, and every sheet has a sheetData - the model
            // creates one when the worksheet is loaded, so this is not a fallback to appending.
            if (sheetElement.GetFirstChild<SheetData>() is { } sheetData)
                sheetElement.InsertBefore(columns, sheetData);
            else
                sheetElement.AppendChild(columns);
        }
        else
        {
            columns.RemoveAllChildren<Column>();
        }

        foreach (var span in spans)
        {
            var column = new Column
            {
                Min = (uint)(span.First + 1),
                Max = (uint)(span.Last + 1)
            };

            // Excel requires a width on every col element, even one that only carries a style. 8.43
            // characters is its own default, and customWidth stays off so it still tracks the sheet's.
            column.Width = span.Width ?? GridMetrics.DefaultColumnWidthCharacters;

            if (span.CustomWidth)
                column.CustomWidth = true;

            // style is the whole of it: unlike a row, a col element has no customFormat flag to gate
            // the index, and Excel writes none.
            if (span.Style is { } style)
                column.Style = style;

            if (span.Hidden)
                column.Hidden = true;

            if (span.BestFit)
                column.BestFit = true;

            if (span.OutlineLevel is { } level)
                column.OutlineLevel = (byte)level;

            if (span.Collapsed)
                column.Collapsed = true;

            columns.AppendChild(column);
        }
    }
}
