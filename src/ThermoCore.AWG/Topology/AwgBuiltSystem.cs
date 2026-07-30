using ThermoCore.Core.Graph;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Topology;

/// <summary>Built AWG graph plus optional cyclic-loop execution metadata.</summary>
public sealed record AwgBuiltSystem
{
    public required SimulationGraph Graph { get; init; }

    public required AwgTopologyMetadata Metadata { get; init; }

    public required AwgSystemConfiguration Configuration { get; init; }

    public required AwgInitialState InitialState { get; init; }

    public IReadOnlyList<SimulationLoopDefinition> Loops { get; init; }
        = Array.Empty<SimulationLoopDefinition>();

    public IReadOnlyDictionary<string, object?> ExternalInputs { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    public bool RequiresCyclicSolver => Loops.Count > 0;
}
