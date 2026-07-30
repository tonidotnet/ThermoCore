namespace ThermoCore.Api.Contracts;

public sealed record SimulationSeriesChannelDto
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Unit { get; init; }

    public required string ComponentId { get; init; }

    public required IReadOnlyList<double> Values { get; init; }
}
