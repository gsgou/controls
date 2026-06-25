using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace Shiny.Maui.Controls.Desktop.TrayIcon;

sealed class MacTrayIcon : TrayIconBase
{
    NSStatusItem statusItem = null!;
    NSMenu? nsMenu;

    public MacTrayIcon()
    {
        MacMainThread.Invoke(() =>
        {
            this.statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(NSStatusItemLength.Variable);
            if (this.statusItem.Button != null)
            {
                this.statusItem.Button.Activated += this.OnButtonActivated;
                // sendActionOn: takes an NSEvent *mask* (1 << eventType), not raw NSEventType
                // values — but the .NET binding only exposes SendActionOn(NSEventType). Passing
                // (NSEventType.LeftMouseUp | NSEventType.RightMouseUp) = 6 actually means the mask
                // (LeftMouseDown | LeftMouseUp); right-click is excluded, so the action never
                // fires on secondary click. Build the correct mask via NSEventMask (LeftMouseUp=4 |
                // RightMouseUp=16 = 20) and cast it back to satisfy the binding signature.
                this.statusItem.Button.SendActionOn((NSEventType)(NSEventMask.LeftMouseUp | NSEventMask.RightMouseUp));
            }
        });
    }

    void OnButtonActivated(object? sender, EventArgs e)
    {
        var ev = NSApplication.SharedApplication.CurrentEvent;
        var location = NSEvent.CurrentMouseLocation;
        var x = (int)location.X;
        var y = (int)location.Y;
        if (ev?.Type == NSEventType.RightMouseUp)
        {
            this.RaiseSecondary(x, y);
            this.ShowMenu();
        }
        else
        {
            this.RaisePrimary(x, y);
            if (ev?.ClickCount >= 2) this.RaiseDouble(x, y);
        }
    }

    protected override void OnIconChanged(Func<Stream> factory)
    {
        using var ms = new MemoryStream();
        using (var src = factory()) src.CopyTo(ms);
        var bytes = ms.ToArray();
        var isTemplate = this.IsTemplateImage;

        MacMainThread.Invoke(() =>
        {
            if (this.statusItem.Button == null) return;
            using var data = NSData.FromArray(bytes);
            var image = new NSImage(data);
            image.Template = isTemplate;
            // Same rationale as OnMenuChanged: release the outgoing image's native
            // backing now rather than leaving it for a GC that the small managed
            // footprint won't promptly trigger.
            var previous = this.statusItem.Button.Image;
            this.statusItem.Button.Image = image;
            previous?.Dispose();
        });
    }

    protected override void OnTooltipChanged(string? value)
        => MacMainThread.Invoke(() =>
        {
            if (this.statusItem.Button != null)
                this.statusItem.Button.ToolTip = value ?? string.Empty;
        });

    protected override void OnTitleChanged(string? value)
        => this.UpdateButtonTitle();

    protected override void OnBadgeChanged(string? value)
        => this.UpdateButtonTitle();

    void UpdateButtonTitle()
        => MacMainThread.Invoke(() =>
        {
            if (this.statusItem.Button == null) return;
            var t = this.Title;
            var b = this.Badge;
            string combined;
            if (string.IsNullOrEmpty(t) && string.IsNullOrEmpty(b)) combined = string.Empty;
            else if (string.IsNullOrEmpty(b)) combined = t ?? string.Empty;
            else if (string.IsNullOrEmpty(t)) combined = b!;
            else combined = $"{t} {b}";
            this.statusItem.Button.Title = combined;
        });

    protected override void OnVisibilityChanged(bool visible)
        => MacMainThread.Invoke(() => this.statusItem.Visible = visible);

    public override void ShowNotification(string title, string message)
        => MacMainThread.Invoke(() =>
        {
            var n = new NSUserNotification
            {
                Title = title ?? string.Empty,
                InformativeText = message ?? string.Empty
            };
            NSUserNotificationCenter.DefaultUserNotificationCenter.DeliverNotification(n);
        });

    protected override void OnMenuChanged(object? sender, EventArgs e)
        => MacMainThread.Invoke(() =>
        {
            if (this.Menu == null) return;

            // Deliberately do NOT assign statusItem.Menu here. Assigning it makes AppKit
            // pop the menu automatically on *any* button click and suppresses the button's
            // action entirely, so primary (left) click could never be distinguished from
            // secondary (right) click. Instead we keep the built menu in a field and pop it
            // on demand from ShowMenu() (secondary click), leaving primary click free to
            // raise PrimaryClick — matching the Windows and MacCatalyst implementations.
            //
            // Dispose the previous menu explicitly. The managed NSMenu peer holds a native
            // +1 until GC, and because the managed footprint of a menu wrapper is tiny while
            // the native NSMenu/NSMenuItem/NSImage graph behind it is large, the GC rarely
            // runs, finalizers don't fire, and the native bytes pile up. Apps that rebuild
            // the menu on a poll/timer leak a whole menu tree each rebuild (observed: tens of
            // GB over a long-running session). Disposing here releases it immediately.
            var previous = this.nsMenu;
            this.nsMenu = BuildNSMenu(this.Menu.Items);
            previous?.Dispose();
        });

