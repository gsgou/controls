namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// Window-level file drop: files dragged from Finder / Explorer / Files onto any part of the app.
/// </summary>
/// <remarks>
/// <para>
/// This is not <c>DropGestureRecognizer</c>. That one is per-view, is unimplemented on the AppKit
/// and GTK4 heads and broken on Mac Catalyst, and — the reason this type exists — it is behind any
/// hosted web content, so an app whose UI is a <c>BlazorWebView</c> never sees the drop at all. This
/// attaches to the <em>native window</em> and, with
/// <see cref="FileDropOptions.SuppressWebViewDrop"/>, stops the web view from claiming the drag
/// first, so a drop anywhere in the window arrives here.
/// </para>
/// <para>
/// Supported on Windows, macOS (both the AppKit head and Mac Catalyst) and Linux/GTK4. Everywhere
/// else <see cref="IsSupported"/> is false, attaching is a no-op and the events never fire — the
/// service is still resolvable, so shared code needs no <c>#if</c>.
/// </para>
/// </remarks>
public interface IFileDropService
{
    /// <summary>Whether this platform has window-level file drop at all.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Turns dropping on and off without detaching. Drags are still watched, but nothing is
    /// reported and the OS shows the "no drop" cursor. True by default.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>The settings this service was built with. Mutating them takes effect on the next drag.</summary>
    FileDropOptions Options { get; }

    /// <summary>A drag carrying files has entered the window.</summary>
    event EventHandler<FileDragEventArgs>? DragEnter;

    /// <summary>The drag has moved. Fires often — bind an overlay's position to it, not a layout pass.</summary>
    event EventHandler<FileDragEventArgs>? DragOver;

    /// <summary>The drag has left the window without dropping.</summary>
    event EventHandler<FileDragEventArgs>? DragLeave;

    /// <summary>Files were dropped.</summary>
    event EventHandler<FileDropEventArgs>? Dropped;

    /// <summary>
    /// Starts watching <paramref name="window"/>. Dispose the result to stop. Calling it twice for
    /// the same window returns the existing attachment.
    /// </summary>
    /// <remarks>
    /// Only needed when <see cref="FileDropOptions.AutoAttachWindows"/> is off, or for a window
    /// created after startup that you want attached immediately rather than on its first page.
    /// </remarks>
    IDisposable AttachTo(Window window);
}
