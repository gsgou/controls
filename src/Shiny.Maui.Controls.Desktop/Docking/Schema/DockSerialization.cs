using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Maui.Controls.Desktop.Docking;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DockRoot))]
public partial class DockJsonContext : JsonSerializerContext
{
}

public static class DockSerialization
{
    public static string Serialize(DockRoot root)
        => JsonSerializer.Serialize(root, DockJsonContext.Default.DockRoot);

    public static DockRoot? Deserialize(string json)
        => JsonSerializer.Deserialize(json, DockJsonContext.Default.DockRoot);
}
