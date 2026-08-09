namespace Shiny.Blazor.Controls;

/// <summary>
/// A self-contained TextEntry tool that shows a clear (✕) button when text is present.
/// </summary>
public class ClearButtonTool : TextEntryTool
{
    public ClearButtonTool()
    {
        Text = "\u2715";
        IsVisible = false;
    }

    public override void OnTextChanged(string? text)
    {
        IsVisible = !string.IsNullOrEmpty(text);
    }

    protected override void OnClick()
    {
        SetEntryText(string.Empty);
    }
}
