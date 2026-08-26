using System.Collections.ObjectModel;

namespace Shiny.Maui.Controls;

/// <summary>
/// A named collection of <see cref="TabAction"/>, so the centre menu's rows can be declared as an
/// attached property in XAML.
/// </summary>
/// <remarks>
/// It exists for the same reason <c>VisualStateGroupList</c> does: an attached property whose value
/// is a collection needs a type the markup can name, so the rows have something to nest inside.
/// <code language="xaml">
/// &lt;shiny:ShinyTabs.Actions&gt;
///     &lt;shiny:TabActionCollection&gt;
///         &lt;shiny:TabAction Text="New" Icon="plus" Command="{Binding NewCommand}" /&gt;
///     &lt;/shiny:TabActionCollection&gt;
/// &lt;/shiny:ShinyTabs.Actions&gt;
/// </code>
/// </remarks>
public class TabActionCollection : ObservableCollection<TabAction>;
