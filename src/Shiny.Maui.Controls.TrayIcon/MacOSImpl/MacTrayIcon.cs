using AppKit;
using Foundation;

namespace Shiny.Maui.Controls.TrayIcon;

sealed class MacTrayIcon : TrayIconBase
{
    readonly NSStatusItem statusItem;
    NSMenu? nsMenu;

    public MacTrayIcon()
    {
        this.statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(NSStatusItemLength.Variable);
        if (this.statusItem.Button != null)
        {
            this.statusItem.Button.Activated += this.OnButtonActivated;
            this.statusItem.Button.SendActionOn(NSEventType.LeftMouseUp | NSEventType.RightMouseUp);
        }
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
        if (this.statusItem.Button == null) return;
        using var ms = new MemoryStream();
        using (var src = factory()) src.CopyTo(ms);
        using var data = NSData.FromArray(ms.ToArray());
        var image = new NSImage(data);
        image.Template = this.IsTemplateImage;
        this.statusItem.Button.Image = image;
    }

    protected override void OnTooltipChanged(string? value)
    {
        if (this.statusItem.Button != null)
            this.statusItem.Button.ToolTip = value ?? string.Empty;
    }

    protected override void OnTitleChanged(string? value)
    {
        if (this.statusItem.Button != null)
            this.statusItem.Button.Title = value ?? string.Empty;
    }

    protected override void OnVisibilityChanged(bool visible) => this.statusItem.Visible = visible;

    protected override void OnMenuChanged(object? sender, EventArgs e)
    {
        if (this.Menu == null) return;
        this.nsMenu = BuildNSMenu(this.Menu.Items);
        this.statusItem.Menu = this.nsMenu;
    }

    public override void ShowMenu()
    {
        if (this.nsMenu != null) this.statusItem.Menu = this.nsMenu;
    }

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
                    nsi = new NSMenuItem(mi.Label, mi.Accelerator ?? string.Empty, (_, _) => mi.RaiseClicked());
                    nsi.Enabled = mi.IsEnabled;
                    break;
                default:
                    continue;
            }
            menu.AddItem(nsi);
        }
        return menu;
    }

    public override void Dispose()
    {
        if (this.statusItem.Button != null) this.statusItem.Button.Activated -= this.OnButtonActivated;
        NSStatusBar.SystemStatusBar.RemoveStatusItem(this.statusItem);
        base.Dispose();
    }
}

sealed class MacTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new MacTrayIcon();
}
