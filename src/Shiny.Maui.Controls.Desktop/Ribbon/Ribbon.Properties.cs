using System.Windows.Input;

namespace Shiny.Maui.Controls.Desktop.Ribbons;

public partial class Ribbon
{
    static BindableProperty Redraw(string name, Type returnType, object? defaultValue = null)
        => BindableProperty.Create(
            name, returnType, typeof(Ribbon), defaultValue,
            propertyChanged: (b, _, _) => ((Ribbon)b).Rebuild()
        );


    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex),
        typeof(int),
        typeof(Ribbon),
        0,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => ((Ribbon)b).OnSelectedIndexChanged((int)o, (int)n)
    );

    public static readonly BindableProperty SelectedTabProperty = BindableProperty.Create(
        nameof(SelectedTab),
        typeof(RibbonTab),
        typeof(Ribbon),
        null,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((Ribbon)b).OnSelectedTabChanged((RibbonTab?)n)
    );

    public static readonly BindableProperty DisplayModeProperty = BindableProperty.Create(
        nameof(DisplayMode),
        typeof(RibbonDisplayMode),
        typeof(Ribbon),
        RibbonDisplayMode.Expanded,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => ((Ribbon)b).OnDisplayModeChanged((RibbonDisplayMode)o, (RibbonDisplayMode)n)
    );

    public static readonly BindableProperty AllowCollapseProperty = Redraw(nameof(AllowCollapse), typeof(bool), true);

    public static readonly BindableProperty ApplicationButtonTextProperty = Redraw(nameof(ApplicationButtonText), typeof(string));

    public static readonly BindableProperty ApplicationButtonCommandProperty = Redraw(nameof(ApplicationButtonCommand), typeof(ICommand));

    public static readonly BindableProperty ShowQuickAccessProperty = Redraw(nameof(ShowQuickAccess), typeof(bool), true);

    public static readonly BindableProperty ShowGroupTitlesProperty = Redraw(nameof(ShowGroupTitles), typeof(bool), true);

    public static readonly BindableProperty ShowTabStripProperty = Redraw(nameof(ShowTabStrip), typeof(bool), true);

    public static readonly BindableProperty SmallItemRowsProperty = Redraw(nameof(SmallItemRows), typeof(int), 3);

    public static readonly BindableProperty AllowGroupCollapseProperty = Redraw(nameof(AllowGroupCollapse), typeof(bool), true);

    public static readonly BindableProperty AccentColorProperty = Redraw(nameof(AccentColor), typeof(Color));

    public static readonly BindableProperty HeaderBackgroundColorProperty = Redraw(nameof(HeaderBackgroundColor), typeof(Color));

    public static readonly BindableProperty BodyBackgroundColorProperty = Redraw(nameof(BodyBackgroundColor), typeof(Color));

    public static readonly BindableProperty ShowTooltipsProperty = Redraw(nameof(ShowTooltips), typeof(bool), true);


    /// <summary>
    /// Index of the tab showing, counted over <see cref="Tabs"/> including hidden ones, so it does not
    /// shift when a contextual tab appears. Two-way.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)this.GetValue(SelectedIndexProperty);
        set => this.SetValue(SelectedIndexProperty, value);
    }

    /// <summary>The tab showing. Two-way, and kept in step with <see cref="SelectedIndex"/>.</summary>
    public RibbonTab? SelectedTab
    {
        get => (RibbonTab?)this.GetValue(SelectedTabProperty);
        set => this.SetValue(SelectedTabProperty, value);
    }

    /// <summary>
    /// Expanded, collapsed to the tab strip, or the one-row simplified layout. Two-way, so a toggle in
    /// the app's own chrome can drive it as well as the ribbon's chevron.
    /// </summary>
    public RibbonDisplayMode DisplayMode
    {
        get => (RibbonDisplayMode)this.GetValue(DisplayModeProperty);
        set => this.SetValue(DisplayModeProperty, value);
    }

    /// <summary>
    /// Offers the chevron that collapses the ribbon, and makes a double-tap on a tab do the same. Turn
    /// it off for chrome that has to stay put; <see cref="DisplayMode"/> still works from code.
    /// </summary>
    public bool AllowCollapse
    {
        get => (bool)this.GetValue(AllowCollapseProperty);
        set => this.SetValue(AllowCollapseProperty, value);
    }

    /// <summary>
    /// Text for the accented button at the head of the strip — "File" in most apps. Null (the default)
    /// leaves it out entirely.
    /// </summary>
    public string? ApplicationButtonText
    {
        get => (string?)this.GetValue(ApplicationButtonTextProperty);
        set => this.SetValue(ApplicationButtonTextProperty, value);
    }

    /// <summary>Runs when the application button is pressed. <see cref="ApplicationButtonClicked"/> follows it.</summary>
    public ICommand? ApplicationButtonCommand
    {
        get => (ICommand?)this.GetValue(ApplicationButtonCommandProperty);
        set => this.SetValue(ApplicationButtonCommandProperty, value);
    }

    /// <summary>Draws <see cref="QuickAccessItems"/> at the trailing end of the tab strip. Default true.</summary>
    public bool ShowQuickAccess
    {
        get => (bool)this.GetValue(ShowQuickAccessProperty);
        set => this.SetValue(ShowQuickAccessProperty, value);
    }

    /// <summary>
    /// Draws each group's caption under it. Default true; always false in
    /// <see cref="RibbonDisplayMode.Simplified"/>, where there is no room for a second line.
    /// </summary>
    public bool ShowGroupTitles
    {
        get => (bool)this.GetValue(ShowGroupTitlesProperty);
        set => this.SetValue(ShowGroupTitlesProperty, value);
    }

    /// <summary>
    /// Draws the tab strip. Set false for a single-tab ribbon that is really just a toolbar — the one
    /// tab's groups still show, without a strip above them saying so.
    /// </summary>
    public bool ShowTabStrip
    {
        get => (bool)this.GetValue(ShowTabStripProperty);
        set => this.SetValue(ShowTabStripProperty, value);
    }

    /// <summary>
    /// How many <see cref="RibbonItemSize.Small"/> items stack in one column before a new one starts.
    /// Three is the ribbon convention and what the body's height is sized from; changing it changes the
    /// height of the whole bar.
    /// </summary>
    public int SmallItemRows
    {
        get => (int)this.GetValue(SmallItemRowsProperty);
        set => this.SetValue(SmallItemRowsProperty, value);
    }

    /// <summary>
    /// Lets groups collapse to a single button when the tab is wider than the ribbon. Turn it off and
    /// the body scrolls horizontally instead, which is the better answer when every group is small.
    /// </summary>
    public bool AllowGroupCollapse
    {
        get => (bool)this.GetValue(AllowGroupCollapseProperty);
        set => this.SetValue(AllowGroupCollapseProperty, value);
    }

    /// <summary>The selected tab's underline and the application button's fill. Falls back to the theme's primary.</summary>
    public Color? AccentColor
    {
        get => (Color?)this.GetValue(AccentColorProperty);
        set => this.SetValue(AccentColorProperty, value);
    }

    /// <summary>Fill behind the tab strip. Falls back to the theme.</summary>
    public Color? HeaderBackgroundColor
    {
        get => (Color?)this.GetValue(HeaderBackgroundColorProperty);
        set => this.SetValue(HeaderBackgroundColorProperty, value);
    }

    /// <summary>Fill behind the groups. Falls back to the theme.</summary>
    public Color? BodyBackgroundColor
    {
        get => (Color?)this.GetValue(BodyBackgroundColorProperty);
        set => this.SetValue(BodyBackgroundColorProperty, value);
    }

    /// <summary>
    /// Hover tooltips on the buttons. On by default because a ribbon is a desktop control with a
    /// pointer over it; turn it off on a touch head, where a hover tip can never be seen anyway.
    /// </summary>
    public bool ShowTooltips
    {
        get => (bool)this.GetValue(ShowTooltipsProperty);
        set => this.SetValue(ShowTooltipsProperty, value);
    }
}
