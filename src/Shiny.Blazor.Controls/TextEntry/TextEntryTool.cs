namespace Shiny.Blazor.Controls;

/// <summary>
/// Base class for tools that can be placed in a TextEntry's left or right tool slots.
/// </summary>
public class TextEntryTool
{
    /// <summary>Icon text/emoji displayed in the tool button.</summary>
    public string? Icon { get; set; }

    /// <summary>Text label displayed in the tool button.</summary>
    public string? Text { get; set; }

    /// <summary>CSS color for the tool. Null follows the on-surface-variant theme token.</summary>
    public string? ToolColor { get; set; }

    /// <summary>Whether the tool is visible.</summary>
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>Callback when the tool is clicked.</summary>
    public Action? Clicked { get; set; }

    /// <summary>CSS class applied to the tool button.</summary>
    public string? CssClass { get; set; }

    /// <summary>Called by TextEntry when text changes. Override to react to text changes.</summary>
    public virtual void OnTextChanged(string? text) { }

    /// <summary>Called by TextEntry when the tool is attached.</summary>
    public virtual void OnAttached(TextEntryContext context) { }

    /// <summary>Called by TextEntry when the tool is detached.</summary>
    public virtual void OnDetached() { }

    /// <summary>Called internally by the tool to update the parent entry text.</summary>
    protected void SetEntryText(string text) => _context?.SetText(text);

    /// <summary>Gets the current entry text.</summary>
    protected string? GetEntryText() => _context?.GetText();

    internal TextEntryContext? _context;

    internal void InternalAttach(TextEntryContext context)
    {
        _context = context;
        OnAttached(context);
    }

    internal void InternalDetach()
    {
        OnDetached();
        _context = null;
    }

    internal void InternalClick()
    {
        Clicked?.Invoke();
        OnClick();
    }

    /// <summary>Override to handle click without using Clicked callback.</summary>
    protected virtual void OnClick() { }
}

/// <summary>
/// Context object provided to tools for interacting with the parent TextEntry.
/// </summary>
public class TextEntryContext
{
    readonly Func<string?> getText;
    readonly Action<string> setText;

    public TextEntryContext(Func<string?> getText, Action<string> setText)
    {
        this.getText = getText;
        this.setText = setText;
    }

    public string? GetText() => getText();
    public void SetText(string text) => setText(text);
}
