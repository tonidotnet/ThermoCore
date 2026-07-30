using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Graph;

public sealed record ComponentStepResult
{
    public IReadOnlyDictionary<string, object?> OutputStates { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    public object? ProposedInternalState { get; init; }

    public ConservationBalance Balance { get; init; } = ConservationBalance.Empty;

    public IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
        = Array.Empty<SimulationDiagnostic>();
}
