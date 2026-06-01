using System.Runtime.InteropServices;
using static Shiny.Maui.Controls.TrayIcon.NativeMethods;

namespace Shiny.Maui.Controls.TrayIcon;

sealed class WindowsTrayIcon : TrayIconBase
{
    static readonly Lazy<IntPtr> WindowClass = new(RegisterWindowClass);
    static readonly Dictionary<IntPtr, WindowsTrayIcon> Instances = new();
    static readonly WndProcDelegate StaticWndProc = StaticHandler;
    static uint NextId = 1;

    readonly uint id;
    readonly IntPtr hwnd;
    IntPtr hIcon = IntPtr.Zero;
    IntPtr popupMenu = IntPtr.Zero;
    bool added;
    bool disposed;
    Dictionary<int, Action>? menuActions;

    public WindowsTrayIcon()
    {
        this.id = Interlocked.Increment(ref NextId);
        this.hwnd = CreateWindowEx(0, "ShinyTrayHostWindow", null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (this.hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create tray host window.");

        lock (Instances)
            Instances[this.hwnd] = this;

        var data = this.BuildData(NIF_MESSAGE);
        data.uCallbackMessage = WM_TRAYICON;
        if (!Shell_NotifyIcon(NIM_ADD, ref data))
            throw new InvalidOperationException("Shell_NotifyIcon NIM_ADD failed.");

        data.uTimeoutOrVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NIM_SETVERSION, ref data);
        this.added = true;
    }

    static IntPtr RegisterWindowClass()
    {
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(StaticWndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = "ShinyTrayHostWindow"
        };
        var atom = RegisterClassEx(ref wc);
        return new IntPtr(atom);
    }

    NOTIFYICONDATA BuildData(uint flags)
    {
        return new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = this.hwnd,
            uID = this.id,
            uFlags = flags,
            hIcon = this.hIcon,
            szTip = this.Tooltip ?? string.Empty,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };
    }

