using System.Collections.ObjectModel;

namespace Shiny.Maui.Controls;

/// <summary>
/// A named collection of <see cref="ToolbarItem"/>, so a page's left and right bar items can be
/// declared as attached properties in XAML.
/// </summary>
/// <remarks>
/// It holds <see cref="ToolbarItem"/> rather than <see cref="NavBarItem"/> so that an existing
/// toolbar item moves into it unchanged; <see cref="NavBarItem"/> derives from it and adds the
/// motion icon and badge.
/// <code language="xaml">
/// &lt;shiny:ShinyNav.LeftItems&gt;
///     &lt;shiny:NavBarItem Icon="close" Command="{Binding CancelCommand}" /&gt;
/// &lt;/shiny:ShinyNav.LeftItems&gt;
/// </code>
/// </remarks>
public class NavBarItemCollection : ObservableCollection<ToolbarItem>;
