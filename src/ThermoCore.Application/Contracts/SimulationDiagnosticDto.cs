namespace ThermoCore.Api.Contracts;

public sealed record SimulationDiagnosticDto
{
    public required string Code { get; init; }

    public required string Severity { get; init; }

    public required string Message { get; init; }

    public string? ComponentId { get; init; }

    public string? PortId { get; init; }

    public int? StepIndex { get; init; }
}
