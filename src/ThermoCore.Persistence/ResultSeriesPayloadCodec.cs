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
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, Encode(valuesByChannelId));
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<double>> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Decode(File.ReadAllBytes(path));
    }

    public static byte[] Encode(IReadOnlyDictionary<string, IReadOnlyList<double>> valuesByChannelId)
    {
        ArgumentNullException.ThrowIfNull(valuesByChannelId);
        var dto = new PayloadDto
        {
            Channels = valuesByChannelId.ToDictionary(
                p => p.Key,
                p => p.Value.ToArray(),
                StringComparer.Ordinal)
        };

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            JsonSerializer.Serialize(gzip, dto, JsonOptions);
        }

        return buffer.ToArray();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<double>> Decode(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        using var buffer = new MemoryStream(payload);
        using var gzip = new GZipStream(buffer, CompressionMode.Decompress);
        var dto = JsonSerializer.Deserialize<PayloadDto>(gzip, JsonOptions)
            ?? throw new InvalidOperationException("Result series payload is empty.");

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
