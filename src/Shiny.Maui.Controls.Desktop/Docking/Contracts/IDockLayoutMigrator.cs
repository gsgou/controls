namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Migrates a layout from one schema version to the next. Migrators chain
/// forward only — register one per version step.
/// </summary>
public interface IDockLayoutMigrator
{
    int FromVersion { get; }
    int ToVersion { get; }
    DockRoot Migrate(DockRoot input);
}
