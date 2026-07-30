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
/// body of any child-touching callback in <see cref="WhenReady(BindableObject, Type, Action)"/>,
/// passing the type that declares the property, and call <see cref="MarkReady"/> as the last
/// line of that same type's constructor:
/// </para>
/// <code>
///     propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(MyControl), () => ...)
///     ...
///     public MyControl()
///     {
///         label = new Label();
///         Content = label;
///         StyleGuard.MarkReady(this, typeof(MyControl));
///     }
/// </code>
/// <para>
/// Readiness is tracked <b>per level of the type hierarchy</b>, not per object. A callback
/// declared by <c>MyControl</c> only needs <c>MyControl</c>'s constructor to have finished -
/// it touches <c>MyControl</c>'s fields and nothing else. Scoping it that way is what lets a
/// control be subclassed: <c>class MyPage : MyControl</c> inherits the guard for free and its
/// author never calls into this class. A subclass that declares guarded properties of its own
/// simply passes its own type, and its level is marked when its own constructor ends.
/// </para>
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
    static readonly ConditionalWeakTable<object, Levels> levels = new();

    sealed class Levels
    {
        public HashSet<Type> Ready { get; } = new();
        public Dictionary<Type, List<Action>> Pending { get; } = new();
    }

    /// <summary>
    /// Declares that <paramref name="declaringType"/>'s constructor has finished building the
    /// children its own callbacks touch, and replays anything that arrived beforehand. Call it
    /// as the last line of that constructor, passing the type whose constructor this is:
    /// <c>StyleGuard.MarkReady(this, typeof(MyControl))</c>.
    /// </summary>
    /// <param name="declaringType">
    /// The type whose constructor is calling. Every class that guards properties of its own
    /// calls this with its own type; base and derived levels are tracked separately, so a base
    /// control's callbacks replay as soon as the base constructor ends even when the object's
    /// runtime type is some subclass that is still half-built.
    /// </param>
    public static void MarkReady(object control, Type declaringType)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(declaringType);

        Flush(control, declaringType);
    }

    static void Flush(object control, Type level)
    {
        List<Action>? queued;
        var state = levels.GetOrCreateValue(control);

        lock (state)
        {
            state.Ready.Add(level);

            if (!state.Pending.TryGetValue(level, out queued))
                return;

            state.Pending.Remove(level);
        }

        // In arrival order - the style's setters ran in declaration order and later ones are
        // expected to win.
        foreach (var action in queued)
            action();
    }

    /// <summary>
    /// Runs <paramref name="apply"/> now if <paramref name="level"/>'s constructor has
    /// finished, otherwise queues it until <see cref="MarkReady"/> is called for that level.
    /// Pass the type that declares the property being guarded.
    /// </summary>
    public static void WhenReady(BindableObject bindable, Type level, Action apply)
    {
        if (bindable is null)
            return;

        var state = levels.GetOrCreateValue(bindable);

        lock (state)
        {
            if (!state.Ready.Contains(level))
            {
                if (!state.Pending.TryGetValue(level, out var queue))
                    state.Pending[level] = queue = new List<Action>();

                queue.Add(apply);
                return;
            }
        }

        apply();
    }

    /// <summary>
    /// Runs <paramref name="apply"/> now if <typeparamref name="T"/>'s constructor has finished,
    /// otherwise queues it until <see cref="MarkReady"/> is called for that level.
    /// <typeparamref name="T"/> is the type that declares the property being guarded.
    /// </summary>
    public static void WhenReady<T>(BindableObject bindable, Action<T> apply)
        where T : class
    {
        if (bindable is not T control)
            return;

        WhenReady(bindable, typeof(T), () => apply(control));
    }

    /// <summary>
    /// True once <see cref="MarkReady"/> has been called for <paramref name="level"/>.
    /// </summary>
    public static bool IsReady(object control, Type level)
    {
        var state = levels.GetOrCreateValue(control);

        lock (state)
            return state.Ready.Contains(level);
    }

    /// <summary>
    /// True if any guarded callback is still parked waiting for a level that was never marked.
    /// Once a control's constructor has returned this must be false: anything still queued is a
    /// styled or XAML-set value that silently never applied, which is precisely the failure this
    /// class exists to make impossible. Tests assert on it.
    /// </summary>
    public static bool HasPending(object control)
    {
        var state = levels.GetOrCreateValue(control);

        lock (state)
            return state.Pending.Count > 0;
    }
}
