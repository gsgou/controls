namespace Shiny.Maui.Controls;

/// <summary>
/// How <see cref="TextEntryTool"/>s docked in a <see cref="TextEntry"/> are painted.
/// </summary>
public enum TextEntryToolStyle
{
    /// <summary>
    /// The tool sits directly on the field background with no separator - a plain tinted
    /// icon/label inside the border, the way Material and iOS text fields render leading and
    /// trailing icons. This is the default.
    /// </summary>
    Inline,

    /// <summary>
    /// The tool is a filled "addon" block with its own surface colour and a hairline separator
    /// between it and the field, mirroring Bootstrap's <c>.input-group-text</c>.
    /// </summary>
    Addon
}
