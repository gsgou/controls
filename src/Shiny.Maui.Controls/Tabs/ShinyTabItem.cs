using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// One tab of a <see cref="ShinyTabbedPage"/> — the chrome that goes in the bar plus the content
/// that goes on screen when it is selected.
/// </summary>
/// <remarks>
/// <para>Content is declared exactly as MAUI's <c>TabbedPage</c> declares it: inline
/// <see cref="StateViewState.Content"/> is built with the markup, and a
/// <see cref="StateViewState.ContentTemplate"/> is built the first time the tab is selected and
/// then kept. Four tabs behind templates cost one view tree on launch, not four.</para>
/// <para>The template may inflate a <see cref="View"/> or a whole <see cref="ContentPage"/>. A page
/// is adopted: its <c>Content</c> is hosted, its <c>Title</c> fills in a tab with no
/// <see cref="Title"/> of its own, and its <c>BindingContext</c> is mirrored onto the hosted view.
/// What it does not get is a place on a navigation stack — the page object is not the page on
/// screen, the <see cref="ShinyTabbedPage"/> is. Code-behind that calls <c>this.Navigation</c> still
/// resolves, because the adopted page is parented to the tabbed page, but it pushes onto the tabbed
/// page's stack.</para>
/// <para>Nor does it get <c>OnAppearing</c>. MAUI raises that for the page the platform presented,
/// and the platform never sees this one — so it is <see cref="ITabAware"/>, or the
/// <see cref="Appearing"/> and <see cref="Disappearing"/> events here, that tell a tab it is on
/// screen.</para>
/// </remarks>
[ContentProperty(nameof(Content))]
public class ShinyTabItem : StateViewState, ITabIcon
{
    /// <summary>Backing store for <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="IconSource"/>.</summary>
    public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
        nameof(IconSource), typeof(MotionIconDefinition), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="IconPathData"/>.</summary>
    public static readonly BindableProperty IconPathDataProperty = BindableProperty.Create(
        nameof(IconPathData), typeof(string), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="IconImage"/>.</summary>
    public static readonly BindableProperty IconImageProperty = BindableProperty.Create(
        nameof(IconImage), typeof(ImageSource), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="Motion"/>.</summary>
    public static readonly BindableProperty MotionProperty = BindableProperty.Create(
        nameof(Motion), typeof(MotionPreset), typeof(ShinyTabItem), MotionPreset.Default,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(
        nameof(Badge), typeof(string), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="BadgeColor"/>.</summary>
    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(
        nameof(BadgeColor), typeof(Color), typeof(ShinyTabItem), null,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="IsEnabled"/>.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
        nameof(IsEnabled), typeof(bool), typeof(ShinyTabItem), true,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseTabChanged());

    /// <summary>Backing store for <see cref="IsVisible"/>.</summary>
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(ShinyTabItem), true,
        propertyChanged: (b, _, _) => ((ShinyTabItem)b).RaiseStructureChanged());

    /// <summary>Backing store for <see cref="Route"/>.</summary>
    public static readonly BindableProperty RouteProperty = BindableProperty.Create(
        nameof(Route), typeof(string), typeof(ShinyTabItem), null);

    /// <summary>Backing store for <see cref="Tag"/>.</summary>
    public static readonly BindableProperty TagProperty = BindableProperty.Create(
        nameof(Tag), typeof(object), typeof(ShinyTabItem), null);

    /// <summary>Creates a tab.</summary>
    public ShinyTabItem()
        // The base class resolves content by matching a string, so every tab needs one that is its
        // own. Names are an implementation detail here - Route is the identity an app uses - so one
        // is minted rather than asked for, and a hand-set Name still wins.
        => this.Name = "shinytab:" + Guid.NewGuid().ToString("n");

    /// <summary>The tab's label.</summary>
    public string? Title
    {
        get => (string?)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

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
    public ImageSource? IconImage
    {
        get => (ImageSource?)this.GetValue(IconImageProperty);
        set => this.SetValue(IconImageProperty, value);
    }

    /// <inheritdoc/>
    public MotionPreset Motion
    {
        get => (MotionPreset)this.GetValue(MotionProperty);
        set => this.SetValue(MotionProperty, value);
    }

    /// <summary>
    /// The badge text. An empty string draws a dot rather than a count; null draws nothing.
    /// </summary>
    /// <remarks>
    /// A tab whose content is still behind a template has no page to ask, which is why this lives on
    /// the tab. Once the page exists, <see cref="ShinyTabs.BadgeProperty"/> set on it wins — so a
    /// count the page owns can be bound there and a count the shell owns can be bound here, without
    /// the two fighting.
    /// </remarks>
    public string? Badge
    {
        get => (string?)this.GetValue(BadgeProperty);
        set => this.SetValue(BadgeProperty, value);
    }

    /// <summary>Unset follows the theme's error colour.</summary>
    public Color? BadgeColor
    {
        get => (Color?)this.GetValue(BadgeColorProperty);
        set => this.SetValue(BadgeColorProperty, value);
    }

    /// <summary>A disabled tab is dimmed and cannot be selected.</summary>
    public bool IsEnabled
    {
        get => (bool)this.GetValue(IsEnabledProperty);
        set => this.SetValue(IsEnabledProperty, value);
    }

    /// <summary>A hidden tab is left out of the bar entirely, and the rest re-space to fill it in.</summary>
    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }

    /// <summary>
    /// A stable identifier for the tab, so selection can be driven by name rather than by index —
    /// see <see cref="ShinyTabbedPage.GoTo(string)"/>. Not a Shell route; a
    /// <see cref="ShinyTabBarBehavior"/> takes its routes from the Shell itself.
    /// </summary>
    public string? Route
    {
        get => (string?)this.GetValue(RouteProperty);
        set => this.SetValue(RouteProperty, value);
    }

    /// <summary>Whatever identifies this tab to your handler.</summary>
    public object? Tag
    {
        get => this.GetValue(TagProperty);
        set => this.SetValue(TagProperty, value);
    }

    /// <summary>
    /// The <see cref="ContentPage"/> a <see cref="StateViewState.ContentTemplate"/> inflated, when
    /// it inflated one. Null for view content, and null until the tab is first selected.
    /// </summary>
    public ContentPage? AdoptedPage { get; private set; }

    /// <summary>
    /// The element the bar reads <see cref="ShinyTabs"/> attached properties off: the adopted page
    /// when there is one, otherwise the hosted view. Null while the tab is still unrealized.
    /// </summary>
    public BindableObject? PageContext => (BindableObject?)this.AdoptedPage ?? this.PeekContent();

    /// <summary>
    /// The element a page adopted from a template should hang off, so its <c>Navigation</c> and
    /// inherited <c>BindingContext</c> resolve. Set by the hosting <see cref="ShinyTabbedPage"/>.
    /// </summary>
    internal Element? Host { get; set; }

    /// <summary>Raised when something the bar draws from has changed.</summary>
    internal event EventHandler? TabChanged;

    /// <summary>Raised when the set or order of cells the bar should draw has changed.</summary>
    internal event EventHandler? StructureChanged;

    void RaiseTabChanged() => this.TabChanged?.Invoke(this, EventArgs.Empty);

    void RaiseStructureChanged() => this.StructureChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>The already-realized content, without realizing anything.</summary>
    View? PeekContent() => this.TemplatedContent ?? this.Content;

    /// <inheritdoc/>
    private protected override View? CreateFromTemplate(DataTemplate template)
    {
        var created = template.CreateContent();
        if (created is not ContentPage page)
            return created as View;

        // Adopt the page. Content has to come out before it can go anywhere else - MAUI throws on a
        // view that already has a parent - and the page keeps existing so its lifecycle overrides
        // and code-behind fields still mean something.
        var content = page.Content;
        page.Content = null;

        // Parent, not child: this puts the page on the element chain so Navigation and the inherited
        // BindingContext resolve, without ever asking MAUI to render a page inside a page.
        if (this.Host is not null)
            page.Parent = this.Host;

        this.AdoptedPage = page;
        page.PropertyChanged += this.OnAdoptedPagePropertyChanged;

        if (content is not null)
            ApplyPageBindingContext(page, content);

        if (String.IsNullOrEmpty(this.Title) && !String.IsNullOrEmpty(page.Title))
            this.Title = page.Title;

        this.RaiseTabChanged();
        return content;
    }

    void OnAdoptedPagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (this.AdoptedPage is not { } page)
            return;

        // The page's own BindingContext does not flow to content that no longer hangs off it, so it
        // is mirrored across. Without this a page that assigns its view model in code-behind renders
        // every binding empty - which reads as "the tab is broken" rather than "the context moved".
        if (e.PropertyName == nameof(BindingContext) && this.PeekContent() is { } view)
            ApplyPageBindingContext(page, view);

        else if (e.PropertyName == nameof(Page.Title) && !String.IsNullOrEmpty(page.Title))
            this.Title = page.Title;
    }

    static void ApplyPageBindingContext(ContentPage page, View content)
    {
        if (page.BindingContext is not null)
            content.BindingContext = page.BindingContext;
    }

    /// <summary>Raised when this tab becomes the selected one on a page that is on screen.</summary>
    public event EventHandler? Appearing;

    /// <summary>Raised when this tab stops being selected, or its page leaves the screen.</summary>
    public event EventHandler? Disappearing;

    internal void NotifyAppearing()
    {
        this.Appearing?.Invoke(this, EventArgs.Empty);
        foreach (var target in this.LifecycleTargets())
            target.OnTabAppearing();
    }

    internal void NotifyDisappearing()
    {
        this.Disappearing?.Invoke(this, EventArgs.Empty);
        foreach (var target in this.LifecycleTargets())
            target.OnTabDisappearing();
    }

    /// <summary>
    /// Everything that asked to hear about this tab: the adopted page, the hosted view, and either
    /// one's view model. De-duplicated, because a view model reachable from both the page and its
    /// content is still one object and must not be told twice.
    /// </summary>
    IEnumerable<ITabAware> LifecycleTargets()
    {
        var seen = new List<object>(4);

        foreach (var candidate in new object?[]
                 {
                     this.AdoptedPage,
                     this.PeekContent(),
                     this.AdoptedPage?.BindingContext,
                     this.PeekContent()?.BindingContext
                 })
        {
            if (candidate is not ITabAware aware || seen.Contains(candidate))
                continue;

            seen.Add(candidate);
            yield return aware;
        }
    }
}
