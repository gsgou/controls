using System.Collections.Concurrent;
using System.Reflection;

namespace Shiny.Maui.Controls.Images;

/// <summary>
/// Reads the image payloads that never touch the network: embedded resources, <c>data:</c> URIs,
/// files on disk, and files shipped inside the app package.
/// </summary>
/// <remarks>
/// These sources have no caching tier of their own and need none - an embedded resource is already
/// in the binary, and a file is already on the device. What they do need is one place that decides
/// what a URI string means, so the control does not grow a chain of guesses.
/// </remarks>
public static class ImageContent
{
    /// <summary>The scheme that addresses an assembly's embedded resources.</summary>
    /// <remarks>
    /// Written <c>resource://MyApp.Assets.logo.svg</c>, or <c>resource://MyLib/MyLib.Assets.logo.svg</c>
    /// to name the assembly outright. Note that the resource name is taken verbatim rather than
    /// parsed as a URI authority: manifest resource names are case-sensitive and a URI host is not.
    /// </remarks>
    public const string ResourceScheme = "resource://";

    // Resolution walks every loaded assembly's manifest in the worst case. Doing that once per
    // distinct URI is fine; doing it per cell in a scrolling list is not.
    static readonly ConcurrentDictionary<string, (Assembly Assembly, string Name)?> resolved = new(StringComparer.Ordinal);


    /// <summary>True for a <c>resource://</c> URI.</summary>
    public static bool IsResource(string? uri)
        => uri is not null && uri.StartsWith(ResourceScheme, StringComparison.OrdinalIgnoreCase);


    /// <summary>True for a <c>data:</c> URI.</summary>
    public static bool IsData(string? uri)
        => uri is not null && uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);


    /// <summary>
    /// Reads an embedded resource.
    /// </summary>
    /// <param name="uri">A <c>resource://</c> URI.</param>
    /// <exception cref="FileNotFoundException">No assembly holds a resource by that name.</exception>
    public static byte[] ReadResource(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        var spec = uri[ResourceScheme.Length..].Trim();
        var found = resolved.GetOrAdd(spec, Locate);

        if (found is not { } hit)
            throw new FileNotFoundException($"No embedded resource matches '{spec}'.", spec);

        using var stream = hit.Assembly.GetManifestResourceStream(hit.Name)
            ?? throw new FileNotFoundException($"Embedded resource '{hit.Name}' could not be opened.", hit.Name);

        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }


    /// <summary>
    /// Decodes a <c>data:</c> URI, base64 or percent-encoded.
    /// </summary>
    /// <exception cref="FormatException">The URI is not a well-formed data URI.</exception>
    public static byte[] ReadData(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        var comma = uri.IndexOf(',');
        if (comma < 0)
            throw new FormatException("A data URI must contain a comma separating its payload.");

        var header = uri[..comma];
        var payload = uri[(comma + 1)..];

        if (header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
            return Convert.FromBase64String(payload);

        // The plain form is percent-encoded text, which is how inline SVG is usually written - the
        // markup stays readable in the markup that carries it.
        return System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
    }


    /// <summary>
    /// Reads a file, from the filesystem if it is there and from the app package if it is not.
    /// </summary>
    /// <remarks>
    /// Both are ordinary for the same string. A path handed over by a picker or written by the app
    /// is on disk; a name like <c>art/logo.svg</c> is a file bundled as <c>MauiAsset</c>, which on
    /// Android lives inside the APK and has no filesystem path at all.
    /// </remarks>
    /// <exception cref="FileNotFoundException">Neither location has the file.</exception>
    public static async Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(path).ConfigureAwait(false);
            using var memory = new MemoryStream();

            await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            return memory.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileNotFoundException($"'{path}' is neither a file on disk nor a bundled app package file.", path, ex);
        }
    }


    static (Assembly Assembly, string Name)? Locate(string spec)
    {
        var slash = spec.IndexOf('/');

        if (slash > 0)
        {
            var assemblyName = spec[..slash];
            var resourceName = spec[(slash + 1)..];

            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                return Match(assembly, resourceName) is { } exact ? (assembly, exact) : null;
            }
            catch (Exception)
            {
                // A named assembly that is not loadable is a typo in the URI, not a crash.
                return null;
            }
        }

        foreach (var assembly in Candidates())
        {
            if (Match(assembly, spec) is { } name)
                return (assembly, name);
        }

        return null;
    }


    // The app's own assembly first: it is where the artwork almost always lives, and checking it
    // before the whole AppDomain keeps the common case off the long path.
    static IEnumerable<Assembly> Candidates()
    {
        var seen = new HashSet<Assembly>();

        if (Application.Current?.GetType().Assembly is { } app && seen.Add(app))
            yield return app;

        if (Assembly.GetEntryAssembly() is { } entry && seen.Add(entry))
            yield return entry;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && seen.Add(assembly))
                yield return assembly;
        }
    }


    static string? Match(Assembly assembly, string spec)
    {
        string[] names;
        try
        {
            names = assembly.GetManifestResourceNames();
        }
        catch (Exception)
        {
            // Reflection-only and some dynamically produced assemblies refuse the question.
            return null;
        }

        foreach (var name in names)
        {
            if (name.Equals(spec, StringComparison.Ordinal))
                return name;
        }

        // The default resource name is the root namespace plus the folder path, which nobody wants
        // to write out - so "Assets.logo.svg" finds "MyApp.Assets.logo.svg". The dot matters: it
        // stops "logo.svg" matching "MyApp.Assets.other_logo.svg".
        var suffix = "." + spec;

        foreach (var name in names)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name;
        }

        foreach (var name in names)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return null;
    }
}
