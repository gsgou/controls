using System.Text.RegularExpressions;
using Path = System.IO.Path;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Source scans for the brush mistakes that compile, never throw, and only show up as a wrong colour
/// on one platform - which is how every one of them reached a user before a test.
/// </summary>
/// <remarks>
/// <para>
/// Every package is scanned, add-ons included. The older theme coverage tests only look at the core
/// library, and two of the Office surfaces carried a brush that never resolved precisely because
/// nothing looked there.
/// </para>
/// <para>
/// The rule all of these enforce: a <c>Brush</c>-typed property (<c>Border.Stroke</c>,
/// <c>Background</c>, <c>Shape.Stroke</c>/<c>Fill</c>, <c>Shadow.Brush</c>) takes the brush twin of a
/// theme token - <c>ShinyThemeKeys.Brush.X</c> through <c>SetDynamicResource</c>,
/// <c>ThemeBrush.Apply</c> or <c>.WithBrush</c> - or a new brush. Never a colour token, and never a
/// brush whose colour is changed after it was handed out.
/// </para>
/// </remarks>
public class BrushThemingGuardTests(ITestOutputHelper output)
{
    /// <summary>
    /// A token or a binding on the brush object itself. The brush is not in the visual tree, so a token
    /// set on it has nothing to resolve through and keeps its seed colour forever (the Office spelling
    /// menu and floating panels sat in literal grey); a binding resolves, but recolours one brush in
    /// place, which Android and Windows never repaint (PR #57: a walkthrough callout stuck dark).
    /// </summary>
    static readonly Regex TokenOrBindingOnABrush = new(
        @"Set(DynamicResource|Binding)\(\s*SolidColorBrush\.ColorProperty",
        RegexOptions.Compiled);

    /// <summary>
    /// A <c>Color</c> token handed to a <c>Brush</c> property. The types never match, the resource is
    /// dropped, and the property stays null - an outline that is simply not drawn. Seventeen borders
    /// across the pickers, chat composer, FAB menu, toolbar, scheduler and ribbon had no outline.
    /// </summary>
    static readonly Regex ColourTokenOnABrushProperty = new(
        @"SetDynamicResource\(\s*[\w.]*\b(Stroke|Background|Fill|Brush)Property\s*,\s*[\w.]*\bColor\.",
        RegexOptions.Compiled);

    /// <summary>The same mistake through the brush helpers, which expect a <c>ShinyThemeKeys.Brush</c> key.</summary>
    static readonly Regex ColourTokenThroughABrushHelper = new(
        @"(ThemeBrush\.Apply|\.WithBrush)\([^;]*ShinyThemeKeys\.Color\.\w+(?!\s*\.AsBrush)\s*[,)]",
        RegexOptions.Compiled);

    /// <summary>
    /// A brush recoloured after it was assigned. Android and Windows keep the native paint the view was
    /// first given; assign a new brush instead.
    /// </summary>
    static readonly Regex BrushRecolouredInPlace = new(
        @"(\b\w*[Bb]rush\w*|\(\s*\(\s*SolidColorBrush\s*\)[^)]*\)|\bas\s+SolidColorBrush\s*\))\??\.Color\s*=(?!=)|SetValue\(\s*SolidColorBrush\.ColorProperty",
        RegexOptions.Compiled);

    /// <summary>
    /// <c>Color.FromRgba(20, 20, 22, 0.86f)</c> binds to the all-float overload, which reads every
    /// channel on a 0-1 scale and clamps - so a near-black chrome paints white. 0 and 255 happen to
    /// survive (they clamp to 0 and 1), which is why the bug hides until someone picks a real shade.
    /// </summary>
    static readonly Regex FromRgbaByteChannelsWithFractionalAlpha = new(
        @"FromRgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*\d*\.\d+[fd]?\s*\)",
        RegexOptions.Compiled);


    [Fact]
    public void NoTokenOrBindingOnABrush()
        => this.ShouldFindNothing(TokenOrBindingOnABrush, "TOKEN ON A BRUSH",
            "Point the property at ShinyThemeKeys.Brush.X (ThemeBrush.Apply / .WithBrush) instead of " +
            "driving the colour of a brush: a token on a brush never resolves, and a bound brush is " +
            "recoloured in place, which Android and Windows never repaint.");


    [Fact]
    public void NoColourTokenOnABrushProperty()
        => this.ShouldFindNothing(ColourTokenOnABrushProperty, "COLOUR TOKEN ON A BRUSH PROPERTY",
            "A Color token on a Brush property is dropped and the property stays null. Use the brush " +
            "twin: ShinyThemeKeys.Brush.X, or colorKey.AsBrush().");


    [Fact]
    public void NoColourTokenThroughABrushHelper()
        => this.ShouldFindNothing(ColourTokenThroughABrushHelper, "COLOUR KEY IN A BRUSH HELPER",
            "ThemeBrush.Apply and .WithBrush take a ShinyThemeKeys.Brush key - a Color key resolves to a " +
            "Color and is dropped. Use ShinyThemeKeys.Brush.X or .AsBrush().");


