namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// GTK 4 backing for the screen-edge glow: an undecorated full-screen window with an empty input
/// region so the pointer passes straight through it.
/// </summary>
/// <remarks>
/// X11 only. On Wayland a client cannot raise a full-screen overlay above other applications'
/// windows, so the glow would either be invisible behind them or cover them completely with no way
/// to stay on top as focus moves — <see cref="IsSupported"/> is false there rather than shipping
/// something that behaves differently on every compositor.
/// </remarks>
static class ScreenGlowPlatform
{
    public static bool IsSupported => OperatingSystem.IsLinux() && !QuickEntryPlatform.IsWayland;

    // GTK is not thread-safe and the glow service reaches these after an await, so each entry point
    // re-marshals rather than trusting where its continuation resumed.
    public static void Initialize(object platformWindow)
        => QuickEntryPlatform.BeginInvokeOnMainThread(() => InitializeCore(platformWindow));

    static void InitializeCore(object platformWindow)
    {
        var window = GetHandle(platformWindow);
        if (window == IntPtr.Zero)
            return;

        Gtk4Interop.WindowSetDecorated(window, 0);
        Gtk4Interop.WindowSetResizable(window, 0);
        Gtk4Interop.WidgetAddCssClass(window, "shiny-screen-glow");
        Gtk4Styling.EnsureCss(window);
        Gtk4Interop.WidgetSetCanTarget(window, 0);
        Gtk4Interop.WidgetSetVisible(window, 0);
    }

    public static void Show(object platformWindow)
        => QuickEntryPlatform.BeginInvokeOnMainThread(() => ShowCore(platformWindow));

    static void ShowCore(object platformWindow)
    {
        var window = GetHandle(platformWindow);
        if (window == IntPtr.Zero)
            return;

        Gtk4Interop.WidgetSetVisible(window, 1);
        Gtk4Interop.WindowFullscreen(window);
        Gtk4Interop.WindowPresent(window);
        ApplyClickThrough(window);
        RaiseAboveEverything(window);
    }

    public static void Hide(object platformWindow)
        => QuickEntryPlatform.BeginInvokeOnMainThread(() =>
        {
            var window = GetHandle(platformWindow);
            if (window != IntPtr.Zero)
                Gtk4Interop.WidgetSetVisible(window, 0);
        });

    public static void Teardown(object platformWindow) { }

    public static Size GetScreenSize()
    {
        var display = X11Interop.OpenDisplay(null);
        if (display == IntPtr.Zero)
            return new Size(1920, 1080);

        try
        {
            var screen = X11Interop.DefaultScreen(display);
            return new Size(X11Interop.DisplayWidth(display, screen), X11Interop.DisplayHeight(display, screen));
        }
        finally
        {
            X11Interop.CloseDisplay(display);
        }
    }

    /// <summary>
    /// <c>gtk_widget_set_can_target</c> only stops GTK routing events internally; the compositor
    /// still hands the surface the pointer. An empty input region is what actually makes it
    /// click-through.
    /// </summary>
    static void ApplyClickThrough(IntPtr window)
    {
        var surface = Gtk4Interop.NativeGetSurface(window);
        if (surface == IntPtr.Zero)
            return;

        try
        {
            var region = Gtk4Interop.CairoRegionCreate();
            if (region == IntPtr.Zero)
                return;

            Gtk4Interop.SurfaceSetInputRegion(surface, region);
            Gtk4Interop.CairoRegionDestroy(region);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    static void RaiseAboveEverything(IntPtr window)
    {
        var surface = Gtk4Interop.NativeGetSurface(window);
        if (surface == IntPtr.Zero)
            return;

        try
        {
            var xid = Gtk4Interop.X11SurfaceGetXid(surface);
            if (xid == 0)
                return;

            var display = X11Interop.OpenDisplay(null);
            if (display == IntPtr.Zero)
                return;

            X11Interop.SetAlwaysOnTop(display, xid);
            X11Interop.CloseDisplay(display);
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (DllNotFoundException)
        {
        }
    }

    static IntPtr GetHandle(object platformWindow) => QuickEntryPlatform.GetNativeHandleForGlow(platformWindow);
}
