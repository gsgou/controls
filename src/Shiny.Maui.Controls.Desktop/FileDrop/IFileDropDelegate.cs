namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// The DI-resolved handler for drops. Register one with <c>UseFileDrop&lt;T&gt;()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The alternative is <see cref="IFileDropService.Dropped"/>, and the two do the same job from
/// different places. Use the event when the handling belongs to a page — you get its lifetime and
/// its view model for free. Use this when the handling belongs to the <em>app</em>: a drop that
/// should import a file no matter which page is showing, that has to work while the window is in
/// the background, or that wants constructor-injected services rather than whatever the current
/// page happened to capture.
/// </para>
/// <para>
/// Registered as a singleton, so it may be called from any thread and it must not assume the UI is
/// on screen. Both routes fire for the same drop; the delegate runs first, and it can mark the drop
/// consumed by setting <see cref="FileDropContext.Handled"/>.
/// </para>
/// </remarks>
public interface IFileDropDelegate
{
    /// <summary>Called once per drop, before <see cref="IFileDropService.Dropped"/> is raised.</summary>
    Task OnFilesDropped(FileDropContext context);
}


/// <summary>What an <see cref="IFileDropDelegate"/> is given, and what it can say back.</summary>
public class FileDropContext(FileDropEventArgs args)
{
    /// <summary>The drop.</summary>
    public FileDropEventArgs Args { get; } = args;

    /// <summary>The accepted files — shorthand for <c>Args.Files</c>.</summary>
    public IReadOnlyList<DroppedFile> Files => this.Args.Files;

    /// <summary>
    /// Set to true to consume the drop, which stops <see cref="IFileDropService.Dropped"/> from
    /// being raised for it.
    /// </summary>
    public bool Handled { get; set; }
}
