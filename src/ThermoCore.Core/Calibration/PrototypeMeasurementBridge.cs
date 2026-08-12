namespace ThermoCore.Core.Calibration;

/// <summary>
/// Bridges wide prototype rows into the existing long-format <see cref="MeasurementDataset"/>
/// used by validate/holdout/calibrate (no parallel comparison stack).
/// </summary>
public static class PrototypeMeasurementBridge
{
    public static MeasurementDataset ToMeasurementDataset(PrototypeMeasurementPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        package.Campaign.Validate();

        var map = ResolveChannelMap(package.Campaign);
        var samples = new List<MeasurementSample>();
        foreach (var row in package.Rows)
        {
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.AmbientTemperatureC, row.AmbientTemperatureC, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.AmbientRhPercent, row.AmbientRhPercent, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.InletTemperatureC, row.InletTemperatureC, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.InletRhPercent, row.InletRhPercent, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.OutletTemperatureC, row.OutletTemperatureC, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.OutletRhPercent, row.OutletRhPercent, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.ColdSurfaceTemperatureC, row.ColdSurfaceTemperatureC, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.HotSideTemperatureC, row.HotSideTemperatureC, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.AirflowM3PerHour, row.AirflowM3PerHour, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.VoltageV, row.VoltageV, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.CurrentA, row.CurrentA, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.PowerW, row.PowerW, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.SolarIrradianceWPerM2, row.SolarIrradianceWPerM2, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.WaterMassG, row.WaterMassG, map);
            Add(samples, row.TimestampUtc, PrototypeWideCsvSchema.SorbentMassG, row.SorbentMassG, map);
        }

        if (samples.Count == 0)
        {
            throw new FormatException("Prototype package produced no long-format measurement samples.");
        }

        var channelIds = samples
            .Select(s => s.ChannelId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return new MeasurementDataset
        {
            SourcePath = package.CsvSourcePath,
            Samples = samples,
            ChannelIds = channelIds,
            Warnings = package.Warnings,
            Campaign = package.Campaign
        };
    }

    private static IReadOnlyDictionary<string, (string ChannelId, string Unit)> ResolveChannelMap(
        PrototypeCampaignDocument campaign)
    {
        if (campaign.ChannelMap is null || campaign.ChannelMap.Count == 0)
        {
            return PrototypeWideCsvSchema.DefaultChannelMap;
        }

        var merged = new Dictionary<string, (string ChannelId, string Unit)>(
            PrototypeWideCsvSchema.DefaultChannelMap,
            StringComparer.OrdinalIgnoreCase);
        foreach (var pair in campaign.ChannelMap)
        {
            var unit = PrototypeWideCsvSchema.DefaultChannelMap.TryGetValue(pair.Key, out var existing)
                ? existing.Unit
                : string.Empty;
            merged[pair.Key] = (pair.Value, unit);
        }

        return merged;
    }

    private static void Add(
        List<MeasurementSample> samples,
        DateTimeOffset timestampUtc,
        string column,
        double? value,
        IReadOnlyDictionary<string, (string ChannelId, string Unit)> map)
    {
        if (value is not { } measured)
        {
            return;
        }

        if (!map.TryGetValue(column, out var target))
        {
            return;
        }

        samples.Add(new MeasurementSample
        {
            TimestampUtc = timestampUtc,
            ChannelId = target.ChannelId,
            Value = measured,
            Unit = target.Unit
        });
    }
}
