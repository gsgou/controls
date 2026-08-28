namespace Shiny.Blazor.Controls.FileDrop;

/// <summary>
/// One file from a browser drop.
/// </summary>
/// <remarks>
/// <para>
/// There is no path — the browser will not give one — so the content is always read as a stream.
/// That is the one real difference from the MAUI side of this feature, and it is a hard limit of
/// the platform rather than a design choice.
/// </para>
/// <para>
/// The same type is also used for the placeholder entries reported while a drag is still moving.
/// The DataTransfer API deliberately hides names and sizes until the drop lands, so those instances
/// have a <see cref="ContentType"/> and nothing else — <see cref="IsMetadataKnown"/> says which kind
/// you are holding, and reading one throws.
/// </para>
/// </remarks>
public sealed class DroppedFile
{
    readonly Func<long, CancellationToken, Task<Stream>> open;

    internal DroppedFile(
        string key,
        string fileName,
        long length,
        string contentType,
        DateTimeOffset lastModified,
        Func<long, CancellationToken, Task<Stream>> open
    )
    {
        this.Key = key;
        this.FileName = fileName;
        this.Length = length;
        this.ContentType = contentType;
        this.LastModified = lastModified;
        this.open = open;
    }

    /// <summary>Identifies the browser-side File this stands for.</summary>
    internal string Key { get; }

    /// <summary>The file name including its extension. Empty while a drag is still in progress.</summary>
    public string FileName { get; }

    /// <summary>Size in bytes, or <c>-1</c> while a drag is still in progress.</summary>
    public long Length { get; }

    /// <summary>The browser's MIME type. Empty when the browser could not work one out.</summary>
    public string ContentType { get; }

    /// <summary>When the file was last written, as reported by the browser.</summary>
    public DateTimeOffset LastModified { get; }

    /// <summary>
    /// False for the placeholder entries reported during a drag, where only
    /// <see cref="ContentType"/> is known and the file cannot be read yet.
    /// </summary>
    public bool IsMetadataKnown => this.Length >= 0;

    /// <summary>The extension including the leading dot, lower-cased. Empty when there is none.</summary>
    public string Extension => Path.GetExtension(this.FileName).ToLowerInvariant();

    /// <summary>
    /// Opens the content for reading. The caller owns the stream.
    /// </summary>
    /// <param name="maxAllowedSize">
    /// A ceiling on what will be read, defaulting to 32MB. Blazor requires one — the stream is
    /// coming from the browser and its length is not something the server can trust.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task<Stream> OpenReadAsync(long maxAllowedSize = 32 * 1024 * 1024, CancellationToken cancellationToken = default)
        => this.open(maxAllowedSize, cancellationToken);

    /// <summary>Reads the whole file into memory.</summary>
    public async Task<byte[]> ReadAllBytesAsync(long maxAllowedSize = 32 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        await using var stream = await this.OpenReadAsync(maxAllowedSize, cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    public override string ToString() => this.FileName;
}
