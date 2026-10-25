using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shiny.Maui.Controls;

/// <summary>
/// The <c>BindingContext</c> of a tab header built from <see cref="ShinyTabBar.TabTemplate"/> or
/// <see cref="ShinyTabItem.HeaderTemplate"/>.
/// </summary>
/// <remarks>
/// <para>The bar keeps owning everything that makes a tab a tab — the tap, selection, the
/// indicator, overflow, automation ids — and a template only replaces what is drawn inside the cell.
/// This is what the bar tells that drawing: the state it would otherwise have painted itself.</para>
/// <para><see cref="Icon"/> is the bar's own realized icon view, tinted and animated exactly as it
/// would be in a default cell. Put it anywhere with <c>&lt;ContentView Content="{Binding Icon}" /&gt;</c>
/// and the selection animation and motion playback come along with it; ignore it and draw your own
/// artwork from <see cref="Item"/> instead.</para>
/// </remarks>
public sealed class TabHeaderContext : INotifyPropertyChanged
{
    internal TabHeaderContext(ShinyTabItem item, bool isOverflow)
    {
        this.Item = item;
        this.IsOverflow = isOverflow;
    }

    /// <summary>The tab this header draws. For the synthesized <b>More</b> tab, a tab the bar made up.</summary>
    public ShinyTabItem Item { get; }

    /// <summary>True for the synthesized <b>More</b> tab that opens the overflow menu.</summary>
    public bool IsOverflow { get; }

    string? title;
    /// <summary>The label the bar resolved — the page's <see cref="ShinyTabs.TitleProperty"/> first, then the tab's.</summary>
    public string? Title
    {
        get => this.title;
        internal set => this.Set(ref this.title, value);
    }

    bool isSelected;
    /// <summary>True while this is the selected tab.</summary>
    public bool IsSelected
    {
        get => this.isSelected;
        internal set => this.Set(ref this.isSelected, value);
    }

    bool isEnabled = true;
    /// <summary>Mirrors <see cref="ShinyTabItem.IsEnabled"/>. The bar already dims a disabled cell.</summary>
    public bool IsEnabled
    {
        get => this.isEnabled;
        internal set => this.Set(ref this.isEnabled, value);
    }

    bool isLabelVisible;
    /// <summary>
    /// Whether the bar would have shown a label here, from <see cref="ShinyTabBar.LabelMode"/> and
    /// <see cref="TabLabelPosition.Hidden"/> — so a template can honour the bar's label rules
    /// without re-implementing them.
    /// </summary>
    public bool IsLabelVisible
    {
        get => this.isLabelVisible;
        internal set => this.Set(ref this.isLabelVisible, value);
    }

    TabLabelPosition labelPosition = TabLabelPosition.Bottom;
    /// <summary>The resolved label position — never <see cref="TabLabelPosition.Default"/>.</summary>
    public TabLabelPosition LabelPosition
    {
        get => this.labelPosition;
        internal set => this.Set(ref this.labelPosition, value);
    }

    Color? color;
    /// <summary>
    /// The explicit <see cref="ShinyTabBar.SelectedColor"/> or <see cref="ShinyTabBar.UnselectedColor"/>
    /// for the current state. Null when the bar is following the theme.
    /// </summary>
    public Color? Color
    {
        get => this.color;
        internal set => this.Set(ref this.color, value);
    }

    string? badge;
    /// <summary>The resolved badge: null for none, empty for a dot, otherwise the text.</summary>
    public string? Badge
    {
        get => this.badge;
        internal set
        {
            if (this.Set(ref this.badge, value))
            {
                this.OnPropertyChanged(nameof(HasBadge));
                this.OnPropertyChanged(nameof(IsBadgeDot));
            }
        }
    }

    /// <summary>True when there is a badge of any kind to draw.</summary>
    public bool HasBadge => this.badge is not null;

    /// <summary>True when the badge is a dot rather than text.</summary>
    public bool IsBadgeDot => this.badge is { Length: 0 };

    Color? badgeColor;
    /// <summary>The resolved badge colour. Null follows the theme's error colour.</summary>
    public Color? BadgeColor
    {
        get => this.badgeColor;
        internal set => this.Set(ref this.badgeColor, value);
    }

    View? icon;
    /// <summary>
    /// The bar's realized icon view — a <see cref="MotionIconView"/> or an <see cref="Image"/> —
    /// or null when the tab declares no icon. It can only be in one place, so bind it once.
    /// </summary>
    public View? Icon
    {
        get => this.icon;
        internal set => this.Set(ref this.icon, value);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    bool Set<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        this.OnPropertyChanged(name);
        return true;
    }
}
