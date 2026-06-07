namespace Shiny.Blazor.Controls.Docking;

public sealed class DockRoot
{
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public int MinReadableVersion { get; set; } = CurrentSchemaVersion;
    public DockWindowState MainWindow { get; set; } = new();
    public List<DockWindowState> FloatingWindows { get; set; } = new();

    public const int CurrentSchemaVersion = 1;
}
