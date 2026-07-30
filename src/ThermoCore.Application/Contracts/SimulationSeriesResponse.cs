namespace ThermoCore.Api.Contracts;

public sealed record SimulationSeriesResponse
{
    public required string SimulationId { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required double IntervalSeconds { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalChannels { get; init; }

    public required IReadOnlyList<SimulationSeriesChannelDto> Channels { get; init; }
}
