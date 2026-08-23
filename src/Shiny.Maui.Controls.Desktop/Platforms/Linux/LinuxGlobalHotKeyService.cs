using Microsoft.Extensions.Logging;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Global hotkeys on Linux, over whichever mechanism the session actually offers: an
/// <c>XGrabKey</c> on X11, or the <c>org.freedesktop.portal.GlobalShortcuts</c> desktop portal on
/// Wayland. Neither available means <see cref="IsSupported"/> is false and every registration
/// declines — a tray icon is the fallback.
/// </summary>
sealed class LinuxGlobalHotKeyService : IGlobalHotKeyService, IDisposable
{
    readonly ILogger? logger;
    readonly ILinuxHotKeyBackend? backend;

    public LinuxGlobalHotKeyService(ILogger<LinuxGlobalHotKeyService>? logger = null)
    {
        this.logger = logger;

        if (!OperatingSystem.IsLinux())
            return;

        if (QuickEntryPlatform.IsWayland)
        {
            this.backend = PortalHotKeyBackend.TryCreate(logger);
            if (this.backend == null)
                logger?.LogWarning("This Wayland compositor does not implement the GlobalShortcuts portal, so no global hotkey can be registered. Open the quick entry popup from a tray icon instead.");
        }
        else
        {
            this.backend = X11HotKeyBackend.TryCreate(logger);
            if (this.backend == null)
                logger?.LogWarning("Could not open an X11 display connection for global hotkeys.");
        }
    }

    public bool IsSupported => this.backend != null;

    public IDisposable? Register(string accelerator, Action pressed)
    {
        if (this.backend == null)
            return null;

        var parsed = TrayAccelerator.Parse(accelerator);
        if (parsed == null)
        {
            this.logger?.LogWarning("Could not parse the accelerator '{Accelerator}'.", accelerator);
            return null;
        }

        // Marshal onto the UI thread here so every backend — including the X11 one, which watches
        // its own connection on a background thread — honours the interface contract.
        return this.backend.Register(parsed, () => QuickEntryPlatform.BeginInvokeOnMainThread(pressed));
    }

    public void Dispose() => (this.backend as IDisposable)?.Dispose();
}

/// <summary>One way of claiming a system-wide key on Linux.</summary>
interface ILinuxHotKeyBackend
{
    IDisposable? Register(TrayAccelerator accelerator, Action pressed);
}
