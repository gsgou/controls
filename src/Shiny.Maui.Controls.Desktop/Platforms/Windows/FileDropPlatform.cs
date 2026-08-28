using System.Runtime.Versioning;
using Microsoft.Maui.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using MauiWindow = Microsoft.Maui.Controls.Window;
using WinUIWindow = Microsoft.UI.Xaml.Window;
using WinUIDragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
using WinUIBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinUIDropOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;

namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// WinUI 3 backing for window-level file drop.
/// </summary>
/// <remarks>
/// <para>
/// XAML's own drag/drop is used rather than OLE <c>RegisterDragDrop</c> on the HWND. Registering an
/// <c>IDropTarget</c> on the top-level window would be fought over by every child HWND that has one
/// of its own — WebView2 has — whereas the XAML router already knows how to walk the element tree.
/// </para>
/// <para>
/// The one thing that has to be dealt with natively is WebView2: it is its own drop target and it
/// wins over anything behind it, so a file dropped on a <c>BlazorWebView</c> navigates the web view
/// to that file instead of reaching the app. <c>AllowExternalDrop = false</c> is the supported way
/// to opt out, and once it is off the drag falls through to the XAML element underneath — which is
/// where this listens.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows10.0.19041.0")]
static class FileDropPlatform
{
    public static bool IsSupported => true;

    public static IDisposable? Attach(IFileDropHost host, MauiWindow window, object platformWindow)
        => platformWindow is WinUIWindow native ? new WindowsDropTarget(host, window, native) : null;
}


[SupportedOSPlatform("windows10.0.19041.0")]
sealed class WindowsDropTarget : IDisposable
{
    readonly IFileDropHost host;
    readonly MauiWindow window;
    readonly WinUIWindow native;

    UIElement? root;
    IReadOnlyList<DroppedFile> hovering = [];
    bool disposed;

    public WindowsDropTarget(IFileDropHost host, MauiWindow window, WinUIWindow native)
    {
        this.host = host;
        this.window = window;
        this.native = native;

        this.Hook();
        this.native.Activated += this.OnActivated;
    }

