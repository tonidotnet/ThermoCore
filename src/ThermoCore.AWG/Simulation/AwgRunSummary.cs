using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Simulation;

/// <summary>Builds <see cref="AwgRunSummary"/> from an engine result.</summary>
public static class AwgRunSummaryBuilder
{
    public static AwgRunSummary Build(
        AwgBuiltSystem built,
        AwgSimulationOptions options,
        SimulationRunResult engineResult)
    {
        ArgumentNullException.ThrowIfNull(built);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engineResult);

        var warningCount = engineResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        var errorCount = engineResult.Diagnostics.Count(d =>
            d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical);

        var temperatures = new Dictionary<string, double?>(StringComparer.Ordinal);
        var humidity = new Dictionary<string, double?>(StringComparer.Ordinal);
        double? busPower = null;
        double? curtailedPower = null;

        if (engineResult.Steps.Count > 0)
        {
            var last = engineResult.Steps[^1];
            foreach (var sample in AwgMeasurementSampler.SampleMoistAir(built, last))
            {
                temperatures[sample.PointId] = UnitConversions.KelvinToCelsius(sample.TemperatureK);
                humidity[sample.PointId] = sample.HumidityRatioKgPerKgDryAir;
            }

            if (last.PortStates.TryGetValue($"{AwgV3TopologyIds.ElectricalBusSink}.inlet", out var bus)
                && bus is ElectricalPowerState busState)
            {
                busPower = busState.PowerW;
            }

            if (last.PortStates.TryGetValue($"{AwgV3TopologyIds.CurtailmentSink}.inlet", out var curtail)
                && curtail is ElectricalPowerState curtailState)
            {
                curtailedPower = curtailState.PowerW;
            }
        }

        return new AwgRunSummary
        {
            Succeeded = engineResult.Succeeded,
            TopologyId = built.Metadata.TopologyId,
            TopologyVersion = built.Metadata.TopologyVersion,
            GraphFingerprint = built.Metadata.GraphFingerprint,
            ComponentCount = built.Graph.Components.Count,
            ConnectionCount = built.Graph.Connections.Count,
            CompletedSteps = engineResult.Steps.Count,
            Duration = options.Duration,
            TimeStep = options.TimeStep,
            AggregatedEnergyResidualJ = engineResult.AggregatedBalance.EnergyResidualJ,
            AggregatedWaterResidualKg = engineResult.AggregatedBalance.WaterMassResidualKg,
            AggregatedDryAirResidualKg = engineResult.AggregatedBalance.DryAirMassResidualKg,
            WarningCount = warningCount,
            ErrorCount = errorCount,
            FinalMoistAirTemperaturesC = temperatures,
            FinalHumidityRatiosKgPerKg = humidity,
            FinalBusPowerW = busPower,
            FinalCurtailedPowerW = curtailedPower
        };
    }
}

/// <summary>Formats an <see cref="AwgRunSummary"/> for console output.</summary>
public static class AwgRunSummaryFormatter
{
    public static string Format(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string>
        {
            "=== ThermoCore AWG Simulation Summary ===",
            $"Succeeded: {summary.Succeeded}",
            $"Topology: {summary.TopologyId} v{summary.TopologyVersion}",
            $"Fingerprint: {summary.GraphFingerprint}",
            $"Components / connections: {summary.ComponentCount} / {summary.ConnectionCount}",
            $"Steps: {summary.CompletedSteps}  Duration: {summary.Duration.TotalSeconds:F0} s  dt: {summary.TimeStep.TotalSeconds:F0} s",
            $"Residuals: E={summary.AggregatedEnergyResidualJ:E3} J  W={summary.AggregatedWaterResidualKg:E3} kg  DA={summary.AggregatedDryAirResidualKg:E3} kg",
            $"Diagnostics: warnings={summary.WarningCount} errors={summary.ErrorCount}"
        };

        if (summary.FinalBusPowerW is { } bus)
        {
            lines.Add($"Electrical bus power: {bus:F2} W");
        }

        if (summary.FinalCurtailedPowerW is { } curtail)
        {
            lines.Add($"Curtailed power: {curtail:F2} W");
        }

        if (summary.FinalMoistAirTemperaturesC.Count > 0)
        {
            lines.Add("Final moist-air temperatures (°C):");
            foreach (var pair in summary.FinalMoistAirTemperaturesC.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var humidity = summary.FinalHumidityRatiosKgPerKg.TryGetValue(pair.Key, out var w) ? w : null;
                lines.Add(
                    humidity is null
                        ? $"  {pair.Key}: T={pair.Value:F2}"
                        : $"  {pair.Key}: T={pair.Value:F2}, W={humidity:F6} kg/kg");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
