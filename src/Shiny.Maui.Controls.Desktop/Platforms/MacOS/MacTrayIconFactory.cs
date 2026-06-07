namespace Shiny.Maui.Controls.Desktop.TrayIcon;

sealed class MacTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => MacMainThread.Invoke<ITrayIcon>(() => new MacTrayIcon());
}
