namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// Resolves a <c>Color</c> theme token on a hidden element in the tree, for code that needs the colour
/// itself — something drawn, an alpha applied to it, ink chosen against it.
/// </summary>
/// <remarks>
/// A <c>Brush</c>-typed property (<c>Border.Stroke</c>, <c>Border.Background</c>, <c>Shadow.Brush</c>)
/// does not need a probe: point it at the brush twin of the token with
/// <see cref="Themes.ThemeBrush.Apply(Element, BindableProperty, Color?, string)"/>. Do not bind a brush's
/// <c>Color</c> to a probe and leave that one brush on the property. Every other platform repaints when the
/// brush's colour changes; Windows keeps the colour the native view was first drawn with, which is how a
/// walkthrough callout stayed in the tooltip's dark inverse surface after switching to a light surface
/// container. A brush token is replaced along with the dictionary on a theme swap, so the property is
/// handed a new brush instead.
/// </remarks>
static class ThemeProbe
{
    /// <summary>
    /// A hidden, zero-sized element that exists only to resolve a colour token. Add it to a layout
    /// inside the control, drive it with <see cref="Tint"/>, and read or watch its <c>Color</c>.
    /// </summary>
    public static BoxView CreateProbe() => new()
    {
        // Exists only to resolve a resource: it must never be measured or paint anything.
        IsVisible = false,
        WidthRequest = 0,
        HeightRequest = 0,
        InputTransparent = true
    };


    /// <summary>
    /// Points a property at an explicit colour, or back at a theme token when the consumer has not set
    /// one. This is the "leave it unset and it follows the theme" behaviour every Shiny control has.
    /// </summary>
    /// <remarks>
    /// <see cref="BindableObject.SetValue(BindableProperty, object)"/> writes a <em>local</em> value,
    /// and a local value outranks a dynamic resource. So going explicit-colour → token has to clear
    /// the local value first: without the <see cref="BindableObject.ClearValue(BindableProperty)"/>
    /// the <c>SetDynamicResource</c> below is accepted, resolves, and is then quietly outranked by the
    /// colour still sitting in the local slot. Nothing errors — the control simply keeps the old
    /// colour, which is how a walkthrough callout that followed a spotlight step (transparent fill)
    /// stayed transparent and rendered its card-less text on the dim.
    /// </remarks>
    public static void Tint(Element target, BindableProperty property, Color? value, string token)
    {
        if (value is null)
        {
            target.ClearValue(property);
            target.SetDynamicResource(property, token);
        }
        else
        {
            target.RemoveDynamicResource(property);
            target.SetValue(property, value);
        }
    }


    /// <summary>
    /// <see cref="Tint"/> for a control that must still paint when no Shiny theme is installed: the
    /// token when the app has one, the literal <paramref name="fallback"/> only when it does not.
    /// </summary>
    /// <remarks>
    /// The fallback cannot simply be seeded first and the token bound over it. That local value
    /// outranks the dynamic resource, so the fallback would win in every theme and through every live
    /// theme switch — which is exactly how dialogs and the Kanban board ignored the theme. A dynamic
    /// resource whose key is missing leaves the property untouched, though, so with no theme at all
    /// something has to paint: the fallback is written only when the key cannot be resolved.
    /// <c>UseShinyControls()</c> always installs a theme, so in an app that is the uncommon path.
    /// </remarks>
    public static void TintOrFallback(Element target, BindableProperty property, Color? value, string token, Color fallback)
    {
        if (value is not null || HasToken(token))
        {
            Tint(target, property, value, token);
            return;
        }

        target.RemoveDynamicResource(property);
        target.SetValue(property, fallback);
    }


    /// <summary>Whether the app's resources (merged theme dictionaries included) define <paramref name="token"/>.</summary>
    public static bool HasToken(string token)
        => Application.Current?.Resources.TryGetValue(token, out _) == true;
}
