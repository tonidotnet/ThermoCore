using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Measurement;

/// <summary>
/// Process-air stations for the full AWG V3 path (15_SystemTopology.md §3)
/// with Hungarian labels matching the product flow diagram.
/// </summary>
public sealed record AwgFullFlowStationDefinition
{
    public required string StationId { get; init; }

    public required string HungarianName { get; init; }

    public required string EnglishName { get; init; }

    public required string ComponentId { get; init; }

    public required string PortId { get; init; }

    public bool RequiresHeatRecovery { get; init; }

    public static IReadOnlyList<AwgFullFlowStationDefinition> V3FullFlow { get; } =
    [
        new()
        {
            StationId = "T0",
            HungarianName = "Környezeti belépés / HR hideg oldal",
            EnglishName = "Ambient / HR cold inlet preheat path",
            ComponentId = AwgV3TopologyIds.AmbientSource,
            PortId = "outlet"
        },
        new()
        {
            StationId = "T2",
            HungarianName = "Peltier meleg oldal",
            EnglishName = "Peltier hot-side HX outlet",
            ComponentId = AwgV3TopologyIds.PeltierHotSideHx,
            PortId = "outlet"
        },
        new()
        {
            StationId = "T1",
            HungarianName = "Napkollektor",
            EnglishName = "Solar collector outlet",
            ComponentId = AwgV3TopologyIds.SolarCollector,
            PortId = "outlet"
        },
        new()
        {
            StationId = "T3",
            HungarianName = "Szilikagél kazetta",
            EnglishName = "Silica-gel bed outlet",
            ComponentId = AwgV3TopologyIds.SilicaGelBed,
            PortId = "outlet"
        },
        new()
        {
            StationId = "T4",
            HungarianName = "Kondenzációs kamra (Peltier hideg oldal)",
            EnglishName = "Condenser / Peltier cold-side outlet",
            ComponentId = AwgV3TopologyIds.Condenser,
            PortId = "outlet"
        },
        new()
        {
            StationId = "T5",
            HungarianName = "Hővisszanyerő (forró oldal)",
            EnglishName = "Heat-recovery hot outlet",
            ComponentId = AwgV3TopologyIds.HeatRecovery,
            PortId = "hot_out",
            RequiresHeatRecovery = true
        },
        new()
        {
            StationId = "TEX",
            HungarianName = "Kifújás",
            EnglishName = "Exhaust",
            ComponentId = AwgV3TopologyIds.ExhaustSink,
            PortId = "inlet"
        }
    ];
}
