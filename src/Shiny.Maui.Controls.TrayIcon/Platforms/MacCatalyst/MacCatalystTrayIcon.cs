using System.Runtime.InteropServices;
using static Shiny.Maui.Controls.TrayIcon.AppKitInterop;

namespace Shiny.Maui.Controls.TrayIcon;

sealed class MacCatalystTrayIcon : TrayIconBase
{
    static readonly Dictionary<long, Action> Callbacks = new();
    static readonly Dictionary<long, WeakReference<MacCatalystTrayIcon>> ButtonOwners = new();
    static long NextTag = 1;
    static IntPtr CallbackClass;
    static IntPtr ActionSel;
    static IntPtr ButtonActionSel;
    static IntPtr SharedCallback;

    static MacCatalystTrayIcon()
    {
        EnsureLoaded();
        var nsObject = GetClass("NSObject");
        CallbackClass = AllocateClassPair(nsObject, "ShinyTrayCB", IntPtr.Zero);
        ActionSel = Sel("handleAction:");
        ButtonActionSel = Sel("handleButton:");
        unsafe
        {
            AddMethod(CallbackClass, ActionSel, (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&HandleAction, "v@:@");
            AddMethod(CallbackClass, ButtonActionSel, (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&HandleButton, "v@:@");
        }
        RegisterClassPair(CallbackClass);
        var alloc = MsgSend(CallbackClass, Sel("alloc"));
        SharedCallback = MsgSend(alloc, Sel("init"));
    }

    [UnmanagedCallersOnly]
    static void HandleAction(IntPtr self, IntPtr sel, IntPtr sender)
    {
        var tag = (long)MsgSend(sender, Sel("tag"));
        Action? action;
        lock (Callbacks) Callbacks.TryGetValue(tag, out action);
        action?.Invoke();
    }

    [UnmanagedCallersOnly]
    static void HandleButton(IntPtr self, IntPtr sel, IntPtr sender)
    {
        var tag = (long)MsgSend(sender, Sel("tag"));
        WeakReference<MacCatalystTrayIcon>? wr;
        lock (ButtonOwners) ButtonOwners.TryGetValue(tag, out wr);
        if (wr != null && wr.TryGetTarget(out var owner))
            owner.OnButtonClicked();
    }

    readonly IntPtr statusItem;
    readonly IntPtr button;
    readonly long buttonTag;
    IntPtr menu = IntPtr.Zero;
    readonly List<long> ownedTags = new();

    public MacCatalystTrayIcon()
    {
        var nsStatusBar = GetClass("NSStatusBar");
        var systemBar = MsgSend(nsStatusBar, Sel("systemStatusBar"));
        this.statusItem = MsgSendDouble(systemBar, Sel("statusItemWithLength:"), -1.0);
        this.button = MsgSend(this.statusItem, Sel("button"));
        this.buttonTag = Interlocked.Increment(ref NextTag);
        lock (ButtonOwners) ButtonOwners[this.buttonTag] = new WeakReference<MacCatalystTrayIcon>(this);
        if (this.button != IntPtr.Zero)
        {
            MsgSendLong(this.button, Sel("setTag:"), this.buttonTag);
            MsgSend(this.button, Sel("setTarget:"), SharedCallback);
            MsgSend(this.button, Sel("setAction:"), ButtonActionSel);
            MsgSendLong(this.button, Sel("sendActionOn:"),
                (long)(NSEventMask.LeftMouseUp | NSEventMask.RightMouseUp));
        }
    }

    [Flags]
    enum NSEventMask : long
    {
        LeftMouseUp = 1L << 2,
        RightMouseUp = 1L << 4
    }

    void OnButtonClicked()
    {
        var nsApp = MsgSend(GetClass("NSApplication"), Sel("sharedApplication"));
        var currentEvent = MsgSend(nsApp, Sel("currentEvent"));
        var typeRaw = currentEvent == IntPtr.Zero ? 0L : (long)MsgSend(currentEvent, Sel("type"));
        if (typeRaw == 4)
        {
            this.RaiseSecondary(0, 0);
            this.ShowMenu();
        }
        else
        {
            this.RaisePrimary(0, 0);
        }
    }

    protected override void OnIconChanged(Func<Stream> factory)
    {
        if (this.button == IntPtr.Zero) return;
        using var ms = new MemoryStream();
        using (var src = factory()) src.CopyTo(ms);
        var data = NSDataFromBytes(ms.ToArray());
        var imgAlloc = MsgSend(GetClass("NSImage"), Sel("alloc"));
        var image = MsgSend(imgAlloc, Sel("initWithData:"), data);
        MsgSendBool(image, Sel("setTemplate:"), this.IsTemplateImage);
        MsgSend(this.button, Sel("setImage:"), image);
    }

    protected override void OnTooltipChanged(string? value)
    {
        if (this.button == IntPtr.Zero) return;
        MsgSend(this.button, Sel("setToolTip:"), NSString(value ?? string.Empty));
    }

    protected override void OnTitleChanged(string? value)
    {
        if (this.button == IntPtr.Zero) return;
        MsgSend(this.button, Sel("setTitle:"), NSString(value ?? string.Empty));
    }

    protected override void OnVisibilityChanged(bool visible)
        => MsgSendBool(this.statusItem, Sel("setVisible:"), visible);

    protected override void OnMenuChanged(object? sender, EventArgs e)
    {
        this.ClearOwnedTags();
        if (this.Menu == null) return;
        this.menu = this.BuildMenu(this.Menu.Items);
    }

    public override void ShowMenu()
    {
        if (this.menu != IntPtr.Zero)
            MsgSend(this.statusItem, Sel("popUpStatusItemMenu:"), this.menu);
    }

    IntPtr BuildMenu(IEnumerable<TrayMenuItemBase> items)
    {
        var menuAlloc = MsgSend(GetClass("NSMenu"), Sel("alloc"));
        var m = MsgSend(menuAlloc, Sel("init"));
        MsgSendBool(m, Sel("setAutoenablesItems:"), false);

        foreach (var item in items)
        {
            if (!item.IsVisible) continue;
            switch (item)
            {
                case TraySeparator:
                {
                    var sep = MsgSend(GetClass("NSMenuItem"), Sel("separatorItem"));
                    MsgSend(m, Sel("addItem:"), sep);
                    break;
                }
                case TraySubmenu sub:
                {
                    var nsi = this.NewItem(sub.Label, null, null);
                    var childMenu = this.BuildMenu(sub.Items);
                    MsgSend(nsi, Sel("setSubmenu:"), childMenu);
                    MsgSendBool(nsi, Sel("setEnabled:"), sub.IsEnabled);
                    MsgSend(m, Sel("addItem:"), nsi);
                    break;
                }
                case TrayCheckMenuItem check:
                {
                    var tag = this.RegisterCallback(() =>
                    {
                        check.RaiseToggled(!check.IsChecked);
                    });
                    var nsi = this.NewItem(check.Label, null, tag);
                    MsgSendLong(nsi, Sel("setState:"), check.IsChecked ? 1 : 0);
                    MsgSendBool(nsi, Sel("setEnabled:"), check.IsEnabled);
                    MsgSend(m, Sel("addItem:"), nsi);
                    break;
                }
                case TrayMenuItem mi:
                {
                    var tag = this.RegisterCallback(() => mi.RaiseClicked());
                    var nsi = this.NewItem(mi.Label, mi.Accelerator, tag);
                    MsgSendBool(nsi, Sel("setEnabled:"), mi.IsEnabled);
                    MsgSend(m, Sel("addItem:"), nsi);
                    break;
                }
            }
        }
        return m;
    }

    IntPtr NewItem(string label, string? keyEquivalent, long? tag)
    {
        var alloc = MsgSend(GetClass("NSMenuItem"), Sel("alloc"));
        var nsi = MsgSend(alloc, Sel("initWithTitle:action:keyEquivalent:"),
            NSString(label), tag.HasValue ? ActionSel : IntPtr.Zero, NSString(keyEquivalent ?? string.Empty));
        if (tag.HasValue)
        {
            MsgSendLong(nsi, Sel("setTag:"), tag.Value);
            MsgSend(nsi, Sel("setTarget:"), SharedCallback);
        }
        return nsi;
    }

    long RegisterCallback(Action action)
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
        lock (ButtonOwners) ButtonOwners.Remove(this.buttonTag);
        var nsStatusBar = GetClass("NSStatusBar");
        var systemBar = MsgSend(nsStatusBar, Sel("systemStatusBar"));
        MsgSend(systemBar, Sel("removeStatusItem:"), this.statusItem);
        base.Dispose();
    }
}
