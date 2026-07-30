using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
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

        double? tankContent = null;
        double? tankLevel = null;
        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.WaterTank)
            is WaterTankComponent tank)
        {
            tankContent = tank.StoredMassKg;
            tankLevel = tank.LevelFraction;
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
            FinalCurtailedPowerW = curtailedPower,
            FinalWaterTankContentKg = tankContent,
            FinalWaterTankLevelFraction = tankLevel
        };
    }
}
