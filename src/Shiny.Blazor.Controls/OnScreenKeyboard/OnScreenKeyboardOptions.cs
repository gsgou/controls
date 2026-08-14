namespace Shiny.Blazor.Controls.OnScreenKeyboard;

/// <summary>How the keyboard paints itself.</summary>
public enum OnScreenKeyboardTheme
{
    /// <summary>Follow the app's Shiny theme tokens. The default, and the only one that tracks a runtime theme switch.</summary>
    Auto,
    Light,
    Dark
}

/// <summary>
/// Configuration for the on-screen keyboard, registered as a singleton by
/// <c>AddShinyOnScreenKeyboard</c>. It is a live object — change a property at runtime and the
/// host picks it up on its next render.
/// </summary>
public sealed class OnScreenKeyboardOptions
{
    /// <summary>Raise the keyboard when a text input, textarea or contenteditable takes focus.</summary>
    public bool AutoShowOnFocus { get; set; } = true;

    /// <summary>Drop the keyboard when focus leaves the field. Tapping a key never counts as leaving.</summary>
    public bool AutoHideOnBlur { get; set; } = true;

    /// <summary>Keyboard height in CSS pixels.</summary>
    public double HeightPx { get; set; } = 280;

    /// <summary>
    /// True pads the page out from under the keyboard so its tail stays reachable; false lets the
    /// keyboard overlay the content. Either way the focused field is scrolled clear on show.
    /// </summary>
    public bool PushContent { get; set; } = true;

    public OnScreenKeyboardTheme Theme { get; set; } = OnScreenKeyboardTheme.Auto;

    /// <summary>
    /// Enter types a newline in a textarea instead of dispatching an Enter key press. Single-line
    /// inputs always dispatch the key press — a newline is meaningless in them.
    /// </summary>
    public bool EnterInsertsNewLine { get; set; }

    /// <summary>How long a key must be held before it starts repeating.</summary>
    public TimeSpan AutoRepeatDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>The gap between repeats once one has started.</summary>
    public TimeSpan AutoRepeatInterval { get; set; } = TimeSpan.FromMilliseconds(50);
}
