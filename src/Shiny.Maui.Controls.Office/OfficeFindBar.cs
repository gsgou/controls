using Microsoft.Maui.Controls.Shapes;
using Shiny.Controls.Office.Icons;
using Shiny.Controls.Office.Text;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// The find control the three Office toolbars carry: a box to type in, a <c>3/12</c> readout, and a
/// pair of arrows that step the view onto each hit.
/// </summary>
/// <remarks>
/// <para>
/// One control for all three editors, bound to an <see cref="IFindController"/> — the document, slide
/// and spreadsheet finders all implement it. The bar has no idea whether "the next one" is a paragraph
/// below the fold, a shape on slide nine or a cell three sheets over; the finder moves the view and
/// this only ever reads a count back.
/// </para>
/// <para>
/// Laid out as one hosted item rather than as four ribbon items. A ribbon fills its small rows
/// column by column, so a box and two arrows handed over separately come out stacked in a way that
/// reads as three unrelated controls — and the readout between the arrows is the part that makes the
/// pair mean anything.
/// </para>
/// </remarks>
public sealed class OfficeFindBar : ContentView
{
    const double BoxWidth = 150;

    readonly Entry entry;
    readonly Label count;
    readonly OfficeToolbarButton previous;
    readonly OfficeToolbarButton next;
    readonly GraphicsView glass;
    readonly OfficeToolbarIconDrawable glassDrawable;

    IFindController? find;
    bool suppress;

    public OfficeFindBar()
    {
        this.glassDrawable = new OfficeToolbarIconDrawable { Icon = OfficeIcon.Find };
        this.glass = new GraphicsView
        {
            Drawable = this.glassDrawable,
            WidthRequest = 16,
            HeightRequest = 16,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        this.entry = new Entry
        {
            Placeholder = "Find",
            FontSize = 13,
            ReturnType = ReturnType.Search,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            AutomationId = "OfficeFindQuery"
        };

        this.entry.TextChanged += this.OnQueryChanged;

        // Enter steps rather than merely committing: the box is the only control on the bar a keyboard
        // reaches without tabbing, and typing a word then pressing Enter is how a find is used.
        this.entry.Completed += (_, _) => this.Step(forward: true);

        var box = new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(8, 0, 4, 0),
            HeightRequest = OfficeToolbarButton.ItemHeight,
            WidthRequest = BoxWidth,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 4,
                Children = { this.glass, this.entry }
            }
        };

        Grid.SetColumn(this.entry, 1);
        box.SetDynamicResource(Border.StrokeProperty, ShinyThemeKeys.Color.OutlineVariant);

        this.count = new Label
        {
            FontSize = 12,

            // Wide enough for "12/34" so the arrows beside it do not shuffle sideways as the count
            // grows a digit, which on a held-down arrow reads as the whole bar twitching.
            MinimumWidthRequest = 44,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            AutomationId = "OfficeFindCount"
        };

        this.count.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        this.previous = new OfficeToolbarButton(OfficeIcon.Previous, "Previous match (Shift+Enter)") { AutomationId = "OfficeFindPrevious" };
        this.next = new OfficeToolbarButton(OfficeIcon.Next, "Next match (Enter)") { AutomationId = "OfficeFindNext" };

        this.previous.Clicked += (_, _) => this.Step(forward: false);
        this.next.Clicked += (_, _) => this.Step(forward: true);

        this.Content = new HorizontalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { box, this.count, this.previous, this.next }
        };

        this.SetDynamicResource(IconColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        this.Refresh();
    }

    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor),
        typeof(Color),
        typeof(OfficeFindBar),
        Colors.Gray,
        propertyChanged: (b, _, _) => ((OfficeFindBar)b).RepaintGlass());

    /// <summary>The colour the magnifier is stroked in. Follows the theme by default.</summary>
    public Color IconColor
    {
        get => (Color)this.GetValue(IconColorProperty);
        set => this.SetValue(IconColorProperty, value);
    }

    public static readonly BindableProperty FindProperty = BindableProperty.Create(
        nameof(Find),
        typeof(IFindController),
        typeof(OfficeFindBar),
        propertyChanged: (b, _, value) => ((OfficeFindBar)b).Attach((IFindController?)value));

    /// <summary>
    /// The finder this bar drives. Null until a document is open, which is what disables it.
    /// </summary>
    /// <remarks>
    /// Re-assigned whenever the editor's controller is rebuilt — opening a second document produces a
    /// new controller and therefore a new finder, and a bar still holding the old one would report a
    /// count for a document that is no longer on screen.
    /// </remarks>
    public IFindController? Find
    {
        get => (IFindController?)this.GetValue(FindProperty);
        set => this.SetValue(FindProperty, value);
    }

    /// <summary>Hover tooltips on the two arrows. See <c>OfficeToolbarButton.TooltipsByDefault</c>.</summary>
    public void SetTooltipsEnabled(bool enabled)
    {
        this.previous.SetTooltipEnabled(enabled);
        this.next.SetTooltipEnabled(enabled);
    }

    /// <summary>Puts the keyboard in the find box, for a host wiring up its own shortcut.</summary>
    /// <remarks>
    /// Named apart from <see cref="VisualElement.Focus"/> rather than overriding it: focusing this
    /// control means focusing the one thing inside it that takes text, and a host that meant the
    /// container should not silently get the box.
    /// </remarks>
    public bool FocusQuery() => this.entry.Focus();

    void Attach(IFindController? controller)
    {
        if (this.find is not null)
            this.find.Changed -= this.OnFindChanged;

        this.find = controller;

        if (this.find is not null)
            this.find.Changed += this.OnFindChanged;

        // The box keeps whatever was typed into it. A controller swap is the host rebuilding the
        // editor underneath, not the user finishing with a search, so pushing the query back into the
        // new finder re-runs it against the new content rather than silently going blank.
        if (this.find is not null && this.entry.Text is { Length: > 0 } query)
            this.find.Query = query;

        this.Refresh();
    }

    void OnFindChanged(object? sender, EventArgs e) => this.Refresh();

    void OnQueryChanged(object? sender, TextChangedEventArgs e)
    {
        if (this.suppress || this.find is null)
            return;

        // Find-as-you-type: the finder steps onto the first hit as the query changes, which is what
        // makes the readout mean something before either arrow has been pressed.
        this.find.Query = e.NewTextValue ?? string.Empty;
        this.Refresh();
    }

    void Step(bool forward)
    {
        if (this.find is null)
            return;

        if (forward)
            this.find.FindNext();
        else
            this.find.FindPrevious();

        this.Refresh();
    }

    void Refresh()
    {
        var controller = this.find;
        var live = controller is not null;
        var hits = live && controller!.Count > 0;

        this.entry.IsEnabled = live;
        this.previous.SetEnabled(hits);
        this.next.SetEnabled(hits);

        this.count.Text = controller?.Status ?? string.Empty;

        // Read out in words rather than as "3/12", which a screen reader says as a fraction.
        SemanticProperties.SetDescription(
            this.count,
            !live || !controller!.IsSearching
                ? "No search"
                : controller.Count == 0
                    ? "No matches"
                    : $"Match {controller.ActiveIndex + 1} of {controller.Count}");

        if (controller is not null && this.entry.Text != controller.Query)
        {
            // A query set in code - by a host, or by the bar being handed a fresh controller - has to
            // reach the box, but writing it back raises TextChanged and would re-enter the finder.
            this.suppress = true;
            this.entry.Text = controller.Query;
            this.suppress = false;
        }
    }

    void RepaintGlass()
    {
        this.glassDrawable.Color = this.IconColor;
        this.glass.Invalidate();
    }
}
