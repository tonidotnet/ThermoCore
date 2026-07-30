using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

/// <summary>Auditable decision record for one transition or protection action.</summary>
public sealed record AwgDecisionTraceEntry
{
    public required string ReasonCode { get; init; }

    public required string PreviousMode { get; init; }

    public required string RequestedMode { get; init; }

    public required string ActiveLimitingConstraint { get; init; }

    public required IReadOnlyDictionary<string, double> ScalarInputs { get; init; }
}
