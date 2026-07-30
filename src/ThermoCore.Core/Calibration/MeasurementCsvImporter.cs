using System.Globalization;

namespace ThermoCore.Core.Calibration;

/// <summary>Imports long-format measurement CSV files (CAL-003).</summary>
public static class MeasurementCsvImporter
{
    public static MeasurementDataset ImportFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Measurement CSV was not found.", path);
        }

        return Import(File.ReadAllText(path), path);
    }

    public static MeasurementDataset Import(string csvText, string sourcePath = "inline")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvText);
        var lines = csvText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            throw new FormatException("Measurement CSV must include a header and at least one data row.");
        }

        var header = ParseCsvLine(lines[0]);
        var timestampIndex = IndexOfHeader(header, MeasurementCsvSchema.TimestampUtc, "timestampUtc", "time");
        var channelIndex = IndexOfHeader(header, MeasurementCsvSchema.ChannelId, "channelId", "channel");
        var valueIndex = IndexOfHeader(header, MeasurementCsvSchema.Value, "measured", "y");
        var unitIndex = IndexOfHeader(header, MeasurementCsvSchema.Unit, "units");

        if (timestampIndex < 0 || channelIndex < 0 || valueIndex < 0)
        {
            throw new FormatException(
                "Measurement CSV header must include timestamp_utc, channel_id, and value columns.");
        }

        var samples = new List<MeasurementSample>();
        var warnings = new List<string>();
        for (var row = 1; row < lines.Length; row++)
        {
            var fields = ParseCsvLine(lines[row]);
            if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Count <= Math.Max(timestampIndex, Math.Max(channelIndex, valueIndex)))
            {
                warnings.Add($"Row {row + 1}: skipped (too few columns).");
                continue;
            }

            if (!DateTimeOffset.TryParse(
                    fields[timestampIndex],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var timestamp))
            {
                warnings.Add($"Row {row + 1}: skipped (invalid timestamp '{fields[timestampIndex]}').");
                continue;
            }

            if (!double.TryParse(
                    fields[valueIndex],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value)
                || double.IsNaN(value)
                || double.IsInfinity(value))
            {
                warnings.Add($"Row {row + 1}: skipped (invalid value '{fields[valueIndex]}').");
                continue;
            }

            var channelId = fields[channelIndex].Trim();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                warnings.Add($"Row {row + 1}: skipped (empty channel_id).");
                continue;
            }

            var unit = unitIndex >= 0 && unitIndex < fields.Count
                ? fields[unitIndex].Trim()
                : string.Empty;

            samples.Add(new MeasurementSample
            {
                TimestampUtc = timestamp.ToUniversalTime(),
                ChannelId = channelId,
                Value = value,
                Unit = unit
            });
        }

        if (samples.Count == 0)
        {
            throw new FormatException("Measurement CSV contained no usable samples.");
        }

        var channelIds = samples
            .Select(s => s.ChannelId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return new MeasurementDataset
        {
            SourcePath = sourcePath,
            Samples = samples,
            ChannelIds = channelIds,
            Warnings = warnings
        };
    }

    private static int IndexOfHeader(IReadOnlyList<string> header, params string[] aliases)
    {
        for (var i = 0; i < header.Count; i++)
        {
            foreach (var alias in aliases)
            {
                if (string.Equals(header[i], alias, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
