using System.Globalization;
using System.Text;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>
/// Builds a multi-regime synthetic measurement CSV from truth simulations (M5 campaign stand-in).
/// Not a substitute for physical prototype data.
/// </summary>
public static class AwgSyntheticCampaignGenerator
{
    public static string GenerateCsv(
        IReadOnlyList<AwgSyntheticCampaignSegment> segments,
        string channelIdContains = "condenser.outlet.temperature")
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0)
        {
            throw new ArgumentException("At least one campaign segment is required.", nameof(segments));
        }

        var runner = new AwgSimulationRunner();
        var sb = new StringBuilder();
        sb.AppendLine(MeasurementCsvSchema.HeaderLine);

        var cursor = DateTimeOffset.Parse("2026-07-01T08:00:00Z", CultureInfo.InvariantCulture);
        foreach (var segment in segments)
        {
            var baseline = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
            var configuration = baseline with
            {
                Ambient = baseline.Ambient with
                {
                    TemperatureK = segment.AmbientTemperatureK,
                    RelativeHumidityFraction = segment.RelativeHumidityFraction,
                    SolarIrradianceWPerSquareMeter = segment.SolarIrradianceWPerM2
                },
                Condenser = baseline.Condenser with
                {
                    BypassFactor = segment.TruthCondenserBypassFactor
                }
            };
            var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
            var options = AwgSimulationOptions.CreateDefault(segment.Duration, segment.TimeStep) with
            {
                StartTimeUtc = cursor
            };

            var run = runner.Run(configuration, initial, options);
            if (!run.EngineResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Campaign segment '{segment.Id}' failed: " +
                    string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Code)));
            }

            var collected = AwgResultExporter.Collect(run);
            var channel = collected.Channels.FirstOrDefault(c =>
                c.Definition.Id.Contains(channelIdContains, StringComparison.Ordinal));
            if (channel is null)
            {
                throw new InvalidOperationException(
                    $"Campaign segment '{segment.Id}' missing channel containing '{channelIdContains}'.");
            }

            for (var i = 0; i < channel.Values.Count; i++)
            {
                var t = cursor + TimeSpan.FromSeconds(segment.TimeStep.TotalSeconds * i);
                sb.Append(t.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                    .Append(channel.Definition.Id).Append(',')
                    .Append(channel.Values[i].ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                    .Append(channel.Definition.Unit)
                    .AppendLine();
            }

            cursor += segment.Duration + TimeSpan.FromMinutes(1);
        }

        return sb.ToString();
    }

    public static IReadOnlyList<AwgSyntheticCampaignSegment> CreateDefaultThreeRegimeSegments()
        =>
        [
            new AwgSyntheticCampaignSegment
            {
                Id = "low-solar-humid",
                AmbientTemperatureK = 297.15,
                RelativeHumidityFraction = 0.80,
                SolarIrradianceWPerM2 = 150.0,
                Duration = TimeSpan.FromSeconds(8),
                TimeStep = TimeSpan.FromSeconds(1),
                TruthCondenserBypassFactor = 0.22
            },
            new AwgSyntheticCampaignSegment
            {
                Id = "high-solar-mid",
                AmbientTemperatureK = 305.15,
                RelativeHumidityFraction = 0.45,
                SolarIrradianceWPerM2 = 900.0,
                Duration = TimeSpan.FromSeconds(8),
                TimeStep = TimeSpan.FromSeconds(1),
                TruthCondenserBypassFactor = 0.22
            },
            new AwgSyntheticCampaignSegment
            {
                Id = "near-zero-solar",
                AmbientTemperatureK = 291.15,
                RelativeHumidityFraction = 0.55,
                SolarIrradianceWPerM2 = 20.0,
                Duration = TimeSpan.FromSeconds(8),
                TimeStep = TimeSpan.FromSeconds(1),
                TruthCondenserBypassFactor = 0.22
            }
        ];
}
