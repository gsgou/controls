namespace Shiny.Maui.Controls.Desktop.Docking;

public sealed class DockRoot
{
    /// <summary>Schema version this layout was written with. Migrators run forward only.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Minimum schema version that can read this layout without migration.</summary>
    public int MinReadableVersion { get; set; } = CurrentSchemaVersion;

    public DockWindowState MainWindow { get; set; } = new();

    /// <summary>Ordered list of floating windows. Order encodes z-order (last = front).</summary>
    public List<DockWindowState> FloatingWindows { get; set; } = new();

    public const int CurrentSchemaVersion = 1;
}
