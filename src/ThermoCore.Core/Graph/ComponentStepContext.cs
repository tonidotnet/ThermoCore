using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Graph;

public sealed record ComponentStepContext
{
    public required SimulationContext Simulation { get; init; }

    public int SolverIteration { get; init; }

    public IReadOnlyDictionary<string, object?> InputStates { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);
}
