namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// The navigation keys the popup host intercepts natively and forwards to its content.
/// MAUI has no cross-platform key-down event, so the host reads them from the native window and
/// routes them through <see cref="IQuickEntryKeyHandler"/>.
/// </summary>
public enum QuickEntryKey
{
    Escape,
    Enter,
    ArrowUp,
    ArrowDown,
    Tab
}

/// <summary>
/// Implemented by popup content that wants keyboard navigation. The host offers each key to the
/// content first; returning true marks it handled and stops the host acting on it — which is how
/// <see cref="PromptView"/> makes the first Escape clear the prompt and the second close the
/// window.
/// </summary>
public interface IQuickEntryKeyHandler
{
    /// <summary>Handle a navigation key. Return true to swallow it.</summary>
    bool HandleKey(QuickEntryKey key);
}

/// <summary>
/// Implemented by popup content that knows its own height, so the window can be sized to it exactly.
/// </summary>
/// <remarks>
/// <para>
/// The host would rather not need this. But the content lives inside the very window whose size it
/// determines, and neither of the obvious signals survives that: a <c>ContentView</c> stretches to
/// whatever space the layout offers it, so its arranged height is the window's, not its content's;
/// and <c>Measure</c> keeps handing back a desired size cached from an earlier pass, so the window
/// gets stuck at whatever it was the first time. A view that knows which of its children is the
/// real content can simply say so.
/// </para>
/// <para>
/// Content that does not implement this is measured instead — which works for anything with a
/// fixed height and is unreliable for anything that grows, so implement it if your popup content
/// changes size.
/// </para>
/// </remarks>
public interface IQuickEntryAutoSize
{
    /// <summary>The height the content wants at <paramref name="width"/>, in device-independent pixels.</summary>
    double GetDesiredHeight(double width);

    /// <summary>Raised whenever that height may have changed.</summary>
    event EventHandler? DesiredHeightChanged;
}
