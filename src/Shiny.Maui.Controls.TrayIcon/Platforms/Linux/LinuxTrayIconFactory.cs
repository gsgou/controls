namespace Shiny.Maui.Controls.TrayIcon;

sealed class LinuxTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new LinuxTrayIcon();
}
