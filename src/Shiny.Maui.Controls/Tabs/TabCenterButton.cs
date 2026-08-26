using System.Windows.Input;
using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// The raised button in the middle of a <see cref="ShinyTabBar"/>. Setting one on the bar is what
/// makes it appear; leaving it null gives an ordinary bar with evenly spaced tabs.
/// </summary>
/// <remarks>
/// <para>The button is not a tab. It never becomes the selection and it has no content of its own —
/// it either runs a command (<see cref="TabCenterMode.Action"/>) or presents something above itself
/// (<see cref="TabCenterMode.Menu"/>).</para>
/// <para>What it presents is looked up on the page that is currently showing, not here: see
/// <see cref="ShinyTabs.ActionsProperty"/> and <see cref="ShinyTabs.MenuContentProperty"/>. The
/// <see cref="Actions"/> and <see cref="MenuContent"/> on this class are the app-wide fallback used
/// by pages that declare neither.</para>
/// </remarks>
[ContentProperty(nameof(Actions))]
public class TabCenterButton : BindableObject, ITabIcon
{
    /// <summary>Backing store for <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(TabCenterButton), "plus");

    /// <summary>Backing store for <see cref="IconSource"/>.</summary>
    public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
        nameof(IconSource), typeof(MotionIconDefinition), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="IconPathData"/>.</summary>
    public static readonly BindableProperty IconPathDataProperty = BindableProperty.Create(
        nameof(IconPathData), typeof(string), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="IconImage"/>.</summary>
    public static readonly BindableProperty IconImageProperty = BindableProperty.Create(
        nameof(IconImage), typeof(ImageSource), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="Motion"/>.</summary>
    public static readonly BindableProperty MotionProperty = BindableProperty.Create(
        nameof(Motion), typeof(MotionPreset), typeof(TabCenterButton), MotionPreset.Default);

    /// <summary>Backing store for <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="Mode"/>.</summary>
    public static readonly BindableProperty ModeProperty = BindableProperty.Create(
        nameof(Mode), typeof(TabCenterMode), typeof(TabCenterButton), TabCenterMode.Menu);

    /// <summary>Backing store for <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(TabCenterButton), 60d);

    /// <summary>Backing store for <see cref="IconSize"/>.</summary>
    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize), typeof(double), typeof(TabCenterButton), 26d);

    /// <summary>Backing store for <see cref="Overhang"/>.</summary>
    public static readonly BindableProperty OverhangProperty = BindableProperty.Create(
        nameof(Overhang), typeof(double), typeof(TabCenterButton), -1d);

    /// <summary>Backing store for <see cref="BackgroundColor"/>.</summary>
    public static readonly BindableProperty BackgroundColorProperty = BindableProperty.Create(
        nameof(BackgroundColor), typeof(Color), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="ForegroundColor"/>.</summary>
    public static readonly BindableProperty ForegroundColorProperty = BindableProperty.Create(
        nameof(ForegroundColor), typeof(Color), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="RotateOnOpen"/>.</summary>
    public static readonly BindableProperty RotateOnOpenProperty = BindableProperty.Create(
        nameof(RotateOnOpen), typeof(double), typeof(TabCenterButton), 45d);

    /// <summary>Backing store for <see cref="Command"/>.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="CommandParameter"/>.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="IsEnabled"/>.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
        nameof(IsEnabled), typeof(bool), typeof(TabCenterButton), true);

    /// <summary>Backing store for <see cref="IsVisible"/>.</summary>
    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(TabCenterButton), true);

    /// <summary>Backing store for <see cref="ContentTemplate"/>.</summary>
    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="MenuContent"/>.</summary>
    public static readonly BindableProperty MenuContentProperty = BindableProperty.Create(
        nameof(MenuContent), typeof(View), typeof(TabCenterButton), null);

    /// <summary>Backing store for <see cref="MenuContentTemplate"/>.</summary>
    public static readonly BindableProperty MenuContentTemplateProperty = BindableProperty.Create(
        nameof(MenuContentTemplate), typeof(DataTemplate), typeof(TabCenterButton), null);

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

    /// <summary>An optional caption under the button, aligned with the tab labels.</summary>
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Whether a press runs a command or presents a menu. Defaults to <see cref="TabCenterMode.Menu"/>.</summary>
    public TabCenterMode Mode
    {
        get => (TabCenterMode)this.GetValue(ModeProperty);
        set => this.SetValue(ModeProperty, value);
    }

    /// <summary>Diameter of the circle. Defaults to 60.</summary>
    public double Size
    {
        get => (double)this.GetValue(SizeProperty);
        set => this.SetValue(SizeProperty, value);
    }

