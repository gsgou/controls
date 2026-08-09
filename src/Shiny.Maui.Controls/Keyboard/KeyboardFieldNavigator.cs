namespace Shiny.Maui.Controls;

/// <summary>
/// Resolves the ordered run of focusable text inputs on a page so an accessory bar can move
/// between them. Deliberately platform-free — this is the only part of the accessory feature with
/// real logic, and it is the part worth testing.
/// </summary>
public static class KeyboardFieldNavigator
{
    /// <summary>
    /// Every navigable input on <paramref name="current"/>'s page, in depth-first visual-tree order -
    /// which for a form laid out top to bottom is the order the user reads it in. MAUI has no
    /// TabIndex to honour (that was a Forms concept), so declaration order is the order.
    /// </summary>
    public static IReadOnlyList<VisualElement> Collect(VisualElement current)
    {
        var root = FindRoot(current);
        if (root is null)
            return [current];

        var group = GroupOf(current);
        var found = new List<VisualElement>();
        Walk(root, found);

        return found
            .Where(x => IsNavigable(x) && GroupOf(x) == group)
            .ToList();
    }

    /// <summary>True when focus can move <paramref name="direction"/> from <paramref name="current"/>.</summary>
    public static bool CanMove(VisualElement current, KeyboardNavigationDirection direction)
        => Target(current, direction) is not null;

    /// <summary>Moves focus. Returns false when there is nothing in that direction.</summary>
    public static bool Move(VisualElement current, KeyboardNavigationDirection direction)
    {
        var target = Target(current, direction);
        if (target is null)
            return false;

        // A Shiny TextEntry is a wrapper — its own Focus() would land on the ContentView, not the
        // input inside it, so the shadowing overload has to be called explicitly.
        if (target is TextEntry entry)
            return entry.Focus();

        return target.Focus();
    }

    static VisualElement? Target(VisualElement current, KeyboardNavigationDirection direction)
    {
        var fields = Collect(current);
        var index = -1;
        for (var i = 0; i < fields.Count; i++)
        {
            if (ReferenceEquals(fields[i], current))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return null;

        var next = direction == KeyboardNavigationDirection.Next ? index + 1 : index - 1;
        return next >= 0 && next < fields.Count ? fields[next] : null;
    }

    static string? GroupOf(VisualElement element) => (string?)element.GetValue(KeyboardField.GroupProperty);

    static bool IsNavigable(VisualElement element)
    {
        if (!element.IsEnabled || !element.IsVisible)
            return false;

        // Walk up: a field inside a collapsed container is not reachable either.
        var parent = element.Parent;
        while (parent is VisualElement ve)
        {
            if (!ve.IsVisible || !ve.IsEnabled)
                return false;
            parent = ve.Parent;
        }

        return element switch
        {
            TextEntry entry => !entry.IsReadOnly,
            InputView input => !input.IsReadOnly,
            _ => false
        };
    }

    static Element? FindRoot(Element element)
    {
        var current = element;
        Element? root = null;
        while (current is not null)
        {
            if (current is Page page)
                return page;

            root = current;
            current = current.Parent;
        }

        return root;
    }

    // A Shiny TextEntry owns a BorderlessEntry internally; once it is collected the walk stops there
    // so the wrapper and its inner input don't both show up as separate stops.
    static void Walk(Element element, List<VisualElement> found)
    {
        if (element is TextEntry entry)
        {
            found.Add(entry);
            return;
        }

        if (element is InputView input)
        {
            found.Add(input);
            return;
        }

        foreach (var child in element.LogicalChildrenInternalWrapper())
            Walk(child, found);
    }
}

/// <summary>
/// Carries the navigation group on whichever element the navigator actually collects. A
/// <see cref="TextEntry"/> is collected as itself; an <see cref="Cells.EntryCell"/> is collected as
/// the input inside it - so the group has to live on the element, not on the control that owns it.
/// </summary>
static class KeyboardField
{
    public static readonly BindableProperty GroupProperty = BindableProperty.CreateAttached(
        "Group", typeof(string), typeof(KeyboardField), null);
}

static class NavigatorElementExtensions
{
    // Element.LogicalChildren is internal in MAUI, so the public surfaces are used instead. This
    // covers every container the controls in this repo build with.
    public static IEnumerable<Element> LogicalChildrenInternalWrapper(this Element element)
        => element switch
        {
            Layout layout => layout.Children.OfType<Element>(),
            ContentView contentView => contentView.Content is null ? [] : [contentView.Content],
            ContentPage contentPage => contentPage.Content is null ? [] : [contentPage.Content],
            ScrollView scrollView => scrollView.Content is null ? [] : [scrollView.Content],
            Border border => border.Content is null ? [] : [border.Content],
            IContentView content => content.PresentedContent is Element e ? [e] : [],
            _ => []
        };
}
