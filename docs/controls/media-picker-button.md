# MediaPickerButton

[← All Shiny Controls](../../README.md)

A button that adds photos from the gallery and/or camera, compresses/re-encodes each to PNG or JPEG at a chosen quality (with optional max-dimension downscale), caps the count with `MaxPhotos` (added one at a time), and shows the collected photos inline as a tappable carousel (opening the **ImageViewer**, with an optional **Edit** button that reuses the **ImageEditor**) or a compact pinch/zoom overlay. Ships in the base packages (no extra package). MAUI uses the built-in `MediaPicker`; Blazor uses a hidden `<input type="file">` (with `capture` for the camera) and compresses on an offscreen canvas.

<!-- TODO: capture screenshots for media-picker-button -->

```xml
<shiny:MediaPickerButton Photos="{Binding Photos}"
                         AllowGallery="True"
                         AllowCamera="True"
                         AllowPhotoEdit="True"
                         ShowAsCarouselInView="True"
                         MaxPhotos="5"
                         CompressionQuality="85"
                         OutputFormat="Jpeg"
                         PermissionDeniedText="Photo access was denied — enable it in Settings." />
```

| Property | Type | Default | Description |
|---|---|---|---|
| AllowGallery | bool | true | Offer "choose from gallery" |
| AllowCamera | bool | true | Offer "take photo" (chooser shown when both are enabled) |
| AllowPhotoEdit | bool | false | Show an Edit button that opens the ImageEditor |
| PermissionDeniedText | string | "Permission denied…" | Shown when camera/gallery access is denied |
| NoImagesTemplate | DataTemplate? | null | Shown when there are no photos yet |
| ShowAsCarouselInView | bool | true | Inline carousel (true) vs compact preview + pinch/zoom overlay (false) |
| MaxPhotos | int | 1 | Maximum photos (added one at a time) |
| CompressionQuality | int | 92 | Encoder quality percentage (1–100) |
| MaxImageDimension | int | 0 | If > 0, longest edge is downscaled to this many pixels |
| OutputFormat | ImageExportFormat | Jpeg | Output encoding (Png or Jpeg) |
| Photos | IList\<MediaPickerItem\> | empty | Collected photos (TwoWay) |

**Events:** `PhotoAdded`, `PhotoRemoved`, `PhotosChanged` (+ `PhotosChangedCommand`), `PermissionDenied`

On Blazor the equivalent `Shiny.Blazor.Controls.MediaPickerButton` mirrors these as `[Parameter]`s (`OutputFormat` is `"jpeg"`/`"png"`), with `@bind-Photos` over `MediaPickerItem` (each exposing a `DataUri` for `<img src>`).
