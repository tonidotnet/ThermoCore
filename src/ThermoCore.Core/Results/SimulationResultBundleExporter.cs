using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

/// <summary>
/// Writes the DOC-029 export package (JSON + CSV + manifest + README).
/// </summary>
public static class SimulationResultBundleExporter
{
    public const string PackageType = "ThermoCoreSimulationExport";

    public const string PackageVersion = "1.0";

    public static JsonSerializerOptions CreateJsonOptions()
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    public static SimulationExportManifest ExportDirectory(
        SimulationResult result,
        string directory,
        SimulationRunResult? run = null,
        IReadOnlyDictionary<string, string>? additionalTextFiles = null,
        string? simulationId = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        var jsonOptions = CreateJsonOptions();
        var written = new Dictionary<string, string>(StringComparer.Ordinal);

        written["metadata.json"] = JsonSerializer.Serialize(result.Metadata, jsonOptions);
        written["summary.json"] = JsonSerializer.Serialize(result.Summary, jsonOptions);
        written["channels.json"] = JsonSerializer.Serialize(result.Channels, jsonOptions);
        written["summary.csv"] = SimulationResultCsvExporter.WriteSummary(result);
        written["series-wide.csv"] = SimulationResultCsvExporter.WriteSeriesWide(result);
        written["series-long.csv"] = SimulationResultCsvExporter.WriteSeriesLong(result);

        if (run is not null)
        {
            written["diagnostics.csv"] = SimulationResultCsvExporter.WriteDiagnostics(
                result.Metadata.StartTimeUtc,
                run);
            written["balances.csv"] = SimulationResultCsvExporter.WriteBalances(
                result.Metadata.StartTimeUtc,
                run);
            written["diagnostics.json"] = JsonSerializer.Serialize(run.Diagnostics, jsonOptions);
            written["aggregated-balance.json"] = JsonSerializer.Serialize(run.AggregatedBalance, jsonOptions);
        }

        if (additionalTextFiles is not null)
        {
            foreach (var pair in additionalTextFiles)
            {
                written[pair.Key] = pair.Value;
            }
        }

        written["README.txt"] = BuildReadme(result, written.Keys.OrderBy(k => k, StringComparer.Ordinal));

        foreach (var pair in written)
        {
            File.WriteAllText(Path.Combine(directory, pair.Key), pair.Value, Encoding.UTF8);
        }

        var files = written
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new SimulationExportManifestFile
            {
                Path = p.Key,
                Sha256 = ComputeSha256Hex(Encoding.UTF8.GetBytes(p.Value))
            })
            .ToArray();

        var manifest = new SimulationExportManifest
        {
            PackageType = PackageType,
            PackageVersion = PackageVersion,
            SimulationId = simulationId ?? Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ResultFormatVersion = result.Metadata.ResultFormatVersion,
            Files = files
        };

        var manifestJson = JsonSerializer.Serialize(manifest, jsonOptions);
        File.WriteAllText(Path.Combine(directory, "manifest.json"), manifestJson, Encoding.UTF8);
        return manifest;
    }

    private static string BuildReadme(SimulationResult result, IEnumerable<string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ThermoCore simulation export package");
        sb.AppendLine($"resultFormatVersion: {result.Metadata.ResultFormatVersion}");
        sb.AppendLine($"status: {result.Status}");
        sb.AppendLine($"startTimeUtc: {result.Metadata.StartTimeUtc:o}");
        sb.AppendLine($"durationSeconds: {result.Metadata.Duration.TotalSeconds}");
        sb.AppendLine($"timeStepSeconds: {result.Metadata.TimeStep.TotalSeconds}");
        sb.AppendLine();
        sb.AppendLine("Files:");
        foreach (var file in files)
        {
            sb.AppendLine($"- {file}");
        }

        sb.AppendLine("- manifest.json");
        return sb.ToString();
    }

    private static string ComputeSha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
