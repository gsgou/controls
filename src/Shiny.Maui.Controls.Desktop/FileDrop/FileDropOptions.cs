namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// App-wide settings for <see cref="IFileDropService"/>, set from <c>UseFileDrop</c>.
/// </summary>
public class FileDropOptions
{
    /// <summary>
    /// Extensions to accept, with or without the leading dot (<c>"pdf"</c> and <c>".pdf"</c> both
    /// work). Empty — the default — accepts everything.
    /// </summary>
    /// <remarks>
    /// Filtering here rather than in the app is what lets the drag feedback be honest: a drag
    /// carrying nothing acceptable reports no files on <see cref="IFileDropService.DragEnter"/>, so
    /// an overlay bound to that shows "not this one" before the user lets go.
    /// </remarks>
    public IList<string> AllowedExtensions { get; } = new List<string>();

    /// <summary>Largest file to accept, in bytes. <c>0</c> (the default) means no limit.</summary>
    public long MaxFileSize { get; set; }

    /// <summary>Most files to accept from one drop. <c>0</c> (the default) means no limit.</summary>
    public int MaxFiles { get; set; }

    /// <summary>Accept dropped folders as well as files. Off by default.</summary>
    public bool AllowDirectories { get; set; }

    /// <summary>
    /// Stop hosted web content from consuming the drop first, so a drop anywhere in the window
    /// reaches your code. On by default, and the reason this works over a <c>BlazorWebView</c>.
    /// </summary>
    /// <remarks>
    /// A <c>WebView2</c> / <c>WKWebView</c> / <c>WebKitWebView</c> is its own drop target and wins
    /// over anything behind it — usually by navigating away to the dropped file, which looks exactly
    /// like the app crashing. Turn this off only if the web content has its own drop handling that
    /// you want to keep.
    /// </remarks>
    public bool SuppressWebViewDrop { get; set; } = true;

    /// <summary>
    /// Attach to every application window as it opens. On by default; turn it off to drive
    /// <see cref="IFileDropService.AttachTo"/> yourself.
    /// </summary>
    public bool AutoAttachWindows { get; set; } = true;

    internal bool Accepts(DroppedFile file)
    {
        if (file.IsDirectory && !this.AllowDirectories)
            return false;

        if (this.MaxFileSize > 0 && file.Length > this.MaxFileSize)
            return false;

        if (this.AllowedExtensions.Count == 0)
            return true;

        var extension = file.Extension;
        foreach (var allowed in this.AllowedExtensions)
        {
            var normalized = allowed.StartsWith('.') ? allowed : "." + allowed;
            if (String.Equals(normalized, extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
