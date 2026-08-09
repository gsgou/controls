using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Inline SVG toolbar icons for <see cref="ImageEditor"/>. These mirror the MAUI icon set stroke
/// for stroke so the two hosts look like the same control; they replace the unicode glyphs the
/// toolbar used to render, which came out at different weights in every browser.
/// </summary>
internal static class ImageEditorIcons
{
    public static readonly MarkupString Move = Svg("M12 3v18M3 12h18M8.5 6.5 12 3l3.5 3.5M8.5 17.5 12 21l3.5-3.5M6.5 8.5 3 12l3.5 3.5M17.5 8.5 21 12l-3.5 3.5");
    public static readonly MarkupString Crop = Svg("M6 2v14h14M4 6h14v14");
    public static readonly MarkupString Rotate = Svg("M19.5 12a7.5 7.5 0 1 1-2.6-5.7M16.2 3.4 19.9 6.6 15.9 9");
    public static readonly MarkupString Draw = Svg("M4 20l.8-3.8L15.6 5.4l3 3L7.8 19.2zM13.4 7.6l3 3");
    public static readonly MarkupString Line = Svg("M5 19 19 5");
    public static readonly MarkupString Arrow = Svg("M4 20 19 5M11.5 5H19v7.5");
    public static readonly MarkupString Text = Svg("M5 5.5h14M12 5.5V19M8.5 19h7");
    public static readonly MarkupString Undo = Svg("M9 14 4 9l5-5M4 9h9a7 7 0 0 1 0 14h-3.5");
    public static readonly MarkupString Redo = Svg("M15 14l5-5-5-5M20 9h-9a7 7 0 0 0 0 14h3.5");
    public static readonly MarkupString Reset = Svg("M4.5 12a7.5 7.5 0 1 0 2.6-5.7M7.8 3.4 4.1 6.6 8.1 9");
    public static readonly MarkupString ZoomIn = Svg("M10.5 17a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13ZM16.5 16.5l4 4M7.4 10.5h6.2M10.5 7.4v6.2");
    public static readonly MarkupString ZoomOut = Svg("M10.5 17a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13ZM16.5 16.5l4 4M7.4 10.5h6.2");
    public static readonly MarkupString ZoomFit = Svg("M3 8V3h5M16 3h5v5M21 16v5h-5M8 21H3v-5");
    public static readonly MarkupString Check = Svg("m4.5 12.5 5.3 5.5L19.5 6.5");
    public static readonly MarkupString Close = Svg("M6 6l12 12M18 6 6 18");

    // width/height live on the element rather than in the stylesheet: this markup is injected as a
    // MarkupString, so it carries no CSS-isolation scope attribute and a scoped `... svg` rule
    // would not reliably match it.
    static MarkupString Svg(string path) => new(
        $"""<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="{path}"/></svg>""");
}
