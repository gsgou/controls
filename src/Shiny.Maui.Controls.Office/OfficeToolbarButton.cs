using Microsoft.Maui.Controls.Shapes;
using Shiny.Controls.Office.Icons;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// One icon-only button on the document or slide toolbar.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Border"/> around a <see cref="GraphicsView"/> rather than a <see cref="Button"/>,
/// for two reasons. A MAUI button can only hold text, so a button that draws a vector icon has to be
/// something else; and a button consumes touch natively and never routes it to its
/// <c>GestureRecognizers</c>, which is what both the tap and the hover tooltip need here.
/// </para>
/// <para>
/// Shared by <see cref="DocumentEditorView"/> and <see cref="SlideEditorView"/>. The two toolbars had
/// their own copy of the button factory before this, which is how they came to disagree about what a
/// toolbar button looks like in the first place.
/// </para>
/// </remarks>
internal sealed class OfficeToolbarButton : Border
{
    /// <summary>
    /// Whether hover tooltips are on unless the host says otherwise.
    /// </summary>
    /// <remarks>
    /// Desktop and web only, as asked: a tooltip that opens on hover has nothing to open it on a
    /// phone, and a long-press tooltip on a toolbar button competes with the tap that button exists
    /// for. Mac Catalyst, macOS and the GTK/plain .NET head all fall through to true.
    /// </remarks>
#if ANDROID || IOS
    public const bool TooltipsByDefault = false;
#else
    public const bool TooltipsByDefault = true;
#endif

    /// <summary>
    /// One height for every control in the bar.
    /// </summary>
    /// <remarks>
    /// 36 rather than a rounder number because that is the minimum height the core package's
    /// FontPickerButton and ColorPickerButton ask for. Anything shorter leaves the icon buttons a
    /// couple of pixels above the pickers, which is small enough to look like a rendering bug and
    /// large enough to see.
    /// </remarks>
    public const double ItemHeight = 36;

    const double IconSize = 20;

    readonly GraphicsView graphics;
    readonly OfficeToolbarIconDrawable drawable;
    bool active;
    bool tooltipWired;


    public OfficeToolbarButton(OfficeIcon icon, string hint)
    {
        this.Hint = hint;

        this.drawable = new OfficeToolbarIconDrawable { Icon = icon };
        this.graphics = new GraphicsView
        {
            Drawable = this.drawable,
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,

            // The gesture belongs to the border, so the whole padded hit target reacts rather than
            // just the 20pt of artwork in the middle of it.
            InputTransparent = true
        };

        this.Content = this.graphics;
        this.StrokeThickness = 0;
        this.BackgroundColor = Colors.Transparent;
        this.StrokeShape = new RoundRectangle { CornerRadius = 5 };
        this.Padding = 0;
        this.WidthRequest = 38;
        this.HeightRequest = ItemHeight;
        this.VerticalOptions = LayoutOptions.Center;

        // Semantics carry what the removed glyph used to imply. An icon-only button is unlabelled to
        // a screen reader otherwise, whatever the tooltip says.
        SemanticProperties.SetDescription(this, hint);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (this.IsEnabled)
                this.Clicked?.Invoke(this, EventArgs.Empty);
        };
        this.GestureRecognizers.Add(tap);

        this.SetDynamicResource(IconColorProperty, ShinyThemeKeys.Color.OnSurface);
        this.SetDynamicResource(ActiveColorProperty, ShinyThemeKeys.Color.Primary);
        this.Repaint();
    }


    /// <summary>What the tooltip says, and what a screen reader reads.</summary>
    public string Hint { get; }

    public event EventHandler? Clicked;


    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor),
        typeof(Color),
        typeof(OfficeToolbarButton),
        Colors.Black,
        propertyChanged: (b, _, _) => ((OfficeToolbarButton)b).Repaint());

    /// <summary>The colour the icon is stroked in. Follows the theme's on-surface token by default.</summary>
    public Color IconColor
    {
        get => (Color)this.GetValue(IconColorProperty);
        set => this.SetValue(IconColorProperty, value);
    }


    public static readonly BindableProperty ActiveColorProperty = BindableProperty.Create(
        nameof(ActiveColor),
        typeof(Color),
        typeof(OfficeToolbarButton),
        Colors.SteelBlue,
        propertyChanged: (b, _, _) => ((OfficeToolbarButton)b).Repaint());

    /// <summary>The accent behind a button whose format is on under the caret.</summary>
    public Color ActiveColor
    {
        get => (Color)this.GetValue(ActiveColorProperty);
        set => this.SetValue(ActiveColorProperty, value);
    }


    /// <summary>Whether the format this button applies is already on under the caret.</summary>
    public bool IsActive
    {
        get => this.active;
        set
        {
            if (this.active == value)
                return;

            this.active = value;
            this.Repaint();
        }
    }


    /// <summary>Enables or disables the button, dimming it to match.</summary>
    public void SetEnabled(bool enabled)
    {
        this.IsEnabled = enabled;
        this.Opacity = enabled ? 1 : 0.35;
    }


    /// <summary>
    /// Turns the hover tooltip on or off.
    /// </summary>
    /// <remarks>
    /// Nothing is built while it is off, so a phone never pays for a tooltip it will not show. Once
    /// one exists it is muted rather than torn down, because the attached tooltip instance is held
    /// against the view for its lifetime either way.
    /// </remarks>
    public void SetTooltipEnabled(bool enabled)
    {
        if (!enabled && !this.tooltipWired)
            return;

        this.tooltipWired = true;

        TooltipProperties.SetPlacement(this, TooltipPlacement.Bottom);
        TooltipProperties.SetAutoDismissDelay(this, 0);
        TooltipProperties.SetTrigger(this, enabled ? TooltipTrigger.Hover : TooltipTrigger.Manual);
        TooltipProperties.SetText(this, enabled ? this.Hint : null);
    }


    void Repaint()
    {
        this.BackgroundColor = this.active ? this.ActiveColor.WithAlpha(0.22f) : Colors.Transparent;
        this.drawable.Color = this.IconColor;
        this.graphics.Invalidate();
    }
}
