using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.AWG.Topology;

public sealed record AwgBuiltSystem
{
    public required SimulationGraph Graph { get; init; }

    public required AwgTopologyMetadata Metadata { get; init; }

    public required AwgSystemConfiguration Configuration { get; init; }

    public required AwgInitialState InitialState { get; init; }
}
