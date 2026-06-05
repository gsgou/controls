using System.Collections;
using System.Windows.Input;

namespace Shiny.Maui.Controls.ParallaxCollectionView;

/// <summary>
/// A CollectionView with a parallax header that translates and (optionally) collapses
/// as the list scrolls. Pure cross-platform MAUI — no platform handlers.
/// </summary>
[ContentProperty(nameof(ItemTemplate))]
public class ParallaxCollectionView : ContentView
{
    readonly Grid root;
    readonly ContentView heroHost;
    readonly BoxView spacerHeader;
    readonly CollectionView collection;

    public ParallaxCollectionView()
    {
        spacerHeader = new BoxView { Color = Colors.Transparent };

        collection = new CollectionView
        {
            BackgroundColor = Colors.Transparent,
            Header = spacerHeader,
            SelectionMode = SelectionMode.None
        };
        collection.Scrolled += OnScrolled;
        collection.SelectionChanged += OnSelectionChanged;

        heroHost = new ContentView
        {
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = false
        };

        root = new Grid
        {
            Children = { heroHost, collection }
        };

        Content = root;
        ApplyHeaderHeight();
    }

    #region Bindable Properties

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(ParallaxCollectionView),
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.ItemsSource = (IEnumerable?)n);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(ParallaxCollectionView),
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.ItemTemplate = (DataTemplate?)n);

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty ItemsLayoutProperty = BindableProperty.Create(
        nameof(ItemsLayout),
        typeof(IItemsLayout),
        typeof(ParallaxCollectionView),
        LinearItemsLayout.Vertical,
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.ItemsLayout =
            (IItemsLayout?)n ?? LinearItemsLayout.Vertical);

    public IItemsLayout ItemsLayout
    {
        get => (IItemsLayout)GetValue(ItemsLayoutProperty);
        set => SetValue(ItemsLayoutProperty, value);
    }

    public static readonly BindableProperty HeaderTemplateProperty = BindableProperty.Create(
        nameof(HeaderTemplate),
        typeof(DataTemplate),
        typeof(ParallaxCollectionView),
        propertyChanged: (b, _, _) => ((ParallaxCollectionView)b).ApplyHeaderTemplate());

    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public static readonly BindableProperty HeaderHeightProperty = BindableProperty.Create(
        nameof(HeaderHeight),
        typeof(double),
        typeof(ParallaxCollectionView),
        240.0,
        propertyChanged: (b, _, _) => ((ParallaxCollectionView)b).ApplyHeaderHeight());

    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public static readonly BindableProperty MinHeaderHeightProperty = BindableProperty.Create(
        nameof(MinHeaderHeight),
        typeof(double),
        typeof(ParallaxCollectionView),
        0.0);

    public double MinHeaderHeight
    {
        get => (double)GetValue(MinHeaderHeightProperty);
        set => SetValue(MinHeaderHeightProperty, value);
    }

    public static readonly BindableProperty ParallaxFactorProperty = BindableProperty.Create(
        nameof(ParallaxFactor),
        typeof(double),
        typeof(ParallaxCollectionView),
        0.5,
        validateValue: (_, v) => (double)v >= 0);

    public double ParallaxFactor
    {
        get => (double)GetValue(ParallaxFactorProperty);
        set => SetValue(ParallaxFactorProperty, value);
    }

    public static readonly BindableProperty CollapseToStickyProperty = BindableProperty.Create(
        nameof(CollapseToSticky),
        typeof(bool),
        typeof(ParallaxCollectionView),
        false);

    public bool CollapseToSticky
    {
        get => (bool)GetValue(CollapseToStickyProperty);
        set => SetValue(CollapseToStickyProperty, value);
    }

    public static readonly BindableProperty FadeHeaderOnScrollProperty = BindableProperty.Create(
        nameof(FadeHeaderOnScroll),
        typeof(bool),
        typeof(ParallaxCollectionView),
        false);

    public bool FadeHeaderOnScroll
    {
        get => (bool)GetValue(FadeHeaderOnScrollProperty);
        set => SetValue(FadeHeaderOnScrollProperty, value);
    }

    public static readonly BindableProperty EmptyViewProperty = BindableProperty.Create(
        nameof(EmptyView),
        typeof(object),
        typeof(ParallaxCollectionView),
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.EmptyView = n);

    public object? EmptyView
    {
        get => GetValue(EmptyViewProperty);
        set => SetValue(EmptyViewProperty, value);
    }

    public static readonly BindableProperty EmptyViewTemplateProperty = BindableProperty.Create(
        nameof(EmptyViewTemplate),
        typeof(DataTemplate),
        typeof(ParallaxCollectionView),
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.EmptyViewTemplate = (DataTemplate?)n);

    public DataTemplate? EmptyViewTemplate
    {
        get => (DataTemplate?)GetValue(EmptyViewTemplateProperty);
        set => SetValue(EmptyViewTemplateProperty, value);
    }

    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
        nameof(SelectionMode),
        typeof(SelectionMode),
        typeof(ParallaxCollectionView),
        SelectionMode.None,
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.SelectionMode = (SelectionMode)n);

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(object),
        typeof(ParallaxCollectionView),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((ParallaxCollectionView)b).collection.SelectedItem = n);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty ItemSelectedCommandProperty = BindableProperty.Create(
        nameof(ItemSelectedCommand),
        typeof(ICommand),
        typeof(ParallaxCollectionView));

    public ICommand? ItemSelectedCommand
    {
        get => (ICommand?)GetValue(ItemSelectedCommandProperty);
        set => SetValue(ItemSelectedCommandProperty, value);
    }

    #endregion

    public event EventHandler<ParallaxScrollEventArgs>? Scrolled;
    public event EventHandler<SelectionChangedEventArgs>? ItemSelected;

    public void ScrollTo(int index, ScrollToPosition position = ScrollToPosition.MakeVisible, bool animate = true)
        => collection.ScrollTo(index, position: position, animate: animate);

    public void ScrollTo(object item, ScrollToPosition position = ScrollToPosition.MakeVisible, bool animate = true)
        => collection.ScrollTo(item, position: position, animate: animate);

    void ApplyHeaderHeight()
    {
        var h = Math.Max(0, HeaderHeight);
        spacerHeader.HeightRequest = h;
        heroHost.HeightRequest = h;
    }

    void ApplyHeaderTemplate()
    {
        if (HeaderTemplate is null)
        {
            heroHost.Content = null;
            return;
        }

        var content = HeaderTemplate.CreateContent();
        var view = content as View ?? (content as ViewCell)?.View;
        if (view is not null)
        {
            view.BindingContext ??= BindingContext;
            heroHost.Content = view;
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (heroHost.Content is not null)
            heroHost.Content.BindingContext = BindingContext;
    }

    void OnScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        var offset = Math.Max(0, e.VerticalOffset);
        var travel = HeaderHeight - MinHeaderHeight;
        var translation = -offset * ParallaxFactor;

        if (CollapseToSticky && travel > 0)
        {
            var maxNegative = -travel;
            if (translation < maxNegative)
                translation = maxNegative;
        }

        heroHost.TranslationY = translation;

        if (FadeHeaderOnScroll && HeaderHeight > 0)
        {
            var fade = 1.0 - Math.Min(1.0, offset / HeaderHeight);
            heroHost.Opacity = fade;
        }
        else if (heroHost.Opacity != 1)
        {
            heroHost.Opacity = 1;
        }

        var visible = Math.Max(MinHeaderHeight, HeaderHeight + translation);
        Scrolled?.Invoke(this, new ParallaxScrollEventArgs(offset, translation, visible));
    }

    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedItem = collection.SelectedItem;
        ItemSelected?.Invoke(this, e);

        var item = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] : null;
        if (item is not null && ItemSelectedCommand?.CanExecute(item) == true)
            ItemSelectedCommand.Execute(item);
    }
}
