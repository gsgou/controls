using System.Runtime.InteropServices;
using static Shiny.Maui.Controls.TrayIcon.LinuxInterop;

namespace Shiny.Maui.Controls.TrayIcon;

sealed class LinuxTrayIcon : TrayIconBase
{
    static readonly Dictionary<long, Action> Callbacks = new();
    static long NextTag = 1;

    static bool gtkInitialized;
    static bool notifyInitialized;
    static bool notifyAvailable = true;

    internal static void EnsureGtk()
    {
        if (gtkInitialized) return;
        var argc = 0;
        var argv = IntPtr.Zero;
        GtkInitCheck(ref argc, ref argv);
        gtkInitialized = true;
    }

    static void EnsureNotify()
    {
        if (notifyInitialized || !notifyAvailable) return;
        try
        {
            if (NotifyInit("Shiny.Maui.Controls.TrayIcon"))
                notifyInitialized = true;
            else
                notifyAvailable = false;
        }
        catch (DllNotFoundException)
        {
            notifyAvailable = false;
        }
    }

    readonly IntPtr indicator;
    readonly List<long> ownedTags = new();
    readonly List<string> menuIconFiles = new();
    readonly string id;
    string? iconFilePath;
    IntPtr accelGroup = IntPtr.Zero;

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

    protected override void OnTitleChanged(string? value) => this.UpdateLabel();

    protected override void OnBadgeChanged(string? value) => this.UpdateLabel();

    void UpdateLabel()
    {
        var t = this.Title;
        var b = this.Badge;
        string combined;
        if (string.IsNullOrEmpty(t) && string.IsNullOrEmpty(b)) combined = string.Empty;
        else if (string.IsNullOrEmpty(b)) combined = t ?? string.Empty;
        else if (string.IsNullOrEmpty(t)) combined = b!;
        else combined = $"{t} {b}";
        AppIndicatorSetTitle(this.indicator, combined);
        AppIndicatorSetLabel(this.indicator, combined, combined);
    }

    protected override void OnVisibilityChanged(bool visible)
        => AppIndicatorSetStatus(this.indicator, (int)(visible ? AppIndicatorStatus.Active : AppIndicatorStatus.Passive));

    public override void ShowNotification(string title, string message)
    {
        EnsureNotify();
        if (!notifyInitialized) return;
        try
        {
            var n = NotifyNotificationNew(title ?? string.Empty, message ?? string.Empty, this.iconFilePath);
            if (n == IntPtr.Zero) return;
            NotifyNotificationShow(n, IntPtr.Zero);
            GObjectUnref(n);
        }
        catch
        {
            // best-effort
        }
    }

    protected override void OnMenuChanged(object? sender, EventArgs e)
    {
        this.ClearOwnedTags();
        this.ClearMenuIconFiles();
        if (this.Menu == null) return;
        this.accelGroup = GtkAccelGroupNew();
        var menu = this.BuildMenu(this.Menu.Items);
        GtkMenuSetAccelGroup(menu, this.accelGroup);
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
                    if (mi.Icon != null)
                    {
                        widget = GtkImageMenuItemNewWithLabel(label);
                        try
                        {
                            var iconFile = Path.Combine(Path.GetTempPath(), this.id + "-mi-" + Guid.NewGuid().ToString("N") + ".png");
                            using (var dst = File.Create(iconFile))
                            using (var src = mi.Icon()) src.CopyTo(dst);
                            this.menuIconFiles.Add(iconFile);
                            var image = GtkImageNewFromFile(iconFile);
                            GtkImageMenuItemSetImage(widget, image);
                            GtkImageMenuItemSetAlwaysShowImage(widget, true);
                        }
                        catch { /* best-effort */ }
                    }
                    else
                    {
                        widget = GtkMenuItemNewWithLabel(label);
                    }
                    GtkWidgetSetSensitive(widget, mi.IsEnabled);
                    var tag = this.Register(() => mi.RaiseClicked());
                    this.Connect(widget, "activate", tag);

                    if (this.accelGroup != IntPtr.Zero && !string.IsNullOrEmpty(mi.Accelerator))
                        this.TryAttachAccelerator(widget, mi.Accelerator);
                    break;
                }
                default: continue;
            }
            GtkMenuShellAppend(menu, widget);
        }
        return menu;
    }

    void TryAttachAccelerator(IntPtr widget, string accelerator)
    {
        var parsed = TrayAccelerator.Parse(accelerator);
        if (parsed == null) return;
        var keyName = MapKeyName(parsed.Key);
        if (string.IsNullOrEmpty(keyName)) return;

        uint keyval;
        try { keyval = GdkKeyvalFromName(keyName); }
        catch (DllNotFoundException) { return; }
        if (keyval == 0) return;

        uint mods = 0;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Control) != 0) mods |= (uint)GdkModifierType.ControlMask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Alt) != 0) mods |= (uint)GdkModifierType.Mod1Mask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Shift) != 0) mods |= (uint)GdkModifierType.ShiftMask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Meta) != 0) mods |= (uint)GdkModifierType.SuperMask;

        try
        {
            GtkWidgetAddAccelerator(widget, "activate", this.accelGroup, keyval, mods, (uint)GtkAccelFlags.Visible);
        }
        catch { /* best-effort */ }
    }

    static string MapKeyName(string key)
    {
        if (key.Length == 1)
            return key.ToLowerInvariant();
        if (key.Length >= 2 && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key.AsSpan(1), out var fn) && fn >= 1 && fn <= 24)
            return "F" + fn;
        return key.ToLowerInvariant() switch
        {
            "esc" or "escape" => "Escape",
            "enter" or "return" => "Return",
            "tab" => "Tab",
            "space" or "spacebar" => "space",
            "back" or "backspace" => "BackSpace",
            "delete" or "del" => "Delete",
            "insert" or "ins" => "Insert",
            "home" => "Home",
            "end" => "End",
            "pgup" or "pageup" => "Page_Up",
            "pgdn" or "pagedown" => "Page_Down",
            "left" => "Left",
            "up" => "Up",
            "right" => "Right",
            "down" => "Down",
            _ => string.Empty
        };
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

    void ClearMenuIconFiles()
    {
        foreach (var f in this.menuIconFiles)
        {
            try { File.Delete(f); } catch { }
        }
        this.menuIconFiles.Clear();
    }

    public override void Dispose()
    {
        this.ClearOwnedTags();
        this.ClearMenuIconFiles();
        AppIndicatorSetStatus(this.indicator, (int)AppIndicatorStatus.Passive);
        GObjectUnref(this.indicator);
        if (this.iconFilePath != null) try { File.Delete(this.iconFilePath); } catch { }
        base.Dispose();
    }
}
