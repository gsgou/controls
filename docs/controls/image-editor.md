# ImageEditor

[← All Shiny Controls](../../README.md)

An inline image editor with cropping, rotation, freehand drawing, line and arrow drawing, **shapes — rectangle, ellipse and circle, each with its own fill and border** — text annotations with font family and font size selection, and **zoom/pan that stays live in every tool** — pinch (or wheel / zoom buttons) to magnify up to 8x and draw, crop or place text with pixel accuracy, then two-finger drag to pan without leaving the tool. Includes a built-in undo/redo stack, reset-to-original, and export to PNG/JPEG/WEBP at configurable resolutions. Every feature can be toggled on/off, and the default toolbar can be replaced with a custom template.

The default toolbar is a floating rounded bar with vector (not glyph-font) icons, a horizontally scrollable tool row that never clips on narrow screens, a contextual options row for the active tool (colour swatch, pen weights, font pickers), and an action row with undo/redo/reset, a zoom cluster, and save.

| Editor | Crop Mode |
|:---:|:---:|
| ![Image Editor](../../assets/imageeditor1.png) | ![Crop Mode](../../assets/imageeditor2.png) |

```xml
<shiny:ImageEditor Source="{Binding ImageSource}"
                   CurrentToolMode="{Binding ToolMode}"
                   AllowCrop="True"
                   AllowRotate="True"
                   AllowDraw="True"
                   AllowTextAnnotation="True"
                   DrawStrokeColor="Red"
                   DrawStrokeWidth="3" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Source | ImageSource? | null | Image to edit (supports file, stream, URI) |
| CurrentToolMode | ImageEditorToolMode | Move | Active tool (Move, Crop, Draw, Text, Line, Arrow, Rectangle, Ellipse, Circle) — TwoWay |
| AllowCrop | bool | true | Enable/disable crop tool |
| AllowRotate | bool | true | Enable/disable rotate action |
| AllowDraw | bool | true | Enable/disable freehand drawing |
| AllowTextAnnotation | bool | true | Enable/disable text annotation |
| AllowLine | bool | true | Enable/disable line drawing tool |
| AllowRectangle | bool | true | Enable/disable the rectangle shape tool |
| AllowEllipse | bool | true | Enable/disable the ellipse shape tool |
| AllowCircle | bool | true | Enable/disable the circle shape tool |
| ShapeFillColor | Color? | null | Shape interior; null draws the outline only. Alpha honoured — TwoWay |
| ShowShapeFillPicker | bool | true | Show the fill swatch and fill on/off toggle while a shape tool is active |
| AllowFontSelection | bool | false | Show font picker button in text mode |
| AllowFontSizeSelection | bool | false | Show font size picker button in text mode |
| AllowZoom | bool | true | Enable/disable zoom & pan |
| ZoomLevel | double | 1 | Current zoom factor where 1.0 is fit-to-view — TwoWay |
| MinZoom | double | 1 | Lower zoom bound |
| MaxZoom | double | 8 | Upper zoom bound |
| ShowZoomControls | bool | true | Show the zoom out / % / zoom in / fit cluster in the toolbar |
| ShowToolLabels | bool | true | Show captions under the tool icons |
| ShowStrokeWidthPicker | bool | true | Show pen-weight presets next to the colour swatch |
| StrokeWidthPresets | IList\<double\> | 2, 4, 8 | Pen weights offered by the stroke-width picker |
| ToolbarBackgroundColor | Color | dark scrim | Background of the default toolbar |
| CanUndo | bool | false | Whether undo is available (OneWayToSource) |
| CanRedo | bool | false | Whether redo is available (OneWayToSource) |
| DrawStrokeColor | Color | White | Drawing stroke color — TwoWay |
| DrawStrokeWidth | double | 3 | Drawing stroke width — TwoWay |
| TextFontSize | double | 16 | Text annotation font size |
| TextFontFamily | string? | null | Font family for text annotations (TwoWay) |
| AnnotationTextColor | Color | White | Text annotation color |
| AvailableFonts | IList\<string\>? | null | Font families shown in font picker |
| AvailableFontSizes | IList\<double\>? | null | Font sizes shown in font size picker |
| SaveCommand | ICommand? | null | Invoked with `EditedImage` parameter on save |
| SaveText | string | "Save" | Save button label |
| CropApplyText | string | "Apply" | Crop apply button label |
| CropCancelText | string | "Cancel" | Crop cancel button label |
| ToolbarTemplate | DataTemplate? | null | Custom toolbar (replaces default) |
| ToolbarPosition | ToolbarPosition | Bottom | Toolbar placement (Top or Bottom) |
| UseFeedback | bool | true | Feedback on actions |

**Features:**
- Zoom and pan in **every** tool: pinch anywhere (two fingers), two-finger drag to pan, double-tap to toggle, plus toolbar zoom buttons and a live zoom % readout. Blazor adds mouse-wheel zoom about the cursor and middle-button pan. Crop chrome and hit targets keep a constant on-screen size at any zoom.
- Crop with drag handles, rule-of-thirds grid, dimmed overlay, and dedicated Apply/Cancel toolbar
- 90° rotation (or arbitrary angles)
- Freehand drawing with configurable color and stroke width (constrained to image bounds)
- Line and arrow drawing between two points with configurable color and width
- Shapes — rectangle, ellipse and circle — dragged corner to corner. The ink colour and pen weight are the border; the fill is its own swatch with its own opacity and an on/off toggle, so the same tool draws a translucent highlight box or an opaque redaction block. The circle tool constrains the drag to a square, and on Blazor holding **Shift** does the same for the rectangle and ellipse.
- Inline text annotations placed by tapping the image with optional font family and size selection
- Integrated color picker for draw color
- Font picker and font size picker integration (when `AllowFontSelection`/`AllowFontSizeSelection` enabled)
- Undo/redo for every edit action
- Reset to original image
- Save via `SaveCommand` with `EditedImage` — call `ToStreamAsync(format)` to get PNG, JPEG, or WEBP
- Image border showing the drawable surface area
- Strokes, lines, shapes and text record the on-screen image size they were drawn at, so annotations made on a small preview (or while zoomed in) keep their proportions when exported at full resolution

**Commands:** `UndoCommand`, `RedoCommand`, `RotateCommand`, `ResetCommand`, `CropCommand`, `DrawCommand`, `TextCommand`, `LineCommand`, `RectangleCommand`, `EllipseCommand`, `CircleCommand`, `SaveCommand`, `ZoomInCommand`, `ZoomOutCommand`, `ZoomToFitCommand`

**Methods:** `Undo()`, `Redo()`, `Rotate(float)`, `Reset()`, `ApplyCrop()`, `GetEditedImage()`, `ZoomIn()`, `ZoomOut()`, `ZoomToFit()`

**Events:** `ZoomChanged`

On Blazor the equivalents are `ZoomInAsync()`, `ZoomOutAsync()`, `ZoomToFitAsync()`, `SetZoomAsync(double)`, the `ZoomLevel` property and the `ZoomLevelChanged` callback, plus a `ToolbarActions` render fragment for host-supplied buttons at the trailing edge of the bar. `ShapeFillColor` there is a `#rrggbb` string and carries its alpha in a companion `ShapeFillOpacity` (0-1), because `<input type="color">` cannot express alpha — MAUI keeps it in the `Color` itself.
