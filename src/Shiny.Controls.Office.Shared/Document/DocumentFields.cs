using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Substitutes computed fields with their value for one page.
/// </summary>
/// <remarks>
/// <para>
/// Only headers and footers go through this, and only because they are laid out per page anyway. A
/// <c>PAGE</c> field in the <em>body</em> keeps whatever result the document was saved with: resolving
/// it would change the text's width, which can change where the line wraps, which can change which
/// page the field is on — the value feeding back into its own input. Word settles that by iterating
/// its layout, and a viewer that did the same would re-paginate the whole document on every repaint.
/// </para>
/// <para>
/// Blocks with no fields in them are returned unchanged rather than rebuilt, so the common case — a
/// header of plain text — allocates nothing.
/// </para>
/// </remarks>
public static class DocumentFields
{
    public static IReadOnlyList<DocumentBlock> Resolve(IReadOnlyList<DocumentBlock> blocks, int pageNumber, int pageCount)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        List<DocumentBlock>? rebuilt = null;

        for (var i = 0; i < blocks.Count; i++)
        {
            var replacement = ResolveBlock(blocks[i], pageNumber, pageCount);
            if (ReferenceEquals(replacement, blocks[i]))
            {
                rebuilt?.Add(blocks[i]);
                continue;
            }

            rebuilt ??= [.. blocks.Take(i)];
            rebuilt.Add(replacement);
        }

        return rebuilt ?? blocks;
    }

    static DocumentBlock ResolveBlock(DocumentBlock block, int pageNumber, int pageCount) => block switch
    {
        DocumentParagraph paragraph => ResolveParagraph(paragraph, pageNumber, pageCount),
        DocumentTable table => ResolveTable(table, pageNumber, pageCount),
        _ => block
    };

    static DocumentBlock ResolveParagraph(DocumentParagraph paragraph, int pageNumber, int pageCount)
    {
        var hasField = false;
        foreach (var run in paragraph.Runs)
        {
            if (run.Field != DocumentFieldKind.None)
            {
                hasField = true;
                break;
            }
        }

        if (!hasField)
            return paragraph;

        var runs = new List<StyledRun>(paragraph.Runs.Count);
        foreach (var run in paragraph.Runs)
        {
            runs.Add(run.Field switch
            {
                DocumentFieldKind.Page => run with { Text = pageNumber.ToString(System.Globalization.CultureInfo.CurrentCulture) },
                DocumentFieldKind.PageCount => run with { Text = pageCount.ToString(System.Globalization.CultureInfo.CurrentCulture) },
                _ => run
            });
        }

        // The element anchor is deliberately dropped: this projection exists only to be measured and
        // drawn, and a paragraph carrying a live w:p that an edit could then write through would let
        // a header's rendering become a way to modify the document.
        return new DocumentParagraph(runs, paragraph.Format)
        {
            List = paragraph.List,
            StyleName = paragraph.StyleName
        };
    }

    static DocumentBlock ResolveTable(DocumentTable table, int pageNumber, int pageCount)
    {
        List<DocumentTableRow>? rows = null;

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            List<DocumentTableCell>? cells = null;

            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cell = row.Cells[c];
                var resolved = Resolve(cell.Blocks, pageNumber, pageCount);
                if (ReferenceEquals(resolved, cell.Blocks))
                {
                    cells?.Add(cell);
                    continue;
                }

                cells ??= [.. row.Cells.Take(c)];
                cells.Add(cell with { Blocks = resolved });
            }

            if (cells is null)
            {
                rows?.Add(row);
                continue;
            }

            rows ??= [.. table.Rows.Take(r)];
            rows.Add(row with { Cells = cells });
        }

        return rows is null ? table : table with { Rows = rows };
    }
}
