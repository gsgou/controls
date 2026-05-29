using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Skeleton;

[ShellMap<SkeletonPage>(registerRoute: false)]
public partial class SkeletonViewModel : ObservableObject
{
    [ObservableProperty] bool isBusy = true;
    [ObservableProperty] bool isCardBusy = true;

    [RelayCommand]
    async Task Reload()
    {
        IsBusy = true;
        await Task.Delay(2500);
        IsBusy = false;
    }

    [RelayCommand]
    async Task ReloadCard()
    {
        IsCardBusy = true;
        await Task.Delay(2500);
        IsCardBusy = false;
    }
}
