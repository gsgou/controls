namespace Shiny.Maui.Controls.Desktop.TrayIcon;

/// <summary>
/// Create new tray icons. Tray icons are not owned by any specific MAUI Window — the host owns
/// them, so request the factory from DI and dispose icons you no longer need.
/// </summary>
public interface ITrayIconFactory
{
    ITrayIcon Create();
}
