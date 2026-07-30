using ThermoCore.Core.Results;

namespace ThermoCore.Core.Calibration;

/// <summary>
/// Aligns measurement samples to simulated series by timestamp and computes error metrics (CAL-004/005).
/// </summary>
public static class SimulationMeasurementComparer
{
    public static MeasurementComparisonReport Compare(
        MeasurementDataset measurements,
        IReadOnlyList<ResultTimeSeriesChannel> simulatedChannels,
        DateTimeOffset startTimeUtc,
        TimeSpan interval,
        TimeSpan? matchTolerance = null)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(simulatedChannels);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
        }

        var tolerance = matchTolerance ?? TimeSpan.FromTicks(interval.Ticks / 2);
        var simById = simulatedChannels.ToDictionary(c => c.Definition.Id, StringComparer.Ordinal);
        var results = new List<ChannelComparisonResult>();
        var missing = new List<string>();
        var warnings = new List<string>(measurements.Warnings);

        foreach (var channelId in measurements.ChannelIds)
        {
            var channelSamples = measurements.Samples
                .Where(s => string.Equals(s.ChannelId, channelId, StringComparison.Ordinal))
                .OrderBy(s => s.TimestampUtc)
                .ToArray();

            if (!simById.TryGetValue(channelId, out var simulated))
            {
                missing.Add(channelId);
                continue;
            }

            var measuredAligned = new List<double>();
            var simulatedAligned = new List<double>();
            var unmatched = 0;
            var unit = channelSamples[0].Unit;

            foreach (var sample in channelSamples)
            {
                if (!TrySampleSimulated(
                        simulated.Values,
                        startTimeUtc,
                        interval,
                        sample.TimestampUtc,
                        tolerance,
                        out var simValue))
                {
                    unmatched++;
                    continue;
                }

                measuredAligned.Add(sample.Value);
                simulatedAligned.Add(simValue);
            }

            if (measuredAligned.Count == 0)
            {
                warnings.Add($"Channel '{channelId}': no samples aligned within {tolerance.TotalSeconds:G} s.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(unit)
                && !string.IsNullOrWhiteSpace(simulated.Definition.Unit)
                && !string.Equals(unit, simulated.Definition.Unit, StringComparison.Ordinal))
            {
                warnings.Add(
                    $"Channel '{channelId}': unit mismatch measurement='{unit}' simulation='{simulated.Definition.Unit}'.");
            }

            results.Add(new ChannelComparisonResult
            {
                ChannelId = channelId,
                Unit = string.IsNullOrWhiteSpace(unit) ? simulated.Definition.Unit : unit,
                Metrics = ErrorMetricsCalculator.Compute(measuredAligned, simulatedAligned),
                MatchedSampleCount = measuredAligned.Count,
                UnmatchedMeasurementCount = unmatched
            });
        }

        return new MeasurementComparisonReport
        {
            MeasurementSourcePath = measurements.SourcePath,
            Channels = results,
            MissingChannels = missing,
            Warnings = warnings
        };
    }

    private static bool TrySampleSimulated(
        IReadOnlyList<double> values,
        DateTimeOffset startTimeUtc,
        TimeSpan interval,
        DateTimeOffset timestampUtc,
        TimeSpan tolerance,
        out double value)
    {
        value = 0;
        if (values.Count == 0)
        {
            return false;
        }

        var offset = timestampUtc - startTimeUtc;
        var index = (int)Math.Round(offset.TotalSeconds / interval.TotalSeconds);
        if (index < 0 || index >= values.Count)
        {
            return false;
        }

        var sampleTime = startTimeUtc + TimeSpan.FromSeconds(index * interval.TotalSeconds);
        if (Math.Abs((timestampUtc - sampleTime).TotalSeconds) > tolerance.TotalSeconds)
        {
            return false;
        }

        value = values[index];
        return true;
    }
}
