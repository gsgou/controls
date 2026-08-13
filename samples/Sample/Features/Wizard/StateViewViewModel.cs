using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Wizard;

[ShellMap<StateViewPage>(registerRoute: false)]
public partial class StateViewViewModel : ObservableObject
{
    [ObservableProperty] string currentState = "Empty";
    [ObservableProperty] string lastChange = "(nothing yet)";

    [RelayCommand]
    void Go(string state) => this.CurrentState = state;

    [RelayCommand]
    async Task Load()
    {
        this.CurrentState = "Loading";
        await Task.Delay(1500);
        this.CurrentState = "Loaded";
    }

    [RelayCommand]
    async Task Fail()
    {
        this.CurrentState = "Loading";
        await Task.Delay(1500);
        this.CurrentState = "Error";
    }

    partial void OnCurrentStateChanged(string value) => this.LastChange = $"CurrentState = {value}";
}
