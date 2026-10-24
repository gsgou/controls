using System.Reflection;
using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Themes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The runtime half of <see cref="BrushThemingGuardTests"/>: every control the library ships is built
/// on a themed page, and every border inside it that has an outline thickness must actually have a
/// stroke to draw it with.
/// </summary>
/// <remarks>
/// A colour token handed to <c>Border.Stroke</c> is dropped without a word and leaves the stroke null,
/// so the control renders with no outline and nothing anywhere reports it. Seventeen borders shipped
/// like that. The source scan catches the spellings we know about; this catches the outcome, however
/// it was spelled - and it picks up a new control the moment it is added, with nobody having to list it.
/// </remarks>
[Collection(ApplicationResourcesCollection.Name)]
public class OutlinedBorderStrokeTests(ITestOutputHelper output)
{
    // async Task, not void: a void test waits on xUnit's sync context for every fire-and-forget
    // operation a control starts, and some never finish without a platform.
    [Fact]
    public async Task EveryOutlinedBorderHasAStroke()
    {
        await Task.Yield();
        TestDispatcherProvider.Install();
        TestDispatcherProvider.Instance.Timers.Clear();
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new BasicLightTheme());

        var offenders = new List<string>();
        var built = 0;

        foreach (var type in ControlTypes())
        {
            View view;
            try
            {
                view = (View)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                // Not this test's concern - some controls need a platform service to exist at all.
                output.WriteLine($"skipped {type.Name}: {ex.GetBaseException().GetType().Name}");
                continue;
            }

            var page = new ContentPage { Content = view };
            page.Parent = app;
            built++;

            foreach (var border in Descendants(view).OfType<Border>())
            {
                if (border.StrokeThickness > 0 && border.IsVisible && border.Stroke is null)
                    offenders.Add($"{type.Name}: a {border.StrokeThickness}px border{Describe(border)} has no Stroke");
            }
        }

        output.WriteLine($"built {built} controls");
        foreach (var o in offenders)
            output.WriteLine("NO STROKE: " + o);

        built.ShouldBeGreaterThan(50, "the control scan found almost nothing to build - the reflection filter is wrong");
        offenders.ShouldBeEmpty(
            "These borders have an outline thickness and no brush to draw it, which is what a Color " +
            "token on Border.Stroke resolves to. Use ShinyThemeKeys.Brush.X (or ThemeBrush.Apply)."
        );
    }


    static string Describe(Border border)
        => string.IsNullOrEmpty(border.AutomationId) ? "" : $" ({border.AutomationId})";


    static IEnumerable<Type> ControlTypes()
        => typeof(ShinyButton).Assembly.GetExportedTypes()
            .Where(t =>
                typeof(View).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters &&
                t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes) is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);


    static IEnumerable<Element> Descendants(Element root)
    {
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren().OfType<Element>())
        {
            yield return child;
            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }
}