    static IntPtr StaticHandler(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        WindowsTrayIcon? icon;
        lock (Instances) Instances.TryGetValue(hWnd, out icon);
        if (icon != null && msg == WM_TRAYICON)
        {
            var ev = (uint)(lParam.ToInt64() & 0xFFFF);
            GetCursorPos(out var pt);
            switch (ev)
            {
                case WM_LBUTTONUP: icon.RaisePrimary(pt.X, pt.Y); break;
                case WM_RBUTTONUP:
                case WM_CONTEXTMENU:
                    icon.RaiseSecondary(pt.X, pt.Y);
                    icon.ShowMenu();
                    break;
                case WM_LBUTTONDBLCLK: icon.RaiseDouble(pt.X, pt.Y); break;
            }
            return IntPtr.Zero;
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    protected override void OnIconChanged(Func<Stream> factory)
    {
        _ = WindowClass.Value;
        var oldIcon = this.hIcon;
        this.hIcon = LoadHIconFromStream(factory());
        var data = this.BuildData(NIF_ICON);
        Shell_NotifyIcon(NIM_MODIFY, ref data);
        if (oldIcon != IntPtr.Zero) DestroyIcon(oldIcon);
    }

    static IntPtr LoadHIconFromStream(Stream stream)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "shinytray_" + Guid.NewGuid().ToString("N") + ".ico");
        try
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                var icoBytes = IsIco(bytes) ? bytes : WrapPngAsIco(bytes);
                File.WriteAllBytes(tmp, icoBytes);
            }
            var cx = GetSystemMetrics(SM_CXSMICON);
            var cy = GetSystemMetrics(SM_CYSMICON);
            return LoadImage(IntPtr.Zero, tmp, IMAGE_ICON, cx, cy, LR_LOADFROMFILE);
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
            stream.Dispose();
        }
    }

    static bool IsIco(byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0;

    static bool IsPng(byte[] bytes)
        => bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    static byte[] WrapPngAsIco(byte[] png)
    {
        if (!IsPng(png))
            throw new NotSupportedException("Tray icon stream must be ICO or PNG on Windows.");

        // PNG IHDR is at bytes 16-23 (width 16-19, height 20-23, big-endian).
        var width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        var height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        var w = (byte)(width >= 256 ? 0 : width);
        var h = (byte)(height >= 256 ? 0 : height);

        var ico = new byte[6 + 16 + png.Length];
        // ICONDIR
        ico[2] = 1; // type = ICO
        ico[4] = 1; // count = 1
        // ICONDIRENTRY
        ico[6] = w;
        ico[7] = h;
        // planes = 1, bpp = 32
        ico[10] = 1;
        ico[12] = 32;
        // size
        var size = png.Length;
        ico[14] = (byte)(size & 0xFF);
        ico[15] = (byte)((size >> 8) & 0xFF);
        ico[16] = (byte)((size >> 16) & 0xFF);
        ico[17] = (byte)((size >> 24) & 0xFF);
        // offset = 22
        ico[18] = 22;
        Buffer.BlockCopy(png, 0, ico, 22, png.Length);
        return ico;
    }

    protected override void OnTooltipChanged(string? value)
    {
        var data = this.BuildData(NIF_TIP | NIF_SHOWTIP);
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    protected override void OnTitleChanged(string? value) { }

    protected override void OnVisibilityChanged(bool visible)
    {
        var data = this.BuildData(NIF_STATE);
        data.dwStateMask = NIS_HIDDEN;
        data.dwState = visible ? 0 : NIS_HIDDEN;
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    protected override void OnMenuChanged(object? sender, EventArgs e)
    {
        if (this.popupMenu != IntPtr.Zero)
            DestroyMenu(this.popupMenu);
        this.popupMenu = IntPtr.Zero;
        this.menuActions = null;
    }

    public override void ShowMenu()
    {
        if (this.Menu == null) return;
        if (this.popupMenu == IntPtr.Zero)
            this.BuildPopupMenu();
        if (this.popupMenu == IntPtr.Zero) return;

        GetCursorPos(out var pt);
        SetForegroundWindow(this.hwnd);
        var cmd = TrackPopupMenuEx(this.popupMenu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.X, pt.Y, this.hwnd, IntPtr.Zero);
        if (cmd != 0 && this.menuActions != null && this.menuActions.TryGetValue(cmd, out var action))
            action();
        PostMessageW(this.hwnd, 0, IntPtr.Zero, IntPtr.Zero);
    }

    void BuildPopupMenu()
    {
        if (this.Menu == null) return;
        this.popupMenu = CreatePopupMenu();
        this.menuActions = new();
        var nextId = 1000;
        AppendItems(this.popupMenu, this.Menu.Items, this.menuActions, ref nextId);
    }

    static void AppendItems(IntPtr menu, IEnumerable<TrayMenuItemBase> items, Dictionary<int, Action> actions, ref int nextId)
    {
        foreach (var item in items)
        {
            if (!item.IsVisible) continue;
            switch (item)
            {
                case TraySeparator:
                    AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
                    break;
                case TraySubmenu sub:
                {
                    var child = CreatePopupMenu();
                    AppendItems(child, sub.Items, actions, ref nextId);
                    var flags = MF_POPUP | (sub.IsEnabled ? 0 : MF_GRAYED);
                    AppendMenu(menu, flags, (UIntPtr)(ulong)child.ToInt64(), sub.Label);
                    break;
                }
                case TrayCheckMenuItem check:
                {
                    var id = nextId++;
                    var flags = MF_STRING | (check.IsEnabled ? 0 : MF_GRAYED) | (check.IsChecked ? MF_CHECKED : 0);
                    AppendMenu(menu, flags, (UIntPtr)id, FormatLabel(check.Label, null));
                    actions[id] = () => check.RaiseToggled(!check.IsChecked);
                    break;
                }
                case TrayMenuItem mi:
                {
                    var id = nextId++;
                    var flags = MF_STRING | (mi.IsEnabled ? 0 : MF_GRAYED);
                    AppendMenu(menu, flags, (UIntPtr)id, FormatLabel(mi.Label, mi.Accelerator));
                    actions[id] = () => mi.RaiseClicked();
                    break;
                }
            }
        }
    }

    static string FormatLabel(string label, string? accelerator)
        => string.IsNullOrEmpty(accelerator) ? label : label + "\t" + accelerator;

    public override void Dispose()
    {
        if (this.disposed) return;
        this.disposed = true;
        base.Dispose();
        if (this.added)
        {
            var data = this.BuildData(0);
            Shell_NotifyIcon(NIM_DELETE, ref data);
        }
        if (this.popupMenu != IntPtr.Zero) DestroyMenu(this.popupMenu);
        if (this.hIcon != IntPtr.Zero) DestroyIcon(this.hIcon);
        lock (Instances) Instances.Remove(this.hwnd);
        DestroyWindow(this.hwnd);
    }
}
