using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Shiny.Maui.Controls.FloorPlan;

public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers what <see cref="FloorPlanView"/> needs.
    /// </summary>
    /// <remarks>
    /// SkiaSharp, because MAUI will not hand an <c>SKCanvasView</c> a platform view without it: a plan
    /// on a page in an app that forgot this call is a blank rectangle with nothing in the log. On the
    /// macOS AppKit head, also the canvas handler SkiaSharp does not ship for it, registered after
    /// <c>UseSkiaSharp</c> so it replaces the stock one. Safe to call alongside <c>UseShinyOffice</c>
    /// or a hand-written <c>UseSkiaSharp</c> - both registrations are idempotent.
    /// </remarks>
    public static MauiAppBuilder UseShinyFloorPlan(this MauiAppBuilder builder)
    {
        builder.UseSkiaSharp();
#if MACOS
        builder.ConfigureMauiHandlers(handlers =>
            handlers.AddHandler<SkiaSharp.Views.Maui.Controls.SKCanvasView, MacOSSKCanvasViewHandler>());
#endif
        return builder;
    }
}
