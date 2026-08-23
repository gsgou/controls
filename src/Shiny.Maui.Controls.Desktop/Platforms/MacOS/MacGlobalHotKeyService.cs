using System.Runtime.InteropServices;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Global hotkeys through the Carbon <c>RegisterEventHotKey</c> API.
/// </summary>
/// <remarks>
/// Carbon is the only route to a system-wide hotkey on macOS that does not require the user to
/// grant Accessibility permission first — an <c>NSEvent</c> global monitor does, and silently
/// delivers nothing until they do, which is a miserable failure mode for a hotkey. The API is
/// ancient but has never been deprecated for this use and is what every macOS launcher still uses.
/// </remarks>
sealed unsafe partial class MacGlobalHotKeyService : IGlobalHotKeyService
{
    const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";

    const uint EventClassKeyboard = 0x6B657962;  // 'keyb'
    const uint EventHotKeyPressed = 5;
    const uint EventParamDirectObject = 0x2D2D2D2D;  // '----'
    const uint TypeEventHotKeyID = 0x686B6964;       // 'hkid'
    const uint HotKeySignature = 0x53686E79;         // 'Shny'

    const uint CmdKey = 0x0100;
    const uint ShiftKey = 0x0200;
    const uint OptionKey = 0x0800;
    const uint ControlKey = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    struct EventHotKeyID
    {
        public uint Signature;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct EventTypeSpec
    {
        public uint EventClass;
        public uint EventKind;
    }

    [LibraryImport(Carbon)]
    private static partial int RegisterEventHotKey(uint code, uint modifiers, EventHotKeyID id, IntPtr target, uint options, out IntPtr outRef);

    [LibraryImport(Carbon)]
    private static partial int UnregisterEventHotKey(IntPtr hotKeyRef);

    [LibraryImport(Carbon)]
    private static partial IntPtr GetApplicationEventTarget();

    [LibraryImport(Carbon)]
    private static partial int InstallEventHandler(IntPtr target, IntPtr handler, uint count, in EventTypeSpec types, IntPtr userData, out IntPtr outRef);

    [LibraryImport(Carbon)]
    private static partial int GetEventParameter(IntPtr evt, uint name, uint type, IntPtr outActualType, uint bufferSize, IntPtr outActualSize, out EventHotKeyID data);

    static readonly Dictionary<uint, Action> Handlers = new();
    static readonly object Gate = new();
    static bool handlerInstalled;
    static uint nextId = 1;

    public bool IsSupported => true;

    public IDisposable? Register(string accelerator, Action pressed)
    {
        var parsed = TrayAccelerator.Parse(accelerator);
        if (parsed == null)
            return null;

        var code = MapKeyCode(parsed.Key);
        if (code == null)
            return null;

        uint modifiers = 0;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Control) != 0) modifiers |= ControlKey;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Alt) != 0) modifiers |= OptionKey;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Shift) != 0) modifiers |= ShiftKey;
        if ((parsed.Modifiers & TrayAcceleratorModifiers.Meta) != 0) modifiers |= CmdKey;

        // Carbon refuses a bare key with no modifiers, and claiming one would hijack that key
        // system-wide anyway.
        if (modifiers == 0)
            return null;

        return MacMainThread.Invoke<IDisposable?>(() =>
        {
            lock (Gate)
            {
                if (!EnsureHandlerInstalled())
                    return null;

                var id = nextId++;
                var hotKeyId = new EventHotKeyID { Signature = HotKeySignature, Id = id };
                var status = RegisterEventHotKey(code.Value, modifiers, hotKeyId, GetApplicationEventTarget(), 0, out var handle);
                if (status != 0 || handle == IntPtr.Zero)
                    return null;

                Handlers[id] = pressed;
                return new Registration(id, handle);
            }
        });
    }

    static bool EnsureHandlerInstalled()
    {
        if (handlerInstalled)
            return true;

        var spec = new EventTypeSpec { EventClass = EventClassKeyboard, EventKind = EventHotKeyPressed };
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, int> callback = &OnHotKey;
        var status = InstallEventHandler(GetApplicationEventTarget(), (IntPtr)callback, 1, in spec, IntPtr.Zero, out _);
        handlerInstalled = status == 0;
        return handlerInstalled;
    }

    [UnmanagedCallersOnly]
    static int OnHotKey(IntPtr callRef, IntPtr evt, IntPtr userData)
    {
        try
        {
            if (GetEventParameter(evt, EventParamDirectObject, TypeEventHotKeyID, IntPtr.Zero, (uint)sizeof(EventHotKeyID), IntPtr.Zero, out var id) != 0)
                return 0;

            Action? action;
            lock (Gate)
                Handlers.TryGetValue(id.Id, out action);

            action?.Invoke();
        }
        catch
        {
            // Never let a managed exception unwind into the Carbon event loop — it takes the
            // process with it.
        }
        return 0;
    }

    sealed class Registration : IDisposable
    {
        readonly uint id;
        IntPtr handle;

        public Registration(uint id, IntPtr handle)
        {
            this.id = id;
            this.handle = handle;
        }

        public void Dispose()
        {
            if (this.handle == IntPtr.Zero)
                return;

            var h = this.handle;
            this.handle = IntPtr.Zero;
            MacMainThread.Invoke(() =>
            {
                lock (Gate)
                    Handlers.Remove(this.id);
                UnregisterEventHotKey(h);
            });
        }
    }

    /// <summary>
    /// Maps an accelerator key token onto a Carbon virtual key code. These are positional codes for
    /// the physical ANSI layout, not characters, which is why they are a table rather than
    /// arithmetic on the letter.
    /// </summary>
    static uint? MapKeyCode(string key) => key.ToLowerInvariant() switch
    {
        "a" => 0, "s" => 1, "d" => 2, "f" => 3, "h" => 4, "g" => 5, "z" => 6, "x" => 7,
        "c" => 8, "v" => 9, "b" => 11, "q" => 12, "w" => 13, "e" => 14, "r" => 15,
        "y" => 16, "t" => 17, "1" => 18, "2" => 19, "3" => 20, "4" => 21, "6" => 22,
        "5" => 23, "=" => 24, "9" => 25, "7" => 26, "-" => 27, "8" => 28, "0" => 29,
        "]" => 30, "o" => 31, "u" => 32, "[" => 33, "i" => 34, "p" => 35, "l" => 37,
        "j" => 38, "'" => 39, "k" => 40, ";" => 41, "\\" => 42, "," => 43, "/" => 44,
        "n" => 45, "m" => 46, "." => 47, "`" => 50,
        "enter" or "return" => 36,
        "tab" => 48,
        "space" or "spacebar" => 49,
        "back" or "backspace" => 51,
        "esc" or "escape" => 53,
        "delete" or "del" => 117,
        "home" => 115,
        "end" => 119,
        "pgup" or "pageup" => 116,
        "pgdn" or "pagedown" => 121,
        "left" => 123,
        "right" => 124,
        "down" => 125,
        "up" => 126,
        "f1" => 122, "f2" => 120, "f3" => 99, "f4" => 118, "f5" => 96, "f6" => 97,
        "f7" => 98, "f8" => 100, "f9" => 101, "f10" => 109, "f11" => 103, "f12" => 111,
        "f13" => 105, "f14" => 107, "f15" => 113, "f16" => 106, "f17" => 64, "f18" => 79,
        "f19" => 80, "f20" => 90,
        _ => null
    };
}
