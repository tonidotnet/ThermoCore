using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Graph;

public sealed record GraphValidationResult
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
