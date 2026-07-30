using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Tree.Internal;

internal class TreeNodeView : Grid
{
    readonly TreeView owner;
    readonly Grid chevronHost;
    readonly ContentView contentHost;
    readonly Border background;
    readonly Grid dropIndicatorAbove;
    readonly Grid dropIndicatorBelow;
    readonly Grid dropIndicatorInto;

    public TreeNodeView(TreeView owner, TreeNode node)
    {
        this.owner = owner;
        Node = node;

        BindingContext = node.Item;
        Padding = 0;
        ColumnSpacing = 0;
        RowSpacing = 0;

        // Background sits behind content and handles selection highlight + tap.
        background = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = owner.RowBackgroundColor,
            Padding = owner.RowPadding,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        // Indent + chevron + content layout
        var inner = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto), // indent + guide lines
                new ColumnDefinition(GridLength.Auto), // chevron
                new ColumnDefinition(GridLength.Star)  // user content
            },
            ColumnSpacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        // Indent column
        var indent = BuildIndent();
        inner.Add(indent, 0, 0);

        // Chevron
        chevronHost = new Grid
        {
            WidthRequest = owner.ChevronSize + 6,
            HeightRequest = owner.ChevronSize + 6,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Transparent
        };
        var chevronTap = new TapGestureRecognizer();
        chevronTap.Tapped += OnChevronTapped;
        chevronHost.GestureRecognizers.Add(chevronTap);
        inner.Add(chevronHost, 1, 0);

        // Content host
        contentHost = new ContentView
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill
        };

        var template = owner.ItemTemplate;
        if (template is DataTemplateSelector selector)
            template = selector.SelectTemplate(node.Item, owner);

        if (template != null && template.CreateContent() is View userView)
        {
            userView.BindingContext = node.Item;
            contentHost.Content = userView;
        }
        else
        {
            contentHost.Content = new Label
            {
                Text = node.Item?.ToString() ?? string.Empty,
                VerticalTextAlignment = TextAlignment.Center
            };
        }
        inner.Add(contentHost, 2, 0);

        background.Content = inner;

        // Row tap = selection
        var rowTap = new TapGestureRecognizer();
        rowTap.Tapped += OnRowTapped;
        background.GestureRecognizers.Add(rowTap);

        Add(background);

        // Drop indicators (always added; opacity toggled during drag)
        dropIndicatorAbove = MakeDropIndicator(LayoutOptions.Start);
        dropIndicatorBelow = MakeDropIndicator(LayoutOptions.End);
        // Drop affordances follow the theme accent rather than a fixed Windows blue.
        dropIndicatorInto = new Grid { IsVisible = false, Opacity = 0.2 };
        dropIndicatorInto.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        Add(dropIndicatorInto);
        Add(dropIndicatorAbove);
        Add(dropIndicatorBelow);

        if (owner.EnableDragDrop)
            WireDragDrop();

        RefreshChevron();
        RefreshSelection();
        HookNodeChanges();
    }

    public TreeNode Node { get; }

    Grid BuildIndent()
    {
        var width = owner.IndentSize * Node.Depth;
        var host = new Grid
        {
            WidthRequest = width,
            HorizontalOptions = LayoutOptions.Start
        };

        if (!owner.ShowGuideLines || Node.Depth == 0)
            return host;

        for (var d = 0; d < Node.Depth; d++)
        {
            var line = new BoxView
            {
                WidthRequest = 1,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness((d * owner.IndentSize) + (owner.IndentSize / 2), 0, 0, 0)
            };
            if (owner.GuideLineColor is Color gc)
                line.Color = gc;
            else
                line.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.OutlineVariant);
            host.Add(line);
        }
        return host;
    }

    static Grid MakeDropIndicator(LayoutOptions vertical)
    {
        var indicator = new Grid
        {
            HeightRequest = 2,
            VerticalOptions = vertical,
            HorizontalOptions = LayoutOptions.Fill,
            IsVisible = false
        };
        indicator.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        return indicator;
    }

    public void RefreshChevron()
    {
        chevronHost.Children.Clear();

        var hasChildren = owner.HasChildren(Node.Item);
        var canExpand = owner.CanExpand(Node.Item);

        if (!hasChildren)
            return;

        if (Node.LoadState == TreeLoadState.Loading)
        {
            var indicator = new ActivityIndicator
            {
                IsRunning = true,
                WidthRequest = owner.ChevronSize,
                HeightRequest = owner.ChevronSize,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            if (owner.ChevronColor is Color cc)
                indicator.Color = cc;
            else
                indicator.SetDynamicResource(ActivityIndicator.ColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
            chevronHost.Children.Add(indicator);
            return;
        }

        if (Node.LoadState == TreeLoadState.Error)
        {
            chevronHost.Children.Add(BuildChevronImage(owner.RetryIcon, "↻"));
            return;
        }

        var icon = Node.IsExpanded ? owner.ExpandedIcon : owner.CollapsedIcon;
        var glyph = Node.IsExpanded ? "▼" : "▶";
        var view = BuildChevronImage(icon, glyph);
        view.Opacity = canExpand ? 1.0 : 0.35;
        chevronHost.Children.Add(view);
    }

    View BuildChevronImage(ImageSource? icon, string fallbackGlyph)
    {
        if (icon != null)
        {
            return new Image
            {
                Source = icon,
                WidthRequest = owner.ChevronSize,
                HeightRequest = owner.ChevronSize,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Aspect = Aspect.AspectFit
            };
        }

        var glyphLabel = new Label
        {
            Text = fallbackGlyph,
            FontSize = owner.ChevronSize * 0.75,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        if (owner.ChevronColor is Color cc)
            glyphLabel.TextColor = cc;
        else
            glyphLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        return glyphLabel;
    }

    public void RefreshSelection()
    {
        if (Node.IsSelected)
        {
            if (owner.SelectedBackgroundColor is Color selected)
                background.BackgroundColor = selected;
            else
                background.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SecondaryContainer);
        }
        else
        {
            // Row background defaults to transparent; clear any dynamic resource binding.
            background.RemoveDynamicResource(VisualElement.BackgroundColorProperty);
            background.BackgroundColor = owner.RowBackgroundColor;
        }
    }

    void OnChevronTapped(object? sender, EventArgs e)
    {
        if (Node.LoadState == TreeLoadState.Error)
        {
            owner.RetryLoad(Node);
            return;
        }
        if (!owner.HasChildren(Node.Item) || !owner.CanExpand(Node.Item))
            return;
        owner.ToggleExpand(Node);
    }

    void OnRowTapped(object? sender, EventArgs e)
    {
        owner.HandleRowTapped(Node);
    }

    void HookNodeChanges()
    {
        Node.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(TreeNode.IsExpanded):
                case nameof(TreeNode.LoadState):
                    RefreshChevron();
                    break;
                case nameof(TreeNode.IsSelected):
                    RefreshSelection();
                    break;
            }
        };
    }

    // ------------- Drag/drop -------------
    public void ShowDropIndicator(TreeDropPosition zone)
    {
        dropIndicatorAbove.IsVisible = zone == TreeDropPosition.Above;
        dropIndicatorBelow.IsVisible = zone == TreeDropPosition.Below;
        dropIndicatorInto.IsVisible = zone == TreeDropPosition.Into;
    }

    public void HideDropIndicators()
    {
        dropIndicatorAbove.IsVisible = false;
        dropIndicatorBelow.IsVisible = false;
        dropIndicatorInto.IsVisible = false;
    }

    public void SetDragging(bool dragging)
    {
        Opacity = dragging ? 0.6 : 1.0;
        ZIndex = dragging ? 1 : 0;
        if (!dragging)
            TranslationY = 0;
    }

