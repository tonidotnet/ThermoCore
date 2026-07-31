using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Simulation;

public sealed record SimulationRequest
{
    public required SimulationGraph Graph { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public NumericalTolerances NumericalTolerances { get; init; } = NumericalTolerances.Default;

    public BalanceTolerance BalanceTolerance { get; init; } = BalanceTolerance.Default;

    /// <summary>
    /// Optional external port values keyed as "componentId.portId".
    /// For torn loops, provide the initial guess at the tear target port key.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ExternalInputs { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    public IReadOnlyList<SimulationLoopDefinition> Loops { get; init; }
        = Array.Empty<SimulationLoopDefinition>();

    /// <summary>Optional hook invoked immediately before each timestep evaluation.</summary>
    public ISimulationStepHook? StepHook { get; init; }
}