    void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        // The window's content is swapped as MAUI builds the page, and a BlazorWebView appears well
        // after the first attach. Re-running both steps on activation is what makes this work for a
        // web view that did not exist yet at startup; both are idempotent.
        this.Hook();
        this.SuppressWebViewDrop();
    }

    void Hook()
    {
        if (this.disposed || this.native.Content is not UIElement content)
            return;

        if (ReferenceEquals(this.root, content))
            return;

        this.Unhook();
        this.root = content;

        content.AllowDrop = true;

        // A Panel with no Background is not hit-testable, and a drag over a hole in the tree is a
        // drag XAML never routes anywhere. Transparent is enough — it does not change what is drawn.
        if (content is Microsoft.UI.Xaml.Controls.Panel { Background: null } panel)
            panel.Background = new WinUIBrush(Microsoft.UI.Colors.Transparent);

        content.DragEnter += this.OnDragEnter;
        content.DragOver += this.OnDragOver;
        content.DragLeave += this.OnDragLeave;
        content.Drop += this.OnDrop;

        this.SuppressWebViewDrop();
    }

    void Unhook()
    {
        if (this.root == null)
            return;

        this.root.DragEnter -= this.OnDragEnter;
        this.root.DragOver -= this.OnDragOver;
        this.root.DragLeave -= this.OnDragLeave;
        this.root.Drop -= this.OnDrop;
        this.root = null;
    }

    void SuppressWebViewDrop()
    {
        if (!this.host.Options.SuppressWebViewDrop || this.root == null)
            return;

        try
        {
            // Two halves of the same job. AllowDrop takes the element out of XAML's own drag routing
            // so the drag walks up to the root panel instead of stopping here...
            foreach (var web in Descendants(this.root).OfType<Microsoft.UI.Xaml.Controls.WebView2>())
                web.AllowDrop = false;

            // ...and revoking the OLE registration handles the half XAML cannot see, because the
            // browser lives in a child HWND with a drop target of its own.
            FileDropInterop.RevokeWebViewDropTargets(WinRT.Interop.WindowNative.GetWindowHandle(this.native));
        }
        catch (Exception ex)
        {
            this.host.LogError("Could not stop WebView2 from handling the drop itself", ex);
        }
    }

    static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;

            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }

    void OnDragEnter(object sender, WinUIDragEventArgs e)
    {
        if (!Carries(e))
            return;

        Accept(e);
        e.Handled = true;

        // Reading the items needs an await and the data view is released the moment this handler
        // returns, so the deferral is not optional.
        _ = this.ReportEnterAsync(e);
    }

    async Task ReportEnterAsync(WinUIDragEventArgs e)
    {
        var files = await this.ReadAsync(e).ConfigureAwait(true);
        if (this.disposed)
            return;

        this.hovering = files;
        this.host.NotifyEnter(this.window, files, Position(e, this.root));
    }

    void OnDragOver(object sender, WinUIDragEventArgs e)
    {
        if (!Carries(e))
            return;

        // No deferral here: DragOver fires continuously and the payload cannot change mid-drag, so
        // the list read once on enter is reused.
        Accept(e);
        e.Handled = true;
        this.host.NotifyOver(this.window, this.hovering, Position(e, this.root));
    }

    void OnDragLeave(object sender, WinUIDragEventArgs e)
    {
        this.hovering = [];
        this.host.NotifyLeave(this.window);
    }

    void OnDrop(object sender, WinUIDragEventArgs e)
    {
        if (!Carries(e))
            return;

        e.Handled = true;
        var position = Position(e, this.root);
        _ = this.ReportDropAsync(e, position);
    }

    async Task ReportDropAsync(WinUIDragEventArgs e, Point position)
    {
        var files = await this.ReadAsync(e).ConfigureAwait(true);
        this.hovering = [];

        if (!this.disposed && files.Count > 0)
            this.host.NotifyDrop(this.window, files, position);
    }

    async Task<IReadOnlyList<DroppedFile>> ReadAsync(WinUIDragEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var files = new List<DroppedFile>(items.Count);

            foreach (var item in items)
            {
                switch (item)
                {
                    case StorageFile file when !String.IsNullOrEmpty(file.Path):
                        files.Add(DroppedFile.FromPath(file.Path));
                        break;

                    // A file with no path is virtual — from a mail client or a compressed folder.
                    // It still has content, it just has to be streamed rather than opened.
                    case StorageFile file:
                        files.Add(new DroppedFile(file.Name, null, -1, async ct =>
                        {
                            var stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
                            return stream;
                        }));
                        break;

                    case StorageFolder folder when !String.IsNullOrEmpty(folder.Path):
                        files.Add(DroppedFile.FromPath(folder.Path));
                        break;
                }
            }

            return files;
        }
        catch (Exception ex)
        {
            this.host.LogError("Reading the dropped storage items failed", ex);
            return [];
        }
        finally
        {
            deferral.Complete();
        }
    }

    static bool Carries(WinUIDragEventArgs e)
        => e.DataView?.Contains(StandardDataFormats.StorageItems) == true;

    void Accept(WinUIDragEventArgs e)
    {
        var accepted = this.host.WouldAccept(this.hovering);
        e.AcceptedOperation = accepted ? WinUIDropOperation.Copy : WinUIDropOperation.None;

        if (e.DragUIOverride != null)
            e.DragUIOverride.IsGlyphVisible = accepted;
    }

    static Point Position(WinUIDragEventArgs e, UIElement? relativeTo)
    {
        var point = e.GetPosition(relativeTo);
        return new Point(point.X, point.Y);
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.native.Activated -= this.OnActivated;
        this.Unhook();
    }
}
