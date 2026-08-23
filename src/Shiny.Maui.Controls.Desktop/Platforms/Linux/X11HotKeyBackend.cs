using Microsoft.Extensions.Logging;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// X11 global hotkeys via <c>XGrabKey</c> on the root window, watched on a dedicated display
/// connection.
/// </summary>
/// <remarks>
/// The watcher gets its own <c>Display*</c> rather than sharing GTK's: Xlib connections are not
/// thread-safe, and grabs have to be read from a loop that would otherwise be competing with the
/// toolkit for events on the same socket.
/// </remarks>
sealed unsafe class X11HotKeyBackend : ILinuxHotKeyBackend, IDisposable
{
    // Every grab is repeated with each combination of the lock modifiers, because X reports Caps
    // Lock and Num Lock in the same state mask as the real modifiers — a single grab silently
    // stops matching the moment either is on.
    static readonly uint[] LockCombinations =
    {
        0,
        X11Interop.LockMask,
        X11Interop.Mod2Mask,
        X11Interop.LockMask | X11Interop.Mod2Mask
    };

    readonly ILogger? logger;
    readonly IntPtr display;
    readonly IntPtr root;
    readonly Dictionary<(int KeyCode, uint Modifiers), Action> handlers = new();
    readonly object gate = new();
    readonly CancellationTokenSource cancellation = new();
    Thread? watcher;

    X11HotKeyBackend(IntPtr display, IntPtr root, ILogger? logger)
    {
        this.display = display;
        this.root = root;
        this.logger = logger;
    }

    public static X11HotKeyBackend? TryCreate(ILogger? logger)
    {
        try
        {
            var display = X11Interop.OpenDisplay(null);
            if (display == IntPtr.Zero)
                return null;

            var root = X11Interop.DefaultRootWindow(display);
            X11Interop.SelectInput(display, root, X11Interop.KeyPressMask);
            return new X11HotKeyBackend(display, root, logger);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }

    public IDisposable? Register(TrayAccelerator accelerator, Action pressed)
    {
        var keysymName = MapKeysym(accelerator.Key);
        if (keysymName == null)
            return null;

        var keysym = X11Interop.StringToKeysym(keysymName);
        if (keysym == IntPtr.Zero)
            return null;

        var keycode = X11Interop.KeysymToKeycode(this.display, keysym);
        if (keycode == 0)
            return null;

        uint modifiers = 0;
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Control) != 0) modifiers |= X11Interop.ControlMask;
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Alt) != 0) modifiers |= X11Interop.Mod1Mask;
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Shift) != 0) modifiers |= X11Interop.ShiftMask;
        if ((accelerator.Modifiers & TrayAcceleratorModifiers.Meta) != 0) modifiers |= X11Interop.Mod4Mask;

        if (modifiers == 0)
            return null;

        // A grab the server refuses (another client owns the combination) arrives asynchronously as
        // a BadAccess, and Xlib's default handler terminates the process for it. Swapping in a
        // no-op handler across the grab turns that into the "could not register" result the
        // interface documents.
        var previousHandler = X11Interop.SetErrorHandler((IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&IgnoreError);
        try
        {
            foreach (var extra in LockCombinations)
                X11Interop.GrabKey(this.display, keycode, modifiers | extra, this.root, 0, X11Interop.GrabModeAsync, X11Interop.GrabModeAsync);

            X11Interop.Sync(this.display, 0);
        }
        finally
        {
            X11Interop.SetErrorHandler(previousHandler);
        }

        lock (this.gate)
            this.handlers[(keycode, modifiers)] = pressed;

        this.EnsureWatcher();
        return new Registration(this, keycode, modifiers);
    }

    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    static int IgnoreError(IntPtr display, IntPtr errorEvent) => 0;

    void EnsureWatcher()
    {
        if (this.watcher != null)
            return;

        this.watcher = new Thread(this.Watch)
        {
            IsBackground = true,
            Name = "Shiny X11 HotKeys"
        };
        this.watcher.Start();
    }

    void Watch()
    {
        var token = this.cancellation.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Poll rather than block in XNextEvent: a blocked call cannot be woken for shutdown
                // without a second connection to inject an event through, and 30ms of latency on a
                // hotkey is imperceptible.
                if (X11Interop.Pending(this.display) == 0)
                {
                    Thread.Sleep(30);
                    continue;
                }

                X11Interop.NextEvent(this.display, out var evt);
                if (evt.Type != X11Interop.KeyPress)
                    continue;

                var (state, keycode) = evt.AsKey();
                var modifiers = state & ~(X11Interop.LockMask | X11Interop.Mod2Mask);

                Action? action;
                lock (this.gate)
                    this.handlers.TryGetValue(((int)keycode, modifiers), out action);

                action?.Invoke();
            }
            catch (Exception ex)
            {
                this.logger?.LogDebug(ex, "X11 hotkey watcher error");
                Thread.Sleep(200);
            }
        }
    }

    void Unregister(int keycode, uint modifiers)
    {
        lock (this.gate)
            this.handlers.Remove((keycode, modifiers));

        foreach (var extra in LockCombinations)
            X11Interop.UngrabKey(this.display, keycode, modifiers | extra, this.root);

        X11Interop.Flush(this.display);
    }

    public void Dispose()
    {
        this.cancellation.Cancel();
        this.watcher?.Join(TimeSpan.FromMilliseconds(500));
        this.watcher = null;

        if (this.display != IntPtr.Zero)
            X11Interop.CloseDisplay(this.display);

        this.cancellation.Dispose();
    }

    sealed class Registration : IDisposable
    {
        readonly X11HotKeyBackend owner;
        readonly int keycode;
        readonly uint modifiers;
        bool disposed;

        public Registration(X11HotKeyBackend owner, int keycode, uint modifiers)
        {
            this.owner = owner;
            this.keycode = keycode;
            this.modifiers = modifiers;
        }

        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;
            this.owner.Unregister(this.keycode, this.modifiers);
        }
    }

    /// <summary>Maps an accelerator key token onto an X keysym name for <c>XStringToKeysym</c>.</summary>
    internal static string? MapKeysym(string key)
    {
        if (key.Length == 1)
        {
            var c = key[0];
            if (Char.IsAsciiLetter(c))
                return Char.ToLowerInvariant(c).ToString();
            if (Char.IsAsciiDigit(c))
                return c.ToString();
        }

        if (key.Length >= 2 && (key[0] == 'F' || key[0] == 'f') && Int32.TryParse(key.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
            return "F" + fn;

        return key.ToLowerInvariant() switch
        {
            "space" or "spacebar" => "space",
            "enter" or "return" => "Return",
            "tab" => "Tab",
            "esc" or "escape" => "Escape",
            "back" or "backspace" => "BackSpace",
            "delete" or "del" => "Delete",
            "insert" or "ins" => "Insert",
            "home" => "Home",
            "end" => "End",
            "pgup" or "pageup" => "Prior",
            "pgdn" or "pagedown" => "Next",
            "left" => "Left",
            "up" => "Up",
            "right" => "Right",
            "down" => "Down",
            "," => "comma",
            "." => "period",
            "/" => "slash",
            ";" => "semicolon",
            "'" => "apostrophe",
            "[" => "bracketleft",
            "]" => "bracketright",
            "\\" => "backslash",
            "-" => "minus",
            "=" => "equal",
            "`" => "grave",
            _ => null
        };
    }
}
