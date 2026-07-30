using System.Text.Json;
using System.Text.Json.Serialization;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Configuration;

/// <summary>Root JSON document for AWG configuration loading (APP-002).</summary>
public sealed record AwgConfigurationDocument
{
    public required AwgSystemConfiguration System { get; init; }

    public required AwgInitialState InitialState { get; init; }
}

/// <summary>Loads and saves AWG configuration documents from JSON.</summary>
public static class AwgConfigurationLoader
{
    public static JsonSerializerOptions CreateSerializerOptions()
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

    public static AwgConfigurationDocument CreateDefaultDocument(
        bool enableElectricalSubsystem = true)
    {
        var system = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem);
        return new AwgConfigurationDocument
        {
            System = system,
            InitialState = AwgSystemDefaults.CreateMvpInitialState(system)
        };
    }

    public static AwgConfigurationDocument LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var document = JsonSerializer.Deserialize<AwgConfigurationDocument>(json, CreateSerializerOptions())
            ?? throw new AwgConfigurationException("Configuration JSON deserialized to null.");

        document.System.Validate();
        document.InitialState.Validate(document.System);
        return document;
    }

    public static AwgConfigurationDocument LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("AWG configuration file was not found.", path);
        }

        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    public static string SaveToJson(AwgConfigurationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.System.Validate();
        document.InitialState.Validate(document.System);
        return JsonSerializer.Serialize(document, CreateSerializerOptions());
    }

    public static void SaveToFile(AwgConfigurationDocument document, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = SaveToJson(document);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);
    }
}
