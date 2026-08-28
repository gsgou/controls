# NavigationPage with left & right toolbar items (MAUI)

[← All Shiny Controls](../../README.md)

`ShinyNavigationPage` **is** a `NavigationPage` — `PushAsync`, `PopAsync`, `PopToRootAsync`, `InsertPageBefore`, `RemovePage`, the modal stack, page lifecycle, Android's hardware back button and `Pushed`/`Popped`/`PoppedToRoot` all still work, unchanged. What it adds is a bar with items on the **left** as well as the right.

No platform's native bar has a left slot to give you: it belongs to the back button on iOS, Android and WinUI alike, and AppKit and GTK4 have no bar at all. So the native bar is hidden and `ShinyNavBar` draws its own — which is also what makes the overflow menu, the badges, the motion icons and the collapsing large title render identically on **every** MAUI head.

> **MAUI only.** There is no Blazor equivalent; the nearest shape there is `ShinyToolbar` inside `AppLayout`.

```xml
<shiny:ShinyNavigationPage x:Class="MyApp.MainNav" LargeTitleDisplay="Collapsing">
    <x:Arguments>
        <local:InboxPage />
    </x:Arguments>
</shiny:ShinyNavigationPage>
```

The items are declared on the **page**, the way `ToolbarItems` already are:

```xml
<ContentPage Title="Inbox" shiny:ShinyNav.Subtitle="12 unread">

    <shiny:ShinyNav.LeftItems>
        <shiny:NavBarItem Icon="menu" Command="{Binding OpenDrawerCommand}" />
    </shiny:ShinyNav.LeftItems>

    <shiny:ShinyNav.RightItems>
        <shiny:NavBarItem Icon="search" Command="{Binding SearchCommand}" />
        <shiny:NavBarItem Icon="bell" Badge="3" Command="{Binding AlertsCommand}" />
        <shiny:NavBarItem Text="Mark all read" Order="Secondary" Command="{Binding MarkAllCommand}" />
        <shiny:NavBarItem IsSeparator="True" Order="Secondary" />
        <shiny:NavBarItem Text="Delete all" Order="Secondary" IsDestructive="True" Command="{Binding DeleteCommand}" />
    </shiny:ShinyNav.RightItems>
    ...
</ContentPage>
```

`NavBarItem` **derives from `ToolbarItem`**, so `Text`, `IconImageSource`, `Command`, `IsEnabled`, `IsDestructive`, `Clicked`, `Order` and `Priority` mean exactly what they already mean — it just adds motion icons (`Icon`, `IconSource`, `IconPathData`, `Motion`), a `Badge`, `Display`, `IsVisible`, `IsSeparator` and `Tag`. Both collections are typed `ToolbarItem`, and a page's own `Page.ToolbarItems` are drawn on the right **automatically**, so adopting the page never means rewriting a toolbar.

`Order="Secondary"` folds an item into the overflow menu however much room there is; anything past `MaxVisibleItems` (3 per side by default) folds in behind it.

Everything MAUI already gives a `NavigationPage` is honoured rather than reinvented — `Page.Title`, `Page.ToolbarItems`, `SetHasBackButton`, `SetBackButtonTitle`, `SetTitleView`, `SetTitleIconImageSource`, `SetIconColor`, and `BarBackground`/`BarBackgroundColor`/`BarTextColor`. The one exception is `SetHasNavigationBar`: it is honoured as the *starting* value, but that property is the slot this page had to take over to hide the native bar, so the runtime switch is `ShinyNav.SetIsNavBarVisible(page, false)` (or `IsNavBarVisible` on the navigation page, for all of them at once).

`LargeTitleDisplay="Collapsing"` gives the iOS-style oversized title that folds into the bar as the page scrolls — it finds the first `ScrollView` or `ItemsView` in the page on its own, and `ShinyNav.ScrollSource` names a different one. A single page opts out with `shiny:ShinyNav.LargeTitleDisplay="None"`.

iOS's edge-swipe-back keeps working: UIKit disables it whenever the bar is hidden, so the page puts it back deliberately (`EnableSwipeBackGesture="False"` opts out).

> **macOS AppKit note.** The bar, its items, badges and back button all render on `net10.0-macos` — the wrapper is installed before the page is presented, which sidesteps the re-parenting problem that affects the app-wide Flyout install there. Two things do not: the overflow menu paints nothing (it is added to a page overlay layer on the tap that opens it — the same pre-existing limitation as Toast, Dialogs and in-app Quick Entry), and a collapsing large title leaves a residual band under the bar at the end of the fold, because that head does not re-measure the row.
