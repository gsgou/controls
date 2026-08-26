namespace Shiny.Maui.Controls;

/// <summary>
/// Implemented by a tab's content — or its view model — to be told when its tab is entered and left.
/// This is the tabbed page's replacement for <c>OnAppearing</c>/<c>OnDisappearing</c>.
/// </summary>
/// <remarks>
/// <para>It exists because MAUI's page lifecycle cannot be borrowed here. <c>OnAppearing</c> is
/// raised by the platform for the page it actually presented; a <see cref="ContentPage"/> that a
/// <see cref="ShinyTabItem"/> adopted is never presented — its content is — so calling
/// <c>IPageController.SendAppearing()</c> on it does nothing at all. A method that silently does
/// nothing is worse than one that does not exist, so the contract is declared rather than
/// impersonated.</para>
/// <para><see cref="ShinyTabbedPage"/> calls it on the tab's content, on the adopted page, and on
/// whichever of the two has a <c>BindingContext</c> that implements this — so a view model gets the
/// callbacks without the view having to relay them. Each object is called once even when it is
/// reachable both ways.</para>
/// <para><see cref="OnTabAppearing"/> runs the moment the tab becomes the selected one — for the
/// first tab, while the page is still being built — and again if the page itself is left and
/// returned to. <see cref="OnTabDisappearing"/> runs when another tab is chosen, and when the page
/// leaves the screen, so a tab never thinks it is visible while the whole page is buried under
/// something else. Neither is ever raised twice in a row.</para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class InboxViewModel : ITabAware
/// {
///     public void OnTabAppearing() =&gt; this.StartPolling();
///     public void OnTabDisappearing() =&gt; this.StopPolling();
/// }
/// </code>
/// </example>
public interface ITabAware
{
    /// <summary>The tab has become the selected one, on a page that is on screen.</summary>
    void OnTabAppearing();

    /// <summary>The tab has stopped being the selected one, or its page has left the screen.</summary>
    void OnTabDisappearing();
}
