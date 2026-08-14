namespace Shiny.Maui.Controls;

/// <summary>
/// The one-liner form of <see cref="Tooltip"/>: attach a hint to any view without adding an element.
/// </summary>
/// <example>
/// <code>
/// &lt;Button Text="Sync"
///         shiny:TooltipProperties.Text="Pushes local changes to the server"
///         shiny:TooltipProperties.Trigger="LongPress" /&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Named like MAUI's own <c>ToolTipProperties</c>, and there for the same reason: inside a
/// <c>DataTemplate</c> or a cell there is often nowhere sensible to put an element, and
/// <c>{x:Reference}</c> cannot see out of the template anyway.
/// </para>
/// <para>
/// Everything here maps onto a real <see cref="Tooltip"/> instance held against the view, so the full
/// control is still what runs — the attached properties are a shorthand, not a second implementation.
/// Reach for the element form when you need binding, templated content or a command.
/// </para>
/// </remarks>
public static class TooltipProperties
{
    /// <summary>The tooltip built for a view, so the attached properties all drive the same instance.</summary>
    static readonly BindableProperty InstanceProperty = BindableProperty.CreateAttached(
        "Instance", typeof(Tooltip), typeof(TooltipProperties), null);


    public static readonly BindableProperty TextProperty = BindableProperty.CreateAttached(
        "Text", typeof(string), typeof(TooltipProperties), null,
        propertyChanged: (b, _, n) => Apply(b, t => t.Text = (string?)n));

    public static readonly BindableProperty TitleProperty = BindableProperty.CreateAttached(
        "Title", typeof(string), typeof(TooltipProperties), null,
        propertyChanged: (b, _, n) => Apply(b, t => t.Title = (string?)n));

    public static readonly BindableProperty PlacementProperty = BindableProperty.CreateAttached(
        "Placement", typeof(TooltipPlacement), typeof(TooltipProperties), TooltipPlacement.Auto,
        propertyChanged: (b, _, n) => Apply(b, t => t.Placement = (TooltipPlacement)n));

    public static readonly BindableProperty TriggerProperty = BindableProperty.CreateAttached(
        "Trigger", typeof(TooltipTrigger), typeof(TooltipProperties), TooltipTrigger.LongPress,
        propertyChanged: (b, _, n) => Apply(b, t => t.Trigger = (TooltipTrigger)n));

    public static readonly BindableProperty ShowTailProperty = BindableProperty.CreateAttached(
        "ShowTail", typeof(bool), typeof(TooltipProperties), true,
        propertyChanged: (b, _, n) => Apply(b, t => t.ShowTail = (bool)n));

    public static readonly BindableProperty AutoDismissDelayProperty = BindableProperty.CreateAttached(
        "AutoDismissDelay", typeof(int), typeof(TooltipProperties), 3_000,
        propertyChanged: (b, _, n) => Apply(b, t => t.AutoDismissDelay = (int)n));

    public static readonly BindableProperty IsOpenProperty = BindableProperty.CreateAttached(
        "IsOpen", typeof(bool), typeof(TooltipProperties), false,
        propertyChanged: (b, _, n) => Apply(b, t => t.IsOpen = (bool)n));


    public static string? GetText(BindableObject view) => (string?)view.GetValue(TextProperty);
    public static void SetText(BindableObject view, string? value) => view.SetValue(TextProperty, value);

    public static string? GetTitle(BindableObject view) => (string?)view.GetValue(TitleProperty);
    public static void SetTitle(BindableObject view, string? value) => view.SetValue(TitleProperty, value);

    public static TooltipPlacement GetPlacement(BindableObject view) => (TooltipPlacement)view.GetValue(PlacementProperty);
    public static void SetPlacement(BindableObject view, TooltipPlacement value) => view.SetValue(PlacementProperty, value);

    public static TooltipTrigger GetTrigger(BindableObject view) => (TooltipTrigger)view.GetValue(TriggerProperty);
    public static void SetTrigger(BindableObject view, TooltipTrigger value) => view.SetValue(TriggerProperty, value);

    public static bool GetShowTail(BindableObject view) => (bool)view.GetValue(ShowTailProperty);
    public static void SetShowTail(BindableObject view, bool value) => view.SetValue(ShowTailProperty, value);

    public static int GetAutoDismissDelay(BindableObject view) => (int)view.GetValue(AutoDismissDelayProperty);
    public static void SetAutoDismissDelay(BindableObject view, int value) => view.SetValue(AutoDismissDelayProperty, value);

    public static bool GetIsOpen(BindableObject view) => (bool)view.GetValue(IsOpenProperty);
    public static void SetIsOpen(BindableObject view, bool value) => view.SetValue(IsOpenProperty, value);


    /// <summary>The tooltip attached to a view, if any — for code that wants to open one by hand.</summary>
    public static Tooltip? GetTooltip(BindableObject view) => (Tooltip?)view.GetValue(InstanceProperty);


    static void Apply(BindableObject bindable, Action<Tooltip> configure)
    {
        if (bindable is not View view)
            return;

        configure(GetOrCreate(view));
    }


    static Tooltip GetOrCreate(View view)
    {
        if (view.GetValue(InstanceProperty) is Tooltip existing)
            return existing;

        var tooltip = new Tooltip
        {
            Target = view,
            Trigger = GetTrigger(view),
            Placement = GetPlacement(view),
            AutoDismissDelay = GetAutoDismissDelay(view)
        };
        view.SetValue(InstanceProperty, tooltip);

        // The tooltip is not in the visual tree — it hangs off the view — so its own Loaded never
        // fires. Wiring is driven from the view's lifecycle instead, and torn down with it so a
        // bubble does not survive the page that owned it.
        view.Loaded += (_, _) => tooltip.SendAttachedLoaded();
        view.Unloaded += (_, _) => tooltip.SendAttachedUnloaded();

        if (view.IsLoaded)
            tooltip.SendAttachedLoaded();

        return tooltip;
    }
}
