using System.Runtime.InteropServices;
using static Shiny.Maui.Controls.TrayIcon.LinuxInterop;

namespace Shiny.Maui.Controls.TrayIcon;

sealed class LinuxTrayIcon : TrayIconBase
{
    static readonly Dictionary<long, Action> Callbacks = new();
    static long NextTag = 1;

    static bool gtkInitialized;

    internal static void EnsureGtk()
    {
        if (gtkInitialized) return;
        var argc = 0;
        var argv = IntPtr.Zero;
        GtkInitCheck(ref argc, ref argv);
        gtkInitialized = true;
    }

    readonly IntPtr indicator;
    readonly List<long> ownedTags = new();
    readonly string id;
    string? iconFilePath;

    public LinuxTrayIcon()
    {
        EnsureGtk();
        this.id = "shiny-tray-" + Guid.NewGuid().ToString("N");
        this.indicator = AppIndicatorNew(this.id, string.Empty, (int)AppIndicatorCategory.ApplicationStatus);
        AppIndicatorSetStatus(this.indicator, (int)AppIndicatorStatus.Active);
    }

    protected override void OnIconChanged(Func<Stream> factory)
    {
        var newPath = Path.Combine(Path.GetTempPath(), this.id + "-" + Guid.NewGuid().ToString("N") + ".png");
        using (var dst = File.Create(newPath))
        using (var src = factory())
            src.CopyTo(dst);
        AppIndicatorSetIconFull(this.indicator, newPath, this.Tooltip ?? string.Empty);
        if (this.iconFilePath != null) try { File.Delete(this.iconFilePath); } catch { }
        this.iconFilePath = newPath;
    }

    protected override void OnTooltipChanged(string? value)
    {
        if (this.iconFilePath != null)
            AppIndicatorSetIconFull(this.indicator, this.iconFilePath, value ?? string.Empty);
    }

    protected override void OnTitleChanged(string? value)
    {
        AppIndicatorSetTitle(this.indicator, value ?? string.Empty);
        AppIndicatorSetLabel(this.indicator, value ?? string.Empty, value ?? string.Empty);
    }

    protected override void OnVisibilityChanged(bool visible)
        => AppIndicatorSetStatus(this.indicator, (int)(visible ? AppIndicatorStatus.Active : AppIndicatorStatus.Passive));

    protected override void OnMenuChanged(object? sender, EventArgs e)
    {
        this.ClearOwnedTags();
        if (this.Menu == null) return;
        var menu = this.BuildMenu(this.Menu.Items);
        GtkWidgetShowAll(menu);
        AppIndicatorSetMenu(this.indicator, menu);
    }

    public override void ShowMenu()
    {
        // app indicators handle this themselves on right-click — no programmatic open API.
    }

    IntPtr BuildMenu(IEnumerable<TrayMenuItemBase> items)
    {
        var menu = GtkMenuNew();
        foreach (var item in items)
        {
            if (!item.IsVisible) continue;
            IntPtr widget;
            switch (item)
            {
                case TraySeparator:
                    widget = GtkSeparatorMenuItemNew();
                    break;
                case TraySubmenu sub:
                    widget = GtkMenuItemNewWithLabel(sub.Label);
                    GtkMenuItemSetSubmenu(widget, this.BuildMenu(sub.Items));
                    GtkWidgetSetSensitive(widget, sub.IsEnabled);
                    break;
                case TrayCheckMenuItem check:
                {
                    widget = GtkCheckMenuItemNewWithLabel(check.Label);
                    GtkCheckMenuItemSetActive(widget, check.IsChecked);
                    GtkWidgetSetSensitive(widget, check.IsEnabled);
                    var tag = this.Register(() =>
                    {
                        var active = GtkCheckMenuItemGetActive(widget);
                        check.RaiseToggled(active);
                    });
                    this.Connect(widget, "activate", tag);
                    break;
                }
                case TrayMenuItem mi:
                {
                    var label = string.IsNullOrEmpty(mi.Accelerator) ? mi.Label : mi.Label + "\t" + mi.Accelerator;
                    widget = GtkMenuItemNewWithLabel(label);
                    GtkWidgetSetSensitive(widget, mi.IsEnabled);
                    var tag = this.Register(() => mi.RaiseClicked());
                    this.Connect(widget, "activate", tag);
                    break;
                }
                default: continue;
            }
            GtkMenuShellAppend(menu, widget);
        }
        return menu;
    }

    unsafe void Connect(IntPtr widget, string signal, long tag)
    {
        var fn = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&MenuItemActivated;
        GSignalConnectData(widget, signal, fn, (IntPtr)tag, IntPtr.Zero, 0);
    }

    [UnmanagedCallersOnly]
    static void MenuItemActivated(IntPtr widget, IntPtr userData)
    {
        var tag = (long)userData;
        Action? action;
        lock (Callbacks) Callbacks.TryGetValue(tag, out action);
        action?.Invoke();
    }

    long Register(Action action)
    {
        var tag = Interlocked.Increment(ref NextTag);
        lock (Callbacks) Callbacks[tag] = action;
        this.ownedTags.Add(tag);
        return tag;
    }

    void ClearOwnedTags()
    {
        lock (Callbacks)
            foreach (var t in this.ownedTags) Callbacks.Remove(t);
        this.ownedTags.Clear();
    }

    public override void Dispose()
    {
        this.ClearOwnedTags();
        AppIndicatorSetStatus(this.indicator, (int)AppIndicatorStatus.Passive);
        GObjectUnref(this.indicator);
        if (this.iconFilePath != null) try { File.Delete(this.iconFilePath); } catch { }
        base.Dispose();
    }
}
