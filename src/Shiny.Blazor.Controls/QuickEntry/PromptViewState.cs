using System.Collections;
using System.Collections.ObjectModel;

namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// The state the host's built-in <see cref="PromptView"/> renders from.
/// </summary>
/// <remarks>
/// A service cannot hand a component parameters directly, so the two meet at this object: the app
/// mutates it from anywhere, the host binds to it, and <see cref="Changed"/> is what tells the host
/// to re-render. Using <see cref="PromptView"/> directly on a page needs none of this — it takes
/// ordinary parameters.
/// </remarks>
public class PromptViewState
{
    string text = String.Empty;
    string placeholder = "Ask anything…";
    bool isBusy;
    string busyText = "Thinking…";
    string? response;
    IEnumerable? suggestions;
    string? icon;
    bool showIcon = true;
    double? dropdownHeight;

    /// <summary>The prompt text.</summary>
    public string Text
    {
        get => this.text;
        set => this.Set(ref this.text, value);
    }

    /// <summary>Placeholder shown in the empty prompt.</summary>
    public string Placeholder
    {
        get => this.placeholder;
        set => this.Set(ref this.placeholder, value);
    }

    /// <summary>Spins the orb, shows a spinner and swaps submit for a stop button.</summary>
    public bool IsBusy
    {
        get => this.isBusy;
        set
        {
            if (this.isBusy == value)
                return;

            this.isBusy = value;
            this.BusyChanged?.Invoke(this, value);
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Status line shown while busy and no response has arrived.</summary>
    public string BusyText
    {
        get => this.busyText;
        set => this.Set(ref this.busyText, value);
    }

    /// <summary>The answer, as markdown-free text. For rich content, set the host's content instead.</summary>
    public string? Response
    {
        get => this.response;
        set => this.Set(ref this.response, value);
    }

    /// <summary>Suggestion rows. <see cref="PromptSuggestion"/> renders with the built-in template.</summary>
    public IEnumerable? Suggestions
    {
        get => this.suggestions;
        set => this.Set(ref this.suggestions, value);
    }

    /// <summary>Leading glyph in place of the animated orb — any string, an emoji or an icon-font character.</summary>
    public string? Icon
    {
        get => this.icon;
        set => this.Set(ref this.icon, value);
    }

    /// <summary>Show the leading slot at all. Default true.</summary>
    public bool ShowIcon
    {
        get => this.showIcon;
        set => this.Set(ref this.showIcon, value);
    }

    /// <summary>
    /// Fixed height for the dropdown area in CSS pixels. Null (the default) sizes it to its content;
    /// a value pins it and scrolls, which is what you want for a list that changes length as the user
    /// types and would otherwise make the popup jump under the pointer.
    /// </summary>
    public double? DropdownHeight
    {
        get => this.dropdownHeight;
        set => this.Set(ref this.dropdownHeight, value);
    }

    /// <summary>
    /// Tools docked beside the orb. Observable, so adding one after the popup has been built shows
    /// up without reconfiguring anything.
    /// </summary>
    public ObservableCollection<PromptTool> LeadingTools { get; } = new();

    /// <summary>Tools docked at the trailing edge, before the microphone and submit buttons.</summary>
    public ObservableCollection<PromptTool> TrailingTools { get; } = new();

    /// <summary>Raised when the user submits — Enter, the submit button, or picking a suggestion.</summary>
    public event EventHandler<PromptSubmittedEventArgs>? Submitted;

    /// <summary>Raised when the stop button is pressed while busy.</summary>
    public event EventHandler? Cancelled;

    /// <summary>Raised whenever <see cref="IsBusy"/> changes — what the glow's WhileBusy trigger listens on.</summary>
    public event EventHandler<bool>? BusyChanged;

    /// <summary>Raised whenever anything here changes, so the host can re-render.</summary>
    public event EventHandler? Changed;

    internal void RaiseSubmitted(string text, object? suggestion)
        => this.Submitted?.Invoke(this, new PromptSubmittedEventArgs(text, suggestion));

    internal void RaiseCancelled() => this.Cancelled?.Invoke(this, EventArgs.Empty);

    void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
