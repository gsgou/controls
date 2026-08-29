namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// Insulates a control's own internal parts from the host app's implicit styles.
/// </summary>
/// <remarks>
/// <para>
/// A Shiny control is meant to be styleable - an implicit <c>Style TargetType="shiny:PillView"</c>
/// should reach it. What is not meant to reach it is the app's implicit style for the <b>primitive
/// types the control happens to be built from</b>. The .NET MAUI project template ships
/// </para>
/// <code>
///     &lt;Style TargetType="Button"&gt;
///       &lt;Setter Property="VisualStateManager.VisualStateGroups"&gt;
///         ... &lt;VisualState x:Name="Disabled"&gt;
///               &lt;Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray600}}" /&gt;
/// </code>
/// <para>
/// and that lands on every <c>Button</c> in the process, including the flat, transparent glyph
/// buttons inside a <c>DataGrid</c> pager. The visible result in dark mode is that the
/// <i>disabled</i> first/previous buttons grow an opaque Gray600 slab while the enabled ones stay
/// transparent - the disabled controls read as the prominent ones.
/// </para>
/// <para>
/// Setting <c>BackgroundColor</c> on the button does not help: a visual state's setters are applied
/// over the top whenever that state is entered. The fix is to give the part its own
/// <see cref="VisualStateGroup"/> list, set directly on the element. A locally-set attached property
/// beats the same property arriving through a style, so the app's group list is never applied and
/// the part keeps the appearance the control designed for it.
/// </para>
/// </remarks>
public static class InternalChrome
{
    /// <summary>
    /// Gives <paramref name="view"/> a neutral CommonStates group so the host's implicit style
    /// cannot repaint it per state. Disabled is expressed as opacity, which works against whatever
    /// background the control has chosen and needs no colour of its own.
    /// </summary>
    /// <param name="view">The internal part to insulate.</param>
    /// <param name="disabledOpacity">Opacity for the Disabled state.</param>
    public static T Neutralize<T>(this T view, double disabledOpacity = 0.35)
        where T : VisualElement
    {
        var common = new VisualStateGroup { Name = "CommonStates" };

        common.States.Add(new VisualState { Name = "Normal" });
        common.States.Add(new VisualState { Name = "PointerOver" });
        common.States.Add(new VisualState
        {
            Name = "Disabled",
            Setters =
            {
                new Setter { Property = VisualElement.OpacityProperty, Value = disabledOpacity }
            }
        });

        VisualStateManager.SetVisualStateGroups(view, [common]);
        return view;
    }
}
