using Foundation;

namespace Shiny.Maui.Controls.Media;

/// <summary>Turns a <see cref="MediaSource"/> into the <see cref="NSUrl"/> AVFoundation wants.</summary>
static class AppleMediaSourceResolver
{
    public static NSUrl? Resolve(MediaSource? source) => source switch
    {
        UriMediaSource { Uri: { } uri } => NSUrl.FromString(uri.AbsoluteUri),
        FileMediaSource { Path: { Length: > 0 } path } => NSUrl.FromFilename(path),
        ResourceMediaSource { Path: { Length: > 0 } path } => ResolveBundled(path),
        _ => null
    };

    static NSUrl? ResolveBundled(string path)
    {
        // MAUI copies Resources/Raw into the app bundle keeping any sub-folders, so ask NSBundle first —
        // it's the only lookup that survives the resource actually being placed somewhere else in the
        // bundle (which the macOS head does, since its workload ignores MauiAsset and we emit
        // BundleResource with a flat LogicalName).
        var directory = System.IO.Path.GetDirectoryName(path);
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var extension = System.IO.Path.GetExtension(path).TrimStart('.');

        var resolved = String.IsNullOrEmpty(directory)
            ? NSBundle.MainBundle.PathForResource(name, extension)
            : NSBundle.MainBundle.PathForResource(name, extension, directory);

        resolved ??= NSBundle.MainBundle.PathForResource(name, extension);

        if (resolved is null)
        {
            var combined = System.IO.Path.Combine(NSBundle.MainBundle.BundlePath, path);
            resolved = File.Exists(combined) ? combined : null;
        }

        return resolved is null ? null : NSUrl.FromFilename(resolved);
    }
}
