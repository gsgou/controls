using System.Collections;
using System.Collections.ObjectModel;

namespace Shiny.Maui.Controls.Desktop.TrayIcon;

/// <summary>
/// A tray context menu. Mutating Items or any item's properties after the menu has been assigned
/// triggers a rebuild on the platform handler.
/// </summary>
public sealed class TrayMenu
{
    public ObservableCollection<TrayMenuItemBase> Items { get; } = new();

    /// <summary>Fired internally whenever the menu shape changes — handlers subscribe to rebuild.</summary>
    internal event EventHandler? Changed;

    public TrayMenu()
    {
        this.Items.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
                foreach (TrayMenuItemBase i in e.NewItems)
                    this.Adopt(i);
            this.RaiseChanged();
        };
    }

    void Adopt(TrayMenuItemBase item)
    {
        item.Owner = this;
        if (item is TraySubmenu sub)
            foreach (var child in sub.Items)
                this.Adopt(child);
    }

    internal void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);

    public static TrayMenu Build(Action<TrayMenuBuilder> configure)
    {
        var b = new TrayMenuBuilder();
        configure(b);
        return b.Build();
    }
}

public abstract class TrayMenuItemBase
{
    bool isEnabled = true;
    bool isVisible = true;

    public bool IsEnabled
    {
        get => this.isEnabled;
        set { if (this.isEnabled != value) { this.isEnabled = value; this.Owner?.RaiseChanged(); } }
    }

    public bool IsVisible
    {
        get => this.isVisible;
        set { if (this.isVisible != value) { this.isVisible = value; this.Owner?.RaiseChanged(); } }
    }

    internal TrayMenu? Owner { get; set; }
}

public sealed class TrayMenuItem : TrayMenuItemBase
{
    string label = string.Empty;
    string? accelerator;
    Func<Stream>? iconFactory;

    public TrayMenuItem() { }
    public TrayMenuItem(string label, Action? clicked = null)
    {
        this.Label = label;
        if (clicked != null)
            this.Clicked += (_, _) => clicked();
    }

    public string Label
    {
        get => this.label;
        set { if (this.label != value) { this.label = value; this.Owner?.RaiseChanged(); } }
    }

    /// <summary>Cross-platform accelerator string, e.g. "Ctrl+S" / "Cmd+Q". Best-effort.</summary>
    public string? Accelerator
    {
        get => this.accelerator;
        set { if (this.accelerator != value) { this.accelerator = value; this.Owner?.RaiseChanged(); } }
    }

    /// <summary>Optional menu item icon. Stream is re-read per render.</summary>
    public Func<Stream>? Icon
    {
        get => this.iconFactory;
        set { this.iconFactory = value; this.Owner?.RaiseChanged(); }
    }

    public event EventHandler? Clicked;

    internal void RaiseClicked() => this.Clicked?.Invoke(this, EventArgs.Empty);
}

public sealed class TrayCheckMenuItem : TrayMenuItemBase
{
    string label = string.Empty;
    bool isChecked;

    public TrayCheckMenuItem() { }
    public TrayCheckMenuItem(string label, bool isChecked = false)
    {
        this.Label = label;
        this.isChecked = isChecked;
    }

    public string Label
    {
        get => this.label;
        set { if (this.label != value) { this.label = value; this.Owner?.RaiseChanged(); } }
    }

    public bool IsChecked
    {
        get => this.isChecked;
        set { if (this.isChecked != value) { this.isChecked = value; this.Owner?.RaiseChanged(); } }
    }

    public event EventHandler<bool>? Toggled;

    internal void RaiseToggled(bool value)
    {
        this.isChecked = value;
        this.Toggled?.Invoke(this, value);
    }
}

public sealed class TraySeparator : TrayMenuItemBase { }

public sealed class TraySubmenu : TrayMenuItemBase, IEnumerable<TrayMenuItemBase>
{
    string label = string.Empty;
    public ObservableCollection<TrayMenuItemBase> Items { get; } = new();

    public TraySubmenu()
    {
        this.Items.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null && this.Owner != null)
                foreach (TrayMenuItemBase i in e.NewItems)
                    AdoptInto(this.Owner, i);
            this.Owner?.RaiseChanged();
        };
    }

    static void AdoptInto(TrayMenu owner, TrayMenuItemBase item)
    {
        item.Owner = owner;
        if (item is TraySubmenu sub)
            foreach (var child in sub.Items)
                AdoptInto(owner, child);
    }

    public TraySubmenu(string label) : this()
    {
        this.Label = label;
    }

    public string Label
    {
        get => this.label;
        set { if (this.label != value) { this.label = value; this.Owner?.RaiseChanged(); } }
    }

    public void Add(TrayMenuItemBase item) => this.Items.Add(item);

    IEnumerator<TrayMenuItemBase> IEnumerable<TrayMenuItemBase>.GetEnumerator() => this.Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.Items.GetEnumerator();
}
