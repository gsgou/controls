using Shiny.Maui.Controls.Camera;
#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
using Microsoft.Maui.Hosting;
#endif

namespace Shiny;

public static class CameraMauiAppBuilderExtensions
{
    /// <summary>
    /// Register the Shiny <see cref="CameraView"/> handler. Call alongside <c>UseShinyControls()</c> in
    /// your MAUI program. Analyzer packages (Barcode/Face/Motion/OCR) are added to a
    /// <see cref="CameraView.Analyzers"/> collection at the call site — no separate registration needed.
    /// </summary>
    public static MauiAppBuilder UseShinyCamera(this MauiAppBuilder builder)
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<CameraView, CameraViewHandler>();
        });
#endif
        return builder;
    }
}
