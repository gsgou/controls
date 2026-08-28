namespace Shiny.Blazor.Controls.FileDrop;

/// <summary>
/// Page-level file drop: files dragged from the desktop onto anywhere in the browser window.
/// </summary>
/// <remarks>
/// <para>
/// The listeners are attached to <c>window</c> in the capture phase, so a drop is caught wherever it
/// lands and before any component in the page can consume it — the browser's equivalent of MAUI's
/// "over top of any web view". It also means the browser's default action for a dropped file,
/// navigating away to it and unloading the app, never happens.
/// </para>
/// <para>
/// Works on WebAssembly, Server and Hybrid alike; nothing here needs a server round trip beyond the
/// events themselves. In a MAUI Blazor Hybrid app, prefer the native
/// <c>Shiny.Maui.Controls.Desktop</c> service instead — it reports real file paths, where the
/// browser can only give you bytes.
/// </para>
/// </remarks>
public interface IFileDropService : IAsyncDisposable
{
    /// <summary>Always true in a browser. Present so shared code reads the same as it does on MAUI.</summary>
    bool IsSupported { get; }

    /// <summary>Whether <see cref="StartAsync"/> has run and the listeners are attached.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Turns dropping on and off without detaching. The browser still refuses to navigate to a
    /// dropped file, but nothing is reported. True by default.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>The settings this service was built with. Mutating them takes effect on the next drag.</summary>
    FileDropOptions Options { get; }

    /// <summary>A drag carrying files has entered the page.</summary>
    event EventHandler<FileDragEventArgs>? DragEnter;

    /// <summary>The drag has moved. Fires often.</summary>
    event EventHandler<FileDragEventArgs>? DragOver;

    /// <summary>The drag has left the page without dropping.</summary>
    event EventHandler<FileDragEventArgs>? DragLeave;

    /// <summary>Files were dropped.</summary>
    event EventHandler<FileDropEventArgs>? Dropped;

    /// <summary>
    /// Attaches the browser listeners. Must be called after the first render — it imports a JS
    /// module, which prerendering cannot do.
    /// </summary>
    /// <remarks>
    /// Placing <c>&lt;FileDropHost /&gt;</c> in your layout does this for you, which is the usual
    /// way in. Calling it twice is harmless.
    /// </remarks>
    Task StartAsync();

    /// <summary>Detaches the browser listeners and forgets any files still held.</summary>
    Task StopAsync();

    /// <summary>
    /// Releases the browser's reference to files from a drop. Only needed when
    /// <see cref="FileDropOptions.ReleaseFilesAfterHandling"/> is off.
    /// </summary>
    Task ReleaseAsync(IEnumerable<DroppedFile> files);
}
