using System.IO.Compression;
using System.Security.Cryptography;

namespace Shiny.Controls.Office.Tests;

public sealed record PackageDiff(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Identical)
{
    public bool IsIdentical => this.Added.Count == 0 && this.Removed.Count == 0 && this.Changed.Count == 0;

    public override string ToString()
        => $"added=[{string.Join(", ", this.Added)}] removed=[{string.Join(", ", this.Removed)}] changed=[{string.Join(", ", this.Changed)}]";
}

/// <summary>
/// Compares two OPC packages entry by entry.
/// </summary>
/// <remarks>
/// This is the test that makes the surgical-edit rule enforceable rather than aspirational. Anything
/// the editor did not deliberately rewrite must come back byte-for-byte, because that is the only
/// evidence that features the editor does not model — macros, custom XML, pivot caches — survived.
/// </remarks>
public static class PackageComparer
{
    public static PackageDiff Compare(byte[] before, byte[] after)
    {
        var left = Hashes(before);
        var right = Hashes(after);

        var added = right.Keys.Where(k => !left.ContainsKey(k)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var removed = left.Keys.Where(k => !right.ContainsKey(k)).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var changed = new List<string>();
        var identical = new List<string>();
        foreach (var (name, hash) in left)
        {
            if (!right.TryGetValue(name, out var other))
                continue;

            if (hash.SequenceEqual(other))
                identical.Add(name);
            else
                changed.Add(name);
        }

        changed.Sort(StringComparer.Ordinal);
        identical.Sort(StringComparer.Ordinal);
        return new PackageDiff(added, removed, changed, identical);
    }

    /// <summary>
    /// Hashes the decompressed content of each entry. Compression level and zip metadata are ignored on
    /// purpose — rewriting the archive with different deflate settings is not a change to the document.
    /// </summary>
    static Dictionary<string, byte[]> Hashes(byte[] package)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using var stream = new MemoryStream(package, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            using var content = entry.Open();
            using var copy = new MemoryStream();
            content.CopyTo(copy);
            result[entry.FullName] = SHA256.HashData(copy.ToArray());
        }

        return result;
    }

    public static IReadOnlyList<string> EntryNames(byte[] package)
    {
        using var stream = new MemoryStream(package, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        return archive.Entries.Select(x => x.FullName).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    public static byte[] ReadEntry(byte[] package, string entryName)
    {
        using var stream = new MemoryStream(package, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException($"No entry '{entryName}'.");
        using var content = entry.Open();
        using var copy = new MemoryStream();
        content.CopyTo(copy);
        return copy.ToArray();
    }
}
