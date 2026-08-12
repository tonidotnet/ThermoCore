using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>JSON load/save for <see cref="AbsorptionPerformanceMap"/>.</summary>
public static class AbsorptionPerformanceMapSerializer
{
    public static JsonSerializerOptions CreateSerializerOptions()
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public static string SaveToJson(AbsorptionPerformanceMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.Validate();
        return JsonSerializer.Serialize(map, CreateSerializerOptions());
    }

    public static AbsorptionPerformanceMap LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var map = JsonSerializer.Deserialize<AbsorptionPerformanceMap>(json, CreateSerializerOptions())
            ?? throw new ArgumentException("Absorption map JSON deserialized to null.");
        return map.Validate();
    }

    public static void SaveToFile(AbsorptionPerformanceMap map, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, SaveToJson(map));
    }

    public static AbsorptionPerformanceMap LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadFromJson(File.ReadAllText(path));
    }
}
