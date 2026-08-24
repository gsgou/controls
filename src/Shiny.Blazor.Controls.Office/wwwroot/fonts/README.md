# Bundled fonts

Metric-compatible substitutes for the fonts Office documents reference but that are not installed
anywhere outside Microsoft Office — and, on Blazor WebAssembly, are not installed *at all*, because
SkiaSharp on WASM has no access to system fonts.

| Bundled | Substitutes for | Licence |
|---|---|---|
| Carlito | Calibri, Calibri Light, Aptos, Segoe UI | SIL Open Font License 1.1 |
| Caladea | Cambria, Cambria Math | SIL Open Font License 1.1 |

Metric-compatible means the glyph advance widths match the originals, so a document laid out against
Calibri breaks its lines in the same places against Carlito. A merely *similar* font would render
legibly and paginate differently.

Full licence texts: `Carlito-OFL.txt`, `Caladea-OFL.txt`. Both carry a Reserved Font Name, so the
files must keep their names.

- Carlito — https://github.com/googlefonts/carlito
- Caladea — https://github.com/huertatipografica/Caladea
