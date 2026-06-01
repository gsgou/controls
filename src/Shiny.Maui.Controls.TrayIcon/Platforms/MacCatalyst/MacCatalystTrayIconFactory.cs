namespace Shiny.Maui.Controls.TrayIcon;

sealed class MacCatalystTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => new MacCatalystTrayIcon();
}
