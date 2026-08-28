using DocumentFormat.OpenXml.Wordprocessing;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Fills in the number in front of every numbered paragraph, in document order.
/// </summary>
/// <remarks>
/// <para>
/// A list number is not a property of the paragraph that carries it — it is a function of every
/// numbered paragraph before it. So <see cref="WordBodyReader"/> records only the
/// <see cref="ListNumbering"/> reference and leaves <see cref="ListLabel.Text"/> empty, and this walks
/// the finished block list to resolve it.
/// </para>
/// <para>
/// A pass rather than read-time counter state is what makes re-reading a single paragraph safe.
/// Counters consumed during the initial read cannot be rewound, so a reader that advanced them again
/// on every re-projection handed an edited list item the number <em>after</em> the document's last
/// one, and pushed it one further on every subsequent edit — including on undo, which re-reads the
/// restored paragraph the same way.
/// </para>
/// <para>
/// Running it from a single hook after every edit also buys the renumbering that never happened
/// before: insert or delete an item and the rest of its list follows.
/// </para>
/// </remarks>
sealed class NumberingSequencer(WordNumbering numbering)
{
    readonly Dictionary<(int NumId, int Level), int> counters = new();

    /// <summary>Renumbers a block list in place, from the start of the sequence.</summary>
    public void Apply(IList<DocumentBlock> blocks)
    {
        if (numbering.IsEmpty)
            return;

        this.counters.Clear();

        for (var i = 0; i < blocks.Count; i++)
        {
            if (this.Renumber(blocks[i]) is { } replacement)
                blocks[i] = replacement;
        }
    }

    /// <summary>
    /// Renumbers a block list the document does not own — a header or footer part.
    /// </summary>
    /// <remarks>
    /// Give each part its own sequencer. A numbered list in a header is its own list: it must neither
    /// continue the body's numbering nor the previous header's.
    /// </remarks>
    public IReadOnlyList<DocumentBlock> Applied(IReadOnlyList<DocumentBlock> blocks)
    {
        var copy = blocks.ToList();
        this.Apply(copy);
        return copy;
    }

    /// <summary>The renumbered block, or null when nothing in it changed.</summary>
    /// <remarks>
    /// Null rather than the block back, so an edit in a document full of lists rebuilds only the
    /// records whose number actually moved instead of every paragraph on every keystroke.
    /// </remarks>
    DocumentBlock? Renumber(DocumentBlock block) => block switch
    {
        DocumentParagraph paragraph => this.Renumber(paragraph),
        DocumentTable table => this.Renumber(table),
        _ => null
    };

    DocumentParagraph? Renumber(DocumentParagraph paragraph)
    {
        if (paragraph.List is not { Numbering: { } reference } label)
            return null;

        var text = this.Next(reference);

        return text == label.Text
            ? null
            : paragraph with { List = label with { Text = text } };
    }

    /// <summary>
    /// Renumbers the paragraphs inside a table's cells.
    /// </summary>
    /// <remarks>
    /// A list in a table cell is part of the body's sequence, so the walk has to descend into every
    /// cell whether or not anything there changes — skipping one would leave the counters short and
    /// renumber everything after the table.
    /// </remarks>
    DocumentTable? Renumber(DocumentTable table)
    {
        List<DocumentTableRow>? rows = null;

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            List<DocumentTableCell>? cells = null;

            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cell = row.Cells[c];
                List<DocumentBlock>? blocks = null;

                for (var b = 0; b < cell.Blocks.Count; b++)
                {
                    if (this.Renumber(cell.Blocks[b]) is not { } replacement)
                        continue;

                    blocks ??= cell.Blocks.ToList();
                    blocks[b] = replacement;
                }

                if (blocks is null)
                    continue;

                cells ??= row.Cells.ToList();
                cells[c] = cell with { Blocks = blocks };
            }

            if (cells is null)
                continue;

            rows ??= table.Rows.ToList();
            rows[r] = row with { Cells = cells };
        }

        return rows is null ? null : table with { Rows = rows };
    }

    /// <summary>Advances the counters and renders the label for one numbered paragraph.</summary>
    string Next(ListNumbering reference)
    {
        var level = numbering.Level(reference.NumId, reference.Level);

        if (level is null || level.IsNone)
            return string.Empty;

        if (level.IsBullet)
            return level.Template;

        this.Advance(reference.NumId, reference.Level, level);

        return this.Substitute(level.Template, reference.NumId);
    }

    void Advance(int numId, int levelIndex, ListLevel level)
    {
        var key = (numId, levelIndex);
        if (this.counters.TryGetValue(key, out var current))
        {
            this.counters[key] = current + 1;
        }
        else
        {
            this.counters[key] = level.Start;
        }

        // Starting a level restarts everything nested inside it, which is what makes 1.1, 1.2, 2.1
        // rather than 1.1, 1.2, 2.3.
        foreach (var deeper in this.counters.Keys.Where(k => k.NumId == numId && k.Level > levelIndex).ToList())
            this.counters.Remove(deeper);
    }

    /// <summary>
    /// Fills a level's <c>lvlText</c> template with the running counters it names.
    /// </summary>
    /// <remarks>
    /// Every placeholder renders in the format of the level it <em>refers to</em>, not the format of
    /// the paragraph being labelled. That is what makes a compounding template like <c>%1%2.</c> come
    /// out as <c>1a.</c> — level 0 is decimal and level 1 is a letter — instead of running both
    /// counters through whichever format the deeper level happens to carry.
    /// </remarks>
    string Substitute(string template, int numId)
    {
        var builder = new System.Text.StringBuilder();

        for (var i = 0; i < template.Length; i++)
        {
            if (template[i] != '%' || i + 1 >= template.Length || !char.IsAsciiDigit(template[i + 1]))
            {
                builder.Append(template[i]);
                continue;
            }

            // %1 is level 0, %2 is level 1, and so on.
            var referenced = template[i + 1] - '1';
            i++;

            var value = this.counters.GetValueOrDefault((numId, referenced), 0);
            if (value == 0)
                continue;

            var format = numbering.Level(numId, referenced)?.Format ?? NumberFormatValues.Decimal;
            builder.Append(WordNumbering.Render(value, format));
        }

        return builder.ToString();
    }
}
