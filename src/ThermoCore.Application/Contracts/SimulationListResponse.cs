namespace ThermoCore.Api.Contracts;

public sealed record SimulationListResponse
{
    public required IReadOnlyList<SimulationStatusResponse> Simulations { get; init; }
}
