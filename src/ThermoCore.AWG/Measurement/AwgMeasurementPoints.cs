using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Measurement;

/// <summary>Stable measurement-point identifier for AWG V3 (docs/04_Simulation/15_SystemTopology.md §17).</summary>
public sealed record AwgMeasurementPointDefinition
{
    public required string PointId { get; init; }

    public required string DisplayName { get; init; }

    public required string ComponentId { get; init; }

    public required string PortId { get; init; }

    public required bool IsMoistAir { get; init; }

    public bool IsOptional { get; init; }
}

/// <summary>Sampled moist-air values at a measurement point.</summary>
public sealed record AwgMoistAirMeasurementSample
{
    public required string PointId { get; init; }

    public required string DisplayName { get; init; }

    public required double TemperatureK { get; init; }

    public required double PressurePa { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double DewPointTemperatureK { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double WaterVaporMassFlowKgPerSecond { get; init; }

    public required double SpecificEnthalpyJPerKgDryAir { get; init; }
}

/// <summary>Catalog of V3 MVP measurement points mapped to topology ports.</summary>
public static class AwgMeasurementPointCatalog
{
    public static IReadOnlyList<AwgMeasurementPointDefinition> V3Mvp { get; } =
    [
        Point("MP-01", "Ambient inlet", AwgV3TopologyIds.AmbientSource, "outlet"),
        Point("MP-03", "After Peltier hot side", AwgV3TopologyIds.PeltierHotSideHx, "outlet"),
        Point("MP-05", "Solar collector outlet", AwgV3TopologyIds.SolarCollector, "outlet"),
        Point("MP-06", "Silica-gel outlet", AwgV3TopologyIds.SilicaGelBed, "outlet"),
        Point("MP-07", "Condenser inlet", AwgV3TopologyIds.Condenser, "inlet"),
        Point("MP-08", "Condenser outlet", AwgV3TopologyIds.Condenser, "outlet"),
        Point("MP-10", "Exhaust", AwgV3TopologyIds.ExhaustSink, "inlet"),
        Point("MP-02", "Mixed inlet", AwgV3TopologyIds.FreshAirMixer, "outlet", optional: true),
        Point("MP-04", "After PV rear channel", AwgV3TopologyIds.PvPanel, "rear_air_out", optional: true),
        Point("MP-09", "Heat-recovery hot outlet", AwgV3TopologyIds.HeatRecovery, "hot_out", optional: true),
        Point("MP-11", "Recirculation return", AwgV3TopologyIds.RecirculationSplitter, "outlet_1", optional: true)
    ];

    private static AwgMeasurementPointDefinition Point(
        string id,
        string name,
        string componentId,
        string portId,
        bool optional = false)
        => new()
        {
            PointId = id,
            DisplayName = name,
            ComponentId = componentId,
            PortId = portId,
            IsMoistAir = true,
            IsOptional = optional
        };
}

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
