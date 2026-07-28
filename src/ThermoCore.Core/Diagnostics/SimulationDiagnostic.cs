namespace ThermoCore.Core.Diagnostics;

public enum DiagnosticSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Structured simulation diagnostic (docs/07_ProjectManagement/18_CodingRules.md §21).
/// </summary>
public sealed record SimulationDiagnostic
{
    public required string Code { get; init; }

    public required DiagnosticSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? ComponentId { get; init; }

    public string? PortId { get; init; }

    public int? StepIndex { get; init; }

    public TimeSpan? SimulationTime { get; init; }

    public int? SolverIteration { get; init; }

    public IReadOnlyDictionary<string, double>? Values { get; init; }

    public string? SuggestedAction { get; init; }
}
