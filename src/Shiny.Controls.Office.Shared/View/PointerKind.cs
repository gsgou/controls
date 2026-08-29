namespace Shiny.Controls.Office.View;

/// <summary>
/// What is driving a pointer event.
/// </summary>
/// <remarks>
/// <para>
/// The surfaces read very differently under a finger and under a mouse, and the difference is not
/// cosmetic. With a mouse, dragging across the grid extends the selection and the wheel scrolls, which
/// is what every desktop spreadsheet does. A finger has no wheel, so if dragging also meant "extend
/// the selection" there would be no gesture left to scroll with — which is exactly how the two
/// editable surfaces ended up unpannable on a phone.
/// </para>
/// <para>
/// So touch takes the mobile convention instead: a tap selects or places the caret, a drag pans, and
/// extending a selection is done by dragging a handle drawn on it. Nothing about the mouse behaviour
/// changes, which is why this has to be carried on the event rather than decided per platform — a
/// Windows tablet and an iPad with a trackpad both see the two kinds in the same session.
/// </para>
/// </remarks>
public enum PointerKind
{
    Mouse,
    Touch,
    Pen
}
