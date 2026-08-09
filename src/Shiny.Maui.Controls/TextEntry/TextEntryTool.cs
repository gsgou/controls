using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public interface ITextEntryAwareTool
{
    void Attach(TextEntry entry);
    void Detach();
}

/// <summary>
/// A tappable icon/label docked to the left or right edge inside a <see cref="TextEntry"/>.
/// Painted according to the parent's <see cref="TextEntry.ToolStyle"/>.
/// </summary>
public class TextEntryTool : IconTextTool
{
    // An inline tool is a bare glyph on the field, so it only needs enough padding to reach a
    // comfortable tap target. An addon tool is a filled block and wants the Bootstrap gutter.
    const double InlinePadding = 8;
    const double AddonPadding = 12;
    const double MinimumTapTarget = 40;

    public TextEntryTool()
    {
        ApplyToolStyle(TextEntryToolStyle.Inline);

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(TextEntryTool));
    }

    internal TextEntry? ParentEntry { get; set; }

    internal void ApplyToolStyle(TextEntryToolStyle style)
    {
        if (style == TextEntryToolStyle.Addon)
        {
            Padding = new Thickness(AddonPadding, 0);
            MinimumWidthRequest = 0;
            BackgroundColor = Colors.Transparent; // the rail behind it carries the addon surface
        }
        else
        {
            Padding = new Thickness(InlinePadding, 0);
            MinimumWidthRequest = MinimumTapTarget;
            BackgroundColor = Colors.Transparent;
        }
    }
}
