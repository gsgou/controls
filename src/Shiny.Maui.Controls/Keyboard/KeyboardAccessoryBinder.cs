namespace Shiny.Maui.Controls;

/// <summary>
/// Implemented by a control that can carry a <see cref="KeyboardAccessoryView"/> — currently
/// <see cref="TextEntry"/> and <see cref="Cells.EntryCell"/>.
///
/// <para>
/// It exists because the two differ in exactly the two ways the bar cares about: which element the
/// field navigator treats as "this field" (a <c>TextEntry</c> is collected as its wrapper, an
/// <c>EntryCell</c> as the input inside it), and how the keyboard is put away (both compose an inner
/// input, so the outer control's <see cref="VisualElement.Unfocus"/> would target the wrong thing).
/// </para>
/// </summary>
public interface IKeyboardAccessoryHost
{
    /// <summary>The element <see cref="KeyboardFieldNavigator"/> treats as the current field.</summary>
    VisualElement NavigationElement { get; }

    /// <summary>Dismiss the keyboard for this field.</summary>
    void DismissKeyboard();
}

/// <summary>
/// Owns the platform lifetime of one accessory bar against one native text input: assigning it,
/// showing and hiding it with focus, and tearing it down when the host goes away.
///
/// <para>
/// The bar lives outside the host's visual tree — a UIKit <c>InputAccessoryView</c> on iOS, a view in
/// the activity's content frame on Android — so none of this is something MAUI would do for us.
/// </para>
/// </summary>
partial class KeyboardAccessoryBinder
{
    readonly IKeyboardAccessoryHost host;
    readonly Entry input;
    KeyboardAccessoryView? bar;

    public KeyboardAccessoryBinder(IKeyboardAccessoryHost host, Entry input, VisualElement lifetime)
    {
        this.host = host;
        this.input = input;

        input.Focused += (_, _) => OnFocusChanged(true);
        input.Unfocused += (_, _) => OnFocusChanged(false);

        // The bar is realized from the native input, which does not exist until the handler does - so
        // a bar set in XAML has to be re-applied once the handler arrives.
        input.HandlerChanged += (_, _) => Apply();

        // Nothing else would tear the bar down, because it is not in the tree being unloaded.
        lifetime.Unloaded += (_, _) => OnFocusChanged(false);
    }

    public KeyboardAccessoryView? Bar => bar;

    public void SetBar(KeyboardAccessoryView? value)
    {
        if (!ReferenceEquals(bar, value))
        {
            bar?.DetachHost(host);
            bar = value;
        }

        Apply();
    }

    void Apply() => ApplyPlatform(bar);

    void OnFocusChanged(bool focused)
    {
        bar?.NotifyFocusChanged(host, focused);
        OnFocusChangedPlatform(focused);
    }

    // Implemented per platform. No implementation on the heads with no soft keyboard, which is the
    // no-op - the properties still compile and simply do nothing there.
    partial void ApplyPlatform(KeyboardAccessoryView? bar);
    partial void OnFocusChangedPlatform(bool focused);
}
