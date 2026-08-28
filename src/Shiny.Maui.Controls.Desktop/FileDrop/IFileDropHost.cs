using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// What the per-platform drop targets call back into. Implemented by <see cref="FileDropService"/>.
/// </summary>
/// <remarks>
/// The platforms report raw drags; every policy decision — filtering, enablement, which event to
/// raise, what thread to raise it on — lives on the other side of this interface so it is written
/// once rather than four times.
/// </remarks>
interface IFileDropHost
{
    FileDropOptions Options { get; }

    /// <summary>
    /// Whether a drag carrying these files should be accepted. Platforms call this while the drag is
    /// still moving, to choose between the copy cursor and the "no drop" cursor.
    /// </summary>
    bool WouldAccept(IReadOnlyList<DroppedFile> files);

    void NotifyEnter(Window window, IReadOnlyList<DroppedFile> files, Point position);
    void NotifyOver(Window window, IReadOnlyList<DroppedFile> files, Point position);
    void NotifyLeave(Window window);
    void NotifyDrop(Window window, IReadOnlyList<DroppedFile> files, Point position);

    void LogError(string message, Exception? ex = null);
    void LogDebug(string message);
}
