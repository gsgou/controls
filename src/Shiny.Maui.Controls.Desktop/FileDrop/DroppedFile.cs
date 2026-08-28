namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// One file from a drop.
/// </summary>
/// <remarks>
/// <para>
/// On every desktop platform a dropped file is a real file on disk and <see cref="FullPath"/> is
/// set, so the fast path is to keep the path and never copy anything. <see cref="OpenReadAsync"/>
/// exists because Mac Catalyst hands over an <c>NSItemProvider</c> rather than a path — there the
/// bytes are staged into a temp file first — and because a browser drop (Blazor) has no path at
/// all. Code written against the stream works on all of them; code written against the path is
/// desktop-only, which is usually fine and is why the path is still exposed.
/// </para>
/// </remarks>
public sealed class DroppedFile
{
    readonly Func<CancellationToken, Task<Stream>> open;

    internal DroppedFile(string fileName, string? fullPath, long length, Func<CancellationToken, Task<Stream>> open)
    {
        this.FileName = fileName;
        this.FullPath = fullPath;
        this.Length = length;
        this.open = open;
    }

    /// <summary>The file name including its extension — never a path.</summary>
    public string FileName { get; }

    /// <summary>
    /// The absolute path on disk, or <see langword="null"/> when the platform only handed over
    /// content (a browser drop).
    /// </summary>
    public string? FullPath { get; }

    /// <summary>Size in bytes. <c>-1</c> when the platform could not report one without reading the file.</summary>
    public long Length { get; }

    /// <summary>The extension including the leading dot, lower-cased. Empty when there is none.</summary>
    public string Extension => Path.GetExtension(this.FileName).ToLowerInvariant();

    /// <summary>A best-effort MIME type derived from <see cref="Extension"/>.</summary>
    public string ContentType => FileDropContentTypes.Resolve(this.Extension);

    /// <summary>True when the drop was a folder rather than a file.</summary>
    public bool IsDirectory { get; internal init; }

    /// <summary>Opens the content for reading. The caller owns the stream.</summary>
    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        => this.open(cancellationToken);

    /// <summary>Reads the whole file into memory.</summary>
    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await this.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Wraps a file that is already on disk. Nothing is copied.</summary>
    public static DroppedFile FromPath(string path)
    {
        var length = -1L;
        var isDirectory = false;

        try
        {
            isDirectory = Directory.Exists(path);
            if (!isDirectory)
            {
                var info = new FileInfo(path);
                if (info.Exists)
                    length = info.Length;
            }
        }
        catch (Exception)
        {
            // A path the sandbox will not stat is still worth surfacing — the app may hold an
            // entitlement this process does not. Length stays -1.
        }

        return new DroppedFile(
            Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)),
            path,
            length,
            ct => Task.FromResult<Stream>(File.OpenRead(path))
        )
        {
            IsDirectory = isDirectory
        };
    }

    public override string ToString() => this.FullPath ?? this.FileName;
}
