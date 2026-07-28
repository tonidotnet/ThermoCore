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
    /// </summary>
    public IReadOnlyDictionary<string, object?> ExternalInputs { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);
}

public sealed record SimulationStepResult
{
    public required int StepIndex { get; init; }

    public required TimeSpan ElapsedTime { get; init; }

    public required bool Committed { get; init; }

    public required ConservationBalance SystemBalance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyDictionary<string, object?> PortStates { get; init; }
}

public sealed record SimulationRunResult
{
    public required bool Succeeded { get; init; }

    public required IReadOnlyList<SimulationStepResult> Steps { get; init; }

    public required ConservationBalance AggregatedBalance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}

public interface ISimulationEngine
{
    SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default);
}
