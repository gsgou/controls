using Shiny.Maui.Controls.Infrastructure;
namespace Shiny.Maui.Controls;

public class ClearButtonTool : TextEntryTool, ITextEntryAwareTool
{
    TextEntry? entry;

    public ClearButtonTool()
    {
        Text = "\u2715"; // unicode X
        ToolColor = Colors.Grey;
        IsVisible = false;
        Clicked += OnClicked;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(ClearButtonTool));
    }

    void ITextEntryAwareTool.Attach(TextEntry e)
    {
        entry = e;
        entry.InternalTextChanged += OnEntryTextChanged;
        UpdateVisibility();
    }

    void ITextEntryAwareTool.Detach()
    {
        if (entry is not null)
            entry.InternalTextChanged -= OnEntryTextChanged;
        entry = null;
    }

    void OnEntryTextChanged(object? sender, EventArgs e) => UpdateVisibility();

    void UpdateVisibility() => IsVisible = !string.IsNullOrEmpty(entry?.Text);

    void OnClicked(object? sender, EventArgs e)
    {
        if (entry is not null)
            entry.Text = string.Empty;
    }
}
