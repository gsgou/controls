using System.Reflection;
using System.Text;

namespace Sample.Blazor;

/// <summary>
/// The gallery's own page sources, embedded by <c>Sample.Blazor.csproj</c> so every demo can show the
/// code that makes it. Look-up is by the routed component's type name, which matches its file name.
/// </summary>
static class SampleSourceFiles
{
    const string Prefix = "SampleSource/";

    static readonly Lazy<Dictionary<string, string>> index = new(BuildIndex);

    static Dictionary<string, string> BuildIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in typeof(SampleSourceFiles).Assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            // The logical name keeps the folder for readability; look-up only wants the file.
            map[name[(name.LastIndexOfAny(['/', '\\']) + 1)..]] = name;
        }
        return map;
    }


    /// <summary>
    /// A page's own files in the order a reader wants them: markup, then the code and styles beside it.
    /// Empty when the component was not embedded - a component from a control package, say.
    /// </summary>
    public static IReadOnlyList<string> FilesFor(Type? pageType)
    {
        if (pageType is null)
            return [];

        var candidates = new[] { pageType.Name + ".razor", pageType.Name + ".razor.cs", pageType.Name + ".razor.css" };
        return candidates.Where(index.Value.ContainsKey).ToArray();
    }


    public static string MarkdownFor(IReadOnlyList<string> files)
    {
        var sb = new StringBuilder();
        foreach (var file in files)
        {
            var code = Read(index.Value[file]);
            if (code is null)
                continue;

            // The markdown demo page has fenced code blocks in its own source, so the fence here has
            // to out-run the longest run of backticks in the file or it closes early.
            var fence = new string('`', Math.Max(3, LongestBacktickRun(code) + 1));

            sb.Append("### ").Append(file).Append("\n\n")
              .Append(fence).Append(LanguageFor(file)).Append('\n')
              .Append(code.TrimEnd()).Append('\n')
              .Append(fence).Append("\n\n");
        }
        return sb.ToString();
    }


    static string? Read(string resourceName)
    {
        using var stream = typeof(SampleSourceFiles).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }


    static string LanguageFor(string file) => file switch
    {
        _ when file.EndsWith(".razor.css", StringComparison.OrdinalIgnoreCase) => "css",
        _ when file.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase) => "csharp",
        _ => "razor"
    };


    static int LongestBacktickRun(string text)
    {
        int longest = 0, run = 0;
        foreach (var c in text)
        {
            run = c == '`' ? run + 1 : 0;
            if (run > longest)
                longest = run;
        }
        return longest;
    }
}
