namespace Shiny.Blazor.Controls.Docking;

public sealed class DockableContentRegistry
{
    readonly Dictionary<string, IDockableContentFactory> map;

    public DockableContentRegistry(IEnumerable<IDockableContentFactory> factories)
    {
        map = new Dictionary<string, IDockableContentFactory>(StringComparer.Ordinal);
        foreach (var f in factories)
            map[f.PanelTypeId] = f;
    }

    public IDockableContentFactory? Resolve(string panelTypeId)
        => map.TryGetValue(panelTypeId, out var f) ? f : null;

    public IReadOnlyCollection<IDockableContentFactory> All => map.Values;
}
