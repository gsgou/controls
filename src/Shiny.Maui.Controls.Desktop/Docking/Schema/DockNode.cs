using System.Text.Json.Serialization;

namespace Shiny.Maui.Controls.Desktop.Docking;

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

    /// <summary>Position of the splitter as a fraction (0..1) of the parent's extent.</summary>
    public double Ratio { get; set; } = 0.5;

    public DockNode First { get; set; } = new DockEmpty();
    public DockNode Second { get; set; } = new DockEmpty();

    // identifies the rendered splitter across rebuilds; never persisted
    internal string RuntimeId { get; } = Guid.NewGuid().ToString("N");
}

public sealed class DockGroup : DockNode
{
    public string GroupId { get; set; } = Guid.NewGuid().ToString("N");
    public List<DockTab> Tabs { get; set; } = new();
    public int ActiveTabIndex { get; set; }

    /// <summary>Indexes into <see cref="Tabs"/> in most-recently-focused order. Used for Ctrl+Tab MRU.</summary>
    public List<int> FocusHistory { get; set; } = new();

    /// <summary>Collapsed groups render only their tab strip; activating a tab expands.</summary>
    public bool IsCollapsed { get; set; }
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
