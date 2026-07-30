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
