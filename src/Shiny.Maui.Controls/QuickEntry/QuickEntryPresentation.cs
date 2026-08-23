namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// How the quick entry popup is put on screen.
/// </summary>
public enum QuickEntryPresentation
{
    /// <summary>
    /// A native desktop window where one is available, the in-app overlay everywhere else. The
    /// default, so one setting works across a shared codebase with no platform checks in it.
    /// </summary>
    Auto,

    /// <summary>
    /// An overlay drawn over the current page. Works on every platform, and is the only option on
    /// iOS, Android and Blazor — which is also why it is worth choosing deliberately on desktop, for
    /// a popup that should stay inside the app rather than float over the whole machine.
    /// </summary>
    InApp,

    /// <summary>
    /// A borderless, always-on-top OS window that opens over other applications. Needs the
    /// <c>Shiny.Maui.Controls.Desktop</c> add-on and a desktop platform; falls back to
    /// <see cref="InApp"/> with a logged warning anywhere else, rather than failing to open at all.
    /// </summary>
    Desktop
}
