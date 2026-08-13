using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Home;

[ShellMap<HomePage>(registerRoute: false)]
public partial class HomeViewModel : ObservableObject
{
    public CatalogSection[] Sections => Catalog.Sections;

    public int TotalControls => Catalog.TotalControls;

    public int TotalSections => Catalog.Sections.Length;

    // Absolute ("//") so a card jumps straight to the flyout item rather than pushing the page onto the
    // home page's own stack — tapping Home afterwards would otherwise land back on the demo.
    [RelayCommand]
    async Task Navigate(string route)
        => await Shell.Current.GoToAsync($"//{route}");
}
