namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Puts the keyboard into an editor's hidden entry.
/// </summary>
/// <remarks>
/// Every Office editor takes typing through a real <see cref="Entry"/> parked at the caret, so the
/// platform's own soft keyboard and IME work without a custom text stack. Getting the keyboard there
/// is one call — except on the macOS AppKit head, where <see cref="VisualElement.Focus"/> does not
/// take: the entry never becomes first responder, so every keystroke goes to whatever did, and the
/// editors could be clicked, scrolled and selected in but not typed into. Verified against a plain,
/// fully visible <c>Entry</c> elsewhere in the sample, so it is the head rather than the hidden entry.
/// </remarks>
static class PlatformFocus
{
    public static void FocusForEditing(this View view)
    {
        view.Focus();

#if MACOS
        // MAUI's IsFocused is still driven by the call above; this is only what actually moves the
        // responder. A view with no window yet is simply not focusable, which the caller handles by
        // focusing again on the next press.
        if (view.Handler?.PlatformView is AppKit.NSView native && native.Window is { } window)
        {
            // The entry is meant to be invisible - it exists to catch keystrokes and to give the IME
            // somewhere to anchor. AppKit draws a focus ring around whatever is first responder, which
            // would put a blue rounded rectangle over the document at the caret.
            if (native is AppKit.NSControl control)
                control.FocusRingType = AppKit.NSFocusRingType.None;

            window.MakeFirstResponder(native);
        }
#endif
    }
}
