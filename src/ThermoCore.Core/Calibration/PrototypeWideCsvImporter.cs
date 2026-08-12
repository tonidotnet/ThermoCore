using System.Globalization;

namespace ThermoCore.Core.Calibration;

/// <summary>Imports wide-format prototype measurement CSV (R3-001 / VAL-001).</summary>
public static class PrototypeWideCsvImporter
{
    public static PrototypeMeasurementPackage ImportPackageFromFiles(
        string campaignDocumentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignDocumentPath);
        var campaign = PrototypeCampaignDocumentLoader.LoadFromFile(campaignDocumentPath);
        var csvPath = PrototypeCampaignDocumentLoader.ResolveMeasurementCsvPath(
            campaignDocumentPath,
            campaign);
        return ImportPackage(campaign, File.ReadAllText(csvPath), csvPath);
    }

    public static PrototypeMeasurementPackage ImportPackage(
        PrototypeCampaignDocument campaign,
        string csvText,
        string csvSourcePath = "inline")
    {
        ArgumentNullException.ThrowIfNull(campaign);
        campaign.Validate();
        var (rows, warnings) = ImportRows(csvText);
        return new PrototypeMeasurementPackage
        {
            Campaign = campaign,
            CsvSourcePath = csvSourcePath,
            Rows = rows,
            Warnings = warnings
        };
    }

    public static (IReadOnlyList<PrototypeWideMeasurementRow> Rows, IReadOnlyList<string> Warnings)
        ImportRowsFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Prototype measurement CSV was not found.", path);
        }

        return ImportRows(File.ReadAllText(path));
    }

    public static (IReadOnlyList<PrototypeWideMeasurementRow> Rows, IReadOnlyList<string> Warnings)
        ImportRows(string csvText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvText);
        var lines = csvText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            throw new FormatException("Prototype CSV must include a header and at least one data row.");
        }

        var header = ParseCsvLine(lines[0]);
        var timestampIndex = IndexOfHeader(header, PrototypeWideCsvSchema.TimestampUtc, "timestamp_utc", "time");
        if (timestampIndex < 0)
        {
            throw new FormatException("Prototype CSV header must include timestampUtc (or timestamp_utc).");
        }

        // At least one of the core measurement columns must be present.
        var corePresent =
            IndexOfHeader(header, PrototypeWideCsvSchema.InletTemperatureC) >= 0
            || IndexOfHeader(header, PrototypeWideCsvSchema.PowerW) >= 0
            || IndexOfHeader(header, PrototypeWideCsvSchema.WaterMassG) >= 0
            || IndexOfHeader(header, PrototypeWideCsvSchema.OutletTemperatureC) >= 0;
        if (!corePresent)
        {
            throw new FormatException(
                "Prototype CSV must include at least one of: inletTemperatureC, outletTemperatureC, powerW, waterMassG.");
        }

        var rows = new List<PrototypeWideMeasurementRow>();
        var warnings = new List<string>();
        for (var row = 1; row < lines.Length; row++)
        {
            var fields = ParseCsvLine(lines[row]);
            if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Count <= timestampIndex)
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

            rows.Add(new PrototypeWideMeasurementRow
            {
                TimestampUtc = timestamp.ToUniversalTime(),
                TestId = ReadString(fields, header, PrototypeWideCsvSchema.TestId),
                VariantId = ReadString(fields, header, PrototypeWideCsvSchema.VariantId),
                AmbientTemperatureC = ReadDouble(fields, header, PrototypeWideCsvSchema.AmbientTemperatureC, warnings, row),
                AmbientRhPercent = ReadDouble(fields, header, PrototypeWideCsvSchema.AmbientRhPercent, warnings, row),
                InletTemperatureC = ReadDouble(fields, header, PrototypeWideCsvSchema.InletTemperatureC, warnings, row),
                InletRhPercent = ReadDouble(fields, header, PrototypeWideCsvSchema.InletRhPercent, warnings, row),
                OutletTemperatureC = ReadDouble(fields, header, PrototypeWideCsvSchema.OutletTemperatureC, warnings, row),
                OutletRhPercent = ReadDouble(fields, header, PrototypeWideCsvSchema.OutletRhPercent, warnings, row),
                ColdSurfaceTemperatureC = ReadDouble(fields, header, PrototypeWideCsvSchema.ColdSurfaceTemperatureC, warnings, row),
                HotSideTemperatureC = ReadDouble(fields, header, PrototypeWideCsvSchema.HotSideTemperatureC, warnings, row),
                AirflowM3PerHour = ReadDouble(fields, header, PrototypeWideCsvSchema.AirflowM3PerHour, warnings, row),
                VoltageV = ReadDouble(fields, header, PrototypeWideCsvSchema.VoltageV, warnings, row),
                CurrentA = ReadDouble(fields, header, PrototypeWideCsvSchema.CurrentA, warnings, row),
                PowerW = ReadDouble(fields, header, PrototypeWideCsvSchema.PowerW, warnings, row),
                SolarIrradianceWPerM2 = ReadDouble(fields, header, PrototypeWideCsvSchema.SolarIrradianceWPerM2, warnings, row),
                WaterMassG = ReadDouble(fields, header, PrototypeWideCsvSchema.WaterMassG, warnings, row),
                SorbentMassG = ReadDouble(fields, header, PrototypeWideCsvSchema.SorbentMassG, warnings, row),
                Notes = ReadString(fields, header, PrototypeWideCsvSchema.Notes)
            });
        }

        if (rows.Count == 0)
        {
            throw new FormatException("Prototype CSV contained no usable rows.");
        }

        return (rows, warnings);
    }

    private static string? ReadString(IReadOnlyList<string> fields, IReadOnlyList<string> header, string column)
    {
        var index = IndexOfHeader(header, column);
        if (index < 0 || index >= fields.Count)
        {
            return null;
        }

        var value = fields[index].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static double? ReadDouble(
        IReadOnlyList<string> fields,
        IReadOnlyList<string> header,
        string column,
        List<string> warnings,
        int rowIndex)
    {
        var index = IndexOfHeader(header, column);
        if (index < 0 || index >= fields.Count)
        {
            return null;
        }

        var raw = fields[index].Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || double.IsNaN(value)
            || double.IsInfinity(value))
        {
            warnings.Add($"Row {rowIndex + 1}: ignored invalid {column} '{raw}'.");
            return null;
        }

        return value;
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
