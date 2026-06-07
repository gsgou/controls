namespace Shiny.Blazor.Controls.Docking;

public interface IDockLayoutMigrator
{
    int FromVersion { get; }
    int ToVersion { get; }
    DockRoot Migrate(DockRoot input);
}
