namespace ThermoCore.Persistence;

/// <summary>Metadata row for a persisted result channel (DOC-021 §10).</summary>
public sealed record StoredResultSeriesDescriptor
{
    public required Guid Id { get; init; }

    public required Guid SimulationSummaryId { get; init; }

    public required string ChannelId { get; init; }

    public required string Unit { get; init; }

    public required string StorageLocation { get; init; }

    public required int SampleCount { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required double IntervalSeconds { get; init; }
}
