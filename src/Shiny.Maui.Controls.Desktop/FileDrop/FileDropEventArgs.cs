using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// A drag is hovering over the window, or has just left it.
/// </summary>
public class FileDragEventArgs : EventArgs
{
    internal FileDragEventArgs(IReadOnlyList<DroppedFile> files, int rejectedCount, Point position, Window? window)
    {
        this.Files = files;
        this.RejectedCount = rejectedCount;
        this.Position = position;
        this.Window = window;
    }

    /// <summary>
    /// The acceptable files under the cursor.
    /// </summary>
    /// <remarks>
    /// Empty on platforms that do not name the payload until the drop lands, and empty when nothing
    /// in the drag passes <see cref="FileDropOptions"/>. Either way <see cref="RejectedCount"/> and
    /// <see cref="HasAcceptableFiles"/> are what an overlay should bind to — a count of zero with a
    /// non-zero rejected count is a drag the app is about to refuse.
    /// </remarks>
    public IReadOnlyList<DroppedFile> Files { get; }

    /// <summary>How many files in the drag were filtered out by <see cref="FileDropOptions"/>.</summary>
    public int RejectedCount { get; }

    /// <summary>True when at least one file in the drag would be accepted.</summary>
    public bool HasAcceptableFiles => this.Files.Count > 0;

    /// <summary>Cursor position in device-independent units, relative to the window's top-left.</summary>
    public Point Position { get; }

    /// <summary>The window under the drag. Null when the drop target could not be traced back to one.</summary>
    public Window? Window { get; }
}


/// <summary>
/// Files have been dropped on the window.
/// </summary>
public class FileDropEventArgs : FileDragEventArgs
{
    internal FileDropEventArgs(IReadOnlyList<DroppedFile> files, int rejectedCount, Point position, Window? window)
        : base(files, rejectedCount, position, window)
    {
    }
}
