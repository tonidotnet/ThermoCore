using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Version-compatible JSON load/save for <see cref="VaporCompressionPerformanceMap"/>.</summary>
public static class VaporCompressionPerformanceMapSerializer
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

    public static string SaveToJson(VaporCompressionPerformanceMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.Validate();
        return JsonSerializer.Serialize(map, CreateSerializerOptions());
    }

    public static VaporCompressionPerformanceMap LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var map = JsonSerializer.Deserialize<VaporCompressionPerformanceMap>(json, CreateSerializerOptions())
            ?? throw new ArgumentException("Vapor-compression map JSON deserialized to null.");
        return map.Validate();
    }

    public static void SaveToFile(VaporCompressionPerformanceMap map, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, SaveToJson(map));
    }

    public static VaporCompressionPerformanceMap LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadFromJson(File.ReadAllText(path));
    }
}
