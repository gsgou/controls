namespace Shiny.Blazor.Controls;

/// <summary>
/// Visual style of <see cref="TextEntry"/>. Mirrors the MAUI control.
/// </summary>
public enum TextEntryVariant
{
    /// <summary>Static placeholder inside the field — the browser's own <c>placeholder</c>.</summary>
    Classic,

    /// <summary>
    /// Material 3 outlined field: the label rides up onto the top border and sits in a notch cut
    /// out of the outline, so it never shares space with the text being typed.
    /// </summary>
    Floating
}

/// <summary>
/// How tools docked in a <see cref="TextEntry"/> are painted.
/// </summary>
public enum TextEntryToolStyle
{
    /// <summary>A bare tinted icon on the field itself, with no separator. The default.</summary>
    Inline,

    /// <summary>A filled block with a hairline separator — Bootstrap's <c>.input-group-text</c>.</summary>
    Addon
}
