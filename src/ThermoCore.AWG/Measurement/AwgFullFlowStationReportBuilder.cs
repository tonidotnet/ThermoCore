using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Measurement;

/// <summary>Builds a T/RH/W station report along the full AWG V3 process path.</summary>
public static class AwgFullFlowStationReportBuilder
{
    public static AwgFullFlowStationReport Build(AwgSimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (run.EngineResult.Steps.Count == 0)
        {
            throw new InvalidOperationException("Cannot build a station report without completed steps.");
        }

        var last = run.EngineResult.Steps[^1];
        var hrEnabled = run.BuiltSystem.Configuration.Topology.EnableHeatRecovery;
        var stations = new List<AwgFullFlowStationSample>();

        foreach (var definition in AwgFullFlowStationDefinition.V3FullFlow)
        {
            if (definition.RequiresHeatRecovery && !hrEnabled)
            {
                continue;
            }

            if (!TryResolve(
                    run.BuiltSystem,
                    last,
                    definition.ComponentId,
                    definition.PortId,
                    out var air))
            {
                continue;
            }

            stations.Add(new AwgFullFlowStationSample
            {
                StationId = definition.StationId,
                HungarianName = definition.HungarianName,
                EnglishName = definition.EnglishName,
                TemperatureC = UnitConversions.KelvinToCelsius(air.TemperatureK),
                RelativeHumidityFraction = air.RelativeHumidityFraction,
                HumidityRatioKgPerKgDryAir = air.HumidityRatioKgPerKgDryAir,
                DryAirMassFlowKgPerSecond = air.DryAirMassFlowKgPerSecond,
                WaterVaporMassFlowKgPerSecond = air.WaterVaporMassFlowKgPerSecond
            });
        }

        return new AwgFullFlowStationReport
        {
            Run = run,
            Stations = stations,
            HeatRecoveryEnabled = hrEnabled
        };
    }

    private static bool TryResolve(
        AwgBuiltSystem built,
        Core.Simulation.SimulationStepResult step,
        string componentId,
        string portId,
        out MoistAirState air)
    {
        var key = $"{componentId}.{portId}";
        if (step.PortStates.TryGetValue(key, out var raw) && raw is MoistAirState direct)
        {
            air = direct;
            return true;
        }

        var inbound = built.Graph.Connections.FirstOrDefault(c =>
            string.Equals(c.TargetComponentId, componentId, StringComparison.Ordinal)
            && string.Equals(c.TargetPortId, portId, StringComparison.Ordinal));
        if (inbound is not null)
        {
            var sourceKey = $"{inbound.SourceComponentId}.{inbound.SourcePortId}";
            if (step.PortStates.TryGetValue(sourceKey, out var upstream) && upstream is MoistAirState sourced)
            {
                air = sourced;
                return true;
            }
        }

        air = null!;
        return false;
    }
}
