# Spreadsheet

[← All Shiny Controls](../../README.md)

> Separate packages: `Shiny.Maui.Controls.Office` and `Shiny.Blazor.Controls.Office`, over the shared
> `Shiny.Controls.Office.Shared` kernel and `Shiny.Controls.Office.Skia` renderer.

Opens, renders and edits `.xlsx` workbooks. Both hosts drive the same controller and paint with the
same SkiaSharp routine, so MAUI and Blazor are not two implementations kept in step — they are one.

```bash
dotnet add package Shiny.Maui.Controls.Office     # or Shiny.Blazor.Controls.Office
```

MAUI registers its Skia surface through one call:

```csharp
builder.UseMauiApp<App>().UseShinyControls().UseShinyOffice();
```

`UseShinyOffice()` calls `UseSkiaSharp()` for you and, on the macOS AppKit head (`net10.0-macos`), adds
the Skia canvas SkiaSharp itself does not ship — it has no `-macos` target, so that head falls back to a
handler whose `CreatePlatformView()` throws and every Office control came up blank. Elsewhere the two
calls are equivalent.

```xml
<office:SpreadsheetView Workbook="{Binding Workbook}" SheetName="Budget" />
```

```razor
<div style="height:420px">
    <SpreadsheetView Workbook="workbook"
                 @bind-SheetName="sheetName"
                 ShowFormulaBar="true"
                 Theme="SpreadsheetTheme.Dark" />
</div>
```

```csharp
using var workbook = await Workbook.OpenAsync("book.xlsx");   // or Workbook.Create("Sheet1")

workbook.Execute(new SetCellValueCommand("Budget", CellRef.Parse("B2"), CellValue.FromNumber(42)));
workbook.Execute(new SetCellFormulaCommand("Budget", CellRef.Parse("D2"), "B2*C2"));
workbook.Undo.Undo();

await workbook.SaveAsync();
```

**Formula bar.** A name box and formula field above the grid, on by default and turned off with
`ShowFormulaBar="false"`. It matters because the grid paints the *result*: a cell reading 156.75 gives
no way to discover that it holds `=SUM(D2:D4)`, and no way to edit it as a formula rather than retyping
it. Editing there is the same undoable command as typing into the cell; Enter commits and moves down,
Escape reverts, and clicking away commits into the cell that was being edited rather than the one
clicked. Typing an address into the name box goes there.

**Formatting toolbar.** `ShowToolbar="true"` puts a `SpreadsheetToolbar` above the formula bar. It has
the usual half — font, size, bold, italic, underline, strikethrough, text colour, alignment on both
axes, wrap text — and the half only a spreadsheet needs: **cell fill** for highlighting, number
formats (general, number, currency in the reader's own culture, percent, scientific, date, time,
text), increase and decrease decimal places, **auto functions** (Σ writes SUM, and its split button
also offers average, count numbers, min and max), fit-column-to-contents, and clear formatting. Every
button is one undoable command through the same `SpreadsheetController` a keyboard shortcut would
reach, so a toolbar action and a typed edit share one undo stack. The toolbar is off by default — the
formula bar and tab strip are how a workbook is *read*, and a viewer should not grow a formatting bar
it never asked for.

Formatting is applied as a **delta**, not as a format assigned wholesale: bolding a range that mixes a
red heading with black body text leaves both colours where they are. Styles are interned, so bolding a
thousand cells adds exactly one entry to the styles part rather than a thousand identical ones.

**Formatting columns.** Select a column from its header and the format is written as a *column* style —
one attribute on one `<col>` element, the way Excel does it — so it applies to every row including the
ones that do not exist yet. That is what makes a column formatted as currency still show currency for a
value typed into it tomorrow. Formatting one cell inside a formatted column still overrides it, and
clearing that cell's formatting does not let the column's come back. Row-header selections work the
same way. Column widths and row heights are now recorded in the file, so a column dragged wider — or
fitted to its contents from the toolbar — survives a save and reopen.

