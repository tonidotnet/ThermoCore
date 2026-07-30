using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Simulation;

public sealed record SimulationStepResult
{
    public required int StepIndex { get; init; }

    public required TimeSpan ElapsedTime { get; init; }

    public required bool Committed { get; init; }

    public required ConservationBalance SystemBalance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyDictionary<string, object?> PortStates { get; init; }
}
