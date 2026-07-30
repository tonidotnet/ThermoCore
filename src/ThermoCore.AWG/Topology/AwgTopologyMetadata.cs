using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.AWG.Topology;

/// <summary>Topology metadata stored with a built AWG graph for reproducibility.</summary>
public sealed record AwgTopologyMetadata
{
    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required bool EnableRecirculation { get; init; }

    public required bool EnableHeatRecovery { get; init; }

    public required bool EnableElectricalSubsystem { get; init; }

    public required IReadOnlyDictionary<string, string> ComponentModelSelections { get; init; }

    public required string GraphFingerprint { get; init; }
}
