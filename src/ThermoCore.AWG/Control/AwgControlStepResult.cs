using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

/// <summary>Controller evaluation output for one timestep.</summary>
public sealed record AwgControlStepResult
{
    public required AwgControlRequest Request { get; init; }

    public required AwgControllerState ProposedState { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyCollection<AwgDecisionTraceEntry> DecisionTrace { get; init; }
}
