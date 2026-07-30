using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Measurement;

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
