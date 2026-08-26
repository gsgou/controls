using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

public partial class Expander
{
    // ---------------------------------------------------------------------------------------------
    // Header
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
        nameof(Header), typeof(View), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>A header view of your own. Wins over <see cref="HeaderTemplate"/> and <see cref="HeaderText"/>.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    public static readonly BindableProperty HeaderTemplateProperty = BindableProperty.Create(
        nameof(HeaderTemplate), typeof(DataTemplate), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>Builds the header against the expander's binding context. Used when <see cref="Header"/> is null.</summary>
    public DataTemplate? HeaderTemplate { get => (DataTemplate?)GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }

    public static readonly BindableProperty HeaderTextProperty = BindableProperty.Create(
        nameof(HeaderText), typeof(string), typeof(Expander), string.Empty,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>Title text for the built-in header, used when neither <see cref="Header"/> nor <see cref="HeaderTemplate"/> is set.</summary>
    public string HeaderText { get => (string)GetValue(HeaderTextProperty); set => SetValue(HeaderTextProperty, value); }

    public static readonly BindableProperty HeaderDetailProperty = BindableProperty.Create(
        nameof(HeaderDetail), typeof(string), typeof(Expander), string.Empty,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>Optional second line under <see cref="HeaderText"/>, for a subtitle or a summary of what is inside.</summary>
    public string HeaderDetail { get => (string)GetValue(HeaderDetailProperty); set => SetValue(HeaderDetailProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Content
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildContent()));
    /// <summary>What the expander reveals. This is the XAML content property, so it can be written between the tags.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildContent()));
    /// <summary>Builds the content against the expander's binding context. Used when <see cref="Content"/> is null.</summary>
    public DataTemplate? ContentTemplate { get => (DataTemplate?)GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    public static readonly BindableProperty LoadContentOnDemandProperty = BindableProperty.Create(
        nameof(LoadContentOnDemand), typeof(bool), typeof(Expander), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildContent()));
    /// <summary>
    /// Hold off on realising <see cref="ContentTemplate"/> until the first expand. A list of twenty
    /// expanders over twenty forms then builds one form, not twenty.
    /// </summary>
    public bool LoadContentOnDemand { get => (bool)GetValue(LoadContentOnDemandProperty); set => SetValue(LoadContentOnDemandProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
        nameof(IsExpanded), typeof(bool), typeof(Expander), false, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(Expander), () =>
            ((Expander)b).OnIsExpandedChanged((bool)o, (bool)n)));
    /// <summary>Whether the content is showing. Two-way by default, so it binds straight to a view model.</summary>
    public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }

    public static readonly BindableProperty IsToggleEnabledProperty = BindableProperty.Create(
        nameof(IsToggleEnabled), typeof(bool), typeof(Expander), true);
    /// <summary>When false the header stops responding to taps; <see cref="IsExpanded"/> still drives it in code.</summary>
    public bool IsToggleEnabled { get => (bool)GetValue(IsToggleEnabledProperty); set => SetValue(IsToggleEnabledProperty, value); }

    public static readonly BindableProperty CanCollapseProperty = BindableProperty.Create(
        nameof(CanCollapse), typeof(bool), typeof(Expander), true);
    /// <summary>
    /// When false, tapping an already-open header does nothing. <see cref="Accordion"/> sets this on the
    /// open item when it is not allowed to close everything.
    /// </summary>
    public bool CanCollapse { get => (bool)GetValue(CanCollapseProperty); set => SetValue(CanCollapseProperty, value); }

    public static readonly BindableProperty ExpandedChangedCommandProperty = BindableProperty.Create(
        nameof(ExpandedChangedCommand), typeof(ICommand), typeof(Expander), null);
    /// <summary>Invoked with the new <see cref="IsExpanded"/> value after every change.</summary>
    public ICommand? ExpandedChangedCommand { get => (ICommand?)GetValue(ExpandedChangedCommandProperty); set => SetValue(ExpandedChangedCommandProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Motion
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty AnimationProperty = BindableProperty.Create(
        nameof(Animation), typeof(ExpanderAnimation), typeof(Expander), ExpanderAnimation.Height | ExpanderAnimation.Fade);
    /// <summary>Which effects run on expand and collapse. Combine them: <c>Animation="Height,Slide,Fade"</c>.</summary>
    public ExpanderAnimation Animation { get => (ExpanderAnimation)GetValue(AnimationProperty); set => SetValue(AnimationProperty, value); }

    public static readonly BindableProperty SlideFromProperty = BindableProperty.Create(
        nameof(SlideFrom), typeof(ExpanderSlideFrom), typeof(Expander), ExpanderSlideFrom.Top);
    /// <summary>Edge the content slides in from when <see cref="ExpanderAnimation.Slide"/> is on.</summary>
    public ExpanderSlideFrom SlideFrom { get => (ExpanderSlideFrom)GetValue(SlideFromProperty); set => SetValue(SlideFromProperty, value); }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration), typeof(uint), typeof(Expander), 250u);
    /// <summary>Length of the expand/collapse animation in milliseconds. Zero snaps.</summary>
    public uint AnimationDuration { get => (uint)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    public static readonly BindableProperty AnimationEasingProperty = BindableProperty.Create(
        nameof(AnimationEasing), typeof(Easing), typeof(Expander), Easing.CubicOut);
    /// <summary>Easing curve for the animation. Defaults to <see cref="Easing.CubicOut"/>.</summary>
    public Easing AnimationEasing { get => (Easing)GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }

    public static readonly BindableProperty ExpandDirectionProperty = BindableProperty.Create(
        nameof(ExpandDirection), typeof(ExpandDirection), typeof(Expander), ExpandDirection.Down,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyLayoutOrder()));
    /// <summary>Whether the content is revealed below the header or above it.</summary>
    public ExpandDirection ExpandDirection { get => (ExpandDirection)GetValue(ExpandDirectionProperty); set => SetValue(ExpandDirectionProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Border
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Outline colour. Unset binds the <c>OutlineVariant</c> theme token, so a theme swap reaches it.</summary>
    public Color? BorderColor { get => (Color?)GetValue(BorderColorProperty); set => SetValue(BorderColorProperty, value); }

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(Expander), ThemeTokens.Unset,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Outline width. Unset binds the <c>Border.Thin</c> theme token; zero removes the outline.</summary>
    public double BorderThickness { get => (double)GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(Expander), ThemeTokens.Unset,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Corner radius. Unset binds the <c>Shape.CornerMedium</c> theme token.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow), typeof(bool), typeof(Expander), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Lift the expander off the page with the theme's level-1 elevation.</summary>
    public bool HasShadow { get => (bool)GetValue(HasShadowProperty); set => SetValue(HasShadowProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Header appearance
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(
        nameof(HeaderBackgroundColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Header fill. Unset binds the <c>SurfaceContainerLow</c> theme token.</summary>
    public Color? HeaderBackgroundColor { get => (Color?)GetValue(HeaderBackgroundColorProperty); set => SetValue(HeaderBackgroundColorProperty, value); }

    public static readonly BindableProperty HeaderTextColorProperty = BindableProperty.Create(
        nameof(HeaderTextColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Colour of the built-in header title. Unset binds the <c>OnSurface</c> theme token.</summary>
    public Color? HeaderTextColor { get => (Color?)GetValue(HeaderTextColorProperty); set => SetValue(HeaderTextColorProperty, value); }

    public static readonly BindableProperty HeaderDetailColorProperty = BindableProperty.Create(
        nameof(HeaderDetailColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Colour of <see cref="HeaderDetail"/>. Unset binds the <c>OnSurfaceVariant</c> theme token.</summary>
    public Color? HeaderDetailColor { get => (Color?)GetValue(HeaderDetailColorProperty); set => SetValue(HeaderDetailColorProperty, value); }

    public static readonly BindableProperty HeaderFontSizeProperty = BindableProperty.Create(
        nameof(HeaderFontSize), typeof(double), typeof(Expander), ThemeTokens.Unset,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Title font size. Unset binds the <c>Type.TitleSmallSize</c> theme token.</summary>
    public double HeaderFontSize { get => (double)GetValue(HeaderFontSizeProperty); set => SetValue(HeaderFontSizeProperty, value); }

    public static readonly BindableProperty HeaderFontFamilyProperty = BindableProperty.Create(
        nameof(HeaderFontFamily), typeof(string), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Title font family. Unset binds the theme's font family.</summary>
    public string? HeaderFontFamily { get => (string?)GetValue(HeaderFontFamilyProperty); set => SetValue(HeaderFontFamilyProperty, value); }

    public static readonly BindableProperty HeaderFontAttributesProperty = BindableProperty.Create(
        nameof(HeaderFontAttributes), typeof(FontAttributes), typeof(Expander), FontAttributes.Bold,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Title font attributes. Defaults to bold.</summary>
    public FontAttributes HeaderFontAttributes { get => (FontAttributes)GetValue(HeaderFontAttributesProperty); set => SetValue(HeaderFontAttributesProperty, value); }

    public static readonly BindableProperty HeaderPaddingProperty = BindableProperty.Create(
        nameof(HeaderPadding), typeof(Thickness), typeof(Expander), new Thickness(16, 12),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Padding inside the header row.</summary>
    public Thickness HeaderPadding { get => (Thickness)GetValue(HeaderPaddingProperty); set => SetValue(HeaderPaddingProperty, value); }

    public static readonly BindableProperty HeaderHeightProperty = BindableProperty.Create(
        nameof(HeaderHeight), typeof(double), typeof(Expander), ThemeTokens.Unset,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Fixed header height. Unset lets the header size to its content, floored at the theme touch target.</summary>
    public double HeaderHeight { get => (double)GetValue(HeaderHeightProperty); set => SetValue(HeaderHeightProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Content appearance
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty ContentBackgroundColorProperty = BindableProperty.Create(
        nameof(ContentBackgroundColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Content fill. Unset binds the <c>Surface</c> theme token.</summary>
    public Color? ContentBackgroundColor { get => (Color?)GetValue(ContentBackgroundColorProperty); set => SetValue(ContentBackgroundColorProperty, value); }

    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(Expander), new Thickness(16, 12),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Padding around the revealed content.</summary>
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Indicator
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty IndicatorModeProperty = BindableProperty.Create(
        nameof(IndicatorMode), typeof(ExpanderIndicatorMode), typeof(Expander), ExpanderIndicatorMode.Rotate,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>Rotate one glyph, swap two, or show none at all.</summary>
    public ExpanderIndicatorMode IndicatorMode { get => (ExpanderIndicatorMode)GetValue(IndicatorModeProperty); set => SetValue(IndicatorModeProperty, value); }

    public static readonly BindableProperty IndicatorPositionProperty = BindableProperty.Create(
        nameof(IndicatorPosition), typeof(ExpanderIndicatorPosition), typeof(Expander), ExpanderIndicatorPosition.End,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>Leading or trailing edge of the header.</summary>
    public ExpanderIndicatorPosition IndicatorPosition { get => (ExpanderIndicatorPosition)GetValue(IndicatorPositionProperty); set => SetValue(IndicatorPositionProperty, value); }

    public static readonly BindableProperty IndicatorViewProperty = BindableProperty.Create(
        nameof(IndicatorView), typeof(View), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).RebuildHeader()));
    /// <summary>
    /// Your own indicator view instead of a glyph. Under <see cref="ExpanderIndicatorMode.Rotate"/> it is
    /// the thing that rotates, so an icon, an image or a motion icon all work.
    /// </summary>
    public View? IndicatorView { get => (View?)GetValue(IndicatorViewProperty); set => SetValue(IndicatorViewProperty, value); }

    public static readonly BindableProperty CollapsedIconProperty = BindableProperty.Create(
        nameof(CollapsedIcon), typeof(string), typeof(Expander), "\u25B6\uFE0E",
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyIndicator(animate: false)));
    /// <summary>
    /// Glyph shown when collapsed — and, under <see cref="ExpanderIndicatorMode.Rotate"/>, the only glyph.
    /// Defaults to ▶ carrying U+FE0E: without that variation selector iOS draws U+25B6 as the glossy blue
    /// play-button <em>emoji</em> rather than a text triangle.
    /// </summary>
    public string CollapsedIcon { get => (string)GetValue(CollapsedIconProperty); set => SetValue(CollapsedIconProperty, value); }

    public static readonly BindableProperty ExpandedIconProperty = BindableProperty.Create(
        nameof(ExpandedIcon), typeof(string), typeof(Expander), "\u25BC\uFE0E",
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyIndicator(animate: false)));
    /// <summary>Glyph shown when expanded under <see cref="ExpanderIndicatorMode.Swap"/>. Defaults to ▼, with the same U+FE0E text-presentation selector as <see cref="CollapsedIcon"/>.</summary>
    public string ExpandedIcon { get => (string)GetValue(ExpandedIconProperty); set => SetValue(ExpandedIconProperty, value); }

    public static readonly BindableProperty IndicatorColorProperty = BindableProperty.Create(
        nameof(IndicatorColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Glyph colour. Unset binds the <c>OnSurfaceVariant</c> theme token.</summary>
    public Color? IndicatorColor { get => (Color?)GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    public static readonly BindableProperty IndicatorSizeProperty = BindableProperty.Create(
        nameof(IndicatorSize), typeof(double), typeof(Expander), 14d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Glyph font size.</summary>
    public double IndicatorSize { get => (double)GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }


    // ---------------------------------------------------------------------------------------------
    // Separator
    // ---------------------------------------------------------------------------------------------

    public static readonly BindableProperty ShowSeparatorProperty = BindableProperty.Create(
        nameof(ShowSeparator), typeof(bool), typeof(Expander), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Draw a hairline between the header and the content while open.</summary>
    public bool ShowSeparator { get => (bool)GetValue(ShowSeparatorProperty); set => SetValue(ShowSeparatorProperty, value); }

    public static readonly BindableProperty SeparatorColorProperty = BindableProperty.Create(
        nameof(SeparatorColor), typeof(Color), typeof(Expander), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(Expander), () => ((Expander)b).ApplyAppearance()));
    /// <summary>Separator colour. Unset binds the <c>OutlineVariant</c> theme token.</summary>
    public Color? SeparatorColor { get => (Color?)GetValue(SeparatorColorProperty); set => SetValue(SeparatorColorProperty, value); }
}
