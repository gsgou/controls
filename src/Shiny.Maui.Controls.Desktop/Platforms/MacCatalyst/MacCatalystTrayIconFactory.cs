namespace Shiny.Maui.Controls.Desktop.TrayIcon;

sealed class MacCatalystTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new MacCatalystTrayIcon();
}
