namespace ThermoCore.Api.Contracts;

public sealed record SimulationDiagnosticsResponse
{
    public required string SimulationId { get; init; }

    public required int TotalCount { get; init; }

    public required IReadOnlyList<SimulationDiagnosticDto> Diagnostics { get; init; }
}
