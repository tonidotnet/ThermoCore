namespace ThermoCore.Api.Contracts;

public sealed record HealthResponse
{
    public required string Status { get; init; }

    public required string ApplicationVersion { get; init; }

    public required string CoreVersion { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }
}
