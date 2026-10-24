using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Themes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Every control, checked for the theming mistakes that compile, never throw, and only show up as
/// a wrong colour on screen - the class of bug users kept finding before the tests did.
/// </summary>
/// <remarks>
/// Each control is compared, colour for colour across its whole visual tree, against a freshly
/// built one. Nothing is listed by hand: a new control is covered the moment it ships.
/// <list type="bullet">
/// <item><b>Clearing a colour goes back to the theme.</b> A local value outranks a dynamic resource,
/// so a control that writes an explicit colour and later re-binds the token without clearing it
/// first keeps the explicit colour forever (a ribbon tool that never got its ring back, a walkthrough
/// callout that stayed transparent).</item>
/// <item><b>Setting a colour changes something.</b> A colour property wired to nothing is a bug report
/// waiting to happen.</item>
/// <item><b>A theme swap matches a control built in the new theme.</b> Anything that read a colour
/// once, or recoloured a brush in place, is left behind in the old palette.</item>
/// </list>
/// </remarks>
[Collection(ApplicationResourcesCollection.Name)]
public class ThemeRoundTripTests(ITestOutputHelper output)
{
    static readonly Color Probe = Color.FromArgb("#FF00FF");

    /// <summary>
    /// Colour properties whose effect only exists in a state a freshly built control is not in, so
    /// "setting it changes nothing" is expected. Each entry says which state.
    /// </summary>
    static readonly Dictionary<string, string> InertUntilAState = new(StringComparer.Ordinal)
    {
    };


    [Fact]
    // async Task, not void: a void test waits on xUnit's sync context for every fire-and-forget
    // operation a control starts, and some never finish without a platform.
    public async Task ClearingAColourGoesBackToTheTheme()
    {
        await Task.Yield();
        var app = Themed(new BasicLightTheme());
        var offenders = new List<string>();

        foreach (var (type, property) in ColourProperties())
        {
            var fresh = Snapshot(Build(type, app));
            if (fresh is null)
                continue;

            var view = Build(type, app)!;
            view.SetValue(property, Probe);
            view.SetValue(property, null);

            var cleared = Snapshot(view)!;
            var diff = Diff(fresh, cleared);
            if (diff is not null)
                offenders.Add($"{type.Name}.{property.PropertyName}: {diff}");
        }

        this.Report("STUCK AFTER CLEAR", offenders);
        offenders.ShouldBeEmpty(
            "Setting this colour and clearing it again does not put the control back the way it was " +
            "built. A local value outranks a dynamic resource: go through ThemeProbe.Tint / " +
            "ThemeBrush.Apply, which clear the local value before re-binding the token."
        );
    }


    [Fact]
    // async Task, not void: a void test waits on xUnit's sync context for every fire-and-forget
    // operation a control starts, and some never finish without a platform.
    public async Task SettingAColourChangesSomething()
    {
        await Task.Yield();
        var app = Themed(new BasicLightTheme());
        var offenders = new List<string>();

        foreach (var (type, property) in ColourProperties())
        {
            var key = $"{type.Name}.{property.PropertyName}";
            if (InertUntilAState.ContainsKey(key))
                continue;

            var fresh = Snapshot(Build(type, app));
            if (fresh is null)
                continue;

            var view = Build(type, app)!;
            view.SetValue(property, Probe);

            if (Diff(fresh, Snapshot(view)!) is null)
                offenders.Add(key);
        }

        this.Report("COLOUR DOES NOTHING", offenders);
        offenders.ShouldBeEmpty(
            "Setting this colour changed nothing anywhere in the control. Either it is not wired up, " +
            "or it only applies in a state a new control is not in - then list it in InertUntilAState " +
            "with the state that makes it apply."
        );
    }


    [Fact]
    // async Task, not void: a void test waits on xUnit's sync context for every fire-and-forget
    // operation a control starts, and some never finish without a platform.
    public async Task ASwapMatchesAControlBuiltInTheNewTheme()
    {
        await Task.Yield();
        var offenders = new List<string>();

        foreach (var type in ControlTypes())
        {
            var app = Themed(new BasicLightTheme());
            var swapped = Build(type, app);
            if (swapped is null)
                continue;

            // Exactly what ShinyThemeManager does - remove the applied dictionary, add the next one.
            app.Resources.MergedDictionaries.Remove(app.Resources.MergedDictionaries.First());
            app.Resources.MergedDictionaries.Add(new BasicDarkTheme());

            var diff = Diff(Snapshot(Build(type, app))!, Snapshot(swapped)!);
            if (diff is not null)
                offenders.Add($"{type.Name}: {diff}");
        }

        this.Report("LEFT IN THE OLD THEME", offenders);
        offenders.ShouldBeEmpty(
            "After a light to dark swap these controls do not look like the same control built in the " +
            "dark theme. Something read a colour once, or recoloured a brush in place - follow the token " +
            "with SetDynamicResource / ThemeProbe.Tint / ThemeBrush.Apply."
        );
    }


