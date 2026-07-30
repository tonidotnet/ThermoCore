using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Simulation;

public sealed record SimulationRunResult
{
    public required bool Succeeded { get; init; }

    public required IReadOnlyList<SimulationStepResult> Steps { get; init; }

    public required ConservationBalance AggregatedBalance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
