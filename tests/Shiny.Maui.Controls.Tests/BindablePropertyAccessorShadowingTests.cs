using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Guards against a silent, fatal overload trap on <see cref="BindableObject"/>.
///
/// C# overload resolution stops at the most-derived type that declares <b>any</b> applicable
/// member of that name. So declaring, say, <c>object? GetValue(object? item)</c> on a
/// BindableObject subclass hides <c>BindableObject.GetValue(BindableProperty)</c> completely -
/// <c>BindableProperty</c> converts to <c>object</c>, so every
/// <c>get =&gt; (T)this.GetValue(SomeProperty)</c> in that class silently retargets to the new
/// overload. If the new overload reads any bindable property of its own, that is unbounded
/// recursion: the control stack-overflows the moment it is used, with no compiler warning.
///
/// <see cref="DataGridColumn"/> shipped exactly that (its accessor is now
/// <c>GetCellValue</c>). These tests keep it from coming back.
/// </summary>
public class BindablePropertyAccessorShadowingTests
{
    /// <summary>Names on BindableObject whose overloads must never be shadowed by a subclass.</summary>
    static readonly string[] Reserved = ["GetValue", "SetValue", "ClearValue", "IsSet", "RemoveBinding", "SetBinding"];

    [Fact]
    public void NoBindableObjectDeclaresAReservedAccessorName()
    {
        var offenders = typeof(AutoCompleteEntry).Assembly
            .GetTypes()
            .Where(t => typeof(BindableObject).IsAssignableFrom(t))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => Reserved.Contains(m.Name, StringComparer.Ordinal))
                .Select(m => $"{t.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"))
            .ToList();

        offenders.ShouldBeEmpty(
            "These members hide BindableObject's own accessors, so every bindable property " +
            "getter/setter on the declaring type retargets to them. Rename them (e.g. " +
            "GetValue(object) -> GetCellValue(object))."
        );
    }

    [Fact]
    public void DataGridBuildsAndReadsCellValues()
    {
        _ = new Application();

        var people = new ObservableCollection<Person>
        {
            new("Ada", 36, "Analytical"),
            new("Grace", 45, "Compilers")
        };

        var grid = new Shiny.Maui.Controls.DataGrid.DataGrid
        {
            SelectionMode = DataGridSelectionMode.Single,
            SortMode = DataGridSortMode.Multiple,
            Groupable = true,
            PageSize = 1,
            Striped = true
        };
        grid.Columns.Add(new DataGridColumn { Title = "Name", PropertyName = nameof(Person.Name) });
        grid.Columns.Add(new DataGridColumn { Title = "Age", PropertyName = nameof(Person.Age) });

        grid.ItemsSource = people;
        grid.SelectedItem = people[0];

        // Reading the bindable property must return what was set, not recurse into the value accessor.
        grid.Columns[0].PropertyName.ShouldBe(nameof(Person.Name));
        grid.Columns[0].Title.ShouldBe("Name");
        grid.Columns[0].IsVisible.ShouldBeTrue();
    }

    record Person(string Name, int Age, string Department);
}
