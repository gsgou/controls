namespace Shiny.Maui.Controls.TrayIcon;

sealed class MacTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create() => MacMainThread.Invoke<ITrayIcon>(() => new MacTrayIcon());
}
