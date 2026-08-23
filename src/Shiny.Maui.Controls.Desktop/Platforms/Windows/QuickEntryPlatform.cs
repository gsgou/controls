using Shiny.Maui.Controls.QuickEntry;
using System.Runtime.Versioning;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using MauiApplication = Microsoft.Maui.Controls.Application;
using WinUIThickness = Microsoft.UI.Xaml.Thickness;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// WinUI 3 backing for the quick entry popup: an <see cref="OverlappedPresenter"/> stripped of its
/// border and title bar, pinned always-on-top, kept out of Alt+Tab, and given an acrylic backdrop
/// so it reads as a HUD rather than a stray dialog.
/// </summary>
[SupportedOSPlatform("windows")]
static class QuickEntryPlatform
{
    sealed class Hooks
    {
        public IDesktopQuickEntryHost Host = null!;
        public IntPtr Hwnd;
        public TypedEventHandler<object, WindowActivatedEventArgs>? Activated;
        public KeyEventHandler? KeyDown;
        public UIElement? KeyRoot;
    }

    static readonly Dictionary<WinUIWindow, Hooks> Registry = new();

    public static bool IsSupported => true;

    public static void BeginInvokeOnMainThread(Action action)
    {
        var dispatcher = MauiApplication.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.IsDispatchRequired == false)
            action();
        else
            dispatcher.Dispatch(action);
    }

    public static void Initialize(IDesktopQuickEntryHost host, object platformWindow, QuickEntryOptions options)
    {
        if (platformWindow is not WinUIWindow window)
            return;

        // Every entry point re-marshals rather than trusting its caller. The service reaches these
        // after an await, and while MAUI does install a synchronisation context on the UI thread,
        // a continuation that resumed anywhere else would hit WinUI with a wrong-thread COM error
        // rather than anything diagnosable.
        BeginInvokeOnMainThread(() => InitializeCore(host, window, options));
    }

    static void InitializeCore(IDesktopQuickEntryHost host, WinUIWindow window, QuickEntryOptions options)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var appWindow = GetAppWindow(window, hwnd);

        if (appWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        if (appWindow != null)
            appWindow.IsShownInSwitchers = options.ShowInTaskbar;

        // WS_EX_TOOLWINDOW is what actually keeps a frameless window out of the taskbar and the
        // Alt+Tab list; AppWindow.IsShownInSwitchers alone does not cover every shell surface.
        if (!options.ShowInTaskbar)
        {
            var ex = (long)QuickEntryInterop.GetWindowLongPtr(hwnd, QuickEntryInterop.GWL_EXSTYLE);
            ex |= QuickEntryInterop.WS_EX_TOOLWINDOW;
            ex &= ~(long)QuickEntryInterop.WS_EX_APPWINDOW;
            QuickEntryInterop.SetWindowLongPtr(hwnd, QuickEntryInterop.GWL_EXSTYLE, (IntPtr)ex);
        }

        // The frame is gone, so the rounded corners have to be asked for explicitly (Windows 11;
        // the call is a harmless no-op on Windows 10).
        var corner = QuickEntryInterop.DWMWCP_ROUND;
        QuickEntryInterop.DwmSetWindowAttribute(hwnd, QuickEntryInterop.DWMWA_WINDOW_CORNER_PREFERENCE, in corner, sizeof(int));

        try
        {
            if (DesktopAcrylicController.IsSupported())
                window.SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        catch
        {
            // Backdrops are best-effort — an unsupported compositor just leaves the window opaque.
        }

        var hooks = new Hooks { Host = host, Hwnd = hwnd };
        hooks.Activated = (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
                host.NotifyDeactivated();
        };
        window.Activated += hooks.Activated;
        Registry[window] = hooks;

        appWindow?.Hide();
    }

    public static void Show(object platformWindow, QuickEntryOptions options, double width, double height)
    {
        if (platformWindow is not WinUIWindow window)
            return;

        BeginInvokeOnMainThread(() => ShowCore(window, options, width, height));
    }

    static void ShowCore(WinUIWindow window, QuickEntryOptions options, double width, double height)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var appWindow = GetAppWindow(window, hwnd);
        if (appWindow == null)
            return;

        appWindow.MoveAndResize(ComputeBounds(hwnd, options, width, height));
        appWindow.Show(options.ActivateOnShow);

        if (options.ActivateOnShow)
        {
            window.Activate();
            QuickEntryInterop.SetForegroundWindow(hwnd);
        }

        AttachKeyHandler(window);
    }

    public static void Hide(object platformWindow)
    {
        if (platformWindow is not WinUIWindow window)
            return;

        BeginInvokeOnMainThread(() =>
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            GetAppWindow(window, hwnd)?.Hide();
        });
    }

    public static void Resize(object platformWindow, QuickEntryOptions options, double width, double height)
    {
        if (platformWindow is not WinUIWindow window)
            return;

        BeginInvokeOnMainThread(() =>
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            GetAppWindow(window, hwnd)?.MoveAndResize(ComputeBounds(hwnd, options, width, height));
        });
    }

    public static void Teardown(object platformWindow)
    {
        if (platformWindow is not WinUIWindow window)
            return;

        if (!Registry.Remove(window, out var hooks))
            return;

        if (hooks.Activated != null)
            window.Activated -= hooks.Activated;
        if (hooks.KeyRoot != null && hooks.KeyDown != null)
            hooks.KeyRoot.RemoveHandler(UIElement.KeyDownEvent, hooks.KeyDown);
    }

    /// <summary>Removes the TextBox chrome so the prompt sits flush on the card.</summary>
    public static void PolishEntry(object? platformView)
    {
        if (platformView is not Microsoft.UI.Xaml.Controls.TextBox box)
            return;

        BeginInvokeOnMainThread(() =>
        {
            box.BorderThickness = new WinUIThickness(0);
            box.Background = null;
            box.Padding = new WinUIThickness(0);
        });
    }

    // -------------------------------------------------------------------------------------

    static AppWindow? GetAppWindow(WinUIWindow window, IntPtr hwnd)
    {
        try
        {
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(id);
        }
        catch
        {
            return window.AppWindow;
        }
    }

    static void AttachKeyHandler(WinUIWindow window)
    {
        if (!Registry.TryGetValue(window, out var hooks) || hooks.KeyRoot != null)
            return;

        if (window.Content is not UIElement root)
            return;

        // handledEventsToo: a TextBox marks the arrow keys handled for caret movement, so a plain
        // KeyDown subscription would never see them — and arrow navigation through the suggestion
        // list is the whole point of intercepting keys here.
        hooks.KeyDown = (_, e) =>
        {
            var key = e.Key switch
            {
                VirtualKey.Escape => (QuickEntryKey?)QuickEntryKey.Escape,
                VirtualKey.Enter => QuickEntryKey.Enter,
                VirtualKey.Up => QuickEntryKey.ArrowUp,
                VirtualKey.Down => QuickEntryKey.ArrowDown,
                VirtualKey.Tab => QuickEntryKey.Tab,
                _ => null
            };

            if (key == null)
                return;

            // Enter is reported but never swallowed — the prompt's own Completed handler is what
            // commits the text.
            if (hooks.Host.NotifyKey(key.Value) && key != QuickEntryKey.Enter)
                e.Handled = true;
        };

        hooks.KeyRoot = root;
        root.AddHandler(UIElement.KeyDownEvent, hooks.KeyDown, true);
    }

    // -------------------------------------------------------------------------------------
    // Placement. AppWindow works in physical pixels while the options are device-independent,
    // so everything is scaled by the target window's DPI.
    // -------------------------------------------------------------------------------------

    static RectInt32 ComputeBounds(IntPtr hwnd, QuickEntryOptions options, double width, double height)
    {
        var dpi = QuickEntryInterop.GetDpiForWindow(hwnd);
        var scale = dpi <= 0 ? 1d : dpi / 96d;

        var w = (int)Math.Round(width * scale);
        var h = (int)Math.Round(height * scale);

        QuickEntryInterop.GetCursorPos(out var cursor);
        var work = GetWorkArea(cursor);
        var workWidth = work.Right - work.Left;
        var workHeight = work.Bottom - work.Top;

        int x, y;
        switch (options.Placement)
        {
            case QuickEntryPlacement.BottomCenter:
                x = work.Left + (workWidth - w) / 2;
                y = work.Bottom - h - (int)Math.Round(workHeight * options.BottomMarginRatio);
                break;

            case QuickEntryPlacement.Center:
                x = work.Left + (workWidth - w) / 2;
                y = work.Top + (workHeight - h) / 2;
                break;

            case QuickEntryPlacement.NearCursor:
                x = cursor.X;
                y = cursor.Y + (int)Math.Round(12 * scale);
                break;

            case QuickEntryPlacement.Manual:
                x = (int)Math.Round(options.X * scale);
                y = (int)Math.Round(options.Y * scale);
                break;

            default:
                x = work.Left + (workWidth - w) / 2;
                y = work.Top + (int)Math.Round(workHeight * options.TopMarginRatio);
                break;
        }

        x = Math.Clamp(x, work.Left, Math.Max(work.Left, work.Right - w));
        y = Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - h));
        return new RectInt32(x, y, w, h);
    }

    static QuickEntryInterop.RECT GetWorkArea(QuickEntryInterop.POINT point)
    {
        var monitor = QuickEntryInterop.MonitorFromPoint(point, QuickEntryInterop.MONITOR_DEFAULTTONEAREST);
        var info = new QuickEntryInterop.MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<QuickEntryInterop.MONITORINFO>() };
        if (monitor != IntPtr.Zero && QuickEntryInterop.GetMonitorInfo(monitor, ref info))
            return info.rcWork;

        return new QuickEntryInterop.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
    }
}
