using System.Text.Json.Serialization;

namespace Shiny.Blazor.Controls.Docking;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(DockSplit), "split")]
[JsonDerivedType(typeof(DockGroup), "group")]
[JsonDerivedType(typeof(DockEmpty), "empty")]
public abstract class DockNode
{
}

public sealed class DockSplit : DockNode
{
    public DockOrientation Orientation { get; set; }
    public double Ratio { get; set; } = 0.5;
    public DockNode First { get; set; } = new DockEmpty();
    public DockNode Second { get; set; } = new DockEmpty();
}

public sealed class DockGroup : DockNode
{
    public string GroupId { get; set; } = Guid.NewGuid().ToString("N");
    public List<DockTab> Tabs { get; set; } = new();
    public int ActiveTabIndex { get; set; }
    public List<int> FocusHistory { get; set; } = new();
}

public sealed class DockEmpty : DockNode
{
}

public sealed class DockTab
{
    public string PanelTypeId { get; set; } = string.Empty;
    public string PanelInstanceId { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsPinned { get; set; }
}
