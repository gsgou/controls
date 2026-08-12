using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class ShinyButton
{
    // ---------------------------------------------------------------------------------------------
    // Text
    // ---------------------------------------------------------------------------------------------

    /// <summary>The button's label. See also <see cref="BusyText"/>, <see cref="SuccessText"/> and
    /// <see cref="ErrorText"/>, which temporarily stand in for it.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyText()));
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Label colour. Unset follows <see cref="Appearance"/> and <see cref="Type"/>.</summary>
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public Color? TextColor
    {
        get => (Color?)this.GetValue(TextColorProperty);
        set => this.SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(ShinyButton), DefaultFontSize,
        propertyChanged: (b, _, n) => Ready(b, x => x.textLabel.FontSize = (double)n));
    [System.ComponentModel.TypeConverter(typeof(FontSizeConverter))]
    public double FontSize
    {
        get => (double)this.GetValue(FontSizeProperty);
        set => this.SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, n) => Ready(b, x => x.textLabel.FontFamily = n as string));
    public string? FontFamily
    {
        get => (string?)this.GetValue(FontFamilyProperty);
        set => this.SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(ShinyButton), Microsoft.Maui.Controls.FontAttributes.None,
        propertyChanged: (b, _, n) => Ready(b, x => x.textLabel.FontAttributes = (FontAttributes)n));
    public FontAttributes FontAttributes
    {
        get => (FontAttributes)this.GetValue(FontAttributesProperty);
        set => this.SetValue(FontAttributesProperty, value);
    }

    public static readonly BindableProperty CharacterSpacingProperty = BindableProperty.Create(
        nameof(CharacterSpacing), typeof(double), typeof(ShinyButton), 0d,
        propertyChanged: (b, _, n) => Ready(b, x => x.textLabel.CharacterSpacing = (double)n));
    public double CharacterSpacing
    {
        get => (double)this.GetValue(CharacterSpacingProperty);
        set => this.SetValue(CharacterSpacingProperty, value);
    }

    /// <summary>Defaults to <see cref="LineBreakMode.NoWrap"/> — a button that reflows mid-press is
    /// worse than one that clips.</summary>
    public static readonly BindableProperty LineBreakModeProperty = BindableProperty.Create(
        nameof(LineBreakMode), typeof(LineBreakMode), typeof(ShinyButton), Microsoft.Maui.LineBreakMode.NoWrap,
        propertyChanged: (b, _, n) => Ready(b, x => x.textLabel.LineBreakMode = (LineBreakMode)n));
    public LineBreakMode LineBreakMode
    {
        get => (LineBreakMode)this.GetValue(LineBreakModeProperty);
        set => this.SetValue(LineBreakModeProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Surface
    // ---------------------------------------------------------------------------------------------

    /// <summary>How much of the button is painted. See <see cref="ButtonAppearance"/>.</summary>
    public static readonly BindableProperty AppearanceProperty = BindableProperty.Create(
        nameof(Appearance), typeof(ButtonAppearance), typeof(ShinyButton), ButtonAppearance.Filled,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public ButtonAppearance Appearance
    {
        get => (ButtonAppearance)this.GetValue(AppearanceProperty);
        set => this.SetValue(AppearanceProperty, value);
    }

    /// <summary>Which semantic colour family the button draws from. See <see cref="ButtonType"/>.</summary>
    public static readonly BindableProperty TypeProperty = BindableProperty.Create(
        nameof(Type), typeof(ButtonType), typeof(ShinyButton), ButtonType.Primary,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public ButtonType Type
    {
        get => (ButtonType)this.GetValue(TypeProperty);
        set => this.SetValue(TypeProperty, value);
    }

    /// <summary>Explicit fill. Wins over <see cref="Appearance"/>/<see cref="Type"/>; unset returns
    /// to the theme token.</summary>
    public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
        nameof(ButtonBackgroundColor), typeof(Color), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public Color? ButtonBackgroundColor
    {
        get => (Color?)this.GetValue(ButtonBackgroundColorProperty);
        set => this.SetValue(ButtonBackgroundColorProperty, value);
    }

    /// <summary>Explicit outline colour. Unset follows the appearance.</summary>
    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public Color? BorderColor
    {
        get => (Color?)this.GetValue(BorderColorProperty);
        set => this.SetValue(BorderColorProperty, value);
    }

    /// <summary>
    /// Outline thickness. The default, <c>-1</c>, lets <see cref="Appearance"/> decide — 1 for
    /// <see cref="ButtonAppearance.Outlined"/> and 0 for everything else.
    /// </summary>
    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(ShinyButton), -1d,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public double BorderThickness
    {
        get => (double)this.GetValue(BorderThicknessProperty);
        set => this.SetValue(BorderThicknessProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ShinyButton), DefaultCornerRadius,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyCornerRadius()));
    public double CornerRadius
    {
        get => (double)this.GetValue(CornerRadiusProperty);
        set => this.SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// The inset between the button's edge and its content. Named apart from
    /// <see cref="View.Margin"/>/<c>Padding</c> because <c>ContentView.Padding</c> sits
    /// <em>outside</em> the painted surface, which is not what a button's padding means.
    /// </summary>
    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(ShinyButton), new Thickness(16, 10),
        propertyChanged: (b, _, n) => Ready(b, x => x.border.Padding = (Thickness)n));
    public Thickness ContentPadding
    {
        get => (Thickness)this.GetValue(ContentPaddingProperty);
        set => this.SetValue(ContentPaddingProperty, value);
    }

    /// <summary>
    /// Drop shadow. Unset lets <see cref="Appearance"/> decide — on for
    /// <see cref="ButtonAppearance.Elevated"/>, off otherwise.
    /// </summary>
    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow), typeof(bool?), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public bool? HasShadow
    {
        get => (bool?)this.GetValue(HasShadowProperty);
        set => this.SetValue(HasShadowProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Icons
    // ---------------------------------------------------------------------------------------------

    /// <summary>An image for the leading slot.</summary>
    public static readonly BindableProperty LeftIconProperty = BindableProperty.Create(
        nameof(LeftIcon), typeof(ImageSource), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public ImageSource? LeftIcon
    {
        get => (ImageSource?)this.GetValue(LeftIconProperty);
        set => this.SetValue(LeftIconProperty, value);
    }

    /// <summary>An image for the trailing slot.</summary>
    public static readonly BindableProperty RightIconProperty = BindableProperty.Create(
        nameof(RightIcon), typeof(ImageSource), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public ImageSource? RightIcon
    {
        get => (ImageSource?)this.GetValue(RightIconProperty);
        set => this.SetValue(RightIconProperty, value);
    }

    /// <summary>
    /// The name of a motion icon for the leading slot — see
    /// <see cref="Shiny.Controls.MotionIcons.MotionIconLibrary"/>. Takes precedence over
    /// <see cref="LeftIcon"/>. The button colours it, and plays it once on tap when
    /// <see cref="MotionIconPlayOnClick"/> is set.
    /// </summary>
    public static readonly BindableProperty LeftMotionIconProperty = BindableProperty.Create(
        nameof(LeftMotionIcon), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public string? LeftMotionIcon
    {
        get => (string?)this.GetValue(LeftMotionIconProperty);
        set => this.SetValue(LeftMotionIconProperty, value);
    }

    /// <summary>A motion icon for the trailing slot. Takes precedence over <see cref="RightIcon"/>.</summary>
    public static readonly BindableProperty RightMotionIconProperty = BindableProperty.Create(
        nameof(RightMotionIcon), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public string? RightMotionIcon
    {
        get => (string?)this.GetValue(RightMotionIconProperty);
        set => this.SetValue(RightMotionIconProperty, value);
    }

    /// <summary>
    /// An arbitrary view for the leading slot — a badge, a spinner of your own, an avatar. Wins over
    /// both <see cref="LeftMotionIcon"/> and <see cref="LeftIcon"/>.
    /// </summary>
    public static readonly BindableProperty LeftIconViewProperty = BindableProperty.Create(
        nameof(LeftIconView), typeof(View), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public View? LeftIconView
    {
        get => (View?)this.GetValue(LeftIconViewProperty);
        set => this.SetValue(LeftIconViewProperty, value);
    }

    /// <summary>An arbitrary view for the trailing slot.</summary>
    public static readonly BindableProperty RightIconViewProperty = BindableProperty.Create(
        nameof(RightIconView), typeof(View), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIcons()));
    public View? RightIconView
    {
        get => (View?)this.GetValue(RightIconViewProperty);
        set => this.SetValue(RightIconViewProperty, value);
    }

    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize), typeof(double), typeof(ShinyButton), DefaultIconSize,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyIconSize()));
    public double IconSize
    {
        get => (double)this.GetValue(IconSizeProperty);
        set => this.SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Icon tint. Unset follows the resolved foreground, so icons match the label by default.
    /// </summary>
    /// <remarks>
    /// A MAUI <see cref="Image"/> cannot be tinted, so this reaches a
    /// <see cref="FontImageSource"/> glyph and a motion icon but leaves a PNG whatever colour it was
    /// drawn — the same limitation, and the same behaviour, as <see cref="IconTextTool"/>.
    /// </remarks>
    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor), typeof(Color), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyAppearance()));
    public Color? IconColor
    {
        get => (Color?)this.GetValue(IconColorProperty);
        set => this.SetValue(IconColorProperty, value);
    }

    /// <summary>Gap between an icon and the label. Only applied where both are present.</summary>
    public static readonly BindableProperty IconSpacingProperty = BindableProperty.Create(
        nameof(IconSpacing), typeof(double), typeof(ShinyButton), DefaultIconSpacing,
        propertyChanged: (b, _, _) => Ready(b, x => x.UpdateContentLayout()));
    public double IconSpacing
    {
        get => (double)this.GetValue(IconSpacingProperty);
        set => this.SetValue(IconSpacingProperty, value);
    }

    /// <summary>Where the icons sit relative to the text.</summary>
    public static readonly BindableProperty ContentLayoutProperty = BindableProperty.Create(
        nameof(ContentLayout), typeof(ButtonContentLayout), typeof(ShinyButton), ButtonContentLayout.Sides,
        propertyChanged: (b, _, _) => Ready(b, x => x.UpdateContentLayout()));
    public ButtonContentLayout ContentLayout
    {
        get => (ButtonContentLayout)this.GetValue(ContentLayoutProperty);
        set => this.SetValue(ContentLayoutProperty, value);
    }

    /// <summary>Whether motion icons in the slots play one cycle when the button is tapped.</summary>
    public static readonly BindableProperty MotionIconPlayOnClickProperty = BindableProperty.Create(
        nameof(MotionIconPlayOnClick), typeof(bool), typeof(ShinyButton), true);
    public bool MotionIconPlayOnClick
    {
        get => (bool)this.GetValue(MotionIconPlayOnClickProperty);
        set => this.SetValue(MotionIconPlayOnClickProperty, value);
    }

    /// <summary>Stroke weight for motion icons, in their own 24-unit space. The set is drawn for 2.</summary>
    public static readonly BindableProperty MotionIconStrokeWidthProperty = BindableProperty.Create(
        nameof(MotionIconStrokeWidth), typeof(double), typeof(ShinyButton), 2d,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyMotionStroke()));
    public double MotionIconStrokeWidth
    {
        get => (double)this.GetValue(MotionIconStrokeWidthProperty);
        set => this.SetValue(MotionIconStrokeWidthProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// What the button is doing. Two-way by default, so a
    /// <see cref="StateRevertDelay"/> expiring reports itself back to a bound view model.
    /// </summary>
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(ButtonState), typeof(ShinyButton), ButtonState.Normal,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, o, n) => Ready(b, x => x.OnStateChanged((ButtonState)o, (ButtonState)n)));
    public ButtonState State
    {
        get => (ButtonState)this.GetValue(StateProperty);
        set => this.SetValue(StateProperty, value);
    }

    /// <summary>
    /// Shorthand for <see cref="State"/> being <see cref="ButtonState.Busy"/>, for the common case
    /// where a view model only has an <c>IsSaving</c> flag.
    /// </summary>
    /// <remarks>
    /// Setting it false only returns the button to <see cref="ButtonState.Normal"/> if it is
    /// currently busy — it will not cut a <see cref="ButtonState.Success"/> or
    /// <see cref="ButtonState.Error"/> short, because a view model clearing its busy flag at the end
    /// of an operation is exactly when the outcome is being shown.
    /// </remarks>
    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(ShinyButton), false,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((ShinyButton)b).OnIsBusyChanged((bool)n));
    public bool IsBusy
    {
        get => (bool)this.GetValue(IsBusyProperty);
        set => this.SetValue(IsBusyProperty, value);
    }

    /// <summary>What the busy state does to the content.</summary>
    public static readonly BindableProperty BusyModeProperty = BindableProperty.Create(
        nameof(BusyMode), typeof(ButtonBusyMode), typeof(ShinyButton), ButtonBusyMode.ReplaceLeftIcon,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyState()));
    public ButtonBusyMode BusyMode
    {
        get => (ButtonBusyMode)this.GetValue(BusyModeProperty);
        set => this.SetValue(BusyModeProperty, value);
    }

    /// <summary>Stands in for <see cref="Text"/> while busy. Null keeps the label as-is.</summary>
    public static readonly BindableProperty BusyTextProperty = BindableProperty.Create(
        nameof(BusyText), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyText()));
    public string? BusyText
    {
        get => (string?)this.GetValue(BusyTextProperty);
        set => this.SetValue(BusyTextProperty, value);
    }

    /// <summary>Stands in for <see cref="Text"/> in the success state.</summary>
    public static readonly BindableProperty SuccessTextProperty = BindableProperty.Create(
        nameof(SuccessText), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyText()));
    public string? SuccessText
    {
        get => (string?)this.GetValue(SuccessTextProperty);
        set => this.SetValue(SuccessTextProperty, value);
    }

    /// <summary>Stands in for <see cref="Text"/> in the error state.</summary>
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
        nameof(ErrorText), typeof(string), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyText()));
    public string? ErrorText
    {
        get => (string?)this.GetValue(ErrorTextProperty);
        set => this.SetValue(ErrorTextProperty, value);
    }

    /// <summary>
    /// The motion icon used as the busy indicator. Defaults to <c>loader</c>; clearing it falls back
    /// to a platform <see cref="ActivityIndicator"/>.
    /// </summary>
    public static readonly BindableProperty BusyMotionIconProperty = BindableProperty.Create(
        nameof(BusyMotionIcon), typeof(string), typeof(ShinyButton), "loader",
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildBusyIndicator()));
    public string? BusyMotionIcon
    {
        get => (string?)this.GetValue(BusyMotionIconProperty);
        set => this.SetValue(BusyMotionIconProperty, value);
    }

    /// <summary>A view of your own to use as the busy indicator. Wins over
    /// <see cref="BusyMotionIcon"/>.</summary>
    public static readonly BindableProperty BusyIconViewProperty = BindableProperty.Create(
        nameof(BusyIconView), typeof(View), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildBusyIndicator()));
    public View? BusyIconView
    {
        get => (View?)this.GetValue(BusyIconViewProperty);
        set => this.SetValue(BusyIconViewProperty, value);
    }

    /// <summary>The motion icon shown in the success state. Defaults to <c>check</c>.</summary>
    public static readonly BindableProperty SuccessMotionIconProperty = BindableProperty.Create(
        nameof(SuccessMotionIcon), typeof(string), typeof(ShinyButton), "check",
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildStateIndicator(ButtonState.Success)));
    public string? SuccessMotionIcon
    {
        get => (string?)this.GetValue(SuccessMotionIconProperty);
        set => this.SetValue(SuccessMotionIconProperty, value);
    }

    /// <summary>The motion icon shown in the error state. Defaults to <c>warning</c>.</summary>
    public static readonly BindableProperty ErrorMotionIconProperty = BindableProperty.Create(
        nameof(ErrorMotionIcon), typeof(string), typeof(ShinyButton), "warning",
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildStateIndicator(ButtonState.Error)));
    public string? ErrorMotionIcon
    {
        get => (string?)this.GetValue(ErrorMotionIconProperty);
        set => this.SetValue(ErrorMotionIconProperty, value);
    }

    /// <summary>An image for the success state, if you would rather not use a motion icon.</summary>
    public static readonly BindableProperty SuccessIconProperty = BindableProperty.Create(
        nameof(SuccessIcon), typeof(ImageSource), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildStateIndicator(ButtonState.Success)));
    public ImageSource? SuccessIcon
    {
        get => (ImageSource?)this.GetValue(SuccessIconProperty);
        set => this.SetValue(SuccessIconProperty, value);
    }

    /// <summary>An image for the error state.</summary>
    public static readonly BindableProperty ErrorIconProperty = BindableProperty.Create(
        nameof(ErrorIcon), typeof(ImageSource), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => Ready(b, x => x.RebuildStateIndicator(ButtonState.Error)));
    public ImageSource? ErrorIcon
    {
        get => (ImageSource?)this.GetValue(ErrorIconProperty);
        set => this.SetValue(ErrorIconProperty, value);
    }

    /// <summary>
    /// How long <see cref="ButtonState.Success"/> and <see cref="ButtonState.Error"/> hold before
    /// returning to <see cref="ButtonState.Normal"/>. <see cref="TimeSpan.Zero"/> holds forever.
    /// </summary>
    public static readonly BindableProperty StateRevertDelayProperty = BindableProperty.Create(
        nameof(StateRevertDelay), typeof(TimeSpan), typeof(ShinyButton), TimeSpan.FromSeconds(1.5));
    public TimeSpan StateRevertDelay
    {
        get => (TimeSpan)this.GetValue(StateRevertDelayProperty);
        set => this.SetValue(StateRevertDelayProperty, value);
    }

    /// <summary>Whether the button stops accepting taps while busy.</summary>
    public static readonly BindableProperty DisableWhileBusyProperty = BindableProperty.Create(
        nameof(DisableWhileBusy), typeof(bool), typeof(ShinyButton), true,
        propertyChanged: (b, _, _) => ((ShinyButton)b).RefreshIsEnabledProperty());
    public bool DisableWhileBusy
    {
        get => (bool)this.GetValue(DisableWhileBusyProperty);
        set => this.SetValue(DisableWhileBusyProperty, value);
    }

    /// <summary>
    /// Whether the button drives its own busy state while an async <see cref="Command"/> runs.
    /// </summary>
    /// <remarks>
    /// Turn this off to drive <see cref="State"/> entirely from a view model — which is also what
    /// full-trim and NativeAOT consumers should do, since the detection uses reflection. See
    /// <c>AsyncCommandProbe</c>.
    /// </remarks>
    public static readonly BindableProperty AutoBusyProperty = BindableProperty.Create(
        nameof(AutoBusy), typeof(bool), typeof(ShinyButton), true);
    public bool AutoBusy
    {
        get => (bool)this.GetValue(AutoBusyProperty);
        set => this.SetValue(AutoBusyProperty, value);
    }

    /// <summary>
    /// Whether a faulted async <see cref="Command"/> puts the button into
    /// <see cref="ButtonState.Error"/>. Only consulted when <see cref="AutoBusy"/> is on.
    /// </summary>
    public static readonly BindableProperty ShowErrorOnFaultProperty = BindableProperty.Create(
        nameof(ShowErrorOnFault), typeof(bool), typeof(ShinyButton), true);
    public bool ShowErrorOnFault
    {
        get => (bool)this.GetValue(ShowErrorOnFaultProperty);
        set => this.SetValue(ShowErrorOnFaultProperty, value);
    }


    // ---------------------------------------------------------------------------------------------
    // Command
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Run on tap. The button follows the command's <see cref="ICommand.CanExecuteChanged"/> — a
    /// command that cannot execute renders and behaves as disabled.
    /// </summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ShinyButton), null,
        propertyChanged: (b, o, n) => ((ShinyButton)b).OnCommandChanged(o as ICommand, n as ICommand));
    public ICommand? Command
    {
        get => (ICommand?)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(ShinyButton), null,
        propertyChanged: (b, _, _) => ((ShinyButton)b).RefreshCanExecute());
    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    /// <summary>Whether a tap routes through <see cref="IFeedbackService"/> (haptics by default).</summary>
    public static readonly BindableProperty UseFeedbackProperty = BindableProperty.Create(
        nameof(UseFeedback), typeof(bool), typeof(ShinyButton), true);
    public bool UseFeedback
    {
        get => (bool)this.GetValue(UseFeedbackProperty);
        set => this.SetValue(UseFeedbackProperty, value);
    }

    /// <summary>
    /// The bottom of the press flash — the button dips to this opacity and back on tap. Set it to 1
    /// to turn press feedback off.
    /// </summary>
    /// <remarks>
    /// A flash rather than a held-down state, because a <see cref="ContentView"/> has no portable
    /// touch-down signal: <see cref="TapGestureRecognizer"/> only reports the release. Faking
    /// <c>Pressed</c>/<c>Released</c> events off a tap would be worse than not offering them, so the
    /// button raises <see cref="ShinyButton.Clicked"/> only.
    /// </remarks>
    public static readonly BindableProperty PressedOpacityProperty = BindableProperty.Create(
        nameof(PressedOpacity), typeof(double), typeof(ShinyButton), 0.6);
    public double PressedOpacity
    {
        get => (double)this.GetValue(PressedOpacityProperty);
        set => this.SetValue(PressedOpacityProperty, value);
    }

    /// <summary>
    /// How far the surface dims while the button is disabled — whether that is
    /// <see cref="VisualElement.IsEnabled"/>, a <see cref="Command"/> that cannot execute, or
    /// <see cref="DisableWhileBusy"/>. Defaults to the Material 3 disabled opacity.
    /// </summary>
    public static readonly BindableProperty DisabledOpacityProperty = BindableProperty.Create(
        nameof(DisabledOpacity), typeof(double), typeof(ShinyButton), 0.38,
        propertyChanged: (b, _, _) => Ready(b, x => x.ApplyEnabledState()));
    public double DisabledOpacity
    {
        get => (double)this.GetValue(DisabledOpacityProperty);
        set => this.SetValue(DisabledOpacityProperty, value);
    }


    // Shorthand for the StyleGuard dance every child-touching callback above needs. See StyleGuard:
    // MAUI applies an implicit Style from its own base constructor, so these callbacks can fire
    // before `border`/`textLabel` exist.
    static void Ready(BindableObject b, Action<ShinyButton> apply)
        => StyleGuard.WhenReady(b, typeof(ShinyButton), () => apply((ShinyButton)b));
}