    [Fact]
    public void NoBrushRecolouredInPlace()
        => this.ShouldFindNothing(BrushRecolouredInPlace, "BRUSH RECOLOURED IN PLACE",
            "Assign a new SolidColorBrush rather than changing the Color of one a view already holds: " +
            "Android and Windows never repaint it.");


    [Fact]
    public void NoFromRgbaMixingByteChannelsWithAFractionalAlpha()
    {
        var offenders = new List<string>();

        foreach (var (file, line, text) in this.Lines())
        {
            foreach (Match m in FromRgbaByteChannelsWithFractionalAlpha.Matches(text))
            {
                var channels = new[] { m.Groups[1], m.Groups[2], m.Groups[3] }.Select(g => int.Parse(g.Value));
                if (channels.Any(c => c > 1 && c < 255))
                    offenders.Add($"{file}:{line}  {text.Trim()}");
            }
        }

        this.Report("FROMRGBA OVERLOAD", offenders);
        offenders.ShouldBeEmpty(
            "Byte channels with a fractional alpha bind to the all-float overload and clamp to white. " +
            "Use Color.FromRgba(r / 255f, g / 255f, b / 255f, a) or Color.FromRgb(r, g, b).WithAlpha(a)."
        );
    }


    void ShouldFindNothing(Regex rule, string label, string advice)
    {
        var offenders = this.Lines()
            .Where(x => rule.IsMatch(x.Text))
            .Select(x => $"{x.File}:{x.Line}  {x.Text.Trim()}")
            .ToList();

        this.Report(label, offenders);
        offenders.ShouldBeEmpty(advice);
    }


    void Report(string label, IEnumerable<string> offenders)
    {
        foreach (var o in offenders)
            output.WriteLine($"{label}: {o}");
    }


    IEnumerable<(string File, int Line, string Text)> Lines()
    {
        var src = FindSrcRoot();
        foreach (var file in SourceFiles(src))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("*", StringComparison.Ordinal))
                    continue;

                yield return (Path.GetRelativePath(src, file), i + 1, lines[i]);
            }
        }
    }


    /// <summary>Every shipped MAUI package, so an add-on cannot quietly reintroduce any of this.</summary>
    static IEnumerable<string> SourceFiles(string src)
        => Directory
            .EnumerateDirectories(src, "Shiny.Maui.Controls*", SearchOption.TopDirectoryOnly)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(f =>
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));


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


/// <summary>
/// Every test that replaces process-wide state must be in <see cref="ApplicationResourcesCollection"/>,
/// which runs them one at a time. <c>new Application()</c> replaces <c>Application.Current</c>, and the
/// theme manager is static: a class outside the collection runs alongside the ones inside it and swaps
/// the theme out from under them. That surfaced as <c>ThemeHandlerHookTests</c> passing on its own and
/// failing only in the full run - the kind of red test that gets ignored until it hides a real one.
/// </summary>
public class TestIsolationGuardTests(ITestOutputHelper output)
{
    static readonly Regex TouchesGlobalState = new(
        // UseShinyControls belongs here too: it installs the theme, which merges into whatever
        // Application.Current is at that moment - another test's, from another thread.
        @"new\s+Application\s*\(|Application\.Current\s*=(?!=)|ShinyThemeManager\.\w+\s*(\(|=(?!=))|nameof\(ShinyThemeManager\.|\.UseShinyControls\s*\(",
        RegexOptions.Compiled);

    static readonly Regex CollectionAttribute = new(
        @"\[Collection\(\s*(\w+)\.Name\s*\)\]",
        RegexOptions.Compiled);

    /// <summary>A collection declared with DisableParallelization, which xUnit runs on its own.</summary>
    static readonly Regex SerialCollection = new(
        @"\[CollectionDefinition\([^)]*DisableParallelization\s*=\s*true[^)]*\)\]\s*public\s+(?:sealed\s+)?class\s+(\w+)",
        RegexOptions.Compiled);


    [Fact]
    public void TestsThatReplaceGlobalStateRunInTheSerialCollection()
    {
        var files = Directory
            .EnumerateFiles(FindTestRoot(), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(f => (File: f, Text: File.ReadAllText(f)))
            .ToList();

        var serial = files
            .SelectMany(x => SerialCollection.Matches(x.Text).Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        var offenders = files
            // Only test classes - a helper that resets the state is not itself racing anything.
            .Where(x => x.Text.Contains("[Fact", StringComparison.Ordinal) || x.Text.Contains("[Theory", StringComparison.Ordinal))
            .Where(x => TouchesGlobalState.IsMatch(x.Text))
            .Where(x => !CollectionAttribute.Matches(x.Text).Any(m => serial.Contains(m.Groups[1].Value)))
            .Select(x => Path.GetFileName(x.File))
            .ToList();

        foreach (var o in offenders)
            output.WriteLine("NOT SERIALISED: " + o);

        offenders.ShouldBeEmpty(
            "These tests create an Application or change the theme manager but can run in parallel " +
            "with every test that depends on them. Add [Collection(ApplicationResourcesCollection.Name)] " +
            "(or another collection declared with DisableParallelization = true)."
        );
    }


    static string FindTestRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Shiny.Maui.Controls.Tests");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/Shiny.Maui.Controls.Tests from the test output directory.");
    }
}
