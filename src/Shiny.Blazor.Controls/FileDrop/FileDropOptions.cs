namespace Shiny.Blazor.Controls.FileDrop;

/// <summary>
/// Settings for <see cref="IFileDropService"/>. Deliberately shaped like MAUI's
/// <c>FileDropOptions</c> so the two hosts read the same.
/// </summary>
public class FileDropOptions
{
    /// <summary>
    /// Extensions to accept, with or without the leading dot. Empty — the default — accepts
    /// everything.
    /// </summary>
    public IList<string> AllowedExtensions { get; } = new List<string>();

    /// <summary>Largest file to accept, in bytes. <c>0</c> (the default) means no limit.</summary>
    public long MaxFileSize { get; set; }

    /// <summary>Most files to accept from one drop. <c>0</c> (the default) means no limit.</summary>
    public int MaxFiles { get; set; }

    /// <summary>
    /// Drop the browser's reference to a drop's files once <see cref="IFileDropService.Dropped"/>
    /// has returned. On by default.
    /// </summary>
    /// <remarks>
    /// The files live in JS memory until released, so a page that takes several large drops and
    /// never lets go grows without bound. Turn this off only if you intend to read a file after the
    /// event handler has finished — and then release it yourself with
    /// <see cref="IFileDropService.ReleaseAsync"/>.
    /// </remarks>
    public bool ReleaseFilesAfterHandling { get; set; } = true;

    internal bool Accepts(DroppedFile file)
    {
        // A hover placeholder cannot be judged on name or size — the browser has not told us either
        // yet. Refusing it would mean showing "no drop" for every drag; the real check happens when
        // the drop lands.
        if (!file.IsMetadataKnown)
            return true;

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
