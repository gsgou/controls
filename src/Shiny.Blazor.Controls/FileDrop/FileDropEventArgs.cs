namespace Shiny.Blazor.Controls.FileDrop;

/// <summary>A drag carrying files is over the page, or has just left it.</summary>
public class FileDragEventArgs : EventArgs
{
    internal FileDragEventArgs(IReadOnlyList<DroppedFile> files, int rejectedCount, double x, double y)
    {
        this.Files = files;
        this.RejectedCount = rejectedCount;
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// The acceptable files under the cursor.
    /// </summary>
    /// <remarks>
    /// While the drag is still moving these are placeholders: the browser will not reveal a name or
    /// a size until the drop lands, so only <see cref="DroppedFile.ContentType"/> is filled in and
    /// <see cref="DroppedFile.IsMetadataKnown"/> is false. The count is real, which is enough for
    /// "drop 3 files here".
    /// </remarks>
    public IReadOnlyList<DroppedFile> Files { get; }

    /// <summary>How many files were filtered out by <see cref="FileDropOptions"/>.</summary>
    public int RejectedCount { get; }

    /// <summary>True when at least one file would be accepted.</summary>
    public bool HasAcceptableFiles => this.Files.Count > 0;

    /// <summary>Cursor position in CSS pixels, relative to the viewport.</summary>
    public double X { get; }

    /// <summary>Cursor position in CSS pixels, relative to the viewport.</summary>
    public double Y { get; }
}


/// <summary>Files have been dropped on the page.</summary>
public class FileDropEventArgs : FileDragEventArgs
{
    internal FileDropEventArgs(IReadOnlyList<DroppedFile> files, int rejectedCount, double x, double y)
        : base(files, rejectedCount, x, y)
    {
    }
}