```csharp
var controller = view.Controller;                    // MAUI: Sheet.Controller, Blazor: view.Controller
controller.ToggleBold();
controller.SetFillColor(new ArgbColor(255, 0xFF, 0xEB, 0x3B));
controller.SetNumberFormat(NumberFormatPreset.Currency);
controller.AdjustDecimals(+1);
controller.ApplyAutoFunction(AutoFunction.Sum);      // false when there is nothing to total
controller.AutoFitColumns();
controller.ActiveFormat;                             // ResolvedFormat — what a toolbar shows the state of
```

**Worksheets.** A workbook is a book, not a sheet, and the control shows it as one: a tab strip under
the grid switches between the visible sheets and adds, renames, duplicates, reorders, hides and deletes
them. Every one of those is an undoable command like any cell edit, and each sheet keeps its own
selection, scroll position and hand-dragged column widths, so moving between tabs comes back to where
you were. Hidden sheets stay off the strip — they are hidden in Excel too — and are reachable from the
overflow menu, which is also the only place to unhide one. Set `ShowSheetTabs="false"` to leave the
strip out, or `AllowSheetEditing="false"` to keep it as a switcher only.

Renaming rewrites every formula and defined name that pointed at the old name — including the quoted
spelling (`'Q1 Sales'!A1`) and both ends of a 3-D span — so a rename cannot leave `#REF!` behind.
Formulas already read across sheets, and always did.

```csharp
workbook.Execute(new AddSheetCommand("Forecast", index: 1));
workbook.Execute(new RenameSheetCommand("Sheet1", "Actuals"));   // rewrites Sheet1!B2 everywhere
workbook.Execute(new DuplicateSheetCommand("Actuals", "Actuals (2)", index: 2));
workbook.Execute(new MoveSheetCommand("Forecast", 0));
workbook.Execute(new SetSheetVisibilityCommand("Scratch", false));
workbook.Execute(new DeleteSheetCommand("Forecast"));            // undo restores it with its contents
```

| Capability | Notes |
|---|---|
| Rendering | Virtualized over all 1,048,576 rows; frozen panes, merged cells, number formats, fonts, fills, alignment, theme colours with tint |
| Editing | Cell values and formulas, range clear, column/row resize, range selection, in-cell editing through a native `Entry` / `<input>` |
| Formula bar | Name box and formula field on both hosts, `ShowFormulaBar` to hide; shows the formula, not the result |
| Formatting | `ShowToolbar` on both hosts: font, bold/italic/underline/strike, text colour, cell fill, alignment on both axes, wrap; applied as a delta so a mixed selection keeps what each cell had |
| Number formats | Currency (culture-aware), percent, scientific, date, time, text, plus increase/decrease decimals |
| Auto functions | Σ writes SUM, AVERAGE, COUNT, MIN or MAX over the range the selection implies — the run above, the run to the left, or one total per column of a block |
| Columns | Header selections format the whole column via a `<col>` style; widths, row heights, auto-fit and hide/show are recorded in the file |
| Worksheets | Tab strip on both hosts: switch, add, rename, duplicate, reorder, hide/unhide, delete — all undoable, with per-sheet selection and scroll |
| Undo | Transactional, with typing-run coalescing; a range clear is one step |
| Formulas | ~80 functions, dependency-ordered incremental recalculation, circular-reference detection |
| Round-trip | Edits are surgical. An unmodified workbook saves byte-identical; macros, tracked changes, pivot caches and custom XML survive untouched |
| Reporting | `UnsupportedFeatureCollector` names anything in a document the editor cannot show or edit |

**Constraints.** Blazor is **WebAssembly only** (a Server round-trip per keystroke is unusable, and
SkiaSharp on WASM needs the `wasm-tools` workload — without it `libSkiaSharp` is never linked into the
runtime and the app fails in the browser, so `Shiny.Blazor.Controls.Office` fails the build up front
with `SHINY0001` instead; bypass with `ShinySkipWasmToolsCheck=true`). MAUI requires `UseShinyOffice()` (which registers SkiaSharp, plus the AppKit canvas on `net10.0-macos`). Inserting and
deleting rows and columns is deliberately not implemented — it requires rewriting references across
formulas, merged cells, conditional formatting, defined names, data validation, charts and tables.
Chart, dialog and macro sheets are preserved on save but have no tab: the grid has nothing to draw for
them. Deleting a worksheet drops any defined name scoped to it, which is what Excel does.