    /// <summary>Size of the glyph inside the circle. Defaults to 26.</summary>
    public double IconSize
    {
        get => (double)this.GetValue(IconSizeProperty);
        set => this.SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// How far the button rises above the bar's top edge. Left at -1 it is a third of
    /// <see cref="Size"/>. Set it to 0 to sit the button entirely inside the bar, or to half of
    /// <see cref="Size"/> to centre the circle exactly on the edge.
    /// </summary>
    public double Overhang
    {
        get => (double)this.GetValue(OverhangProperty);
        set => this.SetValue(OverhangProperty, value);
    }

    /// <summary>Unset follows the theme's primary colour.</summary>
    public Color? BackgroundColor
    {
        get => (Color?)this.GetValue(BackgroundColorProperty);
        set => this.SetValue(BackgroundColorProperty, value);
    }

    /// <summary>Unset follows the theme's on-primary colour.</summary>
    public Color? ForegroundColor
    {
        get => (Color?)this.GetValue(ForegroundColorProperty);
        set => this.SetValue(ForegroundColorProperty, value);
    }

    /// <summary>Degrees the glyph rotates while the menu is open. Zero disables it.</summary>
    public double RotateOnOpen
    {
        get => (double)this.GetValue(RotateOnOpenProperty);
        set => this.SetValue(RotateOnOpenProperty, value);
    }

    /// <summary>Run on press, in both modes.</summary>
    public ICommand? Command
    {
        get => (ICommand?)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    /// <summary>Passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    /// <summary>A disabled button is dimmed and ignores presses.</summary>
    public bool IsEnabled
    {
        get => (bool)this.GetValue(IsEnabledProperty);
        set => this.SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Hiding the button leaves its gap in the bar, so tabs do not jump sideways when it comes and
    /// goes. Remove <see cref="ShinyTabBar.CenterButton"/> entirely to reclaim the space.
    /// </summary>
    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }

    /// <summary>
    /// App-wide menu rows, used by pages that set no <see cref="ShinyTabs.ActionsProperty"/> of
    /// their own.
    /// </summary>
    /// <remarks>
    /// A plain collection rather than a bindable property on purpose. A <c>BindableProperty</c>
    /// holding a collection needs a <c>defaultValueCreator</c> to be usable from markup, and those
    /// never raise <c>propertyChanged</c> - so the bar would have nothing to hang a
    /// <c>CollectionChanged</c> subscription off and rows added later would never appear. One
    /// instance per button, created here, has none of that ambiguity.
    /// </remarks>
    public TabActionCollection Actions { get; } = new();

    /// <summary>
    /// Replaces the button's whole visual — the circle, its background, its shadow and its glyph —
    /// with your own. The template's binding context is this <see cref="TabCenterButton"/>, so
    /// <c>{Binding Size}</c> and the rest are reachable from markup.
    /// </summary>
    /// <remarks>
    /// The bar still owns the press, the overhang and the column the button sits in, so
    /// <see cref="Size"/> and <see cref="Overhang"/> keep meaning something — they are the space the
    /// template is given. Everything painted inside it is yours.
    /// </remarks>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)this.GetValue(ContentTemplateProperty);
        set => this.SetValue(ContentTemplateProperty, value);
    }

    /// <summary>App-wide menu content. Beats <see cref="Actions"/> when both are set.</summary>
    public View? MenuContent
    {
        get => (View?)this.GetValue(MenuContentProperty);
        set => this.SetValue(MenuContentProperty, value);
    }

    /// <summary>
    /// App-wide menu content, built the first time the menu opens rather than with the markup.
    /// Beats <see cref="MenuContent"/>.
    /// </summary>
    public DataTemplate? MenuContentTemplate
    {
        get => (DataTemplate?)this.GetValue(MenuContentTemplateProperty);
        set => this.SetValue(MenuContentTemplateProperty, value);
    }

    /// <summary>The effective overhang, resolving the -1 default.</summary>
    /// <remarks>
    /// A third rather than a half. Half centres the circle on the bar's top edge, which is the
    /// textbook diagram but reads as floating away from the bar on a real screen - the button ends
    /// up nearer the content than the tabs it belongs to.
    /// </remarks>
    internal double EffectiveOverhang => this.Overhang < 0 ? this.Size / 3 : this.Overhang;

    internal void Invoke()
    {
        var command = this.Command;
        if (command?.CanExecute(this.CommandParameter) == true)
            command.Execute(this.CommandParameter);
    }

    /// <summary>
    /// The properties that change the bar's <em>layout</em> rather than just how the button is
    /// painted — the ones that move the centre column or the row the button overhangs into.
    /// </summary>
    internal static bool AffectsLayout(string? propertyName) => propertyName
        is nameof(Size) or nameof(Overhang) or nameof(IsVisible) or nameof(Text) or nameof(ContentTemplate);
}
