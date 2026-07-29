using System.Runtime.CompilerServices;

namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// Protects controls from having their <c>propertyChanged</c> callbacks run before their
/// constructor has built the children those callbacks touch.
///
/// <para>
/// MAUI applies an implicit <see cref="Style"/> from <c>StyleableElement</c>'s <b>own</b>
/// constructor: <c>MergedStyle</c>'s ctor calls <c>RegisterImplicitStyles()</c>, which
/// resolves the style out of <c>Application.Current.Resources</c> and applies it there and
/// then. That is before the derived control's constructor body has run a single line, so any
/// callback that dereferences an instance field throws <see cref="NullReferenceException"/>
/// and the app dies while inflating the page:
/// </para>
/// <code>
///     at Microsoft.Maui.Controls.Setter.Apply(...)
///     at Microsoft.Maui.Controls.MergedStyle.set_ImplicitStyle(...)
///     at Microsoft.Maui.Controls.MergedStyle.RegisterImplicitStyles()
///     at Microsoft.Maui.Controls.MergedStyle..ctor(...)
///     at Microsoft.Maui.Controls.StyleableElement..ctor()
/// </code>
/// <para>
/// Reordering a constructor cannot fix this - nothing in it has run yet. Instead, wrap the
/// body of any child-touching callback in <see cref="WhenReady{T}"/> and call
/// <see cref="MarkReady"/> as the last line of the constructor:
/// </para>
/// <code>
///     propertyChanged: (b, _, n) => StyleGuard.WhenReady&lt;MyControl&gt;(b, c => c.label.Text = (string)n)
///     ...
///     public MyControl()
///     {
///         label = new Label();
///         Content = label;
///         StyleGuard.MarkReady(this);
///     }
/// </code>
/// <para>
/// Callbacks that arrive early are <b>queued rather than dropped</b>, then replayed in order
/// by <see cref="MarkReady"/>. That matters: silently swallowing them would turn a loud crash
/// into styles that mysteriously fail to apply, which is the harder bug to find.
/// </para>
/// </summary>
public static class StyleGuard
{
    // Weak keys, so a control that is never marked ready (or is discarded mid-construction)
    // does not keep its queue alive.
    static readonly ConditionalWeakTable<object, List<Action>> pending = new();
    static readonly ConditionalWeakTable<object, object> ready = new();

    /// <summary>
    /// Declares that <paramref name="control"/> has finished building its children, and
    /// replays anything that arrived beforehand. Call this as the last line of the
    /// constructor, after every field is assigned, passing the type whose constructor this
    /// is: <c>StyleGuard.MarkReady(this, typeof(MyControl))</c>.
    /// </summary>
    /// <param name="declaringType">
    /// The type whose constructor is calling. Base constructors always run first, so a base
    /// marking the control ready would replay while the derived class's own fields are still
    /// null - exactly the bug this class exists to prevent, one level up. Passing the
    /// declaring type means only the most-derived constructor actually marks; every other
    /// call in the chain is a no-op. Every instantiable class in a hierarchy should call it,
    /// so whichever one is the runtime type does the marking.
    /// </param>
    public static void MarkReady(object control, Type declaringType)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (control.GetType() != declaringType)
            return;

        ready.AddOrUpdate(control, control);

        if (!pending.TryGetValue(control, out var queued))
            return;

        pending.Remove(control);

        // In arrival order - the style's setters ran in declaration order and later ones are
        // expected to win.
        foreach (var action in queued)
            action();
    }

    /// <summary>
    /// Runs <paramref name="apply"/> now if the control is ready, otherwise queues it until
    /// <see cref="MarkReady"/>.
    /// </summary>
    public static void WhenReady<T>(BindableObject bindable, Action<T> apply)
        where T : class
    {
        if (bindable is not T control)
            return;

        if (ready.TryGetValue(control, out _))
        {
            apply(control);
            return;
        }

        pending.GetOrCreateValue(control).Add(() => apply(control));
    }

    /// <summary>
    /// Runs <paramref name="apply"/> now if the control is ready, otherwise queues it until
    /// <see cref="MarkReady"/>. This overload lets a callback body be wrapped verbatim -
    /// it already closes over the bindable and the new value.
    /// </summary>
    public static void WhenReady(BindableObject bindable, Action apply)
    {
        if (bindable is null)
            return;

        if (ready.TryGetValue(bindable, out _))
        {
            apply();
            return;
        }

        pending.GetOrCreateValue(bindable).Add(apply);
    }

    /// <summary>
    /// True once <see cref="MarkReady"/> has been called. For controls that need to branch on
    /// readiness themselves rather than queue work.
    /// </summary>
    public static bool IsReady(object control) => ready.TryGetValue(control, out _);
}
