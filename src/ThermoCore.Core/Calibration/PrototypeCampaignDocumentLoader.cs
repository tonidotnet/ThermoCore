using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermoCore.Core.Calibration;

/// <summary>Version-compatible JSON load/save for <see cref="PrototypeCampaignDocument"/>.</summary>
public static class PrototypeCampaignDocumentLoader
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

    public static string SaveToJson(PrototypeCampaignDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Validate();
        return JsonSerializer.Serialize(document, CreateSerializerOptions());
    }

    public static PrototypeCampaignDocument LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var document = JsonSerializer.Deserialize<PrototypeCampaignDocument>(json, CreateSerializerOptions())
            ?? throw new ArgumentException("Prototype campaign JSON deserialized to null.");
        return document.Validate();
    }

    public static PrototypeCampaignDocument LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadFromJson(File.ReadAllText(path));
    }

    public static void SaveToFile(PrototypeCampaignDocument document, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, SaveToJson(document));
    }

    /// <summary>Resolves <see cref="PrototypeCampaignDocument.MeasurementCsvPath"/> relative to the document file.</summary>
    public static string ResolveMeasurementCsvPath(string documentPath, PrototypeCampaignDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        ArgumentNullException.ThrowIfNull(document);
        document.Validate();

        if (Path.IsPathRooted(document.MeasurementCsvPath))
        {
            return document.MeasurementCsvPath;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(documentPath))
            ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(directory, document.MeasurementCsvPath));
    }
}
