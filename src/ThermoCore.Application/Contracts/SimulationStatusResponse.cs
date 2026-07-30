namespace ThermoCore.Api.Contracts;

public sealed record SimulationStatusResponse
{
    public required string SimulationId { get; init; }

    public required string Status { get; init; }

    public required double ProgressFraction { get; init; }

    public required int CompletedSteps { get; init; }

    public required int TotalSteps { get; init; }

    public DateTimeOffset? SimulationTimeUtc { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public string? ErrorMessage { get; init; }
}
