using System.Text.RegularExpressions;
using Path = System.IO.Path;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Source scans over every MAUI package's registration code.
/// </summary>
public class RegistrationGuardTests(ITestOutputHelper output)
{
    /// <summary>
    /// Services that are resolved as a list, where a second registration is not shadowed but runs.
    /// <c>AddSingleton</c> of one of these makes the registering call unsafe to repeat: two startup
    /// initializers, two quick entry popups, every haptic fired twice.
    /// </summary>
    static readonly Regex ListServiceAddedPlainly = new(
        @"\.AddSingleton<\s*(IMauiInitializeService|IQuickEntryPresenter|IScreenGlowPresenter)\b",
        RegexOptions.Compiled);


    [Fact]
    public void ListResolvedServicesAreRegisteredOnce()
    {
        var src = FindSrcRoot();
        var offenders = Directory
            .EnumerateDirectories(src, "Shiny.Maui.Controls*", SearchOption.TopDirectoryOnly)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadAllLines(f).Select((text, i) => (File: f, Line: i + 1, Text: text)))
            .Where(x => ListServiceAddedPlainly.IsMatch(x.Text))
            .Select(x => $"{Path.GetRelativePath(src, x.File)}:{x.Line}  {x.Text.Trim()}")
            .ToList();

        foreach (var o in offenders)
            output.WriteLine("REGISTERED TWICE IF CALLED TWICE: " + o);

        offenders.ShouldBeEmpty(
            "Register these with services.TryAddEnumerable(ServiceDescriptor.Singleton<TService, TImplementation>(...)), " +
            "which keeps different implementations side by side but never adds the same one twice."
        );
    }


    static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src");
            if (Directory.Exists(Path.Combine(candidate, "Shiny.Maui.Controls")))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/ from the test output directory.");
    }
}
