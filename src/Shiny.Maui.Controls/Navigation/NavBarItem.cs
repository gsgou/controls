using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// A <see cref="ToolbarItem"/> that can also carry motion artwork and a badge — what
/// <see cref="ShinyNavBar"/> draws on either side of the title.
/// </summary>
/// <remarks>
/// <para>
/// It derives from <see cref="ToolbarItem"/> on purpose rather than replacing it. Text, icon,
/// command, <see cref="MenuItem.IsEnabled"/>, <see cref="MenuItem.IsDestructive"/>,
/// <see cref="MenuItem.Clicked"/>, <see cref="ToolbarItem.Order"/> and
/// <see cref="ToolbarItem.Priority"/> all mean exactly what they already mean, so an existing
/// <c>ToolbarItem</c> can be moved into <see cref="ShinyNav.LeftItemsProperty"/> unchanged and a
/// page's own <c>ToolbarItems</c> render without being rewritten. This adds only what a
/// <c>ToolbarItem</c> has no way to say.
/// </para>
/// <para>
/// <see cref="ToolbarItem.Order"/> keeps its meaning too:
/// <see cref="ToolbarItemOrder.Secondary"/> puts the item in the overflow menu however much room
/// the bar has, and <see cref="ToolbarItem.Priority"/> orders the items within their group.
/// </para>
/// </remarks>
/// <example>
/// <code language="xaml">
/// &lt;shiny:ShinyNav.RightItems&gt;
///     &lt;shiny:NavBarItem Icon="search" Command="{Binding SearchCommand}" /&gt;
///     &lt;shiny:NavBarItem Icon="bell" Badge="3" Command="{Binding AlertsCommand}" /&gt;
///     &lt;shiny:NavBarItem Text="Archive" Order="Secondary" Command="{Binding ArchiveCommand}" /&gt;
/// &lt;/shiny:ShinyNav.RightItems&gt;
/// </code>
/// </example>
public class NavBarItem : ToolbarItem, ITabIcon
{
    /// <summary>Backing store for <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="IconSource"/>.</summary>
    public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
        nameof(IconSource), typeof(MotionIconDefinition), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="IconPathData"/>.</summary>
    public static readonly BindableProperty IconPathDataProperty = BindableProperty.Create(
        nameof(IconPathData), typeof(string), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="Motion"/>.</summary>
    public static readonly BindableProperty MotionProperty = BindableProperty.Create(
        nameof(Motion), typeof(MotionPreset), typeof(NavBarItem), MotionPreset.Default);

    /// <summary>Backing store for <see cref="IconColor"/>.</summary>
    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor), typeof(Color), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(
        nameof(Badge), typeof(string), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="BadgeColor"/>.</summary>
    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(
        nameof(BadgeColor), typeof(Color), typeof(NavBarItem), null);

    /// <summary>Backing store for <see cref="Display"/>.</summary>
    public static readonly BindableProperty DisplayProperty = BindableProperty.Create(
        nameof(Display), typeof(NavBarItemDisplay), typeof(NavBarItem), NavBarItemDisplay.Auto);

    /// <summary>Backing store for <see cref="IsVisible"/>.</summary>
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(NavBarItem), true);

    /// <summary>Backing store for <see cref="IsSeparator"/>.</summary>
    public static readonly BindableProperty IsSeparatorProperty = BindableProperty.Create(
        nameof(IsSeparator), typeof(bool), typeof(NavBarItem), false);

    /// <summary>Backing store for <see cref="Tag"/>.</summary>
    public static readonly BindableProperty TagProperty = BindableProperty.Create(
        nameof(Tag), typeof(object), typeof(NavBarItem), null);

    /// <inheritdoc/>
    public string? Icon
    {
        get => (string?)this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    /// <inheritdoc/>
    public MotionIconDefinition? IconSource
    {
        get => (MotionIconDefinition?)this.GetValue(IconSourceProperty);
        set => this.SetValue(IconSourceProperty, value);
    }

    /// <inheritdoc/>
    public string? IconPathData
    {
        get => (string?)this.GetValue(IconPathDataProperty);
        set => this.SetValue(IconPathDataProperty, value);
    }

    /// <inheritdoc/>
    public MotionPreset Motion
    {
        get => (MotionPreset)this.GetValue(MotionProperty);
        set => this.SetValue(MotionProperty, value);
    }

    /// <summary>
    /// The motion sources' colour. Unset follows <see cref="ShinyNavBar.BarTextColor"/>, and then the
    /// theme. Only motion artwork is tinted — a bitmap in <see cref="MenuItem.IconImageSource"/> is
    /// drawn exactly as supplied.
    /// </summary>
    public Color? IconColor
    {
        get => (Color?)this.GetValue(IconColorProperty);
        set => this.SetValue(IconColorProperty, value);
    }

    /// <summary>
    /// Badge text drawn over the item's top-right corner. An empty string draws a dot; null draws
    /// nothing.
    /// </summary>
    public string? Badge
    {
        get => (string?)this.GetValue(BadgeProperty);
        set => this.SetValue(BadgeProperty, value);
    }

    /// <summary>The badge's fill. Unset follows the theme's error colour.</summary>
    public Color? BadgeColor
    {
        get => (Color?)this.GetValue(BadgeColorProperty);
        set => this.SetValue(BadgeColorProperty, value);
    }

    /// <summary>Icon, text, or both. Defaults to icon-when-there-is-one.</summary>
    public NavBarItemDisplay Display
    {
        get => (NavBarItemDisplay)this.GetValue(DisplayProperty);
        set => this.SetValue(DisplayProperty, value);
    }

    /// <summary>
    /// Hides the item without taking it out of the collection — which is what a binding needs, since
    /// removing and re-adding loses the item's place among its siblings.
    /// </summary>
    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }

    /// <summary>
    /// Draws a divider instead of a row. Only meaningful in the overflow menu — a separator in the
    /// bar itself is skipped.
    /// </summary>
    public bool IsSeparator
    {
        get => (bool)this.GetValue(IsSeparatorProperty);
        set => this.SetValue(IsSeparatorProperty, value);
    }

    /// <summary>Whatever identifies this item to your handler. <see cref="MenuItem.Text"/> is not unique.</summary>
    public object? Tag
    {
        get => this.GetValue(TagProperty);
        set => this.SetValue(TagProperty, value);
    }

    /// <summary>The plain image half of <see cref="ITabIcon"/> — <see cref="MenuItem.IconImageSource"/>.</summary>
    ImageSource? ITabIcon.IconImage => this.IconImageSource;
}