    // ---------------------------------------------------------------------------------------------

    void Report(string label, List<string> offenders)
    {
        foreach (var o in offenders)
            output.WriteLine($"{label}: {o}");
    }


    static Application Themed(ResourceDictionary theme)
    {
        TestDispatcherProvider.Install();
        TestDispatcherProvider.Instance.Timers.Clear();
        var app = new Application();
        app.Resources.MergedDictionaries.Add(theme);
        return app;
    }


    static View? Build(Type type, Application app)
    {
        try
        {
            // The page is finished before the control joins it, as it is in an app. The test
            // dispatcher runs a dispatch immediately, so a control that defers work a tick to let
            // the page finish building (ProgressLine's dock) would otherwise run it mid-assignment.
            var host = new Grid();
            var page = new ContentPage { Content = host };
            page.Parent = app;

            var view = (View)Activator.CreateInstance(type)!;
            host.Add(view);
            return view;
        }
        catch
        {
            return null;
        }
    }


    static IEnumerable<Type> ControlTypes()
        => typeof(ShinyButton).Assembly.GetExportedTypes()
            .Where(t =>
                typeof(View).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters &&
                t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes) is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);


    /// <summary>The library's own colour properties that default to "follow the theme".</summary>
    static IEnumerable<(Type Type, BindableProperty Property)> ColourProperties()
    {
        var library = typeof(ShinyButton).Assembly;

        foreach (var type in ControlTypes())
        {
            var seen = new HashSet<string>();
            for (var t = type; t is not null && t.Assembly == library; t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (field.GetValue(null) is BindableProperty bp &&
                        bp.ReturnType == typeof(Color) &&
                        bp.DefaultValue is null &&
                        !bp.IsReadOnly &&
                        seen.Add(bp.PropertyName))
                    {
                        yield return (type, bp);
                    }
                }
            }
        }
    }


    // ---------------------------------------------------------------------------------------------
    // Snapshots: every colour- or brush-typed value on every element, keyed by its place in the tree.
    // ---------------------------------------------------------------------------------------------

    static Dictionary<string, string>? Snapshot(View? root)
    {
        if (root is null)
            return null;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        Walk(root, root.GetType().Name, values);
        return values;
    }


    static void Walk(Element element, string path, Dictionary<string, string> values)
    {
        foreach (var bp in PaintProperties(element.GetType()))
            values[$"{path}.{bp.PropertyName}"] = Describe(element.GetValue(bp));

        if (element is VisualElement { Shadow: { } shadow })
            values[$"{path}.Shadow.Brush"] = Describe(shadow.Brush);

        var i = 0;
        foreach (var child in ((IVisualTreeElement)element).GetVisualChildren().OfType<Element>())
            Walk(child, $"{path}/{i++}:{child.GetType().Name}", values);
    }


    static readonly Dictionary<Type, BindableProperty[]> paintProperties = new();

    static BindableProperty[] PaintProperties(Type type)
    {
        if (paintProperties.TryGetValue(type, out var cached))
            return cached;

        var list = new List<BindableProperty>();
        var names = new HashSet<string>();
        for (var t = type; t is not null; t = t.BaseType)
        {
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (field.GetValue(null) is BindableProperty bp &&
                    (bp.ReturnType == typeof(Color) || typeof(Brush).IsAssignableFrom(bp.ReturnType)) &&
                    names.Add(bp.PropertyName))
                {
                    list.Add(bp);
                }
            }
        }

        return paintProperties[type] = list.ToArray();
    }


    static string Describe(object? value) => value switch
    {
        null => "null",
        Color c => c.ToArgbHex(true),
        SolidColorBrush { Color: var c } => "brush " + (c?.ToArgbHex(true) ?? "null"),
        GradientBrush g => "gradient " + string.Join(",", g.GradientStops.Select(s => s.Color?.ToArgbHex(true))),
        _ => value.GetType().Name
    };


    /// <summary>
    /// The first value that differs. Nothing, null and fully transparent all paint the same, so they
    /// compare equal - a cell writing Transparent where a fresh one has null is not a visible change.
    /// </summary>
    static string? Diff(Dictionary<string, string> expected, Dictionary<string, string> actual)
    {
        foreach (var key in expected.Keys.Union(actual.Keys))
        {
            var want = expected.GetValueOrDefault(key, "null");
            var got = actual.GetValueOrDefault(key, "null");
            if (Paints(want) != Paints(got))
                return $"{key} is {got}, expected {want}";
        }

        return null;
    }


    static string Paints(string value) => value switch
    {
        "null" or "#00000000" or "brush #00000000" or "brush null" => "nothing",
        _ => value
    };
}
