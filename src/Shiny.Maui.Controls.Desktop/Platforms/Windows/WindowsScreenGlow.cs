using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Shiny.Maui.Controls.Desktop.TrayIcon;
using Shiny.Maui.Controls.QuickEntry;
using static Shiny.Maui.Controls.Desktop.QuickEntry.QuickEntryInterop;
using SDColor = System.Drawing.Color;
using SDPointF = System.Drawing.PointF;
using SDSize = System.Drawing.Size;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Screen-edge glow on Windows, rendered with GDI+ into four layered Win32 windows — one per screen
/// edge.
/// </summary>
/// <remarks>
/// <para>
/// A WinUI 3 window has no per-pixel alpha, so the transparent-MAUI-window approach the macOS and
/// Linux backends use is simply not available here. A layered window updated through
/// <c>UpdateLayeredWindow</c> is the supported way to get a genuinely see-through, click-through
/// overlay on Windows, and it takes a premultiplied ARGB bitmap — which means rendering the frame
/// ourselves rather than through the MAUI visual tree.
/// </para>
/// <para>
/// Four edge-shaped windows rather than one full-screen one is a deliberate cost decision:
/// <c>UpdateLayeredWindow</c> pushes the entire bitmap every frame, and on a 1440p display a
/// full-screen surface is ~15 MB per frame where the four bands together are closer to 3 MB. The
/// visible result is identical, because only the bands are ever painted.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
sealed class WindowsScreenGlow : IScreenGlowPresenter, IDisposable
{
    enum Edge { Top, Bottom, Left, Right }

    sealed class Band
    {
        public Edge Edge;
        public IntPtr Hwnd;
        public Rectangle Bounds;      // screen coordinates, physical pixels
        public Bitmap? Surface;
        public Graphics? Canvas;
    }

    static readonly NativeMethods.WndProcDelegate StaticWndProc = (h, m, w, l) => NativeMethods.DefWindowProc(h, m, w, l);
    static bool classRegistered;

    readonly ILogger? logger;
    readonly Band[] bands = { new() { Edge = Edge.Top }, new() { Edge = Edge.Bottom }, new() { Edge = Edge.Left }, new() { Edge = Edge.Right } };
    readonly object gate = new();

    Thread? renderThread;
    volatile bool running;
    volatile bool wantVisible;
    double phase;
    double colorPhase;
    double pulsePhase;
    double fade;
    Rectangle screen;
    double scale = 1d;

    public WindowsScreenGlow(ILogger<WindowsScreenGlow>? logger = null)
        => this.logger = logger;

    /// <summary>The options handed in on each call, so a change between opens is picked up by the render loop.</summary>
    ScreenGlowOptions Options { get; set; } = new();

    public QuickEntryPresentation Kind => QuickEntryPresentation.Desktop;

    public bool IsSupported => true;

    public Task ShowAsync(ScreenGlowOptions options)
    {
        this.Options = options;
        if (!this.wantVisible)
        {
            this.wantVisible = true;
            this.EnsureRenderThread();
        }
        return Task.CompletedTask;
    }

    public Task HideAsync(ScreenGlowOptions options)
    {
        this.wantVisible = false;
        return Task.CompletedTask;
    }

    void EnsureRenderThread()
    {
        lock (this.gate)
        {
            if (this.renderThread != null)
                return;

            this.running = true;
            this.renderThread = new Thread(this.RenderLoop)
            {
                IsBackground = true,
                Name = "Shiny Screen Glow"
            };
            // The bands are created on this thread and never receive input, so they only need the
            // token message drain the loop does; keeping them off the UI thread means a heavy frame
            // can never stall the app's own rendering.
            this.renderThread.SetApartmentState(ApartmentState.STA);
            this.renderThread.Start();
        }
    }

