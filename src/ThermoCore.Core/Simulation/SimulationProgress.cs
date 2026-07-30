using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Simulation;

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
