using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.AWG.Topology;

public sealed class AwgConfigurationException : Exception
{
    public AwgConfigurationException(string message, IReadOnlyList<SimulationDiagnostic>? diagnostics = null)
        : base(message)
    {
        Diagnostics = diagnostics ?? Array.Empty<SimulationDiagnostic>();
    }

    public IReadOnlyList<SimulationDiagnostic> Diagnostics { get; }
}

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

public sealed record AwgBuiltSystem
{
    public required SimulationGraph Graph { get; init; }

    public required AwgTopologyMetadata Metadata { get; init; }

    public required AwgSystemConfiguration Configuration { get; init; }

    public required AwgInitialState InitialState { get; init; }
}

public interface IAwgSystemGraphBuilder
{
    AwgBuiltSystem Build(AwgSystemConfiguration configuration, AwgInitialState initialState);
}
