namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// A row in <see cref="PromptView.Suggestions"/> rendered by the built-in template. Bind
/// <see cref="PromptView.SuggestionTemplate"/> instead to render your own type.
/// </summary>
public sealed class PromptSuggestion
{
    public PromptSuggestion() { }

    public PromptSuggestion(string text, string? description = null, string? glyph = null, object? value = null)
    {
        this.Text = text;
        this.Description = description;
        this.Glyph = glyph;
        this.Value = value;
    }

    /// <summary>The suggestion itself. Chosen with Enter or a click, and written into the prompt.</summary>
    public string Text { get; set; } = String.Empty;

    /// <summary>Optional second line — what the suggestion does, a keyboard shortcut, a source.</summary>
    public string? Description { get; set; }

    /// <summary>Optional leading glyph. Any string: an emoji, or a character from an icon font.</summary>
    public string? Glyph { get; set; }

    /// <summary>Anything you want to carry through to the selection handler.</summary>
    public object? Value { get; set; }

    public override string ToString() => this.Text;
}

/// <summary>Raised when the user submits the prompt.</summary>
public sealed class PromptSubmittedEventArgs : EventArgs
{
    public PromptSubmittedEventArgs(string text, object? suggestion)
    {
        this.Text = text;
        this.Suggestion = suggestion;
    }

    /// <summary>The prompt text at the moment of submission.</summary>
    public string Text { get; }

    /// <summary>
    /// The suggestion that was chosen, when submission came from picking one (Enter on a
    /// highlighted row, or a click). Null for a plain typed submit.
    /// </summary>
    public object? Suggestion { get; }
}