    public override void ShowMenu()
        => MacMainThread.Invoke(() =>
        {
            // Pop the menu on demand rather than leaving it assigned to statusItem.Menu, so
            // the button keeps sending its action and primary-click stays distinguishable
            // from secondary-click. PopUpStatusItemMenu is the same call the MacCatalyst head
            // uses; it's been soft-deprecated since 10.10 but remains the simplest reliable
            // way to anchor a status-bar menu under the icon, and there's no AOT-safe
            // replacement bound for status items.
#pragma warning disable CA1422 // PopUpStatusItemMenu obsoleted since 10.10 — intentional, see above
            if (this.nsMenu != null) this.statusItem.PopUpStatusItemMenu(this.nsMenu);
#pragma warning restore CA1422
        });

    static NSMenu BuildNSMenu(IEnumerable<TrayMenuItemBase> items)
    {
        var menu = new NSMenu { AutoEnablesItems = false };
        foreach (var item in items)
        {
            if (!item.IsVisible) continue;
            NSMenuItem nsi;
            switch (item)
            {
                case TraySeparator:
                    nsi = NSMenuItem.SeparatorItem;
                    break;
                case TraySubmenu sub:
                    nsi = new NSMenuItem(sub.Label) { Submenu = BuildNSMenu(sub.Items) };
                    nsi.Enabled = sub.IsEnabled;
                    break;
                case TrayCheckMenuItem check:
                    nsi = new NSMenuItem(check.Label, (s, _) =>
                    {
                        check.RaiseToggled(!check.IsChecked);
                        if (s is NSMenuItem m) m.State = check.IsChecked ? NSCellStateValue.On : NSCellStateValue.Off;
                    });
                    nsi.State = check.IsChecked ? NSCellStateValue.On : NSCellStateValue.Off;
                    nsi.Enabled = check.IsEnabled;
                    break;
                case TrayMenuItem mi:
                {
                    var (keyEq, mask) = ParseAccelerator(mi.Accelerator);
                    nsi = new NSMenuItem(mi.Label, keyEq, (_, _) => mi.RaiseClicked());
                    nsi.KeyEquivalentModifierMask = mask;
                    nsi.Enabled = mi.IsEnabled;
                    if (mi.Icon != null)
                    {
                        try
                        {
                            using var ms = new MemoryStream();
                            using (var src = mi.Icon()) src.CopyTo(ms);
                            using var data = NSData.FromArray(ms.ToArray());
                            var img = new NSImage(data);
                            img.Size = new CGSize(16, 16);
                            nsi.Image = img;
                        }
                        catch { /* best-effort */ }
                    }
                    break;
                }
                default:
                    continue;
            }
            menu.AddItem(nsi);
        }
        return menu;
    }

    static (string keyEquivalent, NSEventModifierMask mask) ParseAccelerator(string? accelerator)
    {
        var parsed = TrayAccelerator.Parse(accelerator);
        if (parsed == null) return (string.Empty, 0);
        NSEventModifierMask mask = 0;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Control) != 0) mask |= NSEventModifierMask.ControlKeyMask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Alt) != 0) mask |= NSEventModifierMask.AlternateKeyMask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Shift) != 0) mask |= NSEventModifierMask.ShiftKeyMask;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Meta) != 0) mask |= NSEventModifierMask.CommandKeyMask;

        var key = MapKey(parsed.Key);
        return (key, mask);
    }

    static string MapKey(string key)
    {
        if (key.Length == 1)
            return key.ToLowerInvariant();

        if (key.Length >= 2 && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key.AsSpan(1), out var fn) && fn >= 1 && fn <= 24)
            return ((char)(0xF704 + fn - 1)).ToString();

        return key.ToLowerInvariant() switch
        {
            "esc" or "escape" => "",
            "enter" or "return" => "\r",
            "tab" => "\t",
            "space" or "spacebar" => " ",
            "back" or "backspace" => "\b",
            "delete" or "del" => "",
            "left" => ((char)0xF702).ToString(),
            "up" => ((char)0xF700).ToString(),
            "right" => ((char)0xF703).ToString(),
            "down" => ((char)0xF701).ToString(),
            "home" => ((char)0xF729).ToString(),
            "end" => ((char)0xF72B).ToString(),
            "pgup" or "pageup" => ((char)0xF72C).ToString(),
            "pgdn" or "pagedown" => ((char)0xF72D).ToString(),
            _ => string.Empty
        };
    }

    public override void Dispose()
    {
        MacMainThread.Invoke(() =>
        {
            if (this.statusItem.Button != null) this.statusItem.Button.Activated -= this.OnButtonActivated;
            NSStatusBar.SystemStatusBar.RemoveStatusItem(this.statusItem);
        });
        base.Dispose();
    }
}