    void RenderLoop()
    {
        try
        {
            this.CreateBands();
            var fps = Math.Clamp(this.Options.FrameRate, 5, 120);
            var frame = TimeSpan.FromMilliseconds(1000d / fps);
            var fadeStep = this.Options.FadeDuration.TotalMilliseconds <= 0
                ? 1d
                : frame.TotalMilliseconds / this.Options.FadeDuration.TotalMilliseconds;

            while (this.running)
            {
                while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                    DispatchMessage(ref msg);

                var target = this.wantVisible ? 1d : 0d;
                if (this.fade < target)
                    this.fade = Math.Min(target, this.fade + fadeStep);
                else if (this.fade > target)
                    this.fade = Math.Max(target, this.fade - fadeStep);

                if (this.fade > 0d)
                {
                    var seconds = 1d / fps;
                    this.phase += this.Options.Speed * seconds;
                    if (this.Options.ColorCycleSeconds > 0)
                        this.colorPhase += seconds / this.Options.ColorCycleSeconds;
                    if (this.Options.PulseSeconds > 0)
                        this.pulsePhase += seconds / this.Options.PulseSeconds;

                    this.RenderFrame();
                    this.SetBandsVisible(true);
                }
                else
                {
                    this.SetBandsVisible(false);
                }

                Thread.Sleep(frame);
            }
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "The screen glow render loop stopped");
        }
        finally
        {
            this.DestroyBands();
        }
    }

    // -------------------------------------------------------------------------------------

    void CreateBands()
    {
        if (!classRegistered)
        {
            var wc = new NativeMethods.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(StaticWndProc),
                hInstance = NativeMethods.GetModuleHandle(null),
                lpszClassName = "ShinyScreenGlowWindow"
            };
            NativeMethods.RegisterClassEx(ref wc);
            classRegistered = true;
        }

        this.screen = this.GetActiveScreenBounds();

        const int exStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        foreach (var band in this.bands)
        {
            band.Hwnd = NativeMethods.CreateWindowEx(
                (uint)exStyle, "ShinyScreenGlowWindow", null, WS_POPUP,
                0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, NativeMethods.GetModuleHandle(null), IntPtr.Zero
            );
        }

        var dpi = GetDpiForWindow(this.bands[0].Hwnd);
        this.scale = dpi <= 0 ? 1d : dpi / 96d;
        this.LayoutBands();
    }

    void LayoutBands()
    {
        // Deep enough to hold a whole pool, not just Thickness: the bands are the only surface the
        // glow has, so a band that stops at Thickness slices the pool off while it is still
        // painting and leaves a hard line running down the screen.
        var thickness = Math.Max(1, (int)Math.Ceiling(ScreenGlowGeometry.RadiusFor(this.Options.Thickness) * this.scale));
        thickness = Math.Min(thickness, Math.Min(this.screen.Width, this.screen.Height) / 2);

        var middleHeight = Math.Max(0, this.screen.Height - thickness * 2);
        foreach (var band in this.bands)
        {
            band.Bounds = band.Edge switch
            {
                Edge.Top => new Rectangle(this.screen.Left, this.screen.Top, this.screen.Width, thickness),
                Edge.Bottom => new Rectangle(this.screen.Left, this.screen.Bottom - thickness, this.screen.Width, thickness),
                Edge.Left => new Rectangle(this.screen.Left, this.screen.Top + thickness, thickness, middleHeight),
                _ => new Rectangle(this.screen.Right - thickness, this.screen.Top + thickness, thickness, middleHeight)
            };

            band.Canvas?.Dispose();
            band.Surface?.Dispose();
            band.Canvas = null;
            band.Surface = null;

            if (band.Bounds.Width <= 0 || band.Bounds.Height <= 0)
                continue;

            band.Surface = new Bitmap(band.Bounds.Width, band.Bounds.Height, PixelFormat.Format32bppArgb);
            band.Canvas = Graphics.FromImage(band.Surface);
            band.Canvas.SmoothingMode = SmoothingMode.AntiAlias;
            band.Canvas.CompositingQuality = CompositingQuality.HighQuality;
            band.Canvas.InterpolationMode = InterpolationMode.HighQualityBilinear;

            SetWindowPos(band.Hwnd, HWND_TOPMOST, band.Bounds.Left, band.Bounds.Top, band.Bounds.Width, band.Bounds.Height, SWP_NOACTIVATE);
        }
    }

    void RenderFrame()
    {
        var thicknessPx = Math.Max(1d, this.Options.Thickness * this.scale);
        var blobs = ScreenGlowGeometry.Compute(this.screen.Width, this.screen.Height, thicknessPx, this.phase, this.colorPhase, this.Options);
        var layers = Math.Clamp(this.Options.Layers, 1, 6);

        var pulseDepth = Math.Clamp(this.Options.PulseDepth, 0d, 1d);
        var breath = this.Options.PulseSeconds <= 0 || pulseDepth <= 0
            ? 1d
            : 1d - pulseDepth * (0.5d - 0.5d * Math.Cos(this.pulsePhase * Math.PI * 2));

        // Split across the passes so stacking them lands near Intensity rather than several times
        // over it — the pools overlap each other along the edge as well.
        var baseAlpha = Math.Clamp(this.Options.Intensity, 0d, 1d) * breath * this.fade * 0.45d / layers;

        foreach (var band in this.bands)
        {
            if (band.Canvas == null || band.Surface == null)
                continue;

            band.Canvas.Clear(SDColor.Transparent);

            for (var layer = 0; layer < layers; layer++)
            {
                // Each pass is a tighter pool over the same centres, so the colour piles up at the
                // edge and thins out inward. Clipping each pass a little further in, which is the
                // obvious way to build the same falloff, cannot work: a clip has a hard edge, so
                // every pass leaves a visible rectangle outline across the screen.
                var tighten = 1f - layer / (layers * 1.5f);

                foreach (var blob in blobs)
                {
                    // Blobs are positioned in screen space; each band draws the part of them that
                    // reaches into its own rectangle.
                    var cx = blob.X - (band.Bounds.Left - this.screen.Left);
                    var cy = blob.Y - (band.Bounds.Top - this.screen.Top);
                    var r = blob.Radius * tighten;
                    if (r <= 0)
                        continue;

                    if (cx + r < 0 || cy + r < 0 || cx - r > band.Bounds.Width || cy - r > band.Bounds.Height)
                        continue;

                    var alpha = (int)Math.Round(Math.Clamp(baseAlpha, 0d, 1d) * 255);
                    if (alpha <= 0)
                        continue;

                    var core = SDColor.FromArgb(alpha, ToByte(blob.Color.Red), ToByte(blob.Color.Green), ToByte(blob.Color.Blue));
                    using var path = new GraphicsPath();
                    path.AddEllipse(cx - r, cy - r, r * 2, r * 2);

                    using var brush = new PathGradientBrush(path)
                    {
                        CenterColor = core,
                        SurroundColors = new[] { SDColor.FromArgb(0, core) },
                        CenterPoint = new SDPointF(cx, cy)
                    };
                    band.Canvas.FillPath(brush, path);
                }
            }

            this.Push(band);
        }
    }

    void Push(Band band)
    {
        if (band.Surface == null)
            return;

        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var hBitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;

        try
        {
            hBitmap = band.Surface.GetHbitmap(SDColor.FromArgb(0));
            previous = SelectObject(memoryDc, hBitmap);

            var destination = new POINT { X = band.Bounds.Left, Y = band.Bounds.Top };
            var size = new SIZE { Width = band.Bounds.Width, Height = band.Bounds.Height };
            var source = new POINT { X = 0, Y = 0 };
            var blend = new BLENDFUNCTION
            {
                BlendOp = AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = AC_SRC_ALPHA
            };

            UpdateLayeredWindow(band.Hwnd, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, ULW_ALPHA);
        }
        finally
        {
            if (previous != IntPtr.Zero)
                SelectObject(memoryDc, previous);
            if (hBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(hBitmap);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    bool bandsVisible;

    void SetBandsVisible(bool visible)
    {
        if (this.bandsVisible == visible)
            return;

        this.bandsVisible = visible;
        foreach (var band in this.bands)
        {
            if (band.Hwnd == IntPtr.Zero)
                continue;

            if (visible)
                SetWindowPos(band.Hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            else
                ShowWindow(band.Hwnd, SW_HIDE);
        }
    }

    Rectangle GetActiveScreenBounds()
    {
        GetCursorPos(out var cursor);
        var monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
        {
            var m = info.rcMonitor;
            return new Rectangle(m.Left, m.Top, m.Right - m.Left, m.Bottom - m.Top);
        }

        return new Rectangle(0, 0, NativeMethods.GetSystemMetrics(0), NativeMethods.GetSystemMetrics(1));
    }

    void DestroyBands()
    {
        foreach (var band in this.bands)
        {
            band.Canvas?.Dispose();
            band.Surface?.Dispose();
            band.Canvas = null;
            band.Surface = null;

            if (band.Hwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(band.Hwnd);
                band.Hwnd = IntPtr.Zero;
            }
        }
    }

    static byte ToByte(float channel) => (byte)Math.Clamp((int)Math.Round(channel * 255), 0, 255);

    public void Teardown()
    {
        this.wantVisible = false;
        this.running = false;
        this.renderThread?.Join(TimeSpan.FromSeconds(1));
        this.renderThread = null;
    }

    public void Dispose() => this.Teardown();
}
