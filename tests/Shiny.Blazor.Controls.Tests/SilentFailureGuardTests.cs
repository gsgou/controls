using System.Text.RegularExpressions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Source scans for the Blazor mistakes that compile, run, and fail somewhere a unit test never
/// goes - Blazor Server with two users, a trimmed WebAssembly publish, a browser event that was
/// never wired. Each one already cost a debugging session; these keep them from coming back.
/// </summary>
/// <remarks>Every shipped Blazor package is scanned, add-ons included.</remarks>
public class SilentFailureGuardTests(ITestOutputHelper output)
{
    /// <summary>
    /// Singleton registrations that are deliberately process-wide, and why that is safe. Anything
    /// else a control registers is per user: as a Singleton on Blazor Server, one user's toast,
    /// dialog or keyboard state is shown to every user, and WebAssembly - one user per process -
    /// can never reproduce it.
    /// </summary>
    static readonly Dictionary<string, string> SingletonIsDeliberate = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Docking/ServiceCollectionExtensions.cs"] = "DockableContentRegistry and the panel factories are a type catalogue, the same for every user, holding no per-user state",
        ["Captcha/CaptchaServiceExtensions.cs"] = "the options and the providers are stateless - a provider's Render builds a widget per component, and the challenge lives in that widget",
    };


    [Fact]
    public void ControlServicesAreNotSingletons()
    {
        var rule = new Regex(@"\b(Try)?AddSingleton\b", RegexOptions.Compiled);

        var offenders = Lines(".cs")
            .Where(x => rule.IsMatch(x.Text) && !SingletonIsDeliberate.Keys.Any(k => Normal(x.File).EndsWith(k, StringComparison.OrdinalIgnoreCase)))
            .Select(Format)
            .ToList();

        this.Report("SINGLETON", offenders);
        offenders.ShouldBeEmpty(
            "Register control services Scoped (TryAddScoped). A Singleton is shared by every user of a " +
            "Blazor Server app. If it really is process-wide, add it to SingletonIsDeliberate with why."
        );
    }


    /// <summary>
    /// Blazor has no event mapping for these: <c>@onanimationend</c> compiles, renders a literal
    /// attribute, and attaches nothing, so the handler simply never runs.
    /// </summary>
    [Fact]
    public void NoUnmappedAnimationEvents()
    {
        var rule = new Regex(@"@on(animation|transition)\w*", RegexOptions.Compiled);

        var offenders = Lines(".razor").Where(x => rule.IsMatch(x.Text)).Select(Format).ToList();

        this.Report("UNMAPPED EVENT", offenders);
        offenders.ShouldBeEmpty(
            "Blazor never attaches animation/transition events. Listen in the component's JS module " +
            "and call back through a [JSInvokable] method."
        );
    }


    /// <summary>
    /// An anonymous type handed to JS interop works in Debug and crashes a trimmed WebAssembly
    /// publish (ConstructorContainsNullParameterNames) - the trimmer strips what the serializer needs.
    /// </summary>
    [Fact]
    public void NoAnonymousTypesOverInterop()
    {
        var rule = new Regex(
            @"\.Invoke(Void)?Async\s*(<[^>(]*>)?\s*\((?:[^;]*?)\bnew\s*\{",
            RegexOptions.Compiled | RegexOptions.Singleline);

        var offenders = new List<string>();
        foreach (var file in SourceFiles(".cs", ".razor"))
        {
            var text = File.ReadAllText(file);
            foreach (Match m in rule.Matches(text))
            {
                var line = text[..m.Index].Count(c => c == '\n') + 1;
                offenders.Add($"{Path.GetRelativePath(FindSrcRoot(), file)}:{line}");
            }
        }

        this.Report("ANONYMOUS INTEROP ARG", offenders);
        offenders.ShouldBeEmpty("Pass a named DTO (a record or class) or primitives to JS interop, never an anonymous type.");
    }


    /// <summary>
    /// Returning <c>DotNet.createJSStreamReference(blob)</c> from a module double-wraps it, and the
    /// throw comes from inside Blazor's own code, nowhere near the call that caused it. Return the
    /// Blob (or ArrayBuffer) itself; the framework wraps it for an IJSStreamReference.
    /// </summary>
    [Fact]
    public void NoPrewrappedStreamReferences()
    {
        var offenders = Lines(".js").Where(x => x.Text.Contains("createJSStreamReference", StringComparison.Ordinal)).Select(Format).ToList();

        this.Report("PREWRAPPED STREAM", offenders);
        offenders.ShouldBeEmpty("Return the Blob or ArrayBuffer raw; Blazor wraps it for IJSStreamReference itself.");
    }


    // ---------------------------------------------------------------------------------------------

    void Report(string label, IEnumerable<string> offenders)
    {
        foreach (var o in offenders)
            output.WriteLine($"{label}: {o}");
    }


    static string Format((string File, int Line, string Text) x) => $"{x.File}:{x.Line}  {x.Text.Trim()}";

    static string Normal(string path) => path.Replace('\\', '/');


    static IEnumerable<(string File, int Line, string Text)> Lines(params string[] extensions)
    {
        var src = FindSrcRoot();
        foreach (var file in SourceFiles(extensions))
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


    static IEnumerable<string> SourceFiles(params string[] extensions)
        => Directory
            .EnumerateDirectories(FindSrcRoot(), "Shiny.Blazor.Controls*", SearchOption.TopDirectoryOnly)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(f =>
                extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase) &&
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}") &&
                !f.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase));


    static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src");
            if (Directory.Exists(Path.Combine(candidate, "Shiny.Blazor.Controls")))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/ from the test output directory.");
    }
}
