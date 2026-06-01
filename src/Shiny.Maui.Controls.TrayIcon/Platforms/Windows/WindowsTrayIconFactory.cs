namespace Shiny.Maui.Controls.TrayIcon;

sealed class WindowsTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new WindowsTrayIcon();
}
