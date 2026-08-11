using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Version-compatible JSON load/save for <see cref="TecManufacturerProfile"/>.</summary>
public static class TecManufacturerProfileSerializer
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
            // Unknown future fields are ignored → forward-compatible reads.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public static string SaveToJson(TecManufacturerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();
        return JsonSerializer.Serialize(profile, CreateSerializerOptions());
    }

    public static TecManufacturerProfile LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var profile = JsonSerializer.Deserialize<TecManufacturerProfile>(json, CreateSerializerOptions())
            ?? throw new ArgumentException("TEC profile JSON deserialized to null.");
        return profile.Validate();
    }

    public static void SaveToFile(TecManufacturerProfile profile, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, SaveToJson(profile));
    }

    public static TecManufacturerProfile LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadFromJson(File.ReadAllText(path));
    }
}
