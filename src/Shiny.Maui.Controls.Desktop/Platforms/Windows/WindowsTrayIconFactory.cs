using System.Runtime.Versioning;

namespace Shiny.Maui.Controls.Desktop.TrayIcon;

[SupportedOSPlatform("windows")]
sealed class WindowsTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new WindowsTrayIcon();
}
