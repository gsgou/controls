using Shiny.Controls.Office.Packaging;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Pulls image files out of a <see cref="DropGestureRecognizer"/> drop.
/// </summary>
/// <remarks>
/// <para>
/// MAUI's <see cref="DropEventArgs"/> carries a <see cref="DataPackageView"/> that knows about text
/// and URIs and nothing else — a dropped <em>file</em> reaches managed code only through
/// <c>PlatformArgs</c>, and differently on every platform. So this is the one place in the package
/// that is written per-platform for a reason other than the spell checker.
/// </para>
/// <para>
/// Guarded with <c>#if</c> in a single file rather than split across <c>Platforms/</c>, because that
/// folder's include rules in this project exist to give Apple one shared head for iOS and Mac
/// Catalyst; adding a second, differently-shaped set of platform files there would make both harder
/// to follow for the sake of four short methods.
/// </para>
/// <para>
/// Where a platform has no file drag at all the answer is an empty list, not an exception: the drop
/// gesture is still attached, and a host that also offers the toolbar's picture button loses nothing.
/// </para>
/// </remarks>
static class OfficeFileDrop
{
    /// <summary>
    /// The largest picture a drop will read.
    /// </summary>
    /// <remarks>
    /// Matches the browser side's ceiling so the two hosts behave the same. Unlike the browser this
    /// could stream straight from disk into the package, but a document that quietly grows by 200MB
    /// because someone dragged a RAW file onto it is not a better outcome than being told no.
    /// </remarks>
    public const long MaxBytes = 32 * 1024 * 1024;

    /// <summary>Reads every image in a drop. Empty when there is nothing usable in it.</summary>
    public static async Task<IReadOnlyList<OfficePickedImage>> ReadImagesAsync(DropEventArgs e)
    {
        var results = new List<OfficePickedImage>();

        foreach (var path in await PathsAsync(e).ConfigureAwait(false))
        {
            if (ImageContentTypes.Resolve(path) is not { } contentType)
                continue;

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0 || info.Length > MaxBytes)
                    continue;

                results.Add(new OfficePickedImage(
                    Path.GetFileName(path),
                    contentType,
                    await File.ReadAllBytesAsync(path).ConfigureAwait(false)));
            }
            catch (Exception)
            {
                // A path the sandbox will not open is a file the user cannot share with this app.
                // Skipping it leaves the rest of the drop working.
            }
        }

        return results;
    }

#if WINDOWS

    /// <summary>
    /// WinUI hands over real <c>StorageFile</c>s, so the paths are simply on them.
    /// </summary>
    /// <remarks>
    /// The only branch that needs a deferral, which is why it is taken here rather than by the caller:
    /// the other platforms hand over payloads that outlive the event on their own.
    /// </remarks>
    static async Task<IReadOnlyList<string>> PathsAsync(DropEventArgs e)
    {
        var args = e.PlatformArgs?.DragEventArgs;
        var view = args?.DataView;

        if (args is null || view is null ||
            !view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            return [];

        // WinUI releases the data view the moment the Drop handler returns, and this is about to
        // await. The deferral is what holds it open until the items have actually been read.
        var deferral = args.GetDeferral();

        try
        {
            var items = await view.GetStorageItemsAsync();

            return items
                .OfType<Windows.Storage.StorageFile>()
                .Select(x => x.Path)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }
        finally
        {
            deferral.Complete();
        }
    }

#elif IOS || MACCATALYST

    /// <summary>
    /// UIKit gives item providers rather than paths, so each one is copied out to a temporary file.
    /// </summary>
    /// <remarks>
    /// <c>LoadFileRepresentation</c> hands back a URL that is deleted the moment the callback returns,
    /// which is why the bytes are copied rather than the URL kept. The copy lands in the app's temp
    /// directory and is left there for the OS to reclaim — deleting it here would race the read that
    /// follows.
    /// </remarks>
    static async Task<IReadOnlyList<string>> PathsAsync(DropEventArgs e)
    {
        var session = e.PlatformArgs?.DropSession;
        if (session is null)
            return [];

        var paths = new List<string>();

        foreach (var item in session.Items)
        {
            var provider = item.ItemProvider;
            if (provider is null)
                continue;

            var source = new TaskCompletionSource<string?>();

            provider.LoadFileRepresentation(
                UniformTypeIdentifiers.UTTypes.Image.Identifier,
                (url, error) =>
                {
                    if (error is not null || url?.Path is not { Length: > 0 } from)
                    {
                        source.TrySetResult(null);
                        return;
                    }

                    try
                    {
                        // Named after the original so the picture keeps a recognisable name, but in a
                        // unique folder so two drops of "screenshot.png" cannot collide.
                        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(folder);

                        var to = Path.Combine(folder, Path.GetFileName(from));
                        File.Copy(from, to, overwrite: true);
                        source.TrySetResult(to);
                    }
                    catch (Exception)
                    {
                        source.TrySetResult(null);
                    }
                });

            if (await source.Task.ConfigureAwait(false) is { } path)
                paths.Add(path);
        }

        return paths;
    }

#else

    /// <summary>
    /// Android and the AppKit head, neither of which surfaces a file drop.
    /// </summary>
    /// <remarks>
    /// Android's drag-and-drop is within an app rather than from a file manager, and the GTK/AppKit
    /// heads have no <c>DropGestureRecognizer</c> implementation behind them at all. Both get the
    /// toolbar's picture button, which is the gesture that exists on those platforms anyway.
    /// </remarks>
    static Task<IReadOnlyList<string>> PathsAsync(DropEventArgs e)
    {
        _ = e;
        return Task.FromResult<IReadOnlyList<string>>([]);
    }

#endif
}
