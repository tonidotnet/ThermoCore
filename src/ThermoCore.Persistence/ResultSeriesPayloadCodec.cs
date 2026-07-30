using System.IO.Compression;
using System.Text.Json;

namespace ThermoCore.Persistence;

/// <summary>Gzip+JSON codec for full result-series payloads beside the SQLite database.</summary>
public static class ResultSeriesPayloadCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static void Write(string path, IReadOnlyDictionary<string, IReadOnlyList<double>> valuesByChannelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(valuesByChannelId);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dto = new PayloadDto
        {
            Channels = valuesByChannelId.ToDictionary(
                p => p.Key,
                p => p.Value.ToArray(),
                StringComparer.Ordinal)
        };

        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        JsonSerializer.Serialize(gzip, dto, JsonOptions);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<double>> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        var dto = JsonSerializer.Deserialize<PayloadDto>(gzip, JsonOptions)
            ?? throw new InvalidOperationException($"Result series payload '{path}' is empty.");

        return dto.Channels.ToDictionary(
            p => p.Key,
            p => (IReadOnlyList<double>)p.Value,
            StringComparer.Ordinal);
    }

    private sealed class PayloadDto
    {
        public Dictionary<string, double[]> Channels { get; set; } = new(StringComparer.Ordinal);
    }
}
