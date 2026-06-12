using System.Text.Json;
using Shiny.ThemeGen;

// Resolve repo root by walking up looking for the solution file.
var root = FindRepoRoot();
if (root is null)
{
    Console.Error.WriteLine("Could not locate repo root (Shiny.Controls.slnx).");
    return 1;
}

var themesDir = Path.Combine(root, "themes");
var jsonFiles = Directory
    .EnumerateFiles(themesDir, "*.json")
    .Where(f => !Path.GetFileName(f).Equals("shiny-theme.schema.json", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .ToList();

if (jsonFiles.Count == 0)
{
    Console.Error.WriteLine($"No theme JSON files found in {themesDir}");
    return 1;
}

Console.WriteLine($"Repo root: {root}");
Console.WriteLine($"Found {jsonFiles.Count} theme(s).\n");

foreach (var file in jsonFiles)
{
    var theme = LoadTheme(file);
    var palettes = Palettes.FromSeeds(theme.Seeds);
    var light = SchemeBuilder.Build(palettes, dark: false);
    var dark = SchemeBuilder.Build(palettes, dark: true);

    // Shape: defaults, optionally overridden by the theme json.
    var shape = Tokens.Shape
        .Select(s => (s.Name, theme.ShapeOverrides.TryGetValue(s.Name, out var v) ? v : s.Value))
        .ToList();

    var data = new ThemeData(theme.Name, theme.Slug, theme.Description, light, dark, shape);

    var isCore = theme.Slug == "basic";
    var mauiDir = isCore
        ? Path.Combine(root, "src", "Shiny.Maui.Controls", "Themes", "Generated")
        : Path.Combine(root, "src", $"Shiny.Maui.Controls.Themes.{theme.Name}", "Generated");
    var blazorCss = isCore
        ? Path.Combine(root, "src", "Shiny.Blazor.Controls", "wwwroot", "css", "shiny-theme.css")
        : Path.Combine(root, "src", $"Shiny.Blazor.Controls.Themes.{theme.Name}", "wwwroot", "css", $"shiny-theme-{theme.Slug}.css");

    WriteFile(Path.Combine(mauiDir, $"{theme.Name}LightTheme.cs"), Emitter.MauiDictionary(data, dark: false));
    WriteFile(Path.Combine(mauiDir, $"{theme.Name}DarkTheme.cs"), Emitter.MauiDictionary(data, dark: true));
    WriteFile(Path.Combine(mauiDir, $"{theme.Name}Theme.cs"), Emitter.MauiTheme(data));
    WriteFile(blazorCss, Emitter.Css(data));

    Console.WriteLine($"  {theme.Name,-10} -> MAUI {(isCore ? "[core]" : "[pack]")}  +  Blazor css");
}

// ShinyThemeKeys is the MAUI contract — emit once into the core library.
WriteFile(
    Path.Combine(root, "src", "Shiny.Maui.Controls", "Themes", "Generated", "ShinyThemeKeys.cs"),
    Emitter.ThemeKeys());

Console.WriteLine("\nDone.");
return 0;

static void WriteFile(string path, string content)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    // Normalize to LF and a single trailing newline for stable diffs.
    var normalized = content.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
    File.WriteAllText(path, normalized);
}

static string? FindRepoRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Shiny.Controls.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
    }
    return null;
}

static ThemeSource LoadTheme(string path)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var rootEl = doc.RootElement;

    var seeds = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var prop in rootEl.GetProperty("seeds").EnumerateObject())
        seeds[prop.Name] = prop.Value.GetString()!;

    var shapeOverrides = new Dictionary<string, double>(StringComparer.Ordinal);
    if (rootEl.TryGetProperty("shape", out var shapeEl))
        foreach (var prop in shapeEl.EnumerateObject())
            shapeOverrides[prop.Name] = prop.Value.GetDouble();

    return new ThemeSource(
        rootEl.GetProperty("name").GetString()!,
        rootEl.GetProperty("slug").GetString()!,
        rootEl.TryGetProperty("description", out var d) ? d.GetString()! : "",
        seeds,
        shapeOverrides);
}

sealed record ThemeSource(
    string Name,
    string Slug,
    string Description,
    IReadOnlyDictionary<string, string> Seeds,
    IReadOnlyDictionary<string, double> ShapeOverrides);