#if IOS || ANDROID || WINDOWS
    void WireDragDrop()
    {
        var drag = new DragGestureRecognizer { CanDrag = true };
        drag.DragStarting += (s, e) =>
        {
            e.Data.Properties["TreeNodeView"] = this;
        };
        background.GestureRecognizers.Add(drag);

        var drop = new DropGestureRecognizer { AllowDrop = true };
        drop.DragOver += OnDragOver;
        drop.DragLeave += OnDragLeave;
        drop.Drop += OnDrop;
        background.GestureRecognizers.Add(drop);
    }

    TreeDropPosition currentZone = TreeDropPosition.Above;

    void OnDragOver(object? sender, DragEventArgs e)
    {
        // DragEventArgs doesn't expose a reliable pointer position on every platform,
        // so the platform-gesture path always targets "below" (reorder as next sibling).
        currentZone = TreeDropPosition.Below;
        ShowDropIndicator(currentZone);
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    void OnDragLeave(object? sender, DragEventArgs e) => HideDropIndicators();

    void OnDrop(object? sender, DropEventArgs e)
    {
        HideDropIndicators();

        if (e.Data?.Properties != null &&
            e.Data.Properties.TryGetValue("TreeNodeView", out var src) &&
            src is TreeNodeView srcView &&
            !ReferenceEquals(srcView, this))
        {
            owner.HandleDrop(srcView.Node, Node, currentZone);
        }
    }
#else
    // Plain net10.0 serves Mac Catalyst, the AppKit (macOS) host and the GTK4 (Linux)
    // host. Catalyst's Drag/DropGestureRecognizers are broken (dotnet/maui#23627) and
    // the labs hosts don't implement them at all, so the drag is driven by a pan
    // gesture and the TreeView resolves the drop target from row geometry.
    void WireDragDrop()
    {
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        background.GestureRecognizers.Add(pan);
    }

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                owner.BeginPointerDrag(this);
                break;
            case GestureStatus.Running:
                owner.UpdatePointerDrag(this, e.TotalY);
                break;
            case GestureStatus.Completed:
                owner.CompletePointerDrag(this);
                break;
            case GestureStatus.Canceled:
                owner.CancelPointerDrag(this);
                break;
        }
    }
#endif
}
