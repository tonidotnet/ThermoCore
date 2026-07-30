namespace ThermoCore.Api.Contracts;

public sealed record CreateSimulationResponse
{
    public required string SimulationId { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
