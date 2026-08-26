namespace Shiny.Maui.Controls;

/// <summary>
/// Animates a tab as it becomes the selected one, and as it stops being it.
/// </summary>
/// <remarks>
/// <para>Set one on <see cref="ShinyTabBar.Animator"/> to replace the built-in
/// <see cref="TabSelectionAnimation"/> entirely. It is called once per tab whose selected state has
/// actually changed — never on a restyle, a badge update or a rebuild, so an animation is not
/// replayed by something the user cannot see.</para>
/// <para>The pieces of the cell are handed over individually because they usually want different
/// treatment: lifting the whole cell moves the label with the icon, which is rarely wanted, and
/// scaling only the icon leaves the label steady underneath it.</para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class SpinAnimator : ITabAnimator
/// {
///     public Task AnimateAsync(TabAnimationContext context)
///         =&gt; context.Icon?.RotateToAsync(context.IsSelected ? 360 : 0, context.Duration) ?? Task.CompletedTask;
/// }
/// </code>
/// </example>
public interface ITabAnimator
{
    /// <summary>
    /// Run the animation. Awaited, but never blocking the selection — the content has already
    /// changed by the time this is called, so a long animation delays nothing but itself.
    /// </summary>
    Task AnimateAsync(TabAnimationContext context);
}


/// <summary>Everything an <see cref="ITabAnimator"/> gets to work with for one tab.</summary>
public class TabAnimationContext
{
    /// <summary>The tab being animated.</summary>
    public required ShinyTabItem Item { get; init; }

    /// <summary>The whole cell — indicator, icon and label together.</summary>
    public required View Cell { get; init; }

    /// <summary>
    /// The icon view: a <see cref="MotionIconView"/> for motion artwork, an <see cref="Image"/> for
    /// a bitmap, or null for a tab with no icon at all.
    /// </summary>
    public View? Icon { get; init; }

    /// <summary>The tab's label. Present even when hidden by <see cref="ShinyTabBar.LabelMode"/>.</summary>
    public required Label Label { get; init; }

    /// <summary>The selection indicator — the pill, line, underline or dot for the current style.</summary>
    public View? Indicator { get; init; }

    /// <summary>True when the tab is becoming selected, false when it is losing the selection.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>The bar's <see cref="ShinyTabBar.AnimationDuration"/>, in milliseconds.</summary>
    public required uint Duration { get; init; }

    /// <summary>The bar itself, for anything the context does not carry.</summary>
    public required ShinyTabBar Bar { get; init; }
}


/// <summary>The built-in tab selection animations.</summary>
public enum TabSelectionAnimation
{
    /// <summary>No animation. The colour and indicator still change, just instantly.</summary>
    None,

    /// <summary>The icon grows slightly into the selection and settles back on the way out. The default.</summary>
    Scale,

    /// <summary>The icon rises a few points and drops back.</summary>
    Lift,

    /// <summary>The icon overshoots and springs back — a livelier <see cref="Scale"/>.</summary>
    Bounce,

    /// <summary>The label fades in and out under a steady icon.</summary>
    Fade,

    /// <summary>The indicator grows in from nothing rather than simply appearing.</summary>
    Indicator
}
