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

/// <summary>
/// Progress snapshot reported during a simulation run
/// (docs/04_Simulation/16_SimulationEngine.md §23 / GRAPH-012).
/// </summary>
public sealed record SimulationProgress
{
    public required long CompletedSteps { get; init; }

    public required long TotalSteps { get; init; }

    public required DateTimeOffset SimulationTimeUtc { get; init; }

    public required string CurrentPhase { get; init; }

    public double FractionComplete => TotalSteps <= 0 ? 0.0 : (double)CompletedSteps / TotalSteps;
}

public interface ISimulationEngine
{
    SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default,
        IProgress<SimulationProgress>? progress = null);
}
