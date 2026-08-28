using Foundation;
using UIKit;
using UniformTypeIdentifiers;
using WebKit;
using Microsoft.Maui.Graphics;
using MauiWindow = Microsoft.Maui.Controls.Window;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// Mac Catalyst backing for window-level file drop, via a <see cref="UIDropInteraction"/> on the
/// <see cref="UIWindow"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the platform with the weakest guarantees of the four. UIKit gives the drop to the
/// deepest view under the cursor that has an interaction willing to take it, so an interaction on
/// the window only wins once nothing in front of it wants the drag — which is why
/// <see cref="FileDropOptions.SuppressWebViewDrop"/> strips the interactions UIKit installed inside
/// a <see cref="WKWebView"/>. There is no supported opt-out here the way WebView2 has one, so this
/// reaches into the web view's own subviews and is the first thing to turn off if hosted web
/// content starts behaving oddly.
/// </para>
/// <para>
/// The payload is also different in kind: UIKit hands over <see cref="NSItemProvider"/>s, not
/// paths, so a drop is staged into the app's temp directory before
/// <see cref="DroppedFile.FullPath"/> can be set. While the drag is still moving there is nothing to
/// stage, so the hover events report the provider's suggested name only — enough for the extension
/// filter, not enough for a size check.
/// </para>
/// </remarks>
static class FileDropPlatform
{
    public static bool IsSupported => true;

    public static IDisposable? Attach(IFileDropHost host, MauiWindow window, object platformWindow)
        => platformWindow is UIWindow native ? new CatalystDropTarget(host, window, native) : null;
}


sealed class CatalystDropTarget : IDisposable
{
    readonly IFileDropHost host;
    readonly MauiWindow window;
    readonly UIWindow native;
    readonly UIDropInteraction interaction;
    bool disposed;

    public CatalystDropTarget(IFileDropHost host, MauiWindow window, UIWindow native)
    {
        this.host = host;
        this.window = window;
        this.native = native;

        this.interaction = new UIDropInteraction(new DropDelegate(this));
        this.native.AddInteraction(this.interaction);

        this.SuppressWebViews();
        this.window.Activated += this.OnActivated;
    }

    void OnActivated(object? sender, EventArgs e) => this.SuppressWebViews();

    internal void SuppressWebViews()
    {
        if (!this.host.Options.SuppressWebViewDrop)
            return;

        try
        {
            StripWebViewDrops(this.native);
        }
        catch (Exception ex)
        {
            this.host.LogError("Could not strip the web view's own drop interactions", ex);
        }
    }

    static void StripWebViewDrops(UIView root)
    {
        foreach (var view in root.Subviews)
        {
            if (view is WKWebView web)
                StripDropInteractions(web);
            else
                StripWebViewDrops(view);
        }
    }

    static void StripDropInteractions(UIView view)
    {
        // The interaction is not on the WKWebView itself but on the private content view inside it,
        // so the whole subtree is walked rather than just the top.
        foreach (var existing in view.Interactions)
        {
            if (existing is UIDropInteraction drop)
                view.RemoveInteraction(drop);
        }

        foreach (var child in view.Subviews)
            StripDropInteractions(child);
    }

    internal bool CanHandle(IUIDropSession session)
        => session.Items.Length > 0 && this.host.WouldAccept(Preview(session));

    internal void Entered(IUIDropSession session)
        => this.host.NotifyEnter(this.window, Preview(session), this.Position(session));

    internal void Updated(IUIDropSession session)
        => this.host.NotifyOver(this.window, Preview(session), this.Position(session));

    internal void Exited() => this.host.NotifyLeave(this.window);

    internal void Perform(IUIDropSession session)
    {
        var position = this.Position(session);
        var providers = session.Items
            .Select(x => x.ItemProvider)
            .Where(x => x != null)
            .ToList();

        _ = this.StageAsync(providers, position);
    }

    async Task StageAsync(List<NSItemProvider> providers, Point position)
    {
        var files = new List<DroppedFile>(providers.Count);

        foreach (var provider in providers)
        {
            if (await StageAsync(provider).ConfigureAwait(true) is { } path)
                files.Add(DroppedFile.FromPath(path));
        }

        if (!this.disposed && files.Count > 0)
            this.host.NotifyDrop(this.window, files, position);
    }

    static Task<string?> StageAsync(NSItemProvider provider)
    {
        var source = new TaskCompletionSource<string?>();

        // LoadFileRepresentation's URL is deleted the moment the callback returns, so the bytes are
        // copied rather than the URL kept. The copy goes in a folder of its own so two drops of the
        // same file name cannot collide, and is left for the OS to reclaim — deleting it here would
        // race the read the caller is about to do.
        provider.LoadFileRepresentation(UTTypes.Item.Identifier, (url, error) =>
        {
            if (error != null || url?.Path is not { Length: > 0 } from)
            {
                source.TrySetResult(null);
                return;
            }

            try
            {
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

        return source.Task;
    }

    /// <summary>
    /// What can be known about the drag before it lands: a name, and nothing else. Enough for the
    /// extension filter, which is what the hover events are for.
    /// </summary>
    static IReadOnlyList<DroppedFile> Preview(IUIDropSession session)
    {
        var files = new List<DroppedFile>(session.Items.Length);

        foreach (var item in session.Items)
        {
            var name = item.ItemProvider?.SuggestedName;
            if (String.IsNullOrEmpty(name))
                continue;

            files.Add(new DroppedFile(name, null, -1, _ =>
                Task.FromException<Stream>(new InvalidOperationException(
                    "A file can only be read once the drop has completed."))));
        }

        return files;
    }

    Point Position(IUIDropSession session)
    {
        var point = session.LocationInView(this.native);
        return new Point(point.X, point.Y);
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.window.Activated -= this.OnActivated;
        this.native.RemoveInteraction(this.interaction);
    }


    sealed class DropDelegate(CatalystDropTarget owner) : UIDropInteractionDelegate
    {
        public override bool CanHandleSession(UIDropInteraction interaction, IUIDropSession session)
        {
            // Cheap, and the one call guaranteed to happen before every drag — so it doubles as the
            // trigger for re-stripping a web view that was created since the last one.
            owner.SuppressWebViews();
            return owner.CanHandle(session);
        }

        public override void SessionDidEnter(UIDropInteraction interaction, IUIDropSession session)
            => owner.Entered(session);

        public override UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session)
        {
            owner.Updated(session);
            return new UIDropProposal(UIDropOperation.Copy);
        }

        public override void SessionDidExit(UIDropInteraction interaction, IUIDropSession session)
            => owner.Exited();

        public override void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
            => owner.Perform(session);
    }
}
