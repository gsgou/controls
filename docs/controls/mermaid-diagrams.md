# MermaidDiagramControl

[← All Shiny Controls](../../README.md)

> Separate NuGet packages: `Shiny.Maui.Controls.MermaidDiagrams` / `Shiny.Blazor.Controls.MermaidDiagrams`

Native Mermaid flowchart renderer — no WebView, no SkiaSharp, AOT compatible on MAUI. Parses Mermaid syntax and renders interactive diagrams with pan and zoom support. Available for both MAUI and Blazor.

| Flowchart | Editor | Themes | Subgraphs |
|:---:|:---:|:---:|:---:|
| ![Flowchart](../../assets/mermaid-flowchart.png) | ![Editor](../../assets/mermaid-editor.png) | ![Themes](../../assets/mermaid-themes.png) | ![Subgraphs](../../assets/mermaid-subgraphs.png) |

```bash
dotnet add package Shiny.Maui.Controls.MermaidDiagrams
```

```xml
xmlns:diagram="http://shiny.net/maui/diagrams"
```

```xml
<diagram:MermaidDiagramControl
    DiagramText="graph TD&#10;    A[Start] --> B{Decision}&#10;    B -->|Yes| C[Do Something]&#10;    B -->|No| D[Do Other]&#10;    C --> E[End]&#10;    D --> E"
    HorizontalOptions="Fill"
    VerticalOptions="Fill" />
```

**Features:**
- Mermaid `graph` / `flowchart` syntax (TD, LR, BT, RL directions)
- 6 node shapes: Rectangle, RoundedRectangle, Stadium, Circle, Diamond, Hexagon
- 6 edge styles: Solid, Open, Dotted, DottedOpen, Thick, ThickOpen
- Subgraph support with nested grouping
- 4 built-in themes: Default, Dark, Forest, Neutral
- Pan and pinch-to-zoom gestures
- Sugiyama layered graph layout algorithm
