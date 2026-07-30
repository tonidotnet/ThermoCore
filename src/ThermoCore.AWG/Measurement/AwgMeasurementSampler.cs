using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Measurement;

/// <summary>Extracts measurement-point samples from a simulation step (AWG-007).</summary>
public static class AwgMeasurementSampler
{
    public static IReadOnlyList<AwgMoistAirMeasurementSample> SampleMoistAir(
        AwgBuiltSystem builtSystem,
        SimulationStepResult step,
        IReadOnlyList<AwgMeasurementPointDefinition>? points = null)
    {
        ArgumentNullException.ThrowIfNull(builtSystem);
        ArgumentNullException.ThrowIfNull(step);
        points ??= AwgMeasurementPointCatalog.V3Mvp;

        var componentIds = new HashSet<string>(
            builtSystem.Graph.Components.Select(c => c.Id),
            StringComparer.Ordinal);
        var samples = new List<AwgMoistAirMeasurementSample>();

        foreach (var point in points.Where(p => p.IsMoistAir))
        {
            if (!componentIds.Contains(point.ComponentId))
            {
                continue;
            }

            if (!TryResolveMoistAir(builtSystem, step, point.ComponentId, point.PortId, out var air))
            {
                continue;
            }

            samples.Add(new AwgMoistAirMeasurementSample
            {
                PointId = point.PointId,
                DisplayName = point.DisplayName,
                TemperatureK = air.TemperatureK,
                PressurePa = air.PressurePa,
                HumidityRatioKgPerKgDryAir = air.HumidityRatioKgPerKgDryAir,
                RelativeHumidityFraction = air.RelativeHumidityFraction,
                DewPointTemperatureK = air.DewPointTemperatureK,
                DryAirMassFlowKgPerSecond = air.DryAirMassFlowKgPerSecond,
                WaterVaporMassFlowKgPerSecond = air.WaterVaporMassFlowKgPerSecond,
                SpecificEnthalpyJPerKgDryAir = air.SpecificEnthalpyJPerKgDryAir
            });
        }

        return samples;
    }

    private static bool TryResolveMoistAir(
        AwgBuiltSystem builtSystem,
        SimulationStepResult step,
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

        // Inlet ports on sinks/mixers may not be published; resolve from the upstream connection.
        var inbound = builtSystem.Graph.Connections.FirstOrDefault(c =>
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
