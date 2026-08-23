using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Shiny.Maui.Controls.Desktop.TrayIcon;
using static Shiny.Maui.Controls.Desktop.TrayIcon.NativeMethods;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Global hotkeys through <c>RegisterHotKey</c>, delivered to a hidden window created on the UI
/// thread so <c>WM_HOTKEY</c> arrives on the app's existing message loop — no extra thread and no
/// message pump of our own to keep alive.
/// </summary>
[SupportedOSPlatform("windows")]
sealed class WindowsGlobalHotKeyService : IGlobalHotKeyService
{
    static readonly WndProcDelegate StaticWndProc = StaticHandler;
    static readonly Dictionary<IntPtr, WindowsGlobalHotKeyService> Instances = new();
    static IntPtr classAtom;

    readonly Dictionary<int, Action> actions = new();
    IntPtr hwnd;
    int nextId = 0xC001;

    public bool IsSupported => true;

    public IDisposable? Register(string accelerator, Action pressed)
    {
        var parsed = TrayAccelerator.Parse(accelerator);
        if (parsed == null)
            return null;

        var vk = MapVirtualKey(parsed.Key);
        if (vk == null)
            return null;

        uint modifiers = 0;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Control) != 0) modifiers |= MOD_CONTROL;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Alt) != 0) modifiers |= MOD_ALT;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Shift) != 0) modifiers |= MOD_SHIFT;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Meta) != 0) modifiers |= MOD_WIN;

        if (modifiers == 0)
            return null;

        IDisposable? result = null;
        QuickEntryPlatform.BeginInvokeOnMainThread(() =>
        {
            if (!this.EnsureWindow())
                return;

            var id = this.nextId++;
            if (!RegisterHotKey(this.hwnd, id, modifiers, vk.Value))
                return;

            this.actions[id] = pressed;
            result = new Registration(this, id);
        });

        // BeginInvokeOnMainThread runs inline when already on the UI thread, which is where
        // registration happens in practice (startup). Off-thread callers get null and a log line
        // rather than a torn result.
        return result;
    }

    bool EnsureWindow()
    {
        if (this.hwnd != IntPtr.Zero)
            return true;

        if (classAtom == IntPtr.Zero)
        {
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(StaticWndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = "ShinyQuickEntryHotKeyWindow"
            };
            classAtom = new IntPtr(RegisterClassEx(ref wc));
        }

        this.hwnd = CreateWindowEx(0, "ShinyQuickEntryHotKeyWindow", null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (this.hwnd == IntPtr.Zero)
            return false;

        lock (Instances)
            Instances[this.hwnd] = this;
        return true;
    }

    static IntPtr StaticHandler(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            WindowsGlobalHotKeyService? owner;
            lock (Instances)
                Instances.TryGetValue(hwnd, out owner);

            if (owner != null && owner.actions.TryGetValue((int)wParam, out var action))
            {
                try { action(); }
                catch { /* a throwing hotkey handler must not take down the message loop */ }
                return IntPtr.Zero;
            }
        }
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    sealed class Registration : IDisposable
    {
        readonly WindowsGlobalHotKeyService owner;
        readonly int id;
        bool disposed;

        public Registration(WindowsGlobalHotKeyService owner, int id)
        {
            this.owner = owner;
            this.id = id;
        }

        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;

            QuickEntryPlatform.BeginInvokeOnMainThread(() =>
            {
                this.owner.actions.Remove(this.id);
                if (this.owner.hwnd != IntPtr.Zero)
                    UnregisterHotKey(this.owner.hwnd, this.id);
            });
        }
    }

    static uint? MapVirtualKey(string key)
    {
        if (key.Length == 1)
        {
            var c = Char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z')
                return c;
            if (c is >= '0' and <= '9')
                return c;
        }

        if (key.Length >= 2 && (key[0] == 'F' || key[0] == 'f') && Int32.TryParse(key.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
            return (uint)(0x70 + fn - 1);

        return key.ToLowerInvariant() switch
        {
            "space" or "spacebar" => 0x20,
            "enter" or "return" => 0x0D,
            "tab" => 0x09,
            "esc" or "escape" => 0x1B,
            "back" or "backspace" => 0x08,
            "delete" or "del" => 0x2E,
            "insert" or "ins" => 0x2D,
            "home" => 0x24,
            "end" => 0x23,
            "pgup" or "pageup" => 0x21,
            "pgdn" or "pagedown" => 0x22,
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            "," => 0xBC,
            "." => 0xBE,
            "/" => 0xBF,
            ";" => 0xBA,
            "'" => 0xDE,
            "[" => 0xDB,
            "]" => 0xDD,
            "\\" => 0xDC,
            "-" => 0xBD,
            "=" => 0xBB,
            "`" => 0xC0,
            _ => null
        };
    }
}
