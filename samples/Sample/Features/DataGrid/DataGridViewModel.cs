using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.Maui.Controls;

using Shiny;

namespace Sample.Features.DataGrid;

[ShellMap<DataGridPage>(registerRoute: false)]
public partial class DataGridViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Tap a row to select";

    [ObservableProperty]
    object? selectedItem;

    public ObservableCollection<Person> People { get; } =
    [
        new("Ada", "Lovelace", 36, "Engineering", 142000, true),
        new("Alan", "Turing", 41, "Research", 158000, true),
        new("Grace", "Hopper", 52, "Engineering", 165000, false),
        new("Katherine", "Johnson", 44, "Mathematics", 134000, true),
        new("Margaret", "Hamilton", 39, "Software", 151000, true),
        new("Edsger", "Dijkstra", 47, "Research", 149000, false),
        new("Donald", "Knuth", 58, "Mathematics", 172000, true),
        new("Barbara", "Liskov", 49, "Software", 161000, true),
    ];

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is Person p)
            this.StatusMessage = $"Selected {p.FirstName} {p.LastName}";
    }
}

public partial class Person(string firstName, string lastName, int age, string department, decimal salary, bool active) : ObservableObject
{
    [ObservableProperty] string firstName = firstName;
    [ObservableProperty] string lastName = lastName;
    [ObservableProperty] int age = age;
    [ObservableProperty] string department = department;
    [ObservableProperty] decimal salary = salary;
    [ObservableProperty] bool active = active;

    public string StatusText => this.Active ? "Active" : "Inactive";
    public PillType StatusPill => this.Active ? PillType.Success : PillType.Caution;
}
