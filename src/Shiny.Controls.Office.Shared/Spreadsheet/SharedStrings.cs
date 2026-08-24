using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// Append-only view over the workbook's shared string table.
/// </summary>
/// <remarks>
/// Strings are never removed. Orphaning an entry is legal OOXML and costs a few bytes; renumbering the
/// table is not, because every <c>t="s"</c> cell in the workbook indexes into it positionally and the
/// editor has no way to know which of those cells it is allowed to rewrite.
/// </remarks>
sealed class SharedStrings
{
    readonly WorkbookPart workbookPart;
    readonly Dictionary<string, int> lookup = new(StringComparer.Ordinal);
    readonly List<string> items = new();
    SharedStringTablePart? part;

    public SharedStrings(WorkbookPart workbookPart)
    {
        this.workbookPart = workbookPart;
        this.part = workbookPart.SharedStringTablePart;

        var table = this.part?.SharedStringTable;
        if (table is null)
            return;

        var index = 0;
        foreach (var item in table.Elements<SharedStringItem>())
        {
            var text = ReadText(item);
            this.items.Add(text);

            // First index wins: a duplicated entry must still resolve to something, and reusing the
            // earlier one on write is harmless.
            this.lookup.TryAdd(text, index);
            index++;
        }
    }

    public string this[int index] => index >= 0 && index < this.items.Count ? this.items[index] : string.Empty;

    public int Count => this.items.Count;

    /// <summary>Returns the index of <paramref name="text"/>, appending it to the table if it is new.</summary>
    public int GetOrAdd(string text)
    {
        text ??= string.Empty;
        if (this.lookup.TryGetValue(text, out var existing))
            return existing;

        this.part ??= CreatePart(this.workbookPart);
        var table = this.part.SharedStringTable ??= new SharedStringTable();

        // Preserve significant whitespace; without xml:space Excel trims leading/trailing spaces.
        var value = new DocumentFormat.OpenXml.Spreadsheet.Text(text);
        if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1])))
            value.Space = SpaceProcessingModeValues.Preserve;

        table.AppendChild(new SharedStringItem(value));

        var index = this.items.Count;
        this.items.Add(text);
        this.lookup[text] = index;
        this.Touched = true;
        return index;
    }

    /// <summary>True once anything has been appended, so the counts need rewriting before save.</summary>
    public bool Touched { get; private set; }

    /// <summary>
    /// Rewrites the table's count attributes. <c>uniqueCount</c> is the number of entries;
    /// <c>count</c> is the number of cells referencing them, which we cannot know cheaply — Excel
    /// tolerates it matching uniqueCount, and recomputes both on save.
    /// </summary>
    public void UpdateCounts()
    {
        if (this.part?.SharedStringTable is not { } table)
            return;

        var unique = (uint)this.items.Count;
        table.UniqueCount = unique;
        table.Count ??= unique;
    }

    static SharedStringTablePart CreatePart(WorkbookPart workbookPart)
    {
        var created = workbookPart.AddNewPart<SharedStringTablePart>();
        created.SharedStringTable = new SharedStringTable();
        return created;
    }

    static string ReadText(SharedStringItem item)
    {
        // A shared string is either a plain <t> or a sequence of formatted <r> runs. Rich runs are
        // flattened for display; the original element is left alone so formatting survives the save.
        if (item.Text is { } plain)
            return plain.Text ?? string.Empty;

        var runs = item.Elements<Run>().ToList();
        if (runs.Count == 0)
            return item.InnerText;

        return string.Concat(runs.Select(r => r.Text?.Text ?? string.Empty));
    }
}
