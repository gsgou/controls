namespace Shiny.Maui.Controls.Desktop.TrayIcon;

sealed class LinuxTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new LinuxTrayIcon();
}
