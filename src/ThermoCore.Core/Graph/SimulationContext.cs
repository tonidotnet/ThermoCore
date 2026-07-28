using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Graph;

public sealed record SimulationContext
{
    public required DateTimeOffset SimulationStart { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required TimeSpan ElapsedTime { get; init; }

    public required int StepIndex { get; init; }

    public NumericalTolerances NumericalTolerances { get; init; } = NumericalTolerances.Default;
}

public sealed record ComponentStepContext
{
    public required SimulationContext Simulation { get; init; }

    public int SolverIteration { get; init; }

    public IReadOnlyDictionary<string, object?> InputStates { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);
}
