namespace ThermoCore.Api.Contracts;

public sealed record PersistedSimulationListResponse
{
    public required IReadOnlyList<PersistedSimulationListItem> Simulations { get; init; }
}
